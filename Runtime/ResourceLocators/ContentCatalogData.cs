using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

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
    /// Abstract base for format-specific catalog data containers.
    /// </summary>
    [Serializable]
    public abstract class ContentCatalogData
    {
        /// <summary>
        /// Magic number written at the start of binary catalog files, used to verify that a file is a valid catalog data file.
        /// </summary>
        /// <remarks>
        /// Previously calculated from nameof(ContentCatalogData).GetHashCode(), but that is not guaranteed to
        /// be stable so it was switched to the hard coded original hash code from Mono.
        /// </remarks>
        protected const int kMagic = 0x0de38942;

        /// <summary>
        /// Version of the binary catalog file format.
        /// </summary>
        /// <remarks>
        /// v3: type identities normalized to runtime-portable form (CoreCLR).
        /// </remarks>
        protected const int kVersion = 3;

        /// <summary>
        /// Stores the local catalog hash
        /// </summary>
        [NonSerialized]
        public string LocalHash;

        [NonSerialized]
        internal IResourceLocation location;

        [SerializeField]
        internal string m_LocatorId;

        [SerializeField]
        internal string m_BuildResultHash;

        /// <summary>
        /// Stores the hash for the build result
        /// </summary>
        public string BuildResultHash { get => m_BuildResultHash; set => m_BuildResultHash = value; }

        /// <summary>
        /// Stores the id of the data provider.
        /// </summary>
        public string ProviderId
        {
            get { return m_LocatorId; }
            set { m_LocatorId = value; }
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

        internal IList<ContentCatalogDataEntry> m_Entries;

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

        internal abstract void CleanData();
        internal abstract IResourceLocator CreateCustomLocator(string overrideId = "", string providerSuffix = null);

        /// <summary>
        /// Returns the catalog bytes for caching to disk after a remote load.
        /// </summary>
        internal abstract byte[] GetSerializedData();

#if UNITY_EDITOR
        internal abstract void SaveToFile(string path);
        public abstract byte[] SerializeToByteArray();
        public abstract void SetData(IList<ContentCatalogDataEntry> data);
#endif
    }
}
