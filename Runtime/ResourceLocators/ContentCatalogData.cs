using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine.AddressableAssets.Utility;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;

namespace UnityEngine.AddressableAssets.ResourceLocators
{
    /// <summary>
    /// Contains serializable data for an IResourceLocation
    /// </summary>
    public class ContentCatalogDataEntry
    {
        /// <summary>
        /// Internl id.
        /// </summary>
        public string InternalId { get; set; }

        /// <summary>
        /// IResourceProvider identifier.
        /// </summary>
        public string Provider { get; private set; }

        /// <summary>
        /// Keys for this location.
        /// </summary>
        public List<object> Keys { get; private set; }

        /// <summary>
        /// Dependency keys.
        /// </summary>
        public List<object> Dependencies { get; private set; }

        /// <summary>
        /// Serializable data for the provider.
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// The type of the resource for th location.
        /// </summary>
        public Type ResourceType { get; private set; }

        /// <summary>
        /// Creates a new ContentCatalogEntry object.
        /// </summary>
        /// <param name="type">The entry type.</param>
        /// <param name="internalId">The internal id.</param>
        /// <param name="provider">The provider id.</param>
        /// <param name="keys">The collection of keys that can be used to retrieve this entry.</param>
        /// <param name="dependencies">Optional collection of keys for dependencies.</param>
        /// <param name="extraData">Optional additional data to be passed to the provider.  For example, AssetBundleProviders use this for cache and crc data.</param>
        public ContentCatalogDataEntry(Type type, string internalId, string provider, IEnumerable<object> keys, IEnumerable<object> dependencies = null, object extraData = null)
        {
            InternalId = internalId;
            Provider = provider;
            ResourceType = type;
            Keys = new List<object>(keys);
            Dependencies = dependencies == null ? new List<object>() : new List<object>(dependencies);
            Data = extraData;
        }
    }

    /// <summary>
    /// Container for ContentCatalogEntries.
    /// </summary>
    [Serializable]
    public class ContentCatalogData
    {
        //used to verify that this is a valid catalog data file
        static int kMagic = nameof(ContentCatalogData).GetHashCode();
        //used to check the version of the data in case the format needs to change in the future
        const int kVersion = 1;

        [NonSerialized]
        internal string localHash;

        [NonSerialized]
        internal IResourceLocation location;

        [SerializeField]
        internal string m_LocatorId;

        [SerializeField]
        internal string m_BuildResultHash;

        /// <summary>
        /// Stores the id of the data provider.
        /// </summary>
        public string ProviderId
        {
            get { return m_LocatorId; }
            internal set { m_LocatorId = value; }
        }

        [SerializeField]
        ObjectInitializationData m_InstanceProviderData;

        /// <summary>
        /// Data for the Addressables.ResourceManager.InstanceProvider initialization;
        /// </summary>
        public ObjectInitializationData InstanceProviderData
        {
            get { return m_InstanceProviderData; }
            set { m_InstanceProviderData = value; }
        }

        [SerializeField]
        ObjectInitializationData m_SceneProviderData;

        /// <summary>
        /// Data for the Addressables.ResourceManager.InstanceProvider initialization;
        /// </summary>
        public ObjectInitializationData SceneProviderData
        {
            get { return m_SceneProviderData; }
            set { m_SceneProviderData = value; }
        }

        [SerializeField]
        internal List<ObjectInitializationData> m_ResourceProviderData = new List<ObjectInitializationData>();

        /// <summary>
        /// The list of resource provider data.  Each entry will add an IResourceProvider to the Addressables.ResourceManager.ResourceProviders list.
        /// </summary>
        public List<ObjectInitializationData> ResourceProviderData
        {
            get { return m_ResourceProviderData; }
            set { m_ResourceProviderData = value; }
        }
        IList<ContentCatalogDataEntry> m_Entries;
        /// <summary>
        /// Creates a new ContentCatalogData object with the specified locator id.
        /// </summary>
        /// <param name="id">The id of the locator.</param>
        public ContentCatalogData(string id)
        {
            m_LocatorId = id;
        }

