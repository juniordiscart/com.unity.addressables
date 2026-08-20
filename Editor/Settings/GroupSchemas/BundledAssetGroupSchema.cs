using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor.AddressableAssets.GUI;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings.GroupSchemas
{
    /// <summary>
    /// Schema used for bundled asset groups.
    /// </summary>
//    [CreateAssetMenu(fileName = "BundledAssetGroupSchema.asset", menuName = "Addressables/Group Schemas/Bundled Assets")]
    [DisplayName("Content Packing & Loading (AssetBundle)")]
    [AddressablesHelpURL("group-inspector-settings-reference.html")]
    public class BundledAssetGroupSchema : AddressableAssetGroupSchema,
        ISerializationCallbackReceiver,
        IBuildableSchema,
        ICanIncludeFolderKeys,
        ICanIncludeLabels
    {
        /// <summary>
        /// Defines how bundles are created.
        /// </summary>
        public enum BundlePackingMode
        {
            /// <summary>
            /// Creates a bundle for all non-scene entries and another for all scenes entries.
            /// </summary>
            PackTogether,

            /// <summary>
            /// Creates a bundle per entry.  This is useful if each entry is a folder as all sub entries will go to the same bundle.
            /// </summary>
            PackSeparately,

            /// <summary>
            /// Creates a bundle per unique set of labels
            /// </summary>
            PackTogetherByLabel
        }

        /// <summary>
        /// Defines how internal bundles are named. This is used for both caching and for inter-bundle dependecies.  If possible, GroupGuidProjectIdHash should be used as it is stable and unique between projects.
        /// </summary>
        public enum BundleInternalIdMode
        {
            /// <summary>
            /// Use the guid of the group asset
            /// </summary>
            GroupGuid,

            /// <summary>
            /// Use the hash of the group asset guid and the project id
            /// </summary>
            GroupGuidProjectIdHash,

            /// <summary>
            /// Use the hash of the group asset, the project id and the guids of the entries in the group
            /// </summary>
            GroupGuidProjectIdEntriesHash
        }

        /// <summary>
        /// Options for compressing bundles in this group.
        /// </summary>
        public enum BundleCompressionMode
        {
            /// <summary>
            /// Use to indicate that bundles will not be compressed.
            /// </summary>
            Uncompressed,

            /// <summary>
            /// Use to indicate that bundles will be compressed using the LZ4 compression algorithm.
            /// </summary>
            LZ4,

            /// <summary>
            /// Use to indicate that bundles will be compressed using the LZMA compression algorithm.
            /// </summary>
            LZMA
        }

        [SerializeField]
        BundleInternalIdMode m_InternalBundleIdMode = BundleInternalIdMode.GroupGuidProjectIdHash;

        const int k_HelpBoxUIPadding = 4;

        /// <summary>
        /// Internal bundle naming mode
        /// </summary>
        public BundleInternalIdMode InternalBundleIdMode
        {
            get => m_InternalBundleIdMode;
            set
            {
                if (m_InternalBundleIdMode != value)
                {
                    m_InternalBundleIdMode = value;
                    SetDirty(true);
                }
            }
        }

        [SerializeField]
        BundleCompressionMode m_Compression = BundleCompressionMode.LZ4;

        /// <summary>
        /// Build compression.
        /// </summary>
        public BundleCompressionMode Compression
        {
            get
            {
                if (UseDefaultSchemaSettings)
                    return GetDefaultSchemaSettings().compression;
                return m_Compression;
            }
            set
            {
                if (m_Compression != value)
                {
                    m_Compression = value;
                    SetDirty(true);
                }
            }
        }

        /// <summary>
        /// Options for internal id of assets in bundles.
        /// </summary>
        public enum AssetNamingMode
        {
            /// <summary>
            /// Use to identify assets by their full path.
            /// </summary>
            FullPath,

            /// <summary>
            /// Use to identify assets by their filename only.  There is a risk of collisions when assets in different folders have the same filename.
            /// </summary>
            Filename,

            /// <summary>
            /// Use to identify assets by their asset guid.  This will save space over using the full path and will be stable if assets move in the project.
            /// </summary>
            GUID,

            /// <summary>
            /// This method attempts to use the smallest identifier for internal asset ids.  For asset bundles with very few items, this can save a significant amount of space in the content catalog.
            /// </summary>
            Dynamic
        }

        [SerializeField]
        bool m_IncludeAddressInCatalog = true;

        [SerializeField]
        bool m_IncludeGUIDInCatalog = true;

        [SerializeField]
        bool m_IncludeLabelsInCatalog = true;

        [SerializeField]
        bool m_IncludeFolderKeysInCatalog = true;

        [SerializeField]
        bool m_IncludeAddressesForFolderChildren = true;

        [SerializeField]
        bool m_IncludeInEditorAssetDatabaseMode = true;

        /// <summary>
        /// If enabled, addresses are included in the content catalog.  This is required if assets are to be loaded via their main address.
        /// </summary>
        public bool IncludeAddressInCatalog
        {
            get => m_IncludeAddressInCatalog;
            set
            {
                if (m_IncludeAddressInCatalog != value)
                {
                    m_IncludeAddressInCatalog = value;
                    SetDirty(true);
                }
            }
        }

        /// <summary>
        /// If enabled, guids are included in content catalogs.  This is required if assets are to be loaded via AssetReference.
        /// </summary>
        public bool IncludeGUIDInCatalog
        {
            get => m_IncludeGUIDInCatalog;
            set
            {
                if (m_IncludeGUIDInCatalog != value)
                {
                    m_IncludeGUIDInCatalog = value;
                    SetDirty(true);
                }
            }
        }

        /// <summary>
        /// If enabled, labels are included in the content catalogs.  This is required if labels are used at runtime load load assets.
        /// </summary>
        public bool IncludeLabelsInCatalog
        {
            get => m_IncludeLabelsInCatalog;
            set
            {
                if (m_IncludeLabelsInCatalog != value)
                {
                    m_IncludeLabelsInCatalog = value;
                    SetDirty(true);
                }
            }
        }

        /// <summary>
        /// If enabled, the group's assets will be included while using Fast Mode during play.
        /// </summary>
        public bool IncludeInEditorAssetDatabaseMode
        {
            get => m_IncludeInEditorAssetDatabaseMode;
            set
            {
                if (m_IncludeInEditorAssetDatabaseMode != value)
                {
                    m_IncludeInEditorAssetDatabaseMode = value;
                    SetDirty(true);
                }
            }
        }

        /// <summary>
        /// If enabled, each addressable folder's own address is included as an extra shared key on
        /// every asset within that folder.  This allows loading every asset in an addressable folder
        /// with a single call, for example Addressables.LoadAssetsAsync(folderAddress, ...), similar to
        /// Resources.LoadAll.  This is useful for reducing the size of the catalog if whole-folder
        /// loading is not needed.
        /// </summary>
        public bool IncludeFolderKeysInCatalog
        {
            get => m_IncludeFolderKeysInCatalog;
            set
            {
                if (m_IncludeFolderKeysInCatalog != value)
                {
                    m_IncludeFolderKeysInCatalog = value;
                    SetDirty(true);
                }
            }
        }

        /// <summary>
        /// If disabled, assets inside an addressable folder do not get their own individual address
        /// added to the catalog -- only the folder's shared key (see IncludeFolderKeysInCatalog) is
        /// added. GUIDs are unaffected, so AssetReferences into folder assets keep working; the GUID
        /// becomes that asset's primary key instead of its address. Disable this if you always load
        /// these assets via the folder and never reference an individual asset by its own full address,
        /// to reduce the size of the catalog. Only takes effect when IncludeFolderKeysInCatalog is enabled.
        /// </summary>
        public bool IncludeAddressesForFolderChildren
        {
            get => m_IncludeAddressesForFolderChildren;
            set
            {
                if (m_IncludeAddressesForFolderChildren != value)
                {
                    m_IncludeAddressesForFolderChildren = value;
                    SetDirty(true);
                }
            }
        }

        /// <summary>
        /// Internal Id mode for assets in bundles.
        /// </summary>
        public AssetNamingMode InternalIdNamingMode
        {
            get => m_InternalIdNamingMode;
            set
            {
                m_InternalIdNamingMode = value;
                SetDirty(true);
            }
        }

        [SerializeField]
        [Tooltip("Indicates how the internal asset name will be generated.")]
        AssetNamingMode m_InternalIdNamingMode = AssetNamingMode.FullPath;


        /// <summary>
        /// Behavior for clearing old bundles from the cache.
        /// </summary>
        public enum CacheClearBehavior
        {
            /// <summary>
            /// Bundles are only removed from the cache when space is needed.
            /// </summary>
            ClearWhenSpaceIsNeededInCache,

            /// <summary>
            /// Bundles are removed from the cache when a newer version has been loaded successfully.
            /// </summary>
            ClearWhenWhenNewVersionLoaded,
        }

        [SerializeField]
        CacheClearBehavior m_CacheClearBehavior = CacheClearBehavior.ClearWhenSpaceIsNeededInCache;

        /// <summary>
        /// Determines how other cached versions of asset bundles are cleared.
        /// </summary>
        public CacheClearBehavior AssetBundledCacheClearBehavior
        {
            get
            {
                if (UseDefaultSchemaSettings)
                    return GetDefaultSchemaSettings().assetBundledCacheClearBehavior;
                return m_CacheClearBehavior;
            }
            set
            {
                if (m_CacheClearBehavior != value)
                {
                    m_CacheClearBehavior = value;
                    SetDirty(true);
                }
            }
        }


        /// <summary>
        /// Gets the build compression settings for bundles in this group.
        /// </summary>
        /// <param name="bundleId">The bundle id.</param>
        /// <returns>The build compression.</returns>
        public virtual BuildCompression GetBuildCompressionForBundle(string bundleId)
        {
            //Unfortunately the BuildCompression struct is not serializable (nor is it settable), therefore this enum needs to be used to return the static members....
            switch (m_Compression)
            {
                case BundleCompressionMode.Uncompressed:
                    return BuildCompression.Uncompressed;
                case BundleCompressionMode.LZ4:
                    return BuildCompression.LZ4;
                case BundleCompressionMode.LZMA:
                    return BuildCompression.LZMA;
            }

            return default(BuildCompression);
        }

        // Retained (serialized) only so a project's previously-stored per-schema value can be migrated up to the
        // group on load. The IncludeInBuild property below no longer reads this field; it forwards to the group.
        [FormerlySerializedAs("m_includeInBuild")]
        [SerializeField]
        bool m_IncludeInBuild = true;

        internal override bool? GetDeprecatedIncludeInBuild() => m_IncludeInBuild;

        /// <summary>
        /// If true, the assets in this group will be included in the build of bundles.
        /// </summary>
        /// <remarks>
        /// Include in Build is stored on the owning <see cref="AddressableAssetGroup"/>. This property forwards to
        /// <see cref="AddressableAssetGroup.IncludeInBuild"/> so the group remains the single source of truth.
        /// </remarks>
        public bool IncludeInBuild
        {
            get => Group == null || Group.IncludeInBuild;
            set
            {
                if (Group != null)
                    Group.IncludeInBuild = value;
            }
        }

        [SerializeField]
        [SerializedTypeRestriction(type = typeof(IResourceProvider))]
        [Tooltip("The provider type to use for loading assets from bundles.")]
        SerializedType m_BundledAssetProviderType;

        /// <summary>
        /// The provider type to use for loading assets from bundles.
        /// </summary>
        public SerializedType BundledAssetProviderType
        {
            get => m_BundledAssetProviderType;
            set
            {
                m_BundledAssetProviderType = value;
                SetDirty(true);
            }
        }
        [SerializeField]
        [Tooltip("If true, assetbundle download data will be stripped from the catalog.  This should only be enabled for local groups.  Only applies to binary catalogs.")]
        bool m_StripDownloadOptions = false;
        /// <summary>
        /// Strip unnecessary assetbundle download data from the catalog.  This should only be enabled for local groups.  Only applies to binary catalogs.
        /// </summary>
        public bool StripDownloadOptions
        {
            get
            {
                if (UseDefaultSchemaSettings)
                    return GetDefaultSchemaSettings().stripDownloadOptions;
                return m_StripDownloadOptions;
            }
            set
            {
                if (m_StripDownloadOptions != value)
                {
                    m_StripDownloadOptions = value;
                    SetDirty(true);
                }
            }
        }

        [SerializeField]
        [Tooltip("If true, the bundle and asset provider for assets in this group will get unique provider ids and will only provide for assets in this group.")]
        bool m_ForceUniqueProvider = false;

        /// <summary>
        /// If true, the bundle and asset provider for assets in this group will get unique provider ids and will only provide for assets in this group.
        /// </summary>
        public bool ForceUniqueProvider
        {
            get => m_ForceUniqueProvider;
            set
            {
                if (m_ForceUniqueProvider != value)
                {
                    m_ForceUniqueProvider = value;
                    SetDirty(true);
                }
            }
        }

        [FormerlySerializedAs("m_useAssetBundleCache")]
        [SerializeField]
        [Tooltip("If true, the Hash value of the asset bundle is used to determine if a bundle can be loaded from the local cache instead of downloaded. (Only applies to remote asset bundles)")]
        bool m_UseAssetBundleCache = true;

        /// <summary>
        /// If true, the CRC and Hash values of the asset bundle are used to determine if a bundle can be loaded from the local cache instead of downloaded.
        /// </summary>
        public bool UseAssetBundleCache
        {
            get
            {
                if (UseDefaultSchemaSettings)
                    return GetDefaultSchemaSettings().useAssetBundleCache;
                return m_UseAssetBundleCache;
            }
            set
            {
                if (m_UseAssetBundleCache != value)
                {
                    m_UseAssetBundleCache = value;
                    SetDirty(true);
                }
            }
        }

        /// <summary>
        /// Determines whether a given schema will be included in a Schema Driven build. This is particularly useful
        /// if you want to alternate between building AssetBundles and ContentDirectories.
        /// Only one buildable schema can be enabled on a group at a time. If you attempt to enable multiple at once, an error will be thrown.
        /// </summary>
        public override bool IsEnabled
        {
            get => m_SchemaIsEnabled;
            set
            {
                if (m_SchemaIsEnabled != value)
                {
                    if (value)
                    {
                        string warningString = CanEnableSchema();
                        if (!String.IsNullOrEmpty(warningString))
                            Debug.LogError(warningString);
                        // Allow the set even when another buildable schema is enabled so the user can enable both;
                        // the group inspector shows an error and logs when both are enabled.
                    }

                    m_SchemaIsEnabled = value;
                    SetDirty(true);
                }
            }
        }

        /// <summary>
        /// Determines whether the BundledAssetGroupSchema can be enabled or not.
        /// A BundledAssetGroupSchema can be enabled if there are no other buildable schemas (such as a Content Directory Schema) enabled.
        /// Used e.g. when adding a schema via Add Schema so the new schema is defaulted to disabled when the other is already enabled.
        /// The user can still manually enable both in the inspector; the group inspector then shows an error.
        /// </summary>
        /// <returns>Returns an empty string if enabling is valid, or an error/warning string if another buildable schema is already enabled.</returns>
        public override string CanEnableSchema()
        {
            foreach (var schema in Group.Schemas)
            {
                if (schema != this && schema is ContentDirectoryGroupSchema cdgs && cdgs.IsEnabled)
                    return AddressablesGUIUtility.CanEnableSchemaError(Group.Name, this.GetType(), schema.GetType());
            }
            return "";
        }


        [SerializeField]
        [Tooltip("If true, the CRC (Cyclic Redundancy Check) of the asset bundle is used to check the integrity.  This can be used for both local and remote bundles.")]
        internal bool m_UseAssetBundleCrc = true;

        /// <summary>
        /// If true, the CRC and Hash values of the asset bundle are used to determine if a bundle can be loaded from the local cache instead of downloaded.
        /// </summary>
        public bool UseAssetBundleCrc
        {
            get
            {
                if (UseDefaultSchemaSettings)
                    return GetDefaultSchemaSettings().useAssetBundleCrc;
                return m_UseAssetBundleCrc;
            }
            set
            {
                if (m_UseAssetBundleCrc != value)
                {
                    m_UseAssetBundleCrc = value;
                    SetDirty(true);
                }

                if (!value)
                    UseAssetBundleCrcForCachedBundles = false;
            }
        }

        [SerializeField]
        [Tooltip("If true, the CRC (Cyclic Redundancy Check) of the asset bundle is used to check the integrity.")]
        internal bool m_UseAssetBundleCrcForCachedBundles = true;

        /// <summary>
        /// If true, the CRC and Hash values of the asset bundle are used to determine if a bundle can be loaded from the local cache instead of downloaded.
        /// </summary>
        public bool UseAssetBundleCrcForCachedBundles
        {
            get
            {
                if (UseDefaultSchemaSettings)
                    return GetDefaultSchemaSettings().useAssetBundleCrcForCachedBundles;
                return m_UseAssetBundleCrcForCachedBundles;
            }
            set
            {
                // UUM-140558: cached-bundle CRC is a sub-behavior of CRC, so enabling it
                // enables CRC too. This keeps the pair order-independent for scripted
                // callers and unreachable in the stale (CRC off, cached on) state.
                if (value && !m_UseAssetBundleCrc)
                    UseAssetBundleCrc = true;

                if (m_UseAssetBundleCrcForCachedBundles != value)
                {
                    m_UseAssetBundleCrcForCachedBundles = value;
                    SetDirty(true);
                }
            }
        }

        [SerializeField]
        [Tooltip("If true, local asset bundles will be loaded through UnityWebRequest.")]
        bool m_UseUWRForLocalBundles = false;

        /// <summary>
        /// If true, local asset bundles will be loaded through UnityWebRequest.
        /// </summary>
        public bool UseUnityWebRequestForLocalBundles
        {
            get => m_UseUWRForLocalBundles;
            set
            {
                if (m_UseUWRForLocalBundles != value)
                {
                    m_UseUWRForLocalBundles = value;
                    SetDirty(true);
                }
            }
        }

        [FormerlySerializedAs("m_timeout")]
        [SerializeField]
        [Tooltip("Attempt to abort after the number of seconds in timeout have passed, where the UnityWebRequest has received no data. (Only applies to remote asset bundles)")]
        [Min(0)]
        int m_Timeout;

        /// <summary>
        /// Attempt to abort after the number of seconds in timeout have passed, where the UnityWebRequest has received no data.
        /// Use 0 for no timeout
        /// </summary>
        public int Timeout
        {
            get => m_Timeout;
            set
            {
                if (value < 0)
                    value = 0;
                if (value > short.MaxValue)
                    value = short.MaxValue;
                if (m_Timeout != value)
                {
                    m_Timeout = value;
                    SetDirty(true);
                }
            }
        }

        [FormerlySerializedAs("m_chunkedTransfer")]
        [SerializeField]
        [Tooltip("Deprecated in 2019.3+. Indicates whether the UnityWebRequest system should employ the HTTP/1.1 chunked-transfer encoding method. (Only applies to remote asset bundles)")]
        bool m_ChunkedTransfer;

        /// <summary>
        /// Indicates whether the UnityWebRequest system should employ the HTTP/1.1 chunked-transfer encoding method.
        /// </summary>
        public bool ChunkedTransfer
        {
            get => m_ChunkedTransfer;
            set
            {
                if (m_ChunkedTransfer != value)
                {
                    m_ChunkedTransfer = value;
                    SetDirty(true);
                }
            }
        }


        [FormerlySerializedAs("m_redirectLimit")]
        [SerializeField]
        [Tooltip("Indicates the number of redirects which this UnityWebRequest will follow before halting with a “Redirect Limit Exceeded” system error. (Only applies to remote asset bundles)")]
        [Range(-1, 128)]
        int m_RedirectLimit = -1;

        /// <summary>
        /// Indicates the number of redirects which this UnityWebRequest will follow before halting with a “Redirect Limit Exceeded” system error.
        /// </summary>
        public int RedirectLimit
        {
            get => m_RedirectLimit;
            set
            {
                if (value < -1)
                    value = -1;
                if (value > 128)
                    value = 128;
                if (m_RedirectLimit != value)
                {
                    m_RedirectLimit = value;
                    SetDirty(true);
                }
            }
        }

        [FormerlySerializedAs("m_retryCount")]
        [SerializeField]
        [Tooltip("Indicates the number of times the request will be retried.")]
        [Range(0, 128)]
        int m_RetryCount = 0;

        /// <summary>
        /// Indicates the number of times the request will be retried.
        /// </summary>
        public int RetryCount
        {
            get => m_RetryCount;
            set
            {
                if (value < 0)
                    value = 0;
                if (value > 128)
                    value = 128;
                if (m_RetryCount != value)
                {
                    m_RetryCount = value;
                    SetDirty(true);
                }
            }
        }

        [FormerlySerializedAs("m_buildPath")]
        [SerializeField]
        [Tooltip("The path to copy asset bundles to.")]
        internal ProfileValueReference m_BuildPath = new ProfileValueReference();

        /// <summary>
        /// The path to copy asset bundles to.
        /// </summary>
        public ProfileValueReference BuildPath
        {
            get { return m_BuildPath; }
        }

        [FormerlySerializedAs("m_loadPath")]
        [SerializeField]
        [Tooltip("The path to load bundles from.")]
        internal ProfileValueReference m_LoadPath = new ProfileValueReference();

        /// <summary>
        /// The path to load bundles from.
        /// </summary>
        public ProfileValueReference LoadPath
        {
            get { return m_LoadPath; }
        }

        //placeholder for UrlSuffix support...
        internal string UrlSuffix
        {
            get { return string.Empty; }
        }

        [FormerlySerializedAs("m_bundleMode")]
        [SerializeField]
        [Tooltip(
            "Controls how bundles are packed.  If set to PackTogether, a single asset bundle will be created for the entire group, with the exception of scenes, which are packed in a second bundle.  If set to PackSeparately, an asset bundle will be created for each entry in the group; in the case that an entry is a folder, one bundle is created for the folder and all of its sub entries.")]
        BundlePackingMode m_BundleMode = BundlePackingMode.PackTogether;

        /// <summary>
        /// Controls how bundles are packed.  If set to PackTogether, a single asset bundle will be created for the entire group, with the exception of scenes, which are packed in a second bundle.  If set to PackSeparately, an asset bundle will be created for each entry in the group; in the case that an entry is a folder, one bundle is created for the folder and all of its sub entries.
        /// </summary>
        public BundlePackingMode BundleMode
        {
            get => m_BundleMode;
            set
            {
                if (m_BundleMode != value)
                {
                    m_BundleMode = value;
                    SetDirty(true);
                }
            }
        }

        [FormerlySerializedAs("m_assetBundleProviderType")]
        [SerializeField]
        [SerializedTypeRestriction(type = typeof(IResourceProvider))]
        [Tooltip("The provider type to use for loading asset bundles.")]
        SerializedType m_AssetBundleProviderType;

        /// <summary>
        /// The provider type to use for loading asset bundles.
        /// </summary>
        public SerializedType AssetBundleProviderType
        {
            get => m_AssetBundleProviderType;
            set
            {
                m_AssetBundleProviderType = value;
                SetDirty(true);
            }
        }

        [SerializeField]
        bool m_UseDefaultSchemaSettings;
        /// <summary>
        /// Determines if user wants to override the default schema settings.
        /// </summary>
        public bool UseDefaultSchemaSettings
        {
            get
            {
                return m_UseDefaultSchemaSettings;
            }
            set
            {
                if (m_UseDefaultSchemaSettings != value)
                {
                    m_UseDefaultSchemaSettings = value;
                    SetDirty(true);
                }
            }
        }

        [SerializeField]
        int m_SelectedPathPairIndex;
        /// <summary>
        /// The selected path pair in use.
        /// Use this with care, as it could change when path pairs are added ore removed. It is generally more
        /// valid to lookup the path pair by Id for the current profile.
        /// </summary>
        public int SelectedPathPairIndex
        {
            get => m_SelectedPathPairIndex;
            set
            {
                if (m_SelectedPathPairIndex != value)
                {
                    m_SelectedPathPairIndex = value;
                    if (m_SelectedPathPairIndex < 0)
                        m_SelectedPathPairIndex = 0;
                    SetDirty(true);
                }
            }
        }

        /// <summary>
        /// Used to determine if dropdown should be custom
        /// </summary>
        internal bool m_UseCustomPaths = false;

        /// <summary>
        /// Internal settings
        /// </summary>
        internal AddressableAssetSettings settings
        {
            get { return AddressableAssetSettingsDefaultObject.Settings; }
        }

        private GUIContent m_BuildAndLoadPathsGUIContent = new GUIContent("Build & Load Paths", "Paths to build or load AssetBundles from");
        private GUIContent m_PathsPreviewGUIContent = new GUIContent("Path Preview", "Preview of what the current paths will be evaluated to");
        /// <summary>
        /// Set default values taken from the assigned group.
        /// </summary>
        /// <param name="group">The group this schema has been added to.</param>
        protected override void OnSetGroup(AddressableAssetGroup group)
        {
            //this can happen during the load of the addressables asset
        }

        internal override void Validate()
        {
            if (Group != null && Group.Settings != null)
            {
                List<string> variableNames = Group.Settings.profileSettings.GetVariableNames();
                SetPathVariable(Group.Settings, ref m_BuildPath, AddressableAssetSettings.kLocalBuildPath, "LocalBuildPath", variableNames);
                SetPathVariable(Group.Settings, ref m_LoadPath, AddressableAssetSettings.kLocalLoadPath, "LocalLoadPath", variableNames);
            }

            if (m_AssetBundleProviderType.Value == null)
                m_AssetBundleProviderType.Value = typeof(AssetBundleProvider);
            if (m_BundledAssetProviderType.Value == null)
                m_BundledAssetProviderType.Value = typeof(BundledAssetProvider);
        }

        internal string GetAssetLoadPath(string assetPath, HashSet<string> otherLoadPaths, Func<string, string> pathToGUIDFunc, bool isScene)
        {
            switch (InternalIdNamingMode)
            {
                case AssetNamingMode.FullPath:
                    return assetPath;
                case AssetNamingMode.Filename:
                    return isScene ? System.IO.Path.GetFileNameWithoutExtension(assetPath) : System.IO.Path.GetFileName(assetPath);
                case AssetNamingMode.GUID:
                    return pathToGUIDFunc(assetPath);
                case AssetNamingMode.Dynamic:
                    {
                        var g = pathToGUIDFunc(assetPath);
                        if (isScene || otherLoadPaths == null)
                            return g;
                        var len = 1;
                        var p = g.Substring(0, len);
                        while (otherLoadPaths.Contains(p))
                            p = g.Substring(0, ++len);
                        otherLoadPaths.Add(p);
                        return p;
                    }
            }

            return assetPath;
        }

        /// <summary>
        /// Implementation of ISerializationCallbackReceiver. Does nothing.
        /// </summary>
        public void OnBeforeSerialize()
        {
        }

        /// <summary>
        /// Impementation of ISerializationCallbackReceiver. Used to set callbacks for ProfileValueReference changes.
        /// </summary>
        public void OnAfterDeserialize()
        {
            BuildPath.OnValueChanged -= OnPathValueChanged;
            BuildPath.OnValueChanged += OnPathValueChanged;
            LoadPath.OnValueChanged -= OnPathValueChanged;
            LoadPath.OnValueChanged += OnPathValueChanged;
            if (m_AssetBundleProviderType.Value == null)
                m_AssetBundleProviderType.Value = typeof(AssetBundleProvider);
            if (m_BundledAssetProviderType.Value == null)
                m_BundledAssetProviderType.Value = typeof(BundledAssetProvider);
        }

        void OnPathValueChanged(ProfileValueReference _)
        {
            SetDirty(true);
        }

        /// <summary>
        /// Returns the id of the asset provider needed to load from this group.
        /// </summary>
        /// <returns>The id of the cached provider needed for this group.</returns>
        public string GetAssetCachedProviderId()
        {
            return ForceUniqueProvider ? string.Format("{0}_{1}", BundledAssetProviderType.Value.FullName, Group.Guid) : BundledAssetProviderType.Value.FullName;
        }

        /// <summary>
        /// Returns the id of the bundle provider needed to load from this group.
        /// </summary>
        /// <returns>The id of the cached provider needed for this group.</returns>
        public string GetBundleCachedProviderId()
        {
            return ForceUniqueProvider ? string.Format("{0}_{1}", AssetBundleProviderType.Value.FullName, Group.Guid) : AssetBundleProviderType.Value.FullName;
        }

        /// <summary>
        /// Used to determine how the final bundle name should look.
        /// </summary>
        public enum BundleNamingStyle
        {
            /// <summary>
            /// Use to indicate that the hash should be appended to the bundle name.
            /// </summary>
            AppendHash,

            /// <summary>
            /// Use to indicate that the bundle name should not contain the hash.
            /// </summary>
            NoHash,

            /// <summary>
            /// Use to indicate that the bundle name should only contain the given hash.
            /// </summary>
            OnlyHash,

            /// <summary>
            /// Use to indicate that the bundle name should only contain the hash of the file name.
            /// </summary>
            FileNameHash
        }

        /// <summary>
        /// Used to draw the Bundle Naming popup
        /// </summary>
        [CustomPropertyDrawer(typeof(BundleNamingStyle))]
        class BundleNamingStylePropertyDrawer : PropertyDrawer
        {
            /// <summary>
            /// Custom Drawer for the BundleNamingStyle in order to display easier to understand display names.
            /// </summary>
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                DrawGUI(position, property, label);
            }

            internal static int DrawGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                bool showMixedValue = EditorGUI.showMixedValue;
                EditorGUI.BeginProperty(position, label, property);
                EditorGUI.showMixedValue = showMixedValue;

                GUIContent[] contents = new GUIContent[4];
                contents[0] = new GUIContent("Filename", "Leave filename unchanged.");
                contents[1] = new GUIContent("Append Hash to Filename", "Append filename with the AssetBundle content hash.");
                contents[2] = new GUIContent("Use Hash of AssetBundle", "Replace filename with AssetBundle hash.");
                contents[3] = new GUIContent("Use Hash of Filename", "Replace filename with hash of filename.");

                int enumValue = property.enumValueIndex;
                enumValue = enumValue == 0 ? 1 : enumValue == 1 ? 0 : enumValue;

                EditorGUI.BeginChangeCheck();
                int newValue = EditorGUI.Popup(position, new GUIContent(label.text, label.tooltip), enumValue, contents);
                if (EditorGUI.EndChangeCheck())
                {
                    newValue = newValue == 0 ? 1 : newValue == 1 ? 0 : newValue;
                    property.enumValueIndex = newValue;
                }

                EditorGUI.EndProperty();
                return newValue;
            }
        }

        [SerializeField]
        BundleNamingStyle m_BundleNaming;

        /// <summary>
        /// Naming style to use for generated AssetBundle(s).
        /// </summary>
        public BundleNamingStyle BundleNaming
        {
            get
            {
                if (UseDefaultSchemaSettings)
                    return GetDefaultSchemaSettings().bundleNaming;
                return m_BundleNaming;
            }
            set
            {
                if (m_BundleNaming != value)
                {
                    m_BundleNaming = value;
                    SetDirty(true);
                }
            }
        }

        [SerializeField]
        AssetLoadMode m_AssetLoadMode;

        /// <summary>
        /// Will load all Assets into memory from the AssetBundle after the AssetBundle is loaded.
        /// </summary>
        public AssetLoadMode AssetLoadMode
        {
            get => m_AssetLoadMode;
            set
            {
                if (m_AssetLoadMode != value)
                {
                    m_AssetLoadMode = value;
                    SetDirty(true);
                }
            }
        }

        private bool m_ShowPaths = true;
        private static GUIStyle s_SmallHelpBoxStyle;

        void DrawContentDirectoryPromotionHelpBox()
        {
            if (s_SmallHelpBoxStyle == null)
            {
                s_SmallHelpBoxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    fontSize = 10
                };
            }

            var content = new GUIContent(
                "For the most up to date way of managing local content, use the Content Directory schema. " +
                "To enable this, add the Content Directory schema to your group and disable this schema.",
                EditorGUIUtility.IconContent("console.infoicon.sml").image);

            EditorGUILayout.LabelField(content, s_SmallHelpBoxStyle);
        }

        /// <summary>
        /// Used for drawing properties in the inspector.
        /// </summary>
        public override void ShowAllProperties()
        {
            m_ShowPaths = true;
            AdvancedOptionsFoldout.IsActive = true;
        }

        /// <inheritdoc/>
        public override void OnGUI()
        {
            EditorGUI.BeginDisabledGroup(!IsEnabled);

            // Show helpbox when Local paths are selected to promote Content Directory schema
            if (Group != null && Group.Settings != null && !m_UseCustomPaths)
            {
                var loadPathName = m_LoadPath.GetName(Group.Settings);
                if (loadPathName == AddressableAssetSettings.kLocalLoadPath)
                {
                    DrawContentDirectoryPromotionHelpBox();
                    GUILayout.Space(k_HelpBoxUIPadding);
                }
            }

            BuildAndLoadPathUIHelper.DrawPathPair(this, SchemaSerializedObject,
                ref m_BuildPath, ref m_LoadPath, ref m_UseCustomPaths, ref m_ShowPaths,
                ref m_SelectedPathPairIndex);

            AdvancedOptionsFoldout.IsActive = GUI.AddressablesGUIUtility.BeginFoldoutHeaderGroupWithHelp(AdvancedOptionsFoldout.IsActive, new GUIContent("Advanced Options"), () =>
            {
                string url = AddressableAssetUtility.GenerateDocsURL("group-inspector-settings-reference.html#advanced-options");
                Application.OpenURL(url);
            }, 10);
            if (AdvancedOptionsFoldout.IsActive)
                ShowAdvancedProperties(SchemaSerializedObject);
            SchemaSerializedObject.ApplyModifiedProperties();
            EditorGUI.EndDisabledGroup();
        }

        /// <inheritdoc/>
        public override void OnGUIMultiple(List<AddressableAssetGroupSchema> otherSchemas)
        {
            List<Action<BundledAssetGroupSchema, BundledAssetGroupSchema>> queuedChanges = null;

            List<BundledAssetGroupSchema> otherBundledSchemas = new List<BundledAssetGroupSchema>();
            foreach (var otherSchema in otherSchemas)
            {
                if (otherSchema is BundledAssetGroupSchema otherBundledSchema)
                    otherBundledSchemas.Add(otherBundledSchema);
            }

            EditorGUI.BeginDisabledGroup(!IsEnabled);

            // Show helpbox when Local paths are selected to promote Content Directory schema
            if (Group != null && Group.Settings != null && !m_UseCustomPaths)
            {
                var loadPathName = m_LoadPath.GetName(Group.Settings);
                if (loadPathName == AddressableAssetSettings.kLocalLoadPath)
                {
                    DrawContentDirectoryPromotionHelpBox();
                    GUILayout.Space(k_HelpBoxUIPadding);
                }
            }

            foreach (var schema in otherBundledSchemas)
                schema.m_ShowPaths = m_ShowPaths;

            bool pathPairModified = BuildAndLoadPathUIHelper.DrawPathPairMulti(this, SchemaSerializedObject, otherSchemas,
                ref m_BuildPath, ref m_LoadPath, ref m_UseCustomPaths, ref m_ShowPaths,
                ref m_SelectedPathPairIndex);

            if (pathPairModified)
            {
                Undo.SetCurrentGroupName("BundledAssetGroupSchemas BuildAndLoad Undos");
                foreach (var schema in otherBundledSchemas)
                {
                    Undo.RecordObject(schema, "BundledAssetGroupSchema BuildAndLoad" + schema.name);
                    SetPathPairOption(this, schema);
                }
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }

            if (otherBundledSchemas.Count > 0)
            {
                EditorGUI.BeginChangeCheck();
                AdvancedOptionsFoldout.IsActive = GUI.AddressablesGUIUtility.BeginFoldoutHeaderGroupWithHelp(AdvancedOptionsFoldout.IsActive, new GUIContent("Advanced Options"), () =>
                {
                    string url = AddressableAssetUtility.GenerateDocsURL("group-inspector-settings-reference.html#advanced-options");
                    Application.OpenURL(url);
                }, 10);
                if (AdvancedOptionsFoldout.IsActive)
                {
                    ShowAdvancedPropertiesMulti(SchemaSerializedObject, otherBundledSchemas, ref queuedChanges);
                }
                EditorGUI.EndFoldoutHeaderGroup();
            }

            SchemaSerializedObject.ApplyModifiedProperties();
            if (queuedChanges != null)
            {
                Undo.SetCurrentGroupName("BundledAssetGroupSchemasUndos");
                foreach (var schema in otherBundledSchemas)
                    Undo.RecordObject(schema, "BundledAssetGroupSchema" + schema.name);

                foreach (var change in queuedChanges)
                {
                    foreach (var schema in otherBundledSchemas)
                        change.Invoke(this, schema);
                }
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }
            EditorGUI.EndDisabledGroup();
        }

        static GUI.FoldoutSessionStateValue AdvancedOptionsFoldout = new GUI.FoldoutSessionStateValue("Addressables.BundledAssetGroup.AdvancedOptions");

        GUIContent m_IncludeInEditorFastModeContent = new GUIContent("Include in Asset Database Playmode", "If disabled, the assets in this group will not be included during Editor play mode when using the 'Use Asset Database (fastest)' playmode option. This is useful for quickly testing the exclusion of content in the Editor.");
        GUIContent m_StripDownloadOptionsContent = new GUIContent("Strip Bundle Download Options", "Strip unused asset bundle download data from catalog.  This should only be enabled for local groups and is disabled if UnityWebRequests are enabled for local bundles.");
        GUIContent m_CompressionContent = new GUIContent("Asset Bundle Compression", "Compression method to use for asset bundles.");
        GUIContent m_UseAssetBundleCacheContent = new GUIContent("Use Asset Bundle Cache", "If enabled and supported, the device will cache  asset bundles.");
        GUIContent m_AssetBundleCrcContent = new GUIContent("Asset Bundle CRC", "Defines which Asset Bundles will have their CRC checked when loading to ensure correct content.");

        private GUIContent[] m_CrcPopupContent = new GUIContent[]
        {
            new GUIContent("Disabled", "Bundles will not have their CRC checked when loading."),
            new GUIContent("Enabled, Including Cached", "All Bundles will have their CRC checked when loading."),
            new GUIContent("Enabled, Excluding Cached", "Bundles that have already been downloaded and cached will not have their CRC check when loading, otherwise CRC check will be performed.")
        };

        GUIContent m_IncludeAddressInCatalogContent = new GUIContent("Include Addresses in Catalog",
            "If disabled, addresses from this group will not be included in the catalog.  This is useful for reducing the size of the catalog if addresses are not needed.");

        GUIContent m_IncludeGUIDInCatalogContent = new GUIContent("Include GUIDs in Catalog",
            "If disabled, guids from this group will not be included in the catalog.  This is useful for reducing the size of the catalog if guids are not needed.");

        GUIContent m_IncludeLabelsInCatalogContent = new GUIContent("Include Labels in Catalog",
            "If disabled, labels from this group will not be included in the catalog.  This is useful for reducing the size of the catalog if labels are not needed.");

        GUIContent m_IncludeFolderKeysInCatalogContent = new GUIContent("Include Folder Keys in Catalog",
            "If enabled, each addressable folder's address is included as a shared key on every asset in that folder, so the folder's address can be used to load every asset inside it in one call.  If disabled, this is useful for reducing the size of the catalog if whole-folder loading is not needed.");

        GUIContent m_IncludeAddressesForFolderChildrenContent = new GUIContent("Include Individual Addresses for Folder Assets",
            "If disabled, assets inside an addressable folder will not have their own individual address included in the catalog -- only the folder's shared key will be included.  GUIDs are unaffected.  Disable this if you always load these assets via the folder to reduce the size of the catalog.");

        GUIContent m_CacheClearBehaviorContent = new GUIContent("Cache Clear Behavior", "Controls how old cached asset bundles are cleared.");
        GUIContent m_BundleNamingModeContent = new GUIContent("Bundle Naming Mode", "Controls the final file naming mode for bundles in this group.");
        GUIContent m_BundlePackModeContent = new GUIContent("Bundle Packing Mode", "Controls how content in a Group gets packed into AssetBundles.");

        private const string k_UseDefaultsLabel = "Use Defaults";
        GUIContent m_UseDefaultSettingsContent = new GUIContent(k_UseDefaultsLabel, $"Determines whether to use the default schema settings.");
        GUIContent m_UseDefaultSettingsContentDisabled = new GUIContent(k_UseDefaultsLabel, "This option is available when \"Build & Load Paths\" is set to \"Local\" or \"Remote\".");

        private float m_PostBlockContentSpace = 10;

        void ShowAdvancedProperties(SerializedObject so)
        {
            if (!m_UseCustomPaths)
            {
                var disableDefaultSchemaSettings = !HasDefaultSchemaSettings();
                EditorGUI.DisabledScope disableScope = new EditorGUI.DisabledScope(disableDefaultSchemaSettings);
                GUIContent toggleLabel = m_UseDefaultSettingsContent;
                if (disableDefaultSchemaSettings)
                    toggleLabel = m_UseDefaultSettingsContentDisabled;

                EditorGUI.BeginChangeCheck();
                bool useDefaultSettings = EditorGUILayout.Toggle(toggleLabel, m_UseDefaultSchemaSettings);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(so.targetObject, so.targetObject.name + nameof(UseDefaultSchemaSettings));
                    UseDefaultSchemaSettings = useDefaultSettings;
                }

                disableScope.Dispose();
            }
            using (new EditorGUI.DisabledScope(!m_UseCustomPaths && UseDefaultSchemaSettings))
            {
                EditorGUI.BeginChangeCheck();
                var compression = (BundleCompressionMode)EditorGUILayout.EnumPopup(m_CompressionContent, Compression);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(so.targetObject, so.targetObject.name + nameof(Compression));
                    Compression = compression;
                }

                bool buildTargetSupportsBundleCaching = BuildTargetSupportsBundleCaching(EditorUserBuildSettings.activeBuildTarget);
                if (buildTargetSupportsBundleCaching)
                {
                    EditorGUI.BeginChangeCheck();
                    bool useAssetBundleCache = EditorGUILayout.Toggle(m_UseAssetBundleCacheContent, UseAssetBundleCache);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(so.targetObject, so.targetObject.name + nameof(UseAssetBundleCache));
                        UseAssetBundleCache = useAssetBundleCache;
                    }

                    if (UseAssetBundleCache)
                    {
                        EditorGUI.BeginChangeCheck();
                        var cacheClearBehavior = (CacheClearBehavior)EditorGUILayout.EnumPopup(m_CacheClearBehaviorContent, AssetBundledCacheClearBehavior);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(so.targetObject, so.targetObject.name + nameof(AssetBundledCacheClearBehavior));
                            AssetBundledCacheClearBehavior = cacheClearBehavior;
                        }
                    }
                }
                CRCPropertyPopupField(so, buildTargetSupportsBundleCaching);

                EditorGUI.BeginChangeCheck();
                SerializedProperty serializedProperty = so.FindProperty(nameof(m_BundleNaming));
                Rect rect = EditorGUILayout.GetControlRect();
                var bundleNaming = (BundleNamingStyle)BundleNamingStylePropertyDrawer.DrawGUI(rect, serializedProperty, m_BundleNamingModeContent);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(so.targetObject, so.targetObject.name + nameof(BundleNaming));
                    BundleNaming = bundleNaming;
                }
                EditorGUI.BeginDisabledGroup(settings?.UseUnityWebRequestForLocalBundles ?? false);
                EditorGUI.BeginChangeCheck();
                bool stripDLOptions = EditorGUILayout.Toggle(m_StripDownloadOptionsContent, StripDownloadOptions);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(so.targetObject, so.targetObject.name + nameof(StripDownloadOptions));
                    StripDownloadOptions = stripDLOptions;
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.Space(m_PostBlockContentSpace);


            EditorGUILayout.PropertyField(so.FindProperty(nameof(m_IncludeInEditorAssetDatabaseMode)), m_IncludeInEditorFastModeContent, true);
            EditorGUILayout.PropertyField(so.FindProperty(nameof(m_IncludeAddressInCatalog)), m_IncludeAddressInCatalogContent, true);
            EditorGUILayout.PropertyField(so.FindProperty(nameof(m_IncludeGUIDInCatalog)), m_IncludeGUIDInCatalogContent, true);
            EditorGUILayout.PropertyField(so.FindProperty(nameof(m_IncludeLabelsInCatalog)), m_IncludeLabelsInCatalogContent, true);
            EditorGUILayout.PropertyField(so.FindProperty(nameof(m_IncludeFolderKeysInCatalog)), m_IncludeFolderKeysInCatalogContent, true);
            if (m_IncludeFolderKeysInCatalog)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(so.FindProperty(nameof(m_IncludeAddressesForFolderChildren)), m_IncludeAddressesForFolderChildrenContent, true);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(so.FindProperty(nameof(m_BundleMode)), m_BundlePackModeContent, true);
        }

        void CRCPropertyPopupField(SerializedObject so, bool buildTargetSupportsCaching)
        {
            if (buildTargetSupportsCaching)
            {
                int enumIndex = 0;
                if (UseAssetBundleCrc)
                    enumIndex = UseAssetBundleCrcForCachedBundles ? 1 : 2;

                int newEnumIndex = EditorGUILayout.Popup(m_AssetBundleCrcContent, enumIndex, m_CrcPopupContent);
                if (enumIndex != newEnumIndex)
                    SetCrcFromPopupIndex(newEnumIndex, so.targetObject);
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                bool useAssetbundlecrc = EditorGUILayout.Toggle(m_AssetBundleCrcContent, UseAssetBundleCrc);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(so.targetObject, so.targetObject.name + nameof(UseAssetBundleCrc));
                    UseAssetBundleCrc = useAssetbundlecrc;
                }
            }
        }

        /// <summary>
        /// Applies a CRC dropdown selection to the two CRC flags, recording undo.
        /// </summary>
        /// <param name="newEnumIndex">The newly selected popup index (0, 1, or 2).</param>
        /// <param name="undoTarget">The object to record for undo.</param>
        internal void SetCrcFromPopupIndex(int newEnumIndex, UnityEngine.Object undoTarget)
        {
            bool useCrc = newEnumIndex != 0;
            bool useCrcForCached = newEnumIndex == 1;

            if (UseAssetBundleCrc != useCrc)
            {
                Undo.RecordObject(undoTarget, undoTarget.name + nameof(UseAssetBundleCrc));
                UseAssetBundleCrc = useCrc;
            }
            if (UseAssetBundleCrcForCachedBundles != useCrcForCached)
            {
                Undo.RecordObject(undoTarget, undoTarget.name + nameof(UseAssetBundleCrcForCachedBundles));
                UseAssetBundleCrcForCachedBundles = useCrcForCached;
            }
        }

        void CRCPropertyPopupFieldMulti(SerializedObject so, bool buildTargetSupportsCaching, List<BundledAssetGroupSchema> otherBundledSchemas, ref List<Action<BundledAssetGroupSchema, BundledAssetGroupSchema>> queuedChanges)
        {
            if (buildTargetSupportsCaching)
            {
                ShowMixedValueAdvancedProperty(this, otherBundledSchemas, (a, b) => a.UseAssetBundleCrc != b.UseAssetBundleCrc);
                if (!EditorGUI.showMixedValue)
                    ShowMixedValueAdvancedProperty(this, otherBundledSchemas, (a, b) => a.UseAssetBundleCrcForCachedBundles != b.UseAssetBundleCrcForCachedBundles);

                EditorGUI.BeginChangeCheck();
                CRCPropertyPopupField(so, buildTargetSupportsCaching);
                if (EditorGUI.EndChangeCheck())
                {
                    AddQueuedChanges(ref queuedChanges,
                        (src, dst) =>
                        {
                            dst.UseAssetBundleCrc = src.UseAssetBundleCrc;
                            dst.UseAssetBundleCrcForCachedBundles = src.UseAssetBundleCrcForCachedBundles;
                        });
                    EditorUtility.SetDirty(this);
                }
                EditorGUI.showMixedValue = false;
            }
        }


        bool ShowMixedValueAdvancedProperty(BundledAssetGroupSchema schema, List<BundledAssetGroupSchema> otherBundledSchemas, Func<BundledAssetGroupSchema, BundledAssetGroupSchema, bool> showMixedValue)
        {
            foreach (BundledAssetGroupSchema bundledSchema in otherBundledSchemas)
            {
                if (showMixedValue.Invoke(schema, bundledSchema))
                {
                    EditorGUI.showMixedValue = true;
                    return true;
                }
            }
            return false;
        }

        void ShowAdvancedPropertiesMulti(SerializedObject so, List<BundledAssetGroupSchema> otherSchemas, ref List<Action<BundledAssetGroupSchema, BundledAssetGroupSchema>> queuedChanges)
        {
            ShowSelectedPropertyDefaultSettingsMulti(so, otherSchemas, ref queuedChanges);
            GUILayout.Space(m_PostBlockContentSpace);

            ShowSelectedPropertyMulti(so, nameof(m_IncludeInEditorAssetDatabaseMode), m_IncludeInEditorFastModeContent, otherSchemas, ref queuedChanges,
                (src, dst) => dst.IncludeInEditorAssetDatabaseMode = src.IncludeInEditorAssetDatabaseMode, ref m_IncludeInEditorAssetDatabaseMode);
            ShowSelectedPropertyMulti(so, nameof(m_IncludeAddressInCatalog), m_IncludeAddressInCatalogContent, otherSchemas, ref queuedChanges,
                (src, dst) => dst.IncludeAddressInCatalog = src.IncludeAddressInCatalog, ref m_IncludeAddressInCatalog);
            ShowSelectedPropertyMulti(so, nameof(m_IncludeGUIDInCatalog), m_IncludeGUIDInCatalogContent, otherSchemas, ref queuedChanges,
                (src, dst) => dst.IncludeGUIDInCatalog = src.IncludeGUIDInCatalog, ref m_IncludeGUIDInCatalog);
            ShowSelectedPropertyMulti(so, nameof(m_IncludeLabelsInCatalog), m_IncludeLabelsInCatalogContent, otherSchemas, ref queuedChanges,
                (src, dst) => dst.IncludeLabelsInCatalog = src.IncludeLabelsInCatalog, ref m_IncludeLabelsInCatalog);
            ShowSelectedPropertyMulti(so, nameof(m_IncludeFolderKeysInCatalog), m_IncludeFolderKeysInCatalogContent, otherSchemas, ref queuedChanges,
                (src, dst) => dst.IncludeFolderKeysInCatalog = src.IncludeFolderKeysInCatalog, ref m_IncludeFolderKeysInCatalog);
            if (m_IncludeFolderKeysInCatalog)
            {
                EditorGUI.indentLevel++;
                ShowSelectedPropertyMulti(so, nameof(m_IncludeAddressesForFolderChildren), m_IncludeAddressesForFolderChildrenContent, otherSchemas, ref queuedChanges,
                    (src, dst) => dst.IncludeAddressesForFolderChildren = src.IncludeAddressesForFolderChildren, ref m_IncludeAddressesForFolderChildren);
                EditorGUI.indentLevel--;
            }
            ShowSelectedPropertyMulti(so, nameof(m_BundleMode), m_BundlePackModeContent, otherSchemas, ref queuedChanges, (src, dst) => dst.BundleMode = src.BundleMode, ref m_BundleMode);
        }

        void ShowSelectedPropertyMulti<T>(SerializedObject so, string propertyName, GUIContent label, List<BundledAssetGroupSchema> otherSchemas,
            ref List<Action<BundledAssetGroupSchema, BundledAssetGroupSchema>> queuedChanges, Action<BundledAssetGroupSchema, BundledAssetGroupSchema> a, ref T propertyValue)
        {
            SerializedProperty serializedProperty = so.FindProperty(propertyName);
            Type propertySystemType = typeof(T);
            if (label == null)
                label = new GUIContent(serializedProperty.displayName);
            ShowMixedValue(serializedProperty, otherSchemas, propertySystemType, propertyName);

            T newValue = default(T);
            SerializedPropertyType serializedPropertyType = SerializedPropertyType.Generic;
            EditorGUI.BeginChangeCheck();
            if (propertySystemType == typeof(bool))
            {
                newValue = (T)(object)EditorGUILayout.Toggle(label, (bool)(object)propertyValue);
                serializedPropertyType = SerializedPropertyType.Boolean;
            }
            else if (propertySystemType.IsEnum)
            {
                serializedPropertyType = SerializedPropertyType.Enum;
                if (propertySystemType == typeof(BundleNamingStyle))
                {
                    Rect rect = EditorGUILayout.GetControlRect();
                    int enumValue = BundleNamingStylePropertyDrawer.DrawGUI(rect, serializedProperty, label);
                    newValue = (T)(object)enumValue;
                }
                else
                {
                    int enumValue = Convert.ToInt32(EditorGUILayout.EnumPopup(label, (Enum)(object)propertyValue));
                    newValue = (T)(object)enumValue;
                }
            }
            else if (propertySystemType == typeof(int))
            {
                newValue = (T)(object)EditorGUILayout.IntField(label, (int)(object)propertyValue);
                serializedPropertyType = SerializedPropertyType.Integer;
            }
            else
            {
                EditorGUILayout.PropertyField(serializedProperty, label, true);
                so.ApplyModifiedProperties();
            }
            if (EditorGUI.EndChangeCheck())
            {
                if (serializedPropertyType != SerializedPropertyType.Generic)
                {
                    HashSet<SerializedProperty> properties = new HashSet<SerializedProperty>() { serializedProperty };
                    foreach (AddressableAssetGroupSchema otherSchema in otherSchemas)
                        properties.Add(otherSchema.SchemaSerializedObject.FindProperty(propertyName));

                    foreach (SerializedProperty propertyForValueDestination in properties)
                    {
                        var destinationSerializedObject = propertyForValueDestination.serializedObject;
                        switch (serializedPropertyType)
                        {
                            case SerializedPropertyType.Boolean:
                                propertyForValueDestination.boolValue = (bool)(object)newValue;
                                break;
                            case SerializedPropertyType.Integer:
                                propertyForValueDestination.intValue = (int)(object)newValue;
                                break;
                            case SerializedPropertyType.Enum:
                                propertyForValueDestination.enumValueIndex = (int)(object)newValue;
                                break;
                        }

                        destinationSerializedObject.ApplyModifiedProperties();
                    }
                }
                else if (a != null)
                {
                    if (queuedChanges == null)
                        queuedChanges = new List<Action<BundledAssetGroupSchema, BundledAssetGroupSchema>>();
                    queuedChanges.Add(a);
                }
            }

            EditorGUI.showMixedValue = false;
        }

        internal int DetermineSelectedIndex(List<ProfileGroupType> groupTypes, int defaultValue, AddressableAssetSettings addressableAssetSettings, HashSet<string> vars)
        {
            return BuildAndLoadPathUIHelper.DetermineSelectedIndex(BuildPath, LoadPath, m_UseCustomPaths, groupTypes, defaultValue, addressableAssetSettings, vars);
        }

        void AddQueuedChanges(ref List<Action<BundledAssetGroupSchema, BundledAssetGroupSchema>> queuedChanges, Action<BundledAssetGroupSchema, BundledAssetGroupSchema> a)
        {
            if (queuedChanges == null)
                queuedChanges = new List<Action<BundledAssetGroupSchema, BundledAssetGroupSchema>>();
            queuedChanges.Add(a);
        }

        void ShowSelectedPropertyDefaultSettingsMulti(SerializedObject so, List<BundledAssetGroupSchema> otherBundledSchemas, ref List<Action<BundledAssetGroupSchema, BundledAssetGroupSchema>> queuedChanges)
        {
            bool selectedSchemaIsUsingCustomPaths = m_UseCustomPaths;
            bool selectedSchemaIsUsingDefaultSettings = UseDefaultSchemaSettings && !m_UseCustomPaths;
            foreach (BundledAssetGroupSchema otherSchema in otherBundledSchemas)
            {
                selectedSchemaIsUsingCustomPaths |= otherSchema.m_UseCustomPaths;
                selectedSchemaIsUsingDefaultSettings |= otherSchema.UseDefaultSchemaSettings && !otherSchema.m_UseCustomPaths;
            }

            if (!selectedSchemaIsUsingCustomPaths)
            {
                ShowMixedValueAdvancedProperty(this, otherBundledSchemas, (a, b) => a.UseDefaultSchemaSettings != b.UseDefaultSchemaSettings);
                EditorGUI.BeginChangeCheck();
                bool useDefaultSettings = EditorGUILayout.Toggle(m_UseDefaultSettingsContent, UseDefaultSchemaSettings);
                if (EditorGUI.EndChangeCheck())
                    AddQueuedChanges(ref queuedChanges, (src, dst) => src.UseDefaultSchemaSettings = dst.UseDefaultSchemaSettings = useDefaultSettings);
                EditorGUI.showMixedValue = false;
            }

            using (new EditorGUI.DisabledScope(selectedSchemaIsUsingDefaultSettings))
            {
                ShowMixedValueAdvancedProperty(this, otherBundledSchemas, (a, b) => a.Compression != b.Compression);
                EditorGUI.BeginChangeCheck();
                var compression = (BundleCompressionMode)EditorGUILayout.EnumPopup(m_CompressionContent, Compression);
                if (EditorGUI.EndChangeCheck())
                    AddQueuedChanges(ref queuedChanges, (src, dst) => src.Compression = dst.Compression = compression);
                EditorGUI.showMixedValue = false;

                bool buildTargetSupportsBundleCaching = BuildTargetSupportsBundleCaching(EditorUserBuildSettings.activeBuildTarget);
                if (buildTargetSupportsBundleCaching)
                {
                    bool showMixedValueUseCache = ShowMixedValueAdvancedProperty(this, otherBundledSchemas, (a, b) => a.UseAssetBundleCache != b.UseAssetBundleCache);
                    EditorGUI.BeginChangeCheck();
                    bool useAssetBundleCache = EditorGUILayout.Toggle(m_UseAssetBundleCacheContent, UseAssetBundleCache);
                    if (EditorGUI.EndChangeCheck())
                        AddQueuedChanges(ref queuedChanges, (src, dst) => src.UseAssetBundleCache = dst.UseAssetBundleCache = useAssetBundleCache);
                    EditorGUI.showMixedValue = false;

                    if (UseAssetBundleCache && !showMixedValueUseCache)
                    {
                        ShowMixedValueAdvancedProperty(this, otherBundledSchemas, (a, b) => a.AssetBundledCacheClearBehavior != b.AssetBundledCacheClearBehavior);
                        EditorGUI.BeginChangeCheck();
                        var cacheClearBehavior = (CacheClearBehavior)EditorGUILayout.EnumPopup(m_CacheClearBehaviorContent, AssetBundledCacheClearBehavior);
                        if (EditorGUI.EndChangeCheck())
                            AddQueuedChanges(ref queuedChanges, (src, dst) => src.AssetBundledCacheClearBehavior = dst.AssetBundledCacheClearBehavior = cacheClearBehavior);
                        EditorGUI.showMixedValue = false;
                    }
                }
                CRCPropertyPopupFieldMulti(so, buildTargetSupportsBundleCaching, otherBundledSchemas, ref queuedChanges);

                ShowMixedValueAdvancedProperty(this, otherBundledSchemas, (a, b) => a.BundleNaming != b.BundleNaming);
                EditorGUI.BeginChangeCheck();
                SerializedProperty serializedProperty = so.FindProperty(nameof(m_BundleNaming));
                Rect rect = EditorGUILayout.GetControlRect();
                var bundleNaming = (BundleNamingStyle)BundleNamingStylePropertyDrawer.DrawGUI(rect, serializedProperty, m_BundleNamingModeContent);
                if (EditorGUI.EndChangeCheck())
                    AddQueuedChanges(ref queuedChanges, (src, dst) => src.BundleNaming = dst.BundleNaming = bundleNaming);
                EditorGUI.showMixedValue = false;


                ShowMixedValueAdvancedProperty(this, otherBundledSchemas, (a, b) => a.StripDownloadOptions != b.StripDownloadOptions);
                EditorGUI.BeginChangeCheck();
                bool stripDLOptions = EditorGUILayout.Toggle(m_StripDownloadOptionsContent, StripDownloadOptions);
                if (EditorGUI.EndChangeCheck())
                    AddQueuedChanges(ref queuedChanges, (src, dst) => src.StripDownloadOptions = dst.StripDownloadOptions = stripDLOptions);
                EditorGUI.showMixedValue = false;

            }
        }

        /// <summary>
        /// A group of build target platforms that share the recommended schema settings.
        /// </summary>
        /// <remarks>
        /// For example, builds that target 32-bit and 64-bit Windows are under the same group and have the same recommended schema settings.
        /// </remarks>
        /// <example>
        /// <code source="../../../Tests/Editor/DocExampleCode/ScriptReference/UsingDefaultSchemaSettingsBuildTargetGroup.cs" region="SAMPLE"/>
        /// </example>
        public enum DefaultSchemaSettingsBuildTargetGroup
        {
            /// <summary>
            /// The default schema settings build target group.
            /// </summary>
            Default = 0,
            /// <summary>
            /// The Standalone schema settings build target group.
            /// </summary>
            StandaloneWindows = 1,
            /// <summary>
            /// The iOS schema settings build target group.
            /// </summary>
            iOS = BuildTargetGroup.iOS,
            /// <summary>
            /// The Android schema settings build target group.
            /// </summary>
            Android = BuildTargetGroup.Android,
            /// <summary>
            /// The WebGL schema settings build target group.
            /// </summary>
            WebGL = BuildTargetGroup.WebGL
        };

        private struct SchemaSettingsPair
        {
            public DefaultSchemaSettings Local;
            public DefaultSchemaSettings Remote;
        }

        /// <summary>
        /// A set of recommmended schema settings.
        /// </summary>
        public struct DefaultSchemaSettings
        {
            /// <summary>
            /// The recommended setting for AssetBundle compression.
            /// </summary>
            public BundleCompressionMode compression;
            /// <summary>
            /// The recommended setting for AssetBundle cache usage.
            /// </summary>
            public bool useAssetBundleCache;
            /// <summary>
            /// The recommended setting for AssetBundle cache clearing.
            /// </summary>
            public CacheClearBehavior assetBundledCacheClearBehavior;
            /// <summary>
            /// The recommended setting for AssetBundle crc usage.
            /// </summary>
            public bool useAssetBundleCrc;
            /// <summary>
            /// The recommended setting for AssetBundle crc usage regarding cached bundles.
            /// </summary>
            public bool useAssetBundleCrcForCachedBundles;
            /// <summary>
            /// The recommended naming style for AssetBundle file name.
            /// </summary>
            public BundleNamingStyle bundleNaming;
            /// <summary>
            /// The recommended setting for stripping additional download metadata.
            /// </summary>
            public bool stripDownloadOptions;
        }

        internal Dictionary<DefaultSchemaSettingsBuildTargetGroup, DefaultSchemaSettings[]> m_DefaultSettings;

        /// <summary>
        /// Create sets of recommended settings based on the build target platform and AssetBundle loading strategy.
        /// </summary>
        /// <returns>Sets of recommended settings.</returns>
        public Dictionary<DefaultSchemaSettingsBuildTargetGroup, DefaultSchemaSettings[]> CreateDefaultSchemaSettings()
        {
            var defaultSettings = new Dictionary<DefaultSchemaSettingsBuildTargetGroup, DefaultSchemaSettings[]>();

            // Default
            {
                DefaultSchemaSettings defaultLocalSettings = default;
                DefaultSchemaSettings defaultRemoteSettings = default;
#if UNITY_SWITCH || UNITY_SWITCH2
                defaultLocalSettings.compression = BundleCompressionMode.Uncompressed;
                defaultLocalSettings.useAssetBundleCache = false; // bundle caching not supported
                defaultLocalSettings.assetBundledCacheClearBehavior = CacheClearBehavior.ClearWhenSpaceIsNeededInCache;
                defaultLocalSettings.useAssetBundleCrc = false;
                defaultLocalSettings.useAssetBundleCrcForCachedBundles = false;
                defaultLocalSettings.bundleNaming = BundleNamingStyle.NoHash;
                defaultLocalSettings.stripDownloadOptions = true;

                defaultRemoteSettings.compression = BundleCompressionMode.Uncompressed;
                defaultRemoteSettings.useAssetBundleCache = false; // bundle caching not supported
                defaultRemoteSettings.assetBundledCacheClearBehavior = CacheClearBehavior.ClearWhenSpaceIsNeededInCache;
                defaultRemoteSettings.useAssetBundleCrc = true;
                defaultRemoteSettings.useAssetBundleCrcForCachedBundles = false;
                defaultRemoteSettings.bundleNaming = BundleNamingStyle.NoHash;
                defaultRemoteSettings.stripDownloadOptions = false;
#elif UNITY_PS4
                defaultLocalSettings.compression = BundleCompressionMode.Uncompressed;
                defaultLocalSettings.useAssetBundleCache = false; // bundle caching not supported
                defaultLocalSettings.assetBundledCacheClearBehavior = CacheClearBehavior.ClearWhenSpaceIsNeededInCache;
                defaultLocalSettings.useAssetBundleCrc = false;
                defaultLocalSettings.useAssetBundleCrcForCachedBundles = false;
                defaultLocalSettings.bundleNaming = BundleNamingStyle.NoHash;
                defaultLocalSettings.stripDownloadOptions = true;

                defaultRemoteSettings.compression = BundleCompressionMode.Uncompressed;
                defaultRemoteSettings.useAssetBundleCache = false; // bundle caching not supported
                defaultRemoteSettings.assetBundledCacheClearBehavior = CacheClearBehavior.ClearWhenSpaceIsNeededInCache;
                defaultRemoteSettings.useAssetBundleCrc = true;
                defaultRemoteSettings.useAssetBundleCrcForCachedBundles = false;
                defaultRemoteSettings.bundleNaming = BundleNamingStyle.NoHash;
                defaultRemoteSettings.stripDownloadOptions = false;
#elif UNITY_PS5
                defaultLocalSettings.compression = BundleCompressionMode.Uncompressed;
                defaultLocalSettings.useAssetBundleCache = false;
                defaultLocalSettings.assetBundledCacheClearBehavior = CacheClearBehavior.ClearWhenSpaceIsNeededInCache;
                defaultLocalSettings.useAssetBundleCrc = false;
                defaultLocalSettings.useAssetBundleCrcForCachedBundles = false;
                defaultLocalSettings.bundleNaming = BundleNamingStyle.NoHash;
                defaultLocalSettings.stripDownloadOptions = true;

                defaultRemoteSettings.compression = BundleCompressionMode.Uncompressed;
                defaultRemoteSettings.useAssetBundleCache = true;
                defaultRemoteSettings.assetBundledCacheClearBehavior = CacheClearBehavior.ClearWhenSpaceIsNeededInCache;
                defaultRemoteSettings.useAssetBundleCrc = true;
                defaultRemoteSettings.useAssetBundleCrcForCachedBundles = false;
                defaultRemoteSettings.bundleNaming = BundleNamingStyle.NoHash;
                defaultRemoteSettings.stripDownloadOptions = false;
#elif UNITY_GAMECORE || UNITY_GAMECORE_XBOXONE || UNITY_GAMECORE_XBOXSERIES || UNITY_XBOXONE
                defaultLocalSettings.compression = BundleCompressionMode.LZ4;
                defaultLocalSettings.useAssetBundleCache = false;
                defaultLocalSettings.assetBundledCacheClearBehavior = CacheClearBehavior.ClearWhenSpaceIsNeededInCache;
                defaultLocalSettings.useAssetBundleCrc = false;
                defaultLocalSettings.useAssetBundleCrcForCachedBundles = false;
                defaultLocalSettings.bundleNaming = BundleNamingStyle.NoHash;
                defaultLocalSettings.stripDownloadOptions = true;

                defaultRemoteSettings.compression = BundleCompressionMode.LZMA;
                defaultRemoteSettings.useAssetBundleCache = true;
                defaultRemoteSettings.assetBundledCacheClearBehavior = CacheClearBehavior.ClearWhenSpaceIsNeededInCache;
                defaultRemoteSettings.useAssetBundleCrc = true;
                defaultRemoteSettings.useAssetBundleCrcForCachedBundles = false;
                defaultRemoteSettings.bundleNaming = BundleNamingStyle.NoHash;
                defaultRemoteSettings.stripDownloadOptions = false;
#else
                defaultLocalSettings.compression = BundleCompressionMode.LZ4;
                defaultLocalSettings.useAssetBundleCache = false;
                defaultLocalSettings.assetBundledCacheClearBehavior = CacheClearBehavior.ClearWhenSpaceIsNeededInCache;
                defaultLocalSettings.useAssetBundleCrc = false;
                defaultLocalSettings.useAssetBundleCrcForCachedBundles = false;
                defaultLocalSettings.bundleNaming = BundleNamingStyle.AppendHash;
                defaultLocalSettings.stripDownloadOptions = true;

                defaultRemoteSettings.compression = BundleCompressionMode.LZMA;
                defaultRemoteSettings.useAssetBundleCache = true;
                defaultRemoteSettings.assetBundledCacheClearBehavior = CacheClearBehavior.ClearWhenSpaceIsNeededInCache;
                defaultRemoteSettings.useAssetBundleCrc = true;
                defaultRemoteSettings.useAssetBundleCrcForCachedBundles = false;
                defaultRemoteSettings.stripDownloadOptions = false;
#endif
                defaultSettings[DefaultSchemaSettingsBuildTargetGroup.Default] = new DefaultSchemaSettings[2] { defaultLocalSettings, defaultRemoteSettings };
            }

            // StandaloneWindows
            {
                DefaultSchemaSettings windowsLocalSettings;
                windowsLocalSettings.compression = BundleCompressionMode.LZ4;
                windowsLocalSettings.useAssetBundleCache = false;
                windowsLocalSettings.assetBundledCacheClearBehavior = CacheClearBehavior.ClearWhenSpaceIsNeededInCache;
                windowsLocalSettings.useAssetBundleCrc = false;
                windowsLocalSettings.useAssetBundleCrcForCachedBundles = false;
                windowsLocalSettings.bundleNaming = BundleNamingStyle.OnlyHash; // help avoid max path limit
                windowsLocalSettings.stripDownloadOptions = true;

                DefaultSchemaSettings windowsRemoteSettings;
                windowsRemoteSettings.compression = BundleCompressionMode.LZMA;
                windowsRemoteSettings.useAssetBundleCache = true;
                windowsRemoteSettings.assetBundledCacheClearBehavior = CacheClearBehavior.ClearWhenSpaceIsNeededInCache;
                windowsRemoteSettings.useAssetBundleCrc = true;
                windowsRemoteSettings.useAssetBundleCrcForCachedBundles = false;
                windowsRemoteSettings.bundleNaming = BundleNamingStyle.OnlyHash; // help avoid max path limit
                windowsRemoteSettings.stripDownloadOptions = false;

                defaultSettings[DefaultSchemaSettingsBuildTargetGroup.StandaloneWindows] = new DefaultSchemaSettings[2] { windowsLocalSettings, windowsRemoteSettings };
            }

            // iOS
            {
                DefaultSchemaSettings iOSLocalSettings;
                iOSLocalSettings.compression = BundleCompressionMode.LZ4;
                iOSLocalSettings.useAssetBundleCache = false;
                iOSLocalSettings.assetBundledCacheClearBehavior = CacheClearBehavior.ClearWhenSpaceIsNeededInCache;
                iOSLocalSettings.useAssetBundleCrc = false;
                iOSLocalSettings.useAssetBundleCrcForCachedBundles = false;
                iOSLocalSettings.bundleNaming = BundleNamingStyle.AppendHash;
                iOSLocalSettings.stripDownloadOptions = true;

                DefaultSchemaSettings iOSRemoteSettings;
                iOSRemoteSettings.compression = BundleCompressionMode.LZMA;
                iOSRemoteSettings.useAssetBundleCache = true;
                iOSRemoteSettings.assetBundledCacheClearBehavior = CacheClearBehavior.ClearWhenWhenNewVersionLoaded; // frequent content updates
                iOSRemoteSettings.useAssetBundleCrc = true;
                iOSRemoteSettings.useAssetBundleCrcForCachedBundles = false;
                iOSRemoteSettings.bundleNaming = BundleNamingStyle.AppendHash;
                iOSRemoteSettings.stripDownloadOptions = false;

                defaultSettings[DefaultSchemaSettingsBuildTargetGroup.iOS] = new DefaultSchemaSettings[2] { iOSLocalSettings, iOSRemoteSettings };
            }

            // Android
            {
                DefaultSchemaSettings androidLocalSettings;
                androidLocalSettings.compression = BundleCompressionMode.LZ4;
                androidLocalSettings.useAssetBundleCache = false;
                androidLocalSettings.assetBundledCacheClearBehavior = CacheClearBehavior.ClearWhenSpaceIsNeededInCache;
                androidLocalSettings.useAssetBundleCrc = false;
                androidLocalSettings.useAssetBundleCrcForCachedBundles = false;
                androidLocalSettings.bundleNaming = BundleNamingStyle.AppendHash;
                androidLocalSettings.stripDownloadOptions = false;

                DefaultSchemaSettings androidRemoteSettings;
                androidRemoteSettings.compression = BundleCompressionMode.LZMA;
                androidRemoteSettings.useAssetBundleCache = true;
                androidRemoteSettings.assetBundledCacheClearBehavior = CacheClearBehavior.ClearWhenWhenNewVersionLoaded; // frequent content updates
                androidRemoteSettings.useAssetBundleCrc = true;
                androidRemoteSettings.useAssetBundleCrcForCachedBundles = false;
                androidRemoteSettings.bundleNaming = BundleNamingStyle.AppendHash;
                androidRemoteSettings.stripDownloadOptions = false;

                defaultSettings[DefaultSchemaSettingsBuildTargetGroup.Android] = new DefaultSchemaSettings[2] { androidLocalSettings, androidRemoteSettings };
            }

            // WebGL
            {
                DefaultSchemaSettings webGLSettings;
                webGLSettings.compression = BundleCompressionMode.LZMA; // can only load bundles by web requests
                webGLSettings.useAssetBundleCache = false; // no bundle caching for this platform
                webGLSettings.assetBundledCacheClearBehavior = CacheClearBehavior.ClearWhenSpaceIsNeededInCache;
                webGLSettings.useAssetBundleCrc = true;
                webGLSettings.useAssetBundleCrcForCachedBundles = false;
                webGLSettings.bundleNaming = BundleNamingStyle.AppendHash;
                webGLSettings.stripDownloadOptions = false;

                defaultSettings[DefaultSchemaSettingsBuildTargetGroup.WebGL] = new DefaultSchemaSettings[2] { webGLSettings, webGLSettings };
            }

            return defaultSettings;
        }

        /// <summary>
        /// Returns the corresponding DefaultSchemaSettingsBuildTargetGroup for the build target specified.
        /// </summary>
        /// <param name="buildTarget">The build target.</param>
        /// <returns>The corresponding DefaultSchemaSettingsBuildTargetGroup.</returns>
        public DefaultSchemaSettingsBuildTargetGroup GetDefaultSchemaSettingsBuildTargetGroup(BuildTarget buildTarget)
        {
            if (buildTarget == BuildTarget.StandaloneWindows || buildTarget == BuildTarget.StandaloneWindows64)
                return DefaultSchemaSettingsBuildTargetGroup.StandaloneWindows;

            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            switch (buildTargetGroup)
            {
                case BuildTargetGroup.iOS:
                    return DefaultSchemaSettingsBuildTargetGroup.iOS;
                case BuildTargetGroup.Android:
                    return DefaultSchemaSettingsBuildTargetGroup.Android;
                case BuildTargetGroup.WebGL:
                    return DefaultSchemaSettingsBuildTargetGroup.WebGL;
                default:
                    return DefaultSchemaSettingsBuildTargetGroup.Default;
            }
        }

        internal bool BuildTargetSupportsBundleCaching(BuildTarget buildTarget)
        {
            return buildTarget != BuildTarget.WebGL &&
                buildTarget != BuildTarget.PS4 &&
                buildTarget != BuildTarget.Switch;
        }

        internal enum DefaultSettingsTarget
        {
            Local,
            Remote
        }

        /// <summary>
        /// Get the default settings for an Addressable Group schema
        /// </summary>
        /// <returns>A set of recommended schema settings</returns>
        public DefaultSchemaSettings GetDefaultSchemaSettings()
        {
            if (m_UseCustomPaths)
                return default;

            if (!HasDefaultSchemaSettings())
                return default;

            if (m_DefaultSettings == null)
                m_DefaultSettings = CreateDefaultSchemaSettings();

            DefaultSchemaSettingsBuildTargetGroup targetGroup = GetDefaultSchemaSettingsBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            if (Group != null && Group.Settings != null)
            {
                var loadPathName = m_LoadPath.GetName(Group.Settings);
                if (loadPathName.Equals(AddressableAssetSettings.kRemoteLoadPath))
                {
                    return m_DefaultSettings[targetGroup][(int)DefaultSettingsTarget.Remote];
                }

                if (loadPathName.Equals(AddressableAssetSettings.kLocalLoadPath))
                {
                    return m_DefaultSettings[targetGroup][(int)DefaultSettingsTarget.Local];
                }
            }

            Debug.LogError("Could not determine default settings for schema as it does not have a group attached. " +
                           "This may be due to the schema being initialized manually without setting its group.");
            return default;
        }

        internal bool HasDefaultSchemaSettings()
        {
            DefaultSchemaSettingsBuildTargetGroup targetGroup = GetDefaultSchemaSettingsBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            if (Group != null && Group.Settings != null)
            {
                var loadPathName = m_LoadPath.GetName(Group.Settings);
                return loadPathName.Equals(AddressableAssetSettings.kRemoteLoadPath) ||
                       loadPathName.Equals(AddressableAssetSettings.kLocalLoadPath);
            }
            return false;
        }

        void SetPathPairOption(BundledAssetGroupSchema src, BundledAssetGroupSchema dst)
        {
            if (dst.m_BuildPath.Id != src.BuildPath.Id)
                dst.m_BuildPath.Id = src.BuildPath.Id;

            if (dst.m_LoadPath.Id != src.m_LoadPath.Id)
                dst.m_LoadPath.Id = src.m_LoadPath.Id;

            if (dst.m_UseCustomPaths != src.m_UseCustomPaths)
                dst.m_UseCustomPaths = src.m_UseCustomPaths;

            if (dst.SelectedPathPairIndex != src.SelectedPathPairIndex)
                dst.SelectedPathPairIndex = src.SelectedPathPairIndex;

            dst.SetDirty(true);
        }
    }
}
