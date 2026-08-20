using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.AddressableAssets.ResourceLocators
{
    /// <summary>
    /// Binary-format catalog data. Serialize via BinaryStorageBuffer; deserialize with BinaryContentCatalogData.Serializer.
    /// </summary>
    [Serializable]
    public class BinaryContentCatalogData : ContentCatalogData
    {
        /// <summary>
        /// ProviderId for the BinaryAssetProvider registered with this catalog's Serializer.
        /// Used by BinaryCatalogProvider when building the inner resource location so both
        /// sides use the same string without referencing the internal generic type directly.
        /// </summary>
        public static readonly string kBinaryAssetProviderId =
            $"{BinaryDataProvider.kBinaryAssetProviderBaseId}<{typeof(Serializer).FullName}>";

        BinaryStorageBuffer.Reader m_Reader;

        internal BinaryContentCatalogData(BinaryStorageBuffer.Reader reader)
        {
            m_Reader = reader;
        }

        /// <summary>
        /// Creates a new BinaryContentCatalogData object with the specified locator id.
        /// </summary>
        /// <param name="id">The id of the locator.</param>
        public BinaryContentCatalogData(string id) : base(id) { }

        /// <summary>
        /// Creates a new BinaryContentCatalogData object without any data.
        /// </summary>
        public BinaryContentCatalogData() { }

        internal byte[] GetBytes()
        {
            return m_Reader.GetBuffer();
        }

        internal override byte[] GetSerializedData() => m_Reader.GetBuffer();

        internal override IResourceLocator CreateCustomLocator(string overrideId = "", string providerSuffix = null)
        {
            m_LocatorId = overrideId;
            return new ResourceLocator(m_LocatorId, m_Reader, providerSuffix);
        }

        internal void CopyToFile(string path)
        {
            File.WriteAllBytes(path, m_Reader.GetBuffer());
        }

        internal override void CleanData()
        {
            m_LocatorId = null;
            // Don't Dispose the Reader: ResourceLocator instances created from this catalog
            // share the same Reader instance for lazy reads. Disposing mutates that shared
            // instance and NREs every locator. The Reader's finalizer frees the GCHandle pin
            // once nothing references it.
            m_Reader = null;
        }

        internal static BinaryContentCatalogData LoadFromFile(string path, bool resolveInternalIds)
        {
            return new BinaryContentCatalogData(new BinaryStorageBuffer.Reader(File.ReadAllBytes(path), 0, 0,
                resolveInternalIds ? new Serializer() : new Serializer().WithInternalIdResolvingDisabled()));
        }

#if UNITY_EDITOR
        internal override void SaveToFile(string path)
        {
            File.WriteAllBytes(path, SerializeToByteArray());
        }

        public override byte[] SerializeToByteArray()
        {
            var wr = new BinaryStorageBuffer.Writer(0, new Serializer());
            wr.WriteObject(this, false);
            return wr.SerializeToByteArray();
        }

        public override void SetData(IList<ContentCatalogDataEntry> entries)
        {
            m_Entries = entries;
        }

        [UnityEditor.MenuItem("Window/Asset Management/Addressables/Optimize Binary Catalog", priority = 2051)]
        private static void OptimizeBinaryCatalogMenuCommand()
        {
            var catalogPath = UnityEditor.EditorUtility.OpenFilePanelWithFilters("Select Binary Catalog", Path.GetDirectoryName(Application.dataPath), new string[] { "Binary Catalog", "bin" });
            if (string.IsNullOrEmpty(catalogPath))
                return;
            var resultDir = catalogPath.Replace(".bin", "-optimized");
            if (Directory.Exists(resultDir))
            {
                if (!UnityEditor.EditorUtility.DisplayDialog("Delete Folder Confirmation",
                    $"{resultDir} exists, do you want to overwrite?  All files in folder will be deleted.", "Yes", "No (Cancel)"))
                    return;
                Directory.Delete(resultDir, true);
            }

            Directory.CreateDirectory(resultDir);
            var data = File.ReadAllBytes(catalogPath);
            var reader = new BinaryStorageBuffer.Reader(data, 1024, 1024, new BinaryContentCatalogData.Serializer().WithInternalIdResolvingDisabled());
            var catalogData = reader.ReadObject<BinaryContentCatalogData>(0, out _, false);
            catalogData = CreateOptimizedCopy(catalogData);
            var optData = catalogData.SerializeToByteArray();
            var newCatalogPath = catalogPath.Replace(".bin", "_optimized.bin");
            var reductionPercent = (int)(100 * (1f - ((float)optData.Length / (float)data.Length)));
            Debug.Log($"{catalogPath} {data.Length / 1024}kb, optimized to {newCatalogPath} {optData.Length / 1024}kb. Reduction: {reductionPercent}%");
            File.Copy(catalogPath, $"{resultDir}/original.bin");
            File.WriteAllBytes($"{resultDir}/optimized.bin", optData);
            ExtractBinaryCatalog($"{resultDir}/original.bin", $"{resultDir}/original.extracted.txt");
            ExtractBinaryCatalog($"{resultDir}/optimized.bin", $"{resultDir}/optimized.extracted.txt");
            UnityEditor.EditorUtility.OpenWithDefaultApp(resultDir);
        }

        [UnityEditor.MenuItem("Window/Asset Management/Addressables/Extract Binary Catalog", priority = 2052)]
        private static void ExtractBinaryCatalogMenuCommand()
        {
            var catalogPath = UnityEditor.EditorUtility.OpenFilePanelWithFilters("Select Binary Catalog", Path.GetDirectoryName(Application.dataPath), new string[] { "Binary Catalog", "bin" });
            if (string.IsNullOrEmpty(catalogPath))
                return;
            var newPath = catalogPath.Replace(".bin", ".extracted.txt");
            ExtractBinaryCatalog(catalogPath, newPath);
        }

        /// <summary>
        /// Converts a binary catalog to a readable text version.  The text version cannot be loaded via the runtime and is intended for debugging purposes only.
        /// </summary>
        /// <param name="binaryCatalogPath">The path of the input binary catalog.</param>
        /// <param name="extractedPath">The path of the catalog output text data.</param>
        public static void ExtractBinaryCatalog(string binaryCatalogPath, string extractedPath)
        {
            var data = File.ReadAllBytes(binaryCatalogPath);
            var reader = new BinaryStorageBuffer.Reader(data, 1024, 1024, new BinaryContentCatalogData.Serializer().WithInternalIdResolvingDisabled());
            var catalogData = reader.ReadObject<BinaryContentCatalogData>(0, out _, false);
            var locator = catalogData.CreateCustomLocator();
            var lines = new List<string>();
            foreach (var key in locator.Keys)
            {
                if (locator.Locate(key, typeof(object), out var locs))
                {
                    foreach (var l in locs)
                    {
                        if ((string)key == l.PrimaryKey)
                        {
                            lines.Add($"{l.PrimaryKey}");
                            lines.Add($"\tResourceType: {l.ResourceType}");
                            lines.Add($"\tProviderId: {l.ProviderId}");
                            lines.Add($"\tInternalId: {l.InternalId}");
                            lines.Add($"\tData: {l.Data}");
                            lines.Add($"\tDependencyHash: {l.DependencyHashCode}");
                            if (l.HasDependencies)
                            {
                                lines.Add($"\tDependencies:");
                                foreach (var d in l.Dependencies)
                                    lines.Add($"\t\t{d.PrimaryKey}");
                            }
                        }
                        else
                        {
                            lines.Add($"{key} -> {l.PrimaryKey}");
                        }
                    }
                }
            }
            File.WriteAllLines(extractedPath, lines);
        }

        /// <summary>
        /// Creates an optimized copy of the catalog data.  This shortens all dependency keys in order to reduce the amount of key strings that get serialized.
        /// Assetbundles generally have a long path that is used as the id and this will replace them with an integer value.
        /// </summary>
        /// <returns>A newly created catalog that can be serialized and loaded via the runtime.</returns>
        public static BinaryContentCatalogData CreateOptimizedCopy(BinaryContentCatalogData original)
        {
            var locator = original.CreateCustomLocator();
            Dictionary<string, (IResourceLocation, HashSet<object>)> pkToLoc = new Dictionary<string, (IResourceLocation, HashSet<object>)>();
            Dictionary<object, object> depRemap = new Dictionary<object, object>();
            foreach (var key in locator.Keys)
            {
                if (locator.Locate(key, typeof(object), out var locs))
                {
                    foreach (var loc in locs)
                    {
                        if (!pkToLoc.TryGetValue(loc.PrimaryKey, out var locKeys))
                            pkToLoc.Add(loc.PrimaryKey, locKeys = (loc, new HashSet<object>()));
                        locKeys.Item2.Add(key);
                        if (loc.HasDependencies)
                        {
                            foreach (var d in loc.Dependencies)
                            {
                                if (!depRemap.ContainsKey(d.PrimaryKey))
                                {
                                    depRemap.Add(d.PrimaryKey, $"{depRemap.Count + 1:X}");
                                }
                            }
                        }
                    }
                }
            }
            var entries = new List<ContentCatalogDataEntry>();
            foreach (var l in pkToLoc)
            {
                var keys = l.Value.Item2;
                if (keys.Count == 1 && depRemap.TryGetValue(keys.FirstOrDefault(), out var newKey))
                {
                    keys.Clear();
                    keys.Add(newKey);
                }
                List<object> deps = null;
                if (l.Value.Item1.HasDependencies)
                {
                    deps = new List<object>();
                    foreach (var d in l.Value.Item1.Dependencies)
                    {
                        if (depRemap.TryGetValue(d.PrimaryKey, out var nk))
                            deps.Add(nk);
                        else
                            deps.Add(d.PrimaryKey);
                    }
                }

                entries.Add(new ContentCatalogDataEntry(
                    l.Value.Item1.ResourceType,
                    l.Value.Item1.InternalId,
                    l.Value.Item1.ProviderId,
                    keys,
                    deps,
                    l.Value.Item1.Data));
            }
            var ccd = new BinaryContentCatalogData(entries, original.ProviderId);
            ccd.BuildResultHash = original.BuildResultHash;
            ccd.InstanceProviderData = original.InstanceProviderData;
            ccd.LocalHash = original.LocalHash;
            ccd.location = original.location;
            ccd.ResourceProviderData = original.ResourceProviderData;
            ccd.SceneProviderData = original.SceneProviderData;
            ccd.SetData(entries);
            return ccd;
        }

        // Required for CreateOptimizedCopy
        BinaryContentCatalogData(IList<ContentCatalogDataEntry> entries, string id) : base(entries, id) { }
#endif

        internal class Serializer : BinaryStorageBuffer.ISerializationAdapter<ContentCatalogData>
        {
            public IEnumerable<BinaryStorageBuffer.ISerializationAdapter> Dependencies => new BinaryStorageBuffer.ISerializationAdapter[]
            {
                new ObjectInitializationData.Serializer(),
                new AssetBundleRequestOptionsSerializationAdapter(),
#if ENABLE_CONTENT_DIRECTORIES
                new ContentDirectoryAssetData.SerializationAdapter(),
#endif
                new ResourceLocator.ResourceLocation.Serializer(resolveInternalIds)
            };

            bool resolveInternalIds = true;
            public Serializer WithInternalIdResolvingDisabled()
            {
                resolveInternalIds = false;
                return this;
            }

            public object Deserialize(BinaryStorageBuffer.Reader reader, Type t, uint offset, out uint size)
            {
                var cd = new BinaryContentCatalogData(reader);
                var h = reader.ReadValue<ResourceLocator.Header>(offset, out var headerSize);
                if (h.magic != kMagic)
                    throw new Exception("Invalid header data!!!");
                if (h.version != kVersion)
                    throw new Exception($"Catalog data version mismatch: expected {kVersion}, found {h.version}. Rebuild your Addressables content with the current package version.");

                cd.InstanceProviderData = reader.ReadObject<ObjectInitializationData>(h.instanceProvider, out var ipdSize, false);
                cd.SceneProviderData = reader.ReadObject<ObjectInitializationData>(h.sceneProvider, out var spdSize, false);
                cd.ResourceProviderData = reader.ReadObjectArray<ObjectInitializationData>(h.initObjectsArray, out var rpdSize, false, false).ToList();

                cd.BuildResultHash = reader.ReadString(h.buildResultHash, out var brhSize);
                size = headerSize + ipdSize + spdSize + rpdSize + brhSize;
                return cd;
            }

            public uint Serialize(BinaryStorageBuffer.Writer writer, object val)
            {
                var cd = val as BinaryContentCatalogData;
                var entries = cd.m_Entries;
                var keyToEntryIndices = new Dictionary<object, List<int>>();
                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];
                    foreach (var k in e.Keys)
                    {
                        if (!keyToEntryIndices.TryGetValue(k, out var indices))
                            keyToEntryIndices.Add(k, indices = new List<int>());
                        indices.Add(i);
                    }
                }
                //reserve header and keys to ensure they are first
                var headerOffset = writer.Reserve<ResourceLocator.Header>();
                var keysOffset = writer.Reserve<ResourceLocator.KeyData>((uint)keyToEntryIndices.Count);
                var header = new ResourceLocator.Header
                {
                    magic = kMagic,
                    version = kVersion,
                    keysOffset = keysOffset,
                    idOffset = writer.WriteString(cd.ProviderId),
                    instanceProvider = writer.WriteObject(cd.InstanceProviderData, false),
                    sceneProvider = writer.WriteObject(cd.SceneProviderData, false),
                    initObjectsArray = writer.WriteObjects(cd.m_ResourceProviderData, false),
                    buildResultHash = writer.WriteString(cd.BuildResultHash)
                };
                writer.Write(headerOffset, in header);

                //create array of all locations
                var entryOffsets = new Dictionary<ContentCatalogDataEntry, uint>(entries.Count);
                var locationIds = new uint[entries.Count];
                for (int i = 0; i < entries.Count; i++)
                    locationIds[i] = writer.WriteObject(new ResourceLocator.ContentCatalogDataEntrySerializationContext { entry = entries[i], allEntries = entries, keyToEntryIndices = keyToEntryIndices, entryOffsets = entryOffsets }, false);

                //create array of all keys
                int keyIndex = 0;
                var allKeys = new ResourceLocator.KeyData[keyToEntryIndices.Count];
                foreach (var k in keyToEntryIndices)
                {
                    //create array of location ids
                    var locationOffsets = k.Value.Select(i => locationIds[i]).ToArray();

                    allKeys[keyIndex++] = new ResourceLocator.KeyData
                    {
                        keyNameOffset = writer.WriteObject(k.Key, true),
                        locationSetOffset = writer.Write(locationOffsets)
                    };
                }
                writer.Write(keysOffset, allKeys);
                return headerOffset;
            }
        }

        internal class ResourceLocator : IResourceLocator
        {
            public struct Header
            {
                public int magic;
                public int version;
                public uint keysOffset;
                public uint idOffset;
                public uint instanceProvider;
                public uint sceneProvider;
                public uint initObjectsArray;
                public uint buildResultHash;
            }

            public struct KeyData
            {
                public uint keyNameOffset;
                public uint locationSetOffset;
            }

            internal class ContentCatalogDataEntrySerializationContext
            {
                public ContentCatalogDataEntry entry;
                public Dictionary<object, List<int>> keyToEntryIndices;
                public IList<ContentCatalogDataEntry> allEntries;

                // Shared catalog serialization cache: maps an already-serialized entry to the
                // offset it was written at to save redundant/duplicate serialization.
                public Dictionary<ContentCatalogDataEntry, uint> entryOffsets;
            }

            internal class ResourceLocation : IResourceLocation
            {
                class ResolvedInternalId
                {
                    public string InternalId;
                }

                public class ResolvedInternalIdSerializer : BinaryStorageBuffer.ISerializationAdapter<ResolvedInternalId>
                {
                    IEnumerable<BinaryStorageBuffer.ISerializationAdapter> BinaryStorageBuffer.ISerializationAdapter.Dependencies => null;

                    object BinaryStorageBuffer.ISerializationAdapter.Deserialize(BinaryStorageBuffer.Reader reader, Type t, uint offset, out uint size)
                    {
                        var str = Addressables.ResolveInternalId(reader.ReadString(offset, out size, '/', true));
                        return new ResolvedInternalId { InternalId = str };
                    }

                    uint BinaryStorageBuffer.ISerializationAdapter.Serialize(BinaryStorageBuffer.Writer writer, object val)
                    {
                        throw new NotImplementedException();
                    }
                }

                public class Serializer : BinaryStorageBuffer.ISerializationAdapter<ResourceLocation>, BinaryStorageBuffer.ISerializationAdapter<ContentCatalogDataEntrySerializationContext>
                {
                    public struct Data
                    {
                        public uint primaryKeyOffset;
                        public uint internalIdOffset;
                        public uint providerOffset;
                        public uint dependencySetOffset;
                        public int dependencyHashValue;
                        public uint extraDataOffset;
                        public uint typeId;
                    }

                    public IEnumerable<BinaryStorageBuffer.ISerializationAdapter> Dependencies => new BinaryStorageBuffer.ISerializationAdapter[]
                    {
                        new ResolvedInternalIdSerializer(),
                        new ProviderLoadRequestOptions.SerializationAdatapter()
                    };

                    bool resolveInternalIds;
                    public Serializer(bool resolveInternalIds)
                    {
                        this.resolveInternalIds = resolveInternalIds;
                    }
                    //read as location
                    public object Deserialize(BinaryStorageBuffer.Reader reader, Type t, uint offset, out uint size)
                    {
                        return new ResourceLocation(reader, offset, out size, resolveInternalIds);
                    }

                    //write from data entry
                    public uint Serialize(BinaryStorageBuffer.Writer writer, object val)
                    {
                        var ec = val as ContentCatalogDataEntrySerializationContext;
                        var e = ec.entry;

                        // Return the previously-computed offset for this entry instead of re-serializing
                        if (ec.entryOffsets != null && ec.entryOffsets.TryGetValue(e, out var memoizedOffset))
                            return memoizedOffset;

                        uint depId = uint.MaxValue;
                        if (e.Dependencies != null && e.Dependencies.Count > 0)
                        {
                            var depIds = new HashSet<uint>();
                            foreach (var k in e.Dependencies)
                                foreach (var i in ec.keyToEntryIndices[k])
                                    depIds.Add(writer.WriteObject(new ResourceLocator.ContentCatalogDataEntrySerializationContext { entry = ec.allEntries[i], allEntries = ec.allEntries, keyToEntryIndices = ec.keyToEntryIndices, entryOffsets = ec.entryOffsets }, false));
                            depId = writer.Write(depIds.ToArray(), false);
                        }
                        var data = new Data
                        {
                            primaryKeyOffset = writer.WriteString(e.Keys[0] as string, '/'),
                            internalIdOffset = writer.WriteString(e.InternalId, '/'),
                            providerOffset = writer.WriteString(e.Provider, '.'),
                            dependencySetOffset = depId,
                            extraDataOffset = writer.WriteObject(e.Data, true),
                            typeId = writer.WriteObject(e.ResourceType, false)
                        };
                        var offset = writer.Write(data);
                        if (ec.entryOffsets != null)
                            ec.entryOffsets[e] = offset;
                        return offset;
                    }
                }
                BinaryStorageBuffer.Reader reader;
                public ResourceLocation(BinaryStorageBuffer.Reader r, uint id, out uint size, bool resolveInternalId)
                {
                    reader = r;
                    var data = reader.ReadValue<Serializer.Data>(id, out var locDataSize);
                    size = locDataSize;
                    ProviderId = reader.ReadString(data.providerOffset, out var pidSize, '.', true);
                    size += pidSize;
                    PrimaryKey = reader.ReadString(data.primaryKeyOffset, out var pkSize, '/', true);
                    size += pkSize;
                    Data = reader.ReadObject(data.extraDataOffset, out var dataSize, true);
                    size += dataSize;

                    if (resolveInternalId)
                    {
                        //this allows the internal id to be cached as the final resolved version
                        InternalId = reader.ReadObject<ResolvedInternalId>(data.internalIdOffset, out var iidSize, true).InternalId;
                        size += iidSize;
                    }
                    else
                    {
                        InternalId = reader.ReadString(data.internalIdOffset, out var iidSize, '/', true);
                        size += iidSize;
                    }

                    dependencyDataOffset = data.dependencySetOffset;
                    ResourceType = reader.ReadObject<Type>(data.typeId, out var typeSize);
                    size += typeSize;
                }
                public string InternalId { get; internal set; }
                public string ProviderId { get; internal set; }
                List<IResourceLocation> _deps;
                static void ProcDependencies(ResourceLocation l, ResourceLocation d, int i, int count)
                {
                    if (d._deps == null)
                        d._deps = new List<IResourceLocation>(count);
                    d._deps.Add(l);
                }

                public IList<IResourceLocation> Dependencies
                {
                    get
                    {
                        if (_deps == null)
                        {
                            _deps = new List<IResourceLocation>();
                            reader.ProcessObjectArray<ResourceLocation, ResourceLocation>(dependencyDataOffset, out var size, this, ProcDependencies, true);
                        }
                        return _deps;
                    }
                }
                uint dependencyDataOffset;
                public int DependencyHashCode => dependencyDataOffset.GetHashCode();
                public bool HasDependencies => dependencyDataOffset != uint.MaxValue;
                public object Data { get; internal set; }
                public string PrimaryKey { get; internal set; }
                public Type ResourceType { get; internal set; }

                public override string ToString()
                {
                    return InternalId;
                }

                public int Hash(Type resultType)
                {
                    return (int)InternalId.GetHashCode() * 31 + ResourceType.GetHashCode();
                }
            }

            Dictionary<object, uint> keyData;
            BinaryStorageBuffer.Reader reader;
            public string LocatorId { get; private set; }
            public IEnumerable<object> Keys => keyData.Keys;
            string providerSuffix;

            //TODO: this is VERY expensive with this locator since it will expand the entire thing into memory and then throw most of it away.
            public IEnumerable<IResourceLocation> AllLocations
            {
                get
                {
                    var allLocs = new HashSet<IResourceLocation>(new ResourceLocationComparer());
                    foreach (var kvp in keyData)
                    {
                        if (Locate(kvp.Key, null, out var locs))
                        {
                            foreach (var l in locs)
                                allLocs.Add(l);
                        }
                    }
                    return allLocs;
                }
            }

            internal ResourceLocator(string id, BinaryStorageBuffer.Reader reader, string providerSuffix)
            {
                LocatorId = id;
                this.providerSuffix = providerSuffix;
                this.reader = reader;
                keyData = new Dictionary<object, uint>();
                var header = reader.ReadValue<Header>(0, out var _);
                var keyDataArray = reader.ReadValueArray<KeyData>(header.keysOffset, out var _, false);
                int index = 0;
                foreach (var k in keyDataArray)
                {
                    var key = reader.ReadObject(k.keyNameOffset, out _);
                    keyData.Add(key, k.locationSetOffset);
                    index++;
                }
                reader.ResetCache(keyData.Count * 3, 0);
            }

            class LocateProcContext
            {
                public IList<IResourceLocation> locations;
                public Type type;
            }
            LocateProcContext sharedContext = new LocateProcContext();
            static void ProcFunc(ResourceLocation loc, LocateProcContext context, int i, int count)
            {
                if (context.type == null || context.type == typeof(object) || context.type.IsAssignableFrom(loc.ResourceType))
                {
                    if (context.locations == null)
                        context.locations = new List<IResourceLocation>(count);
                    context.locations.Add(loc);
                }
            }

            public bool Locate(object key, Type type, out IList<IResourceLocation> locations)
            {
                if (!keyData.TryGetValue(key, out var locationSetOffset))
                {
                    locations = null;
                    return false;
                }
                try
                {
                    sharedContext.type = type;
                    var count = reader.ProcessObjectArray<ResourceLocation, LocateProcContext>(
                        locationSetOffset, out var sizeRead,
                        sharedContext, ProcFunc);
                    locations = sharedContext.locations;
                    sharedContext.locations = null;
                    sharedContext.type = null;

                    if (providerSuffix != null && locations != null)
                    {
                        foreach (var l in locations)
                        {
                            if (!l.ProviderId.EndsWith(providerSuffix))
                            {
                                (l as ResourceLocation).ProviderId = l.ProviderId + providerSuffix;
                            }
                        }
                    }

                    return locations != null;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    locations = null;
                    return false;
                }
            }
        }

        internal class AssetBundleRequestOptionsSerializationAdapter : BinaryStorageBuffer.ISerializationAdapter<AssetBundleRequestOptions>
        {
            struct SerializedData
            {
                //since this data is likely to be duplicated, save it separately to allow the serialization system to dedupe
                public struct Common
                {
                    public short timeout;
                    public byte redirectLimit;
                    public byte retryCount;
                    public int flags;

                    public AssetLoadMode assetLoadMode
                    {
                        get => (flags & 1) == 1 ? AssetLoadMode.AllPackedAssetsAndDependencies : AssetLoadMode.RequestedAssetAndDependencies;
                        set => flags = (flags & ~1) | (int)value;
                    }

                    public bool chunkedTransfer
                    {
                        get => (flags & 2) == 2;
                        set => flags = (flags & ~2) | (int)(value ? 2 : 0);
                    }
                    public bool useCrcForCachedBundle
                    {
                        get => (flags & 4) == 4;
                        set => flags = (flags & ~4) | (int)(value ? 4 : 0);
                    }
                    public bool useUnityWebRequestForLocalBundles
                    {
                        get => (flags & 8) == 8;
                        set => flags = (flags & ~8) | (int)(value ? 8 : 0);
                    }
                    public bool clearOtherCachedVersionsWhenLoaded
                    {
                        get => (flags & 16) == 16;
                        set => flags = (flags & ~16) | (int)(value ? 16 : 0);
                    }
                }

                public uint hashId;
                public uint bundleNameId;
                public uint crc;
                public uint bundleSize;
                public uint commonId;
            }
            public IEnumerable<BinaryStorageBuffer.ISerializationAdapter> Dependencies => null;
            public object Deserialize(BinaryStorageBuffer.Reader reader, Type type, uint offset, out uint size)
            {
                size = 0;
                if (type != typeof(AssetBundleRequestOptions))
                    return null;

                var sd = reader.ReadValue<SerializedData>(offset, out var sdSize);
                var com = reader.ReadValue<SerializedData.Common>(sd.commonId, out var comSize);
                var hash = reader.ReadValue<Hash128>(sd.hashId, out var hashSize).ToString();
                var bundleName = reader.ReadString(sd.bundleNameId, out var bnSize, '_');
                var res = new AssetBundleRequestOptions
                {
                    Hash = hash,
                    BundleName = bundleName,
                    Crc = sd.crc,
                    BundleSize = sd.bundleSize,
                    Timeout = com.timeout,
                    RetryCount = com.retryCount,
                    RedirectLimit = com.redirectLimit,
                    AssetLoadMode = com.assetLoadMode,
                    ChunkedTransfer = com.chunkedTransfer,
                    UseUnityWebRequestForLocalBundles = com.useUnityWebRequestForLocalBundles,
                    UseCrcForCachedBundle = com.useCrcForCachedBundle,
                    ClearOtherCachedVersionsWhenLoaded = com.clearOtherCachedVersionsWhenLoaded
                };
                size = sdSize + comSize + hashSize + bnSize;
                return res;
            }

            public uint Serialize(BinaryStorageBuffer.Writer writer, object obj)
            {
                var options = obj as AssetBundleRequestOptions;
                var hash = Hash128.Parse(options.Hash);

                // ensure the correct values for casting to smaller
                short timeout = (short)Mathf.Clamp(options.Timeout, 0, short.MaxValue);
                byte retryCount = (byte)Mathf.Clamp(options.RetryCount, 0, 128);
                byte redirectLimit = options.RedirectLimit < 0 ? (byte)32 : (byte)Mathf.Clamp(options.RedirectLimit, 0, 128);

                var sd = new SerializedData
                {
                    hashId = writer.Write(hash),
                    bundleNameId = writer.WriteString(options.BundleName, '_'),
                    crc = options.Crc,
                    bundleSize = (uint)options.BundleSize,

                    commonId = writer.Write(new SerializedData.Common
                    {
                        timeout = timeout,
                        redirectLimit = redirectLimit,
                        retryCount = retryCount,
                        assetLoadMode = options.AssetLoadMode,
                        chunkedTransfer = options.ChunkedTransfer,
                        clearOtherCachedVersionsWhenLoaded = options.ClearOtherCachedVersionsWhenLoaded,
                        useCrcForCachedBundle = options.UseCrcForCachedBundle,
                        useUnityWebRequestForLocalBundles = options.UseUnityWebRequestForLocalBundles
                    })
                };
                return writer.Write(sd);
            }
        }
    }
}