        /// <summary>
        /// Create a new ContentCatalogData object without any data.
        /// </summary>
        public ContentCatalogData()
        {
        }

#if UNITY_EDITOR
        /// <summary>
        /// Creates a new ContentCatalogData object with the specified locator id.
        /// </summary>
        /// <param name="id">The id of the locator.</param>
        public ContentCatalogData(IList<ContentCatalogDataEntry> entries, string id = null)
        {
            m_LocatorId = id;
            SetData(entries);
        }
#endif


#if ENABLE_BINARY_CATALOG

        internal void CleanData()
        {
            m_LocatorId = null;
            m_Reader = null;
        }

        internal void CopyToFile(string path)
        {
            byte[] bytes = m_Reader.GetBuffer();
            File.WriteAllBytes(path, bytes);
        }


#if UNITY_EDITOR
        /// <summary>
        /// Construct catalog data with entries.
        /// </summary>
        /// <param name="data">The data entries.</param>
        /// <param name="id">The locator id.</param>
        public byte[] SerializeToByteArray()
        {
            var wr = new BinaryStorageBuffer.Writer(0, new Serializer());
            wr.WriteObject(this, false);
            return wr.SerializeToByteArray();
        }

        public void SetData(IList<ContentCatalogDataEntry> entries)
        {
            m_Entries = entries;
            m_Reader = new BinaryStorageBuffer.Reader(SerializeToByteArray(), 1024, new Serializer());
        }

        internal void SaveToFile(string path)
        {
            var bytes = SerializeToByteArray();
            File.WriteAllBytes(path, bytes);
        }
#endif

        BinaryStorageBuffer.Reader m_Reader;
        internal ContentCatalogData(BinaryStorageBuffer.Reader reader)
        {
            m_Reader = reader;
        }

        internal IResourceLocator CreateCustomLocator(string overrideId = "", string providerSuffix = null, int locatorCacheSize = 100)
        {
            m_LocatorId = overrideId;
            return new ResourceLocator(m_LocatorId, m_Reader, locatorCacheSize, providerSuffix);
        }

        internal class Serializer : BinaryStorageBuffer.ISerializationAdapter<ContentCatalogData>
        {
            public IEnumerable<BinaryStorageBuffer.ISerializationAdapter> Dependencies => new BinaryStorageBuffer.ISerializationAdapter[]
            {
                new ObjectInitializationData.Serializer(),
                new AssetBundleRequestOptionsSerializationAdapter(),
                new ResourceLocator.ResourceLocation.Serializer()
            };

            public object Deserialize(BinaryStorageBuffer.Reader reader, Type t, uint offset)
            {
                var cd = new ContentCatalogData(reader);
                var h = reader.ReadValue<ResourceLocator.Header>(offset);
                if (h.magic != kMagic)
                {
                    var h2 = reader.ReadValue<ResourceLocator.Header>(offset);
                    throw new Exception("Invalid header data!!!");
                }
                cd.InstanceProviderData = reader.ReadObject<ObjectInitializationData>(h.instanceProvider);
                cd.SceneProviderData = reader.ReadObject<ObjectInitializationData>(h.sceneProvider);
                cd.ResourceProviderData = reader.ReadObjectArray<ObjectInitializationData>(h.initObjectsArray).ToList();

                cd.m_BuildResultHash = reader.ReadString(h.buildResultHash);
                return cd;
            }

            public uint Serialize(BinaryStorageBuffer.Writer writer, object val)
            {
                var cd = val as ContentCatalogData;
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
                    buildResultHash = writer.WriteString(cd.m_BuildResultHash)
                };
                writer.Write(headerOffset, in header);

                //create array of all locations
                var locationIds = new uint[entries.Count];
                for (int i = 0; i < entries.Count; i++)
                    locationIds[i] = writer.WriteObject(new ResourceLocator.ContentCatalogDataEntrySerializationContext { entry = entries[i], allEntries = entries, keyToEntryIndices = keyToEntryIndices }, false);


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

        internal static ContentCatalogData LoadFromFile(string path, int cacheSize = 1024)
        {
            return new ContentCatalogData(new BinaryStorageBuffer.Reader(File.ReadAllBytes(path), cacheSize, new Serializer()));
        }

#else
        /// <summary>
        /// The IDs for the Resource Providers.
        /// </summary>
        public string[] ProviderIds
        {
            get { return m_ProviderIds; }
        }

        /// <summary>
        /// Internal Content Catalog Entry IDs for Addressable Assets.
        /// </summary>
        public string[] InternalIds
        {
            get { return m_InternalIds; }
        }

