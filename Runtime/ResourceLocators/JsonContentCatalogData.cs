using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine.AddressableAssets.Utility;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;

namespace UnityEngine.AddressableAssets.ResourceLocators
{
    /// <summary>
    /// JSON-format catalog data. Serialized with JsonUtility.
    /// </summary>
    [Serializable]
    public class JsonContentCatalogData : ContentCatalogData
    {
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

        /// <summary>
        /// Creates a new JsonContentCatalogData object with the specified locator id.
        /// </summary>
        /// <param name="id">The id of the locator.</param>
        public JsonContentCatalogData(string id) : base(id) { }

        /// <summary>
        /// Creates a new JsonContentCatalogData object without any data.
        /// </summary>
        public JsonContentCatalogData() { }

        internal override IResourceLocator CreateCustomLocator(string overrideId = "", string providerSuffix = null)
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

        internal static JsonContentCatalogData LoadFromFile(string path)
        {
            return JsonUtility.FromJson<JsonContentCatalogData>(File.ReadAllText(path));
        }


        internal override void CleanData()
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

        internal override byte[] GetSerializedData() => Encoding.UTF8.GetBytes(JsonUtility.ToJson(this));

#if UNITY_EDITOR
        internal override void SaveToFile(string path)
        {
            File.WriteAllText(path, JsonUtility.ToJson(this));
        }

        public override byte[] SerializeToByteArray()
        {
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(this));
        }

        /// <summary>
        /// Sets the catalog data before serialization.
        /// </summary>
        /// <param name="data">The list of catalog entries.</param>
        public override void SetData(IList<ContentCatalogDataEntry> data)
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
            int hash = 17;
            foreach (object o in set)
                hash = hash * 31 + o.GetHashCode();
            return hash;
        }
#endif

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
#endif
    }
}