        [FormerlySerializedAs("m_providerIds")]
        [SerializeField]
        internal string[] m_ProviderIds = null;

        [FormerlySerializedAs("m_internalIds")]
        [SerializeField]
        internal string[] m_InternalIds = null;

        [FormerlySerializedAs("m_keyDataString")]
        [SerializeField]
        internal string m_KeyDataString = null;

        [FormerlySerializedAs("m_bucketDataString")]
        [SerializeField]
        internal string m_BucketDataString = null;

        [FormerlySerializedAs("m_entryDataString")]
        [SerializeField]
        internal string m_EntryDataString = null;

        const int kBytesPerInt32 = 4;
        const int k_EntryDataItemPerEntry = 7;

        [FormerlySerializedAs("m_extraDataString")]
        [SerializeField]
        internal string m_ExtraDataString = null;

        [SerializeField]
        internal SerializedType[] m_resourceTypes = null;

        [SerializeField]
        string[] m_InternalIdPrefixes = null;

        struct Bucket
        {
            public int dataOffset;
            public int[] entries;
        }

        class CompactLocation : IResourceLocation
        {
            ResourceLocationMap m_Locator;
            string m_InternalId;
            string m_ProviderId;
            object m_Dependency;
            object m_Data;
            int m_HashCode;
            int m_DependencyHashCode;
            string m_PrimaryKey;
            Type m_Type;

            public string InternalId
            {
                get { return m_InternalId; }
            }

            public string ProviderId
            {
                get { return m_ProviderId; }
            }

            public IList<IResourceLocation> Dependencies
            {
                get
                {
                    if (m_Dependency == null)
                        return null;
                    IList<IResourceLocation> results;
                    m_Locator.Locate(m_Dependency, typeof(object), out results);
                    return results;
                }
            }

            public bool HasDependencies
            {
                get { return m_Dependency != null; }
            }

            public int DependencyHashCode
            {
                get { return m_DependencyHashCode; }
            }

            public object Data
            {
                get { return m_Data; }
            }

            public string PrimaryKey
            {
                get { return m_PrimaryKey; }
                set { m_PrimaryKey = value; }
            }

            public Type ResourceType
            {
                get { return m_Type; }
            }

            public override string ToString()
            {
                return m_InternalId;
            }

            public int Hash(Type t)
            {
                return (m_HashCode * 31 + t.GetHashCode()) * 31 + DependencyHashCode;
            }

            public CompactLocation(ResourceLocationMap locator, string internalId, string providerId, object dependencyKey, object data, int depHash, string primaryKey, Type type)
            {
                m_Locator = locator;
                m_InternalId = internalId;
                m_ProviderId = providerId;
                m_Dependency = dependencyKey;
                m_Data = data;
                m_HashCode = internalId.GetHashCode() * 31 + providerId.GetHashCode();
                m_DependencyHashCode = depHash;
                m_PrimaryKey = primaryKey;
                m_Type = type == null ? typeof(object) : type;
            }
        }

        internal static ContentCatalogData LoadFromFile(string path, int cacheSize = 1024)
        {
            return JsonUtility.FromJson<ContentCatalogData>(File.ReadAllText(path));
        }

        internal void SaveToFile(string path)
        {
            File.WriteAllText(path, JsonUtility.ToJson(this));
        }

        internal void CleanData()
        {
            m_KeyDataString = "";
            m_BucketDataString = "";
            m_EntryDataString = "";
            m_ExtraDataString = "";
            m_InternalIds = null;
            m_LocatorId = "";
            m_ProviderIds = null;
            m_ResourceProviderData = null;
            m_resourceTypes = null;
        }

        internal ResourceLocationMap CreateCustomLocator(string overrideId = "", string providerSuffix = null)
        {
            m_LocatorId = overrideId;
            return CreateLocator(providerSuffix);
        }

        /// <summary>
        /// Create IResourceLocator object
        /// </summary>
        /// <param name="providerSuffix">If specified, this value will be appeneded to all provider ids.  This is used when loading additional catalogs that need to have unique providers.</param>
        /// <returns>ResourceLocationMap, which implements the IResourceLocator interface.</returns>
        public ResourceLocationMap CreateLocator(string providerSuffix = null)
        {
            var bucketData = Convert.FromBase64String(m_BucketDataString);
            int bucketCount = BitConverter.ToInt32(bucketData, 0);
            var buckets = new Bucket[bucketCount];
            int bi = 4;
            for (int i = 0; i < bucketCount; i++)
            {
                var index = SerializationUtilities.ReadInt32FromByteArray(bucketData, bi);
                bi += 4;
                var entryCount = SerializationUtilities.ReadInt32FromByteArray(bucketData, bi);
                bi += 4;
                var entryArray = new int[entryCount];
                for (int c = 0; c < entryCount; c++)
                {
                    entryArray[c] = SerializationUtilities.ReadInt32FromByteArray(bucketData, bi);
                    bi += 4;
                }

                buckets[i] = new Bucket { entries = entryArray, dataOffset = index };
            }

            if (!string.IsNullOrEmpty(providerSuffix))
            {
                for (int i = 0; i < m_ProviderIds.Length; i++)
                {
                    if (!m_ProviderIds[i].EndsWith(providerSuffix, StringComparison.Ordinal))
                        m_ProviderIds[i] = m_ProviderIds[i] + providerSuffix;
                }
            }

            var extraData = Convert.FromBase64String(m_ExtraDataString);

            var keyData = Convert.FromBase64String(m_KeyDataString);
            var keyCount = BitConverter.ToInt32(keyData, 0);
            var keys = new object[keyCount];
            for (int i = 0; i < buckets.Length; i++)
                keys[i] = SerializationUtilities.ReadObjectFromByteArray(keyData, buckets[i].dataOffset);

            var locator = new ResourceLocationMap(m_LocatorId, buckets.Length);

            var entryData = Convert.FromBase64String(m_EntryDataString);
            int count = SerializationUtilities.ReadInt32FromByteArray(entryData, 0);
            var locations = new IResourceLocation[count];
            for (int i = 0; i < count; i++)
            {
                var index = kBytesPerInt32 + i * (kBytesPerInt32 * k_EntryDataItemPerEntry);
                var internalId = SerializationUtilities.ReadInt32FromByteArray(entryData, index);
                index += kBytesPerInt32;
                var providerIndex = SerializationUtilities.ReadInt32FromByteArray(entryData, index);
                index += kBytesPerInt32;
                var dependencyKeyIndex = SerializationUtilities.ReadInt32FromByteArray(entryData, index);
                index += kBytesPerInt32;
                var depHash = SerializationUtilities.ReadInt32FromByteArray(entryData, index);
                index += kBytesPerInt32;
                var dataIndex = SerializationUtilities.ReadInt32FromByteArray(entryData, index);
                index += kBytesPerInt32;
                var primaryKey = SerializationUtilities.ReadInt32FromByteArray(entryData, index);
                index += kBytesPerInt32;
                var resourceType = SerializationUtilities.ReadInt32FromByteArray(entryData, index);
                object data = dataIndex < 0 ? null : SerializationUtilities.ReadObjectFromByteArray(extraData, dataIndex);
                locations[i] = new CompactLocation(locator, Addressables.ResolveInternalId(ExpandInternalId(m_InternalIdPrefixes, m_InternalIds[internalId])),
                   m_ProviderIds[providerIndex], dependencyKeyIndex < 0 ? null : keys[dependencyKeyIndex], data, depHash, keys[primaryKey].ToString(), m_resourceTypes[resourceType].Value);
            }

            for (int i = 0; i < buckets.Length; i++)
            {
                var bucket = buckets[i];
                var key = keys[i];
                var locs = new IResourceLocation[bucket.entries.Length];
                for (int b = 0; b < bucket.entries.Length; b++)
                    locs[b] = locations[bucket.entries[b]];
                locator.Add(key, locs);
            }

            return locator;
        }

        internal IList<ContentCatalogDataEntry> GetData()
        {
            var loc = CreateLocator();
            var res = new List<ContentCatalogDataEntry>();
            var locsToKeys = new Dictionary<IResourceLocation, List<object>>();
            foreach (var k in loc.Keys)
            {
                //foreach (var l in k.Value)
                loc.Locate(k, null, out var locs);
                foreach (var l in locs)
                {
                    if (!locsToKeys.TryGetValue(l, out var keys))
                        locsToKeys.Add(l, keys = new List<object>());
                    keys.Add(k.ToString());
                }
            }
            foreach (var k in locsToKeys)
            {
                res.Add(new ContentCatalogDataEntry(k.Key.ResourceType, k.Key.InternalId, k.Key.ProviderId, k.Value, k.Key.Dependencies == null ? null : k.Key.Dependencies.Select(d => d.PrimaryKey).ToList(), k.Key.Data));
            }

            return res;
        }

        internal static string ExpandInternalId(string[] internalIdPrefixes, string v)
        {
            if (internalIdPrefixes == null || internalIdPrefixes.Length == 0)
                return v;
            int nextHash = v.LastIndexOf('#');
            if (nextHash < 0)
                return v;
            int index = 0;
            var numStr = v.Substring(0, nextHash);
            if (!int.TryParse(numStr, out index))
                return v;
            return internalIdPrefixes[index] + v.Substring(nextHash + 1);
        }

#if UNITY_EDITOR

        class KeyIndexer<T>
        {
            public List<T> values;
            public Dictionary<T, int> map;

            public KeyIndexer(IEnumerable<T> keyCollection, int capacity)
            {
                values = new List<T>(capacity);
                map = new Dictionary<T, int>(capacity);
                if (keyCollection != null)
                    Add(keyCollection);
            }

            public void Add(IEnumerable<T> keyCollection)
            {
                bool isNew = false;
                foreach (var key in keyCollection)
                    Add(key, ref isNew);
            }

            public void Add(T key, ref bool isNew)
            {
                int index;
                if (!map.TryGetValue(key, out index))
                {
                    isNew = true;
                    map.Add(key, values.Count);
                    values.Add(key);
                }
            }
        }

        class KeyIndexer<TVal, TKey>
        {
            public List<TVal> values;
            public Dictionary<TKey, int> map;

            public KeyIndexer(IEnumerable<TKey> keyCollection, Func<TKey, TVal> func, int capacity)
            {
                values = new List<TVal>(capacity);
                map = new Dictionary<TKey, int>(capacity);
                if (keyCollection != null)
                    Add(keyCollection, func);
            }

            void Add(IEnumerable<TKey> keyCollection, Func<TKey, TVal> func)
            {
                foreach (var key in keyCollection)
                    Add(key, func(key));
            }

            public void Add(TKey key, TVal val)
            {
                int index;
                if (!map.TryGetValue(key, out index))
                {
                    map.Add(key, values.Count);
                    values.Add(val);
                }
            }

            public TVal this[TKey key]
            {
                get { return values[map[key]]; }
            }
        }

        /// <summary>
        /// Sets the catalog data before serialization.
        /// </summary>
        /// <param name="data">The list of catalog entries.</param>
        /// <param name="optimizeSize">Whether to optimize the catalog size by extracting common internal id prefixes.</param>
        [Obsolete("The ]optimizeSize parameter for SetData is no longer used.")]
        public void SetData(IList<ContentCatalogDataEntry> data, bool optimizeSize)
        {
        }

        /// <summary>
        /// Sets the catalog data before serialization.
        /// </summary>
        /// <param name="data">The list of catalog entries.</param>
        public void SetData(IList<ContentCatalogDataEntry> data)
        {
            if (data == null)
                return;
            var providers = new KeyIndexer<string>(data.Select(s => s.Provider), 10);
            var internalIds = new KeyIndexer<string>(data.Select(s => s.InternalId), data.Count);
            var keys = new KeyIndexer<object>(data.SelectMany(s => s.Keys), data.Count * 3);
            var types = new KeyIndexer<Type>(data.Select(s => s.ResourceType), 50);

            keys.Add(data.SelectMany(s => s.Dependencies));
            var keyIndexToEntries = new KeyIndexer<List<ContentCatalogDataEntry>, object>(keys.values, s => new List<ContentCatalogDataEntry>(), keys.values.Count);
            var entryToIndex = new Dictionary<ContentCatalogDataEntry, int>(data.Count);
            var extraDataList = new List<byte>(8 * 1024);
            var entryIndexToExtraDataIndex = new Dictionary<int, int>();

            int extraDataIndex = 0;
            //create buckets of key to data entry
            for (int i = 0; i < data.Count; i++)
            {
                var e = data[i];
                int extraDataOffset = -1;
                if (e.Data != null)
                {
                    var len = SerializationUtilities.WriteObjectToByteList(e.Data, extraDataList);
                    if (len > 0)
                    {
                        extraDataOffset = extraDataIndex;
                        extraDataIndex += len;
                    }
                }

                entryIndexToExtraDataIndex.Add(i, extraDataOffset);
                entryToIndex.Add(e, i);
                foreach (var k in e.Keys)
                    keyIndexToEntries[k].Add(e);
            }

            m_ExtraDataString = Convert.ToBase64String(extraDataList.ToArray());

            //create extra entries for dependency sets
            Dictionary<int, object> hashSources = new Dictionary<int, object>();
            int originalEntryCount = data.Count;
            for (int i = 0; i < originalEntryCount; i++)
            {
                var entry = data[i];
                if (entry.Dependencies == null || entry.Dependencies.Count < 2)
                    continue;

                var hashCode = CalculateCollectedHash(entry.Dependencies, hashSources).ToString();

                bool isNew = false;
                keys.Add(hashCode, ref isNew);
                if (isNew)
                {
                    //if this combination of dependecies is new, add a new entry and add its key to all contained entries
                    var deps = entry.Dependencies.Select(d => keyIndexToEntries[d][0]).ToList();
                    keyIndexToEntries.Add(hashCode, deps);
                    foreach (var dep in deps)
                        dep.Keys.Add(hashCode);
                }

                //reset the dependency list to only contain the key of the new set
                entry.Dependencies.Clear();
                entry.Dependencies.Add(hashCode);
            }

            //serialize internal ids and providers
            m_InternalIds = internalIds.values.ToArray();
            m_ProviderIds = providers.values.ToArray();
            m_resourceTypes = types.values.Select(t => new SerializedType() { Value = t }).ToArray();

            //serialize entries
            {
                var entryData = new byte[data.Count * (kBytesPerInt32 * k_EntryDataItemPerEntry) + kBytesPerInt32];
                var entryDataOffset = SerializationUtilities.WriteInt32ToByteArray(entryData, data.Count, 0);
                for (int i = 0; i < data.Count; i++)
                {
                    var e = data[i];
                    entryDataOffset = SerializationUtilities.WriteInt32ToByteArray(entryData, internalIds.map[e.InternalId], entryDataOffset);
                    entryDataOffset = SerializationUtilities.WriteInt32ToByteArray(entryData, providers.map[e.Provider], entryDataOffset);
                    entryDataOffset = SerializationUtilities.WriteInt32ToByteArray(entryData, e.Dependencies.Count == 0 ? -1 : keyIndexToEntries.map[e.Dependencies[0]], entryDataOffset);
                    entryDataOffset = SerializationUtilities.WriteInt32ToByteArray(entryData, GetHashCodeForEnumerable(e.Dependencies), entryDataOffset);
                    entryDataOffset = SerializationUtilities.WriteInt32ToByteArray(entryData, entryIndexToExtraDataIndex[i], entryDataOffset);
                    entryDataOffset = SerializationUtilities.WriteInt32ToByteArray(entryData, keys.map[e.Keys.First()], entryDataOffset);
                    entryDataOffset = SerializationUtilities.WriteInt32ToByteArray(entryData, (ushort)types.map[e.ResourceType], entryDataOffset);
                }

                m_EntryDataString = Convert.ToBase64String(entryData);
            }

            //serialize keys and mappings
            {
                var entryCount = keyIndexToEntries.values.Aggregate(0, (a, s) => a += s.Count);
                var bucketData = new byte[4 + keys.values.Count * 8 + entryCount * 4];
                var keyData = new List<byte>(keys.values.Count * 100);
                keyData.AddRange(BitConverter.GetBytes(keys.values.Count));
                int keyDataOffset = 4;
                int bucketDataOffset = SerializationUtilities.WriteInt32ToByteArray(bucketData, keys.values.Count, 0);
                for (int i = 0; i < keys.values.Count; i++)
                {
                    var key = keys.values[i];
                    bucketDataOffset = SerializationUtilities.WriteInt32ToByteArray(bucketData, keyDataOffset, bucketDataOffset);
                    keyDataOffset += SerializationUtilities.WriteObjectToByteList(key, keyData);
                    var entries = keyIndexToEntries[key];
                    bucketDataOffset = SerializationUtilities.WriteInt32ToByteArray(bucketData, entries.Count, bucketDataOffset);
                    foreach (var e in entries)
                        bucketDataOffset = SerializationUtilities.WriteInt32ToByteArray(bucketData, entryToIndex[e], bucketDataOffset);
                }

                m_BucketDataString = Convert.ToBase64String(bucketData);
                m_KeyDataString = Convert.ToBase64String(keyData.ToArray());
            }
        }

        internal int CalculateCollectedHash(List<object> objects, Dictionary<int, object> hashSources)
        {
            var hashSource = new HashSet<object>(objects);
            var hashCode = GetHashCodeForEnumerable(hashSource);
            if (hashSources.TryGetValue(hashCode, out var previousHashSource))
            {
                if (!(previousHashSource is HashSet<object> b) || !hashSource.SetEquals(b))
                    throw new Exception($"INCORRECT HASH: the same hash ({hashCode}) for different dependency lists:\nsource 1: {previousHashSource}\nsource 2: {hashSource}");
            }
            else
                hashSources.Add(hashCode, hashSource);

            return hashCode;
        }

        internal static int GetHashCodeForEnumerable(IEnumerable<object> set)
        {
            int hash = 0;
            foreach (object o in set)
                hash = hash * 31 + o.GetHashCode();
            return hash;
        }
#endif



#endif
#if ENABLE_BINARY_CATALOG
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
            }

            internal class ResourceLocation : IResourceLocation
            {
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

                    public IEnumerable<BinaryStorageBuffer.ISerializationAdapter> Dependencies => null;

                    //read as location
                    public object Deserialize(BinaryStorageBuffer.Reader reader, Type t, uint offset)
                    {
                        return new ResourceLocation(reader, offset);
                    }

                    //write from data entry
                    public uint Serialize(BinaryStorageBuffer.Writer writer, object val)
                    {
                        var ec = val as ContentCatalogDataEntrySerializationContext;
                        var e = ec.entry;
                        uint depId = uint.MaxValue;
                        if (e.Dependencies != null && e.Dependencies.Count > 0)
                        {
                            var depIds = new HashSet<uint>();
                            foreach (var k in e.Dependencies)
                                foreach (var i in ec.keyToEntryIndices[k])
                                    depIds.Add(writer.WriteObject(new ResourceLocator.ContentCatalogDataEntrySerializationContext { entry = ec.allEntries[i], allEntries = ec.allEntries, keyToEntryIndices = ec.keyToEntryIndices }, false));
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
                        return writer.Write(data);
                    }
                }

                public ResourceLocation(BinaryStorageBuffer.Reader r, uint id)
                {
                    var d = r.ReadValue<Serializer.Data>(id);
                    PrimaryKey = r.ReadString(d.primaryKeyOffset, '/', false);
                    InternalId = Addressables.ResolveInternalId(r.ReadString(d.internalIdOffset, '/', false));
                    Data = r.ReadObject(d.extraDataOffset, false);
                    ProviderId = r.ReadString(d.providerOffset, '.', true);
                    Dependencies = r.ReadObjectArray<ResourceLocation>(d.dependencySetOffset, true);
                    DependencyHashCode = (int)d.dependencySetOffset;
                    ResourceType = r.ReadObject<Type>(d.typeId);
                }

                public override string ToString()
                {
                    return InternalId;
                }

                public string PrimaryKey { private set; get; }
                public string InternalId { private set; get; }
                public object Data { private set; get; }
                public string ProviderId { set; get; }
                public IList<IResourceLocation> Dependencies { private set; get; }
                public int DependencyHashCode { private set; get; }
                public bool HasDependencies => DependencyHashCode >= 0;
                public Type ResourceType { private set; get; }
                public int Hash(Type t) => (InternalId.GetHashCode() * 31 + t.GetHashCode()) * 31 + DependencyHashCode;
            }

            struct CacheKey : IEquatable<CacheKey>
            {
                public object key;
                public Type type;
                int hashCode;
                public CacheKey(object o, Type t)
                {
                    key = o;
                    type = t;
                    hashCode = type == null ? key.GetHashCode() : key.GetHashCode() * 31 + type.GetHashCode();
                }
                public bool Equals(CacheKey other) => key.Equals(other.key) && ((type == null && other.type == null) || type.Equals(other.type));
                public override int GetHashCode() => hashCode;
            }
            LRUCache<CacheKey, IList<IResourceLocation>> m_Cache;
            Dictionary<object, uint> keyData;
            BinaryStorageBuffer.Reader reader;
            public string LocatorId { get; private set; }
            public IEnumerable<object> Keys => keyData.Keys;

            //TODO: this is VERY expensive with this locator since it will expand the entire thing into memory and then throw most of it away.
            public IEnumerable<IResourceLocation> AllLocations
            {
                get
                {
                    var allLocs = new HashSet<IResourceLocation>();
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

            string providerSuffix;
            internal ResourceLocator(string id, BinaryStorageBuffer.Reader reader, int cacheLimit, string providerSuffix)
            {
                LocatorId = id;
                this.providerSuffix = providerSuffix;
                this.reader = reader;
                m_Cache = new LRUCache<CacheKey, IList<IResourceLocation>>(cacheLimit);
                keyData = new Dictionary<object, uint>();
                var header = reader.ReadValue<Header>(0);
                var keyDataArray = reader.ReadValueArray<KeyData>(header.keysOffset, false);
                int index = 0;
                foreach (var k in keyDataArray)
                {
                    var key = reader.ReadObject(k.keyNameOffset);
                    keyData.Add(key, k.locationSetOffset);
                    index++;
                }
            }

            public bool Locate(object key, Type type, out IList<IResourceLocation> locations)
            {
                var cacheKey = new CacheKey(key, type);
                if (m_Cache.TryGet(cacheKey, out locations))
                    return true;
                if (!keyData.TryGetValue(key, out var locationSetOffset))
                {
                    locations = null;
                    return false;
                }
                var locs = reader.ReadObjectArray<ResourceLocation>(locationSetOffset);
                if (providerSuffix != null)
                {
                    foreach (var l in locs)
                    {
                        if (!l.ProviderId.EndsWith(providerSuffix))
                            l.ProviderId = l.ProviderId + providerSuffix;
                    }
                }
                if (type == null || type == typeof(object))
                {
                    locations = new List<IResourceLocation>(locs);
                    m_Cache.TryAdd(cacheKey, locations);
                    return true;
                }
                var validTypeCount = 0;
                foreach (var l in locs)
                    if (type.IsAssignableFrom(l.ResourceType))
                        validTypeCount++;

                if (validTypeCount == 0)
                {
                    locations = null;
                    m_Cache.TryAdd(cacheKey, locations);
                    return false;
                }

                if (validTypeCount == locs.Length)
                {
                    locations = new List<IResourceLocation>(locs);
                    m_Cache.TryAdd(cacheKey, locations);
                    return true;
                }

                locations = new List<IResourceLocation>();
                foreach (var l in locs)
                {
                    if (type.IsAssignableFrom(l.ResourceType))
                        locations.Add(l);
                }
                m_Cache.TryAdd(cacheKey, locations);
                return locations != null;
            }
        }

        class AssetBundleRequestOptionsSerializationAdapter : BinaryStorageBuffer.ISerializationAdapter<AssetBundleRequestOptions>
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
            public object Deserialize(BinaryStorageBuffer.Reader reader, Type type, uint offset)
            {
                if (type != typeof(AssetBundleRequestOptions))
                    return null;

                var sd = reader.ReadValue<SerializedData>(offset);
                var com = reader.ReadValue<SerializedData.Common>(sd.commonId);
                return new AssetBundleRequestOptions
                {
                    Hash = reader.ReadValue<Hash128>(sd.hashId).ToString(),
                    BundleName = reader.ReadString(sd.bundleNameId, '_'),
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
            }

            public uint Serialize(BinaryStorageBuffer.Writer writer, object obj)
            {
                var options = obj as AssetBundleRequestOptions;
                var hash = Hash128.Parse(options.Hash);

                // ensure the correct values for casting to smaller
                short timeout = (short)Mathf.Clamp(options.Timeout, short.MinValue, short.MaxValue);
                byte retryCount = options.RetryCount < 0 ? (byte)32 : (byte)Mathf.Clamp(options.RetryCount, 0, 128);
                byte redirectLimit = (byte)Mathf.Clamp(options.RetryCount, 0, 128);

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
#endif
    }
}
