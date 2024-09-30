using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

#if ENABLE_CCD
using Unity.Services.Ccd.Management;
#endif

namespace UnityEditor.AddressableAssets.Build
{
    /// <summary>
    /// Option for how to deal with automatically checking for content update restrictions as part of the Update a Previous Build workflow.
    /// </summary>
    public enum CheckForContentUpdateRestrictionsOptions
    {
        /// <summary>
        /// If assets are modified that have been previously built in a Cannot Change Post Release group,
        /// the build will be paused and the Update Restrictions Check window is opened
        /// </summary>
        ListUpdatedAssetsWithRestrictions = 0,

        /// <summary>
        /// If assets are modified that have been previously built in a Cannot Change Post Release group, the Content Update build will fail.
        /// </summary>
        FailBuild = 1,

        /// <summary>
        /// Updating a previous build does not automatically run the Check for Update Restrictions rule.
        /// </summary>
        Disabled = 2
    }

#if ENABLE_CCD
    /// <summary>
    /// This is used to determine the behavior of Update a Previous Build when taking advantage of the Build & Release feature.
    /// </summary>
    public enum BuildAndReleaseContentStateBehavior
    {
        /// <summary>
        /// Uses the Previous Content State bin file path set in the AddressableAssetSettings
        /// </summary>
        UsePresetLocation = 0,
        /// <summary>
        /// Pulls the Previous Content State bin from the associated Cloud Content Delivery bucket set in the profile variables.
        /// </summary>
        UseCCDBucket = 1

    }
#endif

    /// <summary>
    /// The given state of an Asset.  Represented by its guid and hash.
    /// </summary>
    [Serializable]
    public struct AssetState : IEquatable<AssetState>
    {
        /// <summary>
        /// Asset states GUID.
        /// </summary>
        public GUID guid;

        /// <summary>
        /// Asset State hash.
        /// </summary>
        public Hash128 hash;

        /// <summary>
        /// Check if one asset state is equal to another.
        /// </summary>
        /// <param name="other">Right hand side of comparision.</param>
        /// <returns>Returns true if the Asset States are equal to one another.</returns>
        public bool Equals(AssetState other)
        {
            return guid == other.guid && hash == other.hash;
        }
    }

    /// <summary>
    /// The Cached Asset State of an Addressable Asset.
    /// </summary>
    [Serializable]
    public class CachedAssetState : IEquatable<CachedAssetState>
    {
        /// <summary>
        /// The Asset State.
        /// </summary>
        public AssetState asset;

        /// <summary>
        /// The Asset State of all dependencies.
        /// </summary>
        public AssetState[] dependencies;

        /// <summary>
        /// The guid for the group the cached asset state belongs to.
        /// </summary>
        public string groupGuid;

        /// <summary>
        /// The name of the cached asset states bundle file.
        /// </summary>
        public string bundleFileId;

        /// <summary>
        /// The cached asset state data.
        /// </summary>
        public object data;

        /// <summary>
        /// Checks if one cached asset state is equal to another given the asset state and dependency state.
        /// </summary>
        /// <param name="other">Right hand side of comparision.</param>
        /// <returns>Returns true if the cached asset states are equal to one another.</returns>
        public bool Equals(CachedAssetState other)
        {
            bool result = other != null && asset.Equals(other.asset);
            result &= dependencies != null && other.dependencies != null;
            result &= dependencies.Length == other.dependencies.Length;
            var index = 0;
            while (result && index < dependencies.Length)
            {
                result &= dependencies[index].Equals(other.dependencies[index]);
                index++;
            }

            return result;
        }
    }

    /// <summary>
    /// Cached state of asset bundles.
    /// </summary>
    [Serializable]
    public class CachedBundleState
    {
        /// <summary>
        /// The name of the cached asset states bundle file.
        /// </summary>
        public string bundleFileId;

        /// <summary>
        /// The cached bundle state data.
        /// </summary>
        public object data;
    }

    /// <summary>
    /// Data stored with each build that is used to generated content updates.
    /// </summary>
    [Serializable]
    public class AddressablesContentState
    {
        /// <summary>
        /// The version that the player was built with.  This is usually set to AddressableAssetSettings.PlayerBuildVersion.
        /// </summary>
        [SerializeField]
        public string playerVersion;

        /// <summary>
        /// The version of the unity editor used to build the player.
        /// </summary>
        [SerializeField]
        public string editorVersion;

        /// <summary>
        /// Dependency information for all assets in the build that have been marked StaticContent.
        /// </summary>
        [SerializeField]
        public CachedAssetState[] cachedInfos;

        /// <summary>
        /// The path of a remote catalog.  This is the only place the player knows to look for an updated catalog.
        /// </summary>
        [SerializeField]
        public string remoteCatalogLoadPath;

        /// <summary>
        /// Information about asset bundles created for the build.
        /// </summary>
        [SerializeField]
        public CachedBundleState[] cachedBundles;
    }

    internal struct ContentUpdateUsageData
    {
        public string ContentUpdateInterruptMessage;
        public bool UsingCCD;
    }

    internal struct ContentUpdateBuildData
    {
        public string Error;
        public double BuildDuration;
    }

    /// <summary>
    /// Contains methods used for the content update workflow.
    /// </summary>
    public static class ContentUpdateScript
    {
        internal static readonly string FirstTimeUpdatePreviousBuild = nameof(FirstTimeUpdatePreviousBuild);

        /// <summary>
        /// Contains build information used for updating assets.
        /// </summary>
        public struct ContentUpdateContext
        {
            /// <summary>
            /// The mapping of an asset's guid to its cached asset state.
            /// </summary>
            public Dictionary<string, CachedAssetState> GuidToPreviousAssetStateMap;

            /// <summary>
            /// The mapping of an asset's or bundle's internal id to its catalog entry.
            /// </summary>
            public Dictionary<string, ContentCatalogDataEntry> IdToCatalogDataEntryMap;

            /// <summary>
            /// The mapping of a bundle's name to its internal bundle id.
            /// </summary>
            public Dictionary<string, string> BundleToInternalBundleIdMap;

            /// <summary>
            /// Stores the asset bundle write information.
            /// </summary>
            public IBundleWriteData WriteData;

            /// <summary>
            /// Stores the cached build data.
            /// </summary>
            public AddressablesContentState ContentState;

            /// <summary>
            /// Stores the paths of the files created during a build.
            /// </summary>
            public FileRegistry Registry;

            /// <summary>
            /// The list of asset state information gathered from the previous build.
            /// </summary>
            public List<CachedAssetState> PreviousAssetStateCarryOver;
        }

        private static string m_BinFileCachePath = "Library/com.unity.addressables/AddressablesBinFileDownload/addressables_content_state.bin";

        /// <summary>
        /// If the previous content state file location is a remote location, this path is where the file is downloaded to as part of a
        /// content update build.  In the event of a fresh build where the previous state file build path is remote, this is the location the
        /// file is built to.
        /// </summary>
        public static string PreviousContentStateFileCachePath
        {
            get { return m_BinFileCachePath; }
            set { m_BinFileCachePath = value; }
        }

        static bool GetAssetState(GUID asset, out AssetState assetState)
        {
            assetState = new AssetState();
            if (asset.Empty())
                return false;

            var path = AssetDatabase.GUIDToAssetPath(asset.ToString());
            if (string.IsNullOrEmpty(path))
                return false;

            var hash = AssetDatabase.GetAssetDependencyHash(path);
            if (!hash.isValid)
                return false;

            assetState.guid = asset;
            assetState.hash = hash;
            return true;
        }

        static bool GetCachedAssetStateForData(GUID asset, string bundleFileId, string groupGuid, object data, IEnumerable<GUID> dependencies, out CachedAssetState cachedAssetState)
        {
            cachedAssetState = null;

            AssetState assetState;
            if (!GetAssetState(asset, out assetState))
                return false;

            var visited = new HashSet<GUID>();
            visited.Add(asset);
            var dependencyStates = new List<AssetState>();
            foreach (var dependency in dependencies)
            {
                if (!visited.Add(dependency))
                    continue;

                AssetState dependencyState;
                if (!GetAssetState(dependency, out dependencyState))
                    continue;
                dependencyStates.Add(dependencyState);
            }

            cachedAssetState = new CachedAssetState();
            cachedAssetState.asset = assetState;
            cachedAssetState.dependencies = dependencyStates.ToArray();
            cachedAssetState.groupGuid = groupGuid;
            cachedAssetState.bundleFileId = bundleFileId;
            cachedAssetState.data = data;

            return true;
        }

        static bool HasAssetOrDependencyChanged(CachedAssetState cachedInfo)
        {
            CachedAssetState newCachedInfo;
            if (!GetCachedAssetStateForData(cachedInfo.asset.guid, cachedInfo.bundleFileId, cachedInfo.groupGuid, cachedInfo.data, cachedInfo.dependencies.Select(x => x.guid), out newCachedInfo))
                return true;
            return !cachedInfo.Equals(newCachedInfo);
        }

        /// <summary>
        /// Save the content update information for a set of AddressableAssetEntry objects.
        /// </summary>
        /// <param name="path">File to write content stat info to.  If file already exists, it will be deleted before the new file is created.</param>
        /// <param name="entries">The entries to save.</param>
        /// <param name="dependencyData">The raw dependency information generated from the build.</param>
        /// <param name="playerVersion">The player version to save. This is usually set to AddressableAssetSettings.PlayerBuildVersion.</param>
        /// <param name="remoteCatalogPath">The server path (if any) that contains an updateable content catalog.  If this is empty, updates cannot occur.</param>
        /// <returns>True if the file is saved, false otherwise.</returns>
        [Obsolete]
        public static bool SaveContentState(string path, List<AddressableAssetEntry> entries, IDependencyData dependencyData, string playerVersion, string remoteCatalogPath)
        {
            return SaveContentState(new List<ContentCatalogDataEntry>(), path, entries, dependencyData, playerVersion, remoteCatalogPath);
        }

        /// <summary>
        /// Save the content update information for a set of AddressableAssetEntry objects.
        /// </summary>
        /// <param name="locations">The ContentCatalogDataEntry locations that were built into the Content Catalog.</param>
        /// <param name="path">File to write content stat info to.  If file already exists, it will be deleted before the new file is created.</param>
        /// <param name="entries">The entries to save.</param>
        /// <param name="dependencyData">The raw dependency information generated from the build.</param>
        /// <param name="playerVersion">The player version to save. This is usually set to AddressableAssetSettings.PlayerBuildVersion.</param>
        /// <param name="remoteCatalogPath">The server path (if any) that contains an updateable content catalog.  If this is empty, updates cannot occur.</param>
        /// <returns>True if the file is saved, false otherwise.</returns>
        public static bool SaveContentState(List<ContentCatalogDataEntry> locations, string path, List<AddressableAssetEntry> entries, IDependencyData dependencyData, string playerVersion,
            string remoteCatalogPath)
        {
            return SaveContentState(locations, path, entries, dependencyData, playerVersion, remoteCatalogPath, null);
        }

        /// <summary>
        /// Save the content update information for a set of AddressableAssetEntry objects.
        /// </summary>
        /// <param name="locations">The ContentCatalogDataEntry locations that were built into the Content Catalog.</param>
        /// <param name="path">File to write content stat info to.  If file already exists, it will be deleted before the new file is created.</param>
        /// <param name="entries">The entries to save.</param>
        /// <param name="dependencyData">The raw dependency information generated from the build.</param>
        /// <param name="playerVersion">The player version to save. This is usually set to AddressableAssetSettings.PlayerBuildVersion.</param>
        /// <param name="remoteCatalogPath">The server path (if any) that contains an updateable content catalog.  If this is empty, updates cannot occur.</param>
        /// <param name="carryOverCacheState">Cached state that needs to carry over from the previous build.  This mainly affects Content Update.</param>
        /// <returns>True if the file is saved, false otherwise.</returns>
        public static bool SaveContentState(List<ContentCatalogDataEntry> locations, string path, List<AddressableAssetEntry> entries, IDependencyData dependencyData, string playerVersion,
            string remoteCatalogPath, List<CachedAssetState> carryOverCacheState)
        {
            return SaveContentState(locations, null, path, entries, dependencyData, playerVersion, remoteCatalogPath, carryOverCacheState);
        }

        /// <summary>
        /// Save the content update information for a set of AddressableAssetEntry objects.
        /// </summary>
        /// <param name="locations">The ContentCatalogDataEntry locations that were built into the Content Catalog.</param>
        /// <param name="guidToCatalogLocation">Mapping of asset Guid to catalog locations entries for lookup of extra data.</param>
        /// <param name="path">File to write content stat info to.  If file already exists, it will be deleted before the new file is created.</param>
        /// <param name="entries">The entries to save.</param>
        /// <param name="dependencyData">The raw dependency information generated from the build.</param>
        /// <param name="playerVersion">The player version to save. This is usually set to AddressableAssetSettings.PlayerBuildVersion.</param>
        /// <param name="remoteCatalogPath">The server path (if any) that contains an updateable content catalog.  If this is empty, updates cannot occur.</param>
        /// <param name="carryOverCacheState">Cached state that needs to carry over from the previous build.  This mainly affects Content Update.</param>
        /// <returns>True if the file is saved, false otherwise.</returns>
        internal static bool SaveContentState(List<ContentCatalogDataEntry> locations, Dictionary<GUID, List<ContentCatalogDataEntry>> guidToCatalogLocation, string path, List<AddressableAssetEntry> entries, IDependencyData dependencyData, string playerVersion,
            string remoteCatalogPath, List<CachedAssetState> carryOverCacheState)
        {
            try
            {
                var cachedInfos = GetCachedAssetStates(guidToCatalogLocation, entries, dependencyData);

                var cachedBundleInfos = new List<CachedBundleState>();
                foreach (ContentCatalogDataEntry ccEntry in locations)
                {
                    if (typeof(IAssetBundleResource).IsAssignableFrom(ccEntry.ResourceType))
                        cachedBundleInfos.Add(new CachedBundleState() {bundleFileId = ccEntry.InternalId, data = ccEntry.Data});
                }

                if (carryOverCacheState != null)
                {
                    foreach (var cs in carryOverCacheState)
                        cachedInfos.Add(cs);
                }

                var cacheData = new AddressablesContentState
                {
                    cachedInfos = cachedInfos.ToArray(),
                    playerVersion = playerVersion,
                    editorVersion = Application.unityVersion,
                    remoteCatalogLoadPath = remoteCatalogPath,
                    cachedBundles = cachedBundleInfos.ToArray()
                };
                var formatter = new BinaryFormatter();
                if (File.Exists(path))
                    File.Delete(path);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                formatter.Serialize(stream, cacheData);
                stream.Flush();
                stream.Close();
                stream.Dispose();
                return true;
            }
            catch (UnauthorizedAccessException uae)
            {
                if (!AddressableAssetUtility.IsVCAssetOpenForEdit(path))
                    Debug.LogErrorFormat("Cannot access the file {0}. It may be locked by version control.", path);
                else
                    Debug.LogException(uae);
                return false;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        static IList<CachedAssetState> GetCachedAssetStates(Dictionary<GUID, List<ContentCatalogDataEntry>> guidToCatalogLocation,
            List<AddressableAssetEntry> entries, IDependencyData dependencyData)
        {
            IList<CachedAssetState> gatheredCachedInfos = new List<CachedAssetState>();

            Dictionary<string, AddressableAssetEntry> guidToEntries = new Dictionary<string, AddressableAssetEntry>();
            foreach (AddressableAssetEntry entry in entries)
                if (!guidToEntries.ContainsKey(entry.guid))
                    guidToEntries[entry.guid] = entry;

            foreach (KeyValuePair<GUID, AssetLoadInfo> assetData in dependencyData.AssetInfo)
                GetCachedAssetState(guidToCatalogLocation, guidToEntries, assetData.Key, assetData.Value.referencedObjects, gatheredCachedInfos);
            foreach (KeyValuePair<GUID, SceneDependencyInfo> sceneData in dependencyData.SceneInfo)
                GetCachedAssetState(guidToCatalogLocation, guidToEntries, sceneData.Key, sceneData.Value.referencedObjects, gatheredCachedInfos);

            return gatheredCachedInfos;
        }

        private static void GetCachedAssetState(Dictionary<GUID, List<ContentCatalogDataEntry>> guidToCatalogLocation,
            Dictionary<string, AddressableAssetEntry> guidToEntries, GUID guid,
            IReadOnlyCollection<ObjectIdentifier> dependencies, IList<CachedAssetState> cachedInfos)
        {
            guidToEntries.TryGetValue(guid.ToString(), out AddressableAssetEntry addressableEntry);
            List<ContentCatalogDataEntry> catalogLocationsForSceneGuid = null;
            guidToCatalogLocation?.TryGetValue(guid, out catalogLocationsForSceneGuid);

            if (addressableEntry != null)
            {
                object catalogData = catalogLocationsForSceneGuid != null && catalogLocationsForSceneGuid.Count > 0
                    ? catalogLocationsForSceneGuid[0].Data
                    : null;

                if (GetCachedAssetStateForData(guid, addressableEntry.BundleFileId,
                        addressableEntry.parentGroup.Guid, catalogData,
                        dependencies.Select(x => x.guid),
                        out CachedAssetState cachedAssetState))
                    cachedInfos.Add(cachedAssetState);
            }
        }

        /// <summary>
        /// Gets the path of the cache data from a selected build.
        /// </summary>
        /// <param name="browse">If true, the user is allowed to browse for a specific file.</param>
        /// <returns>The path of the previous state .bin file used to detect changes from the previous build to the content update build.</returns>
        public static string GetContentStateDataPath(bool browse)
        {
            return GetContentStateDataPath(browse, null);
        }

        internal static string GetContentStateDataPath(bool browse, AddressableAssetSettings settings)
        {
            if (settings == null)
                settings = AddressableAssetSettingsDefaultObject.Settings;
            var profileSettings = settings == null ? null : settings.profileSettings;
            string assetPath = profileSettings != null ? profileSettings.EvaluateString(settings.activeProfileId, settings.ContentStateBuildPath) : "";

            if (string.IsNullOrEmpty(assetPath))
            {
                assetPath = settings != null
                    ? settings.GetContentStateBuildPath()
                    : Path.Combine(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder, PlatformMappingService.GetPlatformPathSubFolder());
            }

            if (browse)
            {
                if (string.IsNullOrEmpty(assetPath))
                    assetPath = Application.dataPath;

                assetPath = EditorUtility.OpenFilePanel("Build Data File", Path.GetDirectoryName(assetPath), "bin");

                if (string.IsNullOrEmpty(assetPath))
                    return null;

                return assetPath;
            }

            if (!ResourceManagerConfig.ShouldPathUseWebRequest(assetPath))
            {
                try
                {
                    Directory.CreateDirectory(assetPath);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message + "\nCheck \"Content State Build Path\" in Addressables settings. Falling back to config folder location.");
                    assetPath = Path.Combine(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
                        PlatformMappingService.GetPlatformPathSubFolder());
                    Directory.CreateDirectory(assetPath);
                }
            }

#if ENABLE_CCD
            switch(settings.BuildAndReleaseBinFileOption)
            {
                case BuildAndReleaseContentStateBehavior.UsePresetLocation:
                    //do nothing
                    break;
                case BuildAndReleaseContentStateBehavior.UseCCDBucket:
                    assetPath = settings.RemoteCatalogLoadPath.GetValue(settings);
                    break;
            }
#endif

            var path = Path.Combine(assetPath, "addressables_content_state.bin");
            return path;
        }

        /// <summary>
        /// Downloads the content state bin to a temporary directory
        /// </summary>
        /// <param name="url">The url of the bin file</param>
        /// <returns>The temp path the bin file was downloaded to.</returns>
        internal static string DownloadBinFileToTempLocation(string url)
        {
            if (!Directory.Exists(ContentUpdateScript.PreviousContentStateFileCachePath))
                Directory.CreateDirectory(Path.GetDirectoryName(ContentUpdateScript.PreviousContentStateFileCachePath));
            else if (File.Exists(ContentUpdateScript.PreviousContentStateFileCachePath))
                File.Delete(ContentUpdateScript.PreviousContentStateFileCachePath);

            try
            {
                var bytes = new WebClient().DownloadData(url);
                File.WriteAllBytes(ContentUpdateScript.PreviousContentStateFileCachePath, bytes);
            }
            catch
            {
                //Do nothing, nothing will get downloaded and the users can select a file manually if they want.
            }

            return ContentUpdateScript.PreviousContentStateFileCachePath;
        }

        /// <summary>
        /// Loads cache data from a specific location
        /// </summary>
        /// <param name="contentStateDataPath"></param>
        /// <returns>The ContentState object.</returns>
        public static AddressablesContentState LoadContentState(string contentStateDataPath)
        {
            if (string.IsNullOrEmpty(contentStateDataPath))
            {
                Debug.LogErrorFormat("Unable to load cache data from {0}.", contentStateDataPath);
                return null;
            }

            var stream = new FileStream(contentStateDataPath, FileMode.Open, FileAccess.Read);
            var formatter = new BinaryFormatter();
            var cacheData = formatter.Deserialize(stream) as AddressablesContentState;
            if (cacheData == null)
            {
                Addressables.LogError(
                    "Invalid hash data file.  This file is usually named addressables_content_state.bin and is saved in the same folder as your source AddressableAssetsSettings.asset file.");
                return null;
            }

            stream.Dispose();
            return cacheData;
        }

        static bool s_StreamingAssetsExists;
        static string kStreamingAssetsPath = "Assets/StreamingAssets";

        internal static void Cleanup(bool deleteStreamingAssetsFolderIfEmpty, bool cleanBuildPath)
        {
            if (cleanBuildPath)
            {
                DirectoryUtility.DeleteDirectory(Addressables.BuildPath, onlyIfEmpty: false, recursiveDelete: true);
            }

            if (deleteStreamingAssetsFolderIfEmpty)
            {
                DirectoryUtility.DeleteDirectory(kStreamingAssetsPath, onlyIfEmpty: true);
            }
        }

        /// <summary>
        /// Builds player content using the player content version from a specified cache file.
        /// </summary>
        /// <param name="settings">The settings object to use for the build.</param>
        /// <param name="contentStateDataPath">The path of the cache data to use.</param>
        /// <returns>The build operation.</returns>
        public static AddressablesPlayerBuildResult BuildContentUpdate(AddressableAssetSettings settings, string contentStateDataPath)
        {
            var cacheData = LoadContentState(contentStateDataPath);
            if (!IsCacheDataValid(settings, cacheData))
                return null;

            s_StreamingAssetsExists = Directory.Exists("Assets/StreamingAssets");
            var context = new AddressablesDataBuilderInput(settings, cacheData.playerVersion);
            context.IsContentUpdateBuild = true;
            context.PreviousContentState = cacheData;

            Cleanup(!s_StreamingAssetsExists, false);

            SceneManagerState.Record();
            var result = settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(context);
            if (!string.IsNullOrEmpty(result.Error))
                Debug.LogError(result.Error);
            SceneManagerState.Restore();
            return result;
        }

        internal static bool IsCacheDataValid(AddressableAssetSettings settings, AddressablesContentState cacheData)
        {
            if (cacheData == null)
                return false;

            if (cacheData.editorVersion != Application.unityVersion)
                Addressables.LogWarningFormat("Building content update with Unity editor version `{0}`, data was created with version `{1}`.  This may result in incompatible data.",
                    Application.unityVersion, cacheData.editorVersion);

            if (string.IsNullOrEmpty(cacheData.remoteCatalogLoadPath))
            {
                Addressables.LogError("Previous build had 'Build Remote Catalog' disabled.  You cannot update a player that has no remote catalog specified");
                return false;
            }

            if (!settings.BuildRemoteCatalog)
            {
                Addressables.LogError("Current settings have 'Build Remote Catalog' disabled.  You cannot update a player that has no remote catalog to look to.");
                return false;
            }

            if (cacheData.remoteCatalogLoadPath != settings.RemoteCatalogLoadPath.GetValue(settings))
            {
                Addressables.LogErrorFormat(
                    "Current 'Remote Catalog Load Path' does not match load path of original player.  Player will only know to look up catalog at original location. Original: {0}  Current: {1}",
                    cacheData.remoteCatalogLoadPath, settings.RemoteCatalogLoadPath.GetValue(settings));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get all modified addressable asset entries in groups that have BundledAssetGroupSchema and ContentUpdateGroupSchema with static content enabled.
        /// This includes any Addressable dependencies that are affected by the modified entries.
        /// </summary>
        /// <param name="settings">Addressable asset settings.</param>
        /// <param name="cacheDataPath">The cache data path.</param>
        /// <returns>A list of all modified entries and dependencies (list is empty if there are none); null if failed to load cache data.</returns>
        public static List<AddressableAssetEntry> GatherModifiedEntries(AddressableAssetSettings settings, string cacheDataPath)
        {
            HashSet<AddressableAssetEntry> retVal = new HashSet<AddressableAssetEntry>();
            var entriesMap = GatherModifiedEntriesWithDependencies(settings, cacheDataPath);
            foreach (var entry in entriesMap.Keys)
            {
                if (!retVal.Contains(entry))
                    retVal.Add(entry);

                foreach (var dependency in entriesMap[entry])
                    if (!retVal.Contains(dependency))
                        retVal.Add(dependency);
            }

            return retVal.ToList();
        }

        internal static void GatherExplicitModifiedEntries(AddressableAssetSettings settings, ref Dictionary<AddressableAssetEntry, List<AddressableAssetEntry>> dependencyMap,
            AddressablesContentState cacheData)
        {
            List<string> noBundledAssetGroupSchema = new List<string>();
            List<string> noStaticContent = new List<string>();

            var allEntries = new List<AddressableAssetEntry>();
            settings.GetAllAssets(allEntries, false, g =>
            {
                if (g == null)
                    return false;

                if (!g.HasSchema<BundledAssetGroupSchema>())
                {
                    noBundledAssetGroupSchema.Add(g.Name);
                    return false;
                }

                if (!g.HasSchema<ContentUpdateGroupSchema>())
                {
                    noStaticContent.Add(g.Name);
                    return false;
                }

                if (!g.GetSchema<ContentUpdateGroupSchema>().StaticContent)
                {
                    noStaticContent.Add(g.Name);
                    return false;
                }

                g.FlaggedDuringContentUpdateRestriction = false;
                return true;
            });

            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("Skipping Prepare for Content Update on {0} group(s):\n\n",
                noBundledAssetGroupSchema.Count + noStaticContent.Count);


            AddInvalidGroupsToLogMessage(builder, noBundledAssetGroupSchema, "Group Did Not Contain BundledAssetGroupSchema");
            AddInvalidGroupsToLogMessage(builder, noStaticContent, "Static Content Not Enabled In Schemas");

            Debug.Log(builder.ToString());

            var entryToCacheInfo = new Dictionary<string, CachedAssetState>();
            foreach (var cacheInfo in cacheData.cachedInfos)
                if (cacheInfo != null)
                    entryToCacheInfo[cacheInfo.asset.guid.ToString()] = cacheInfo;
            var modifiedEntries = new List<AddressableAssetEntry>();
            foreach (var entry in allEntries)
            {
                if (!entryToCacheInfo.TryGetValue(entry.guid, out CachedAssetState cachedInfo) || HasAssetOrDependencyChanged(cachedInfo))
                {
                    Type mainType = AddressableAssetUtility.MapEditorTypeToRuntimeType(entry.MainAssetType, false);
                    if ((mainType == null || mainType == typeof(DefaultAsset)) && !entry.IsInResources)
                    {
                        entry.FlaggedDuringContentUpdateRestriction = false;
                    }
                    else
                    {
                        modifiedEntries.Add(entry);
                        entry.FlaggedDuringContentUpdateRestriction = true;
                        entry.parentGroup.FlaggedDuringContentUpdateRestriction = true;
                    }
                }
                else
                    entry.FlaggedDuringContentUpdateRestriction = false;
            }

            AddAllDependentScenesFromModifiedEntries(modifiedEntries);
            foreach (var entry in modifiedEntries)
            {
                if (!dependencyMap.ContainsKey(entry))
                    dependencyMap.Add(entry, new List<AddressableAssetEntry>());
            }
        }

        internal static void ClearContentUpdateNotifications(AddressableAssetGroup assetGroup)
        {
            if (assetGroup == null)
                return;

            if (assetGroup.FlaggedDuringContentUpdateRestriction)
            {
                ClearContentUpdateFlagForEntries(assetGroup.entries);
                assetGroup.FlaggedDuringContentUpdateRestriction = false;
            }
        }

        static void ClearContentUpdateFlagForEntries(ICollection<AddressableAssetEntry> entries)
        {
            foreach (var e in entries)
            {
                if (e != null)
                    e.FlaggedDuringContentUpdateRestriction = false;
                if (e.IsFolder)
                {
                    List<AddressableAssetEntry> folderEntries = new List<AddressableAssetEntry>();
                    e.GatherFolderEntries(folderEntries, true, true, null);
                    ClearContentUpdateFlagForEntries(folderEntries);
                }
            }
        }

        /// <summary>
        /// Get a Dictionary of all modified values and their dependencies.  Dependencies will be Addressable and part of a group
        /// with static content enabled.
        /// </summary>
        /// <param name="settings">Addressable asset settings.</param>
        /// <param name="cachePath">The cache data path.</param>
        /// <returns>A dictionary mapping explicit changed entries to their dependencies.</returns>
        public static Dictionary<AddressableAssetEntry, List<AddressableAssetEntry>> GatherModifiedEntriesWithDependencies(AddressableAssetSettings settings, string cachePath)
        {
            var modifiedData = new Dictionary<AddressableAssetEntry, List<AddressableAssetEntry>>();
            AddressablesContentState cacheData = LoadContentState(cachePath);
            if (cacheData == null)
                return modifiedData;

            GatherExplicitModifiedEntries(settings, ref modifiedData, cacheData);
            GetStaticContentDependenciesForEntries(settings, ref modifiedData, GetGroupGuidToCacheBundleNameMap(cacheData));
            GetEntriesDependentOnModifiedEntries(settings, ref modifiedData);
            return modifiedData;
        }

        internal static void GetEntriesDependentOnModifiedEntries(AddressableAssetSettings settings, ref Dictionary<AddressableAssetEntry, List<AddressableAssetEntry>> dependencyMap)
        {
            var groups = GetStaticGroups(settings);
            Dictionary<AddressableAssetEntry, string[]> entryToDependencies = new Dictionary<AddressableAssetEntry, string[]>();
            foreach (AddressableAssetGroup group in groups)
            {
                foreach (AddressableAssetEntry entry in group.entries)
                {
                    string[] dependencies = AssetDatabase.GetDependencies(entry.AssetPath);
                    entryToDependencies.Add(entry, dependencies);
                }
            }

            HashSet<AddressableAssetEntry> modifiedEntries = new HashSet<AddressableAssetEntry>();
            foreach (KeyValuePair<AddressableAssetEntry, List<AddressableAssetEntry>> mappedEntry in dependencyMap)
            {
                modifiedEntries.Add(mappedEntry.Key);
                foreach (AddressableAssetEntry dependencyEntry in mappedEntry.Value)
                    modifiedEntries.Add(dependencyEntry);
            }

            // if an entry is dependant on a modified entry, then it too should be modified to reference the moved asset
            foreach (AddressableAssetEntry modifiedEntry in modifiedEntries)
            {
                foreach (KeyValuePair<AddressableAssetEntry, string[]> dependency in entryToDependencies)
                {
                    if (dependency.Key != modifiedEntry &&
                        dependency.Value.Contains(modifiedEntry.AssetPath) &&
                        dependencyMap.TryGetValue(modifiedEntry, out var value))
                    {
                        if (!value.Contains(dependency.Key))
                            value.Add(dependency.Key);
                    }
                }
            }
        }

        internal static List<AddressableAssetGroup> GetStaticGroups(AddressableAssetSettings settings)
        {
            List<AddressableAssetGroup> staticGroups = new List<AddressableAssetGroup>();
            foreach (AddressableAssetGroup group in settings.groups)
            {
                var staticSchema = group.GetSchema<ContentUpdateGroupSchema>();
                if (staticSchema == null)
                    continue;
                var bundleSchema = group.GetSchema<BundledAssetGroupSchema>();
                if (bundleSchema == null)
                    continue;

                if (staticSchema.StaticContent)
                    staticGroups.Add(group);
            }

            return staticGroups;
        }

        internal static Dictionary<string, string> GetGroupGuidToCacheBundleNameMap(AddressablesContentState cacheData)
        {
            var bundleIdToCacheInfo = new Dictionary<string, string>();
            foreach (CachedBundleState bundleInfo in cacheData.cachedBundles)
            {
                if (bundleInfo != null && bundleInfo.data is AssetBundleRequestOptions options)
                    bundleIdToCacheInfo[bundleInfo.bundleFileId] = options.BundleName;
            }

            var groupGuidToCacheBundleName = new Dictionary<string, string>();
            foreach (CachedAssetState cacheInfo in cacheData.cachedInfos)
            {
                if (cacheInfo != null && bundleIdToCacheInfo.TryGetValue(cacheInfo.bundleFileId, out string bundleName))
                    groupGuidToCacheBundleName[cacheInfo.groupGuid] = bundleName;
            }

            return groupGuidToCacheBundleName;
        }

        internal static HashSet<string> GetGroupGuidsWithUnchangedBundleName(AddressableAssetSettings settings, Dictionary<AddressableAssetEntry, List<AddressableAssetEntry>> dependencyMap,
            Dictionary<string, string> groupGuidToCacheBundleName)
        {
            var result = new HashSet<string>();
            if (groupGuidToCacheBundleName == null || groupGuidToCacheBundleName.Count == 0)
                return result;

            var entryGuidToDeps = new Dictionary<string, List<AddressableAssetEntry>>();
            foreach (KeyValuePair<AddressableAssetEntry, List<AddressableAssetEntry>> entryToDeps in dependencyMap)
            {
                entryGuidToDeps.Add(entryToDeps.Key.guid, entryToDeps.Value);
            }

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null || !group.HasSchema<BundledAssetGroupSchema>())
                    continue;

                var schema = group.GetSchema<BundledAssetGroupSchema>();
                List<AssetBundleBuild> bundleInputDefinitions = new List<AssetBundleBuild>();

                BuildScriptPackedMode.PrepGroupBundlePacking(group, bundleInputDefinitions, schema, entry => !entryGuidToDeps.ContainsKey(entry.guid));
                BuildScriptPackedMode.HandleBundleNames(bundleInputDefinitions);

                for (int i = 0; i < bundleInputDefinitions.Count; i++)
                {
                    string bundleName = Path.GetFileNameWithoutExtension(bundleInputDefinitions[i].assetBundleName);
                    if (groupGuidToCacheBundleName.TryGetValue(group.Guid, out string cacheBundleName) && cacheBundleName == bundleName)
                        result.Add(group.Guid);
                }
            }

            return result;
        }

        internal static void GetStaticContentDependenciesForEntries(AddressableAssetSettings settings, ref Dictionary<AddressableAssetEntry, List<AddressableAssetEntry>> dependencyMap,
            Dictionary<string, string> groupGuidToCacheBundleName = null)
        {
            if (dependencyMap == null)
                return;

            Dictionary<AddressableAssetGroup, bool> groupHasStaticContentMap = new Dictionary<AddressableAssetGroup, bool>();
            HashSet<string> groupGuidsWithUnchangedBundleName = GetGroupGuidsWithUnchangedBundleName(settings, dependencyMap, groupGuidToCacheBundleName);

            foreach (AddressableAssetEntry entry in dependencyMap.Keys)
            {
                //since the entry here is from our list of modified entries we know that it must be a part of a static content group.
                //Since it's part of a static content update group we can go ahead and set the value to true in the dictionary without explicitly checking it.
                if (!groupHasStaticContentMap.ContainsKey(entry.parentGroup))
                    groupHasStaticContentMap.Add(entry.parentGroup, true);

                string[] dependencies = AssetDatabase.GetDependencies(entry.AssetPath);
                foreach (string dependency in dependencies)
                {
                    string guid = AssetDatabase.AssetPathToGUID(dependency);
                    var depEntry = settings.FindAssetEntry(guid, true);
                    if (depEntry == null)
                        continue;
                    if (groupGuidsWithUnchangedBundleName.Contains(depEntry.parentGroup.Guid))
                        continue;

                    if (!groupHasStaticContentMap.TryGetValue(depEntry.parentGroup, out bool groupHasStaticContentEnabled))
                    {
                        groupHasStaticContentEnabled = depEntry.parentGroup.HasSchema<ContentUpdateGroupSchema>() &&
                                                       depEntry.parentGroup.GetSchema<ContentUpdateGroupSchema>().StaticContent;

                        groupHasStaticContentMap.Add(depEntry.parentGroup, groupHasStaticContentEnabled);
                    }

                    if (!dependencyMap.ContainsKey(depEntry) && groupHasStaticContentEnabled)
                    {
                        if (!dependencyMap.ContainsKey(entry))
                            dependencyMap.Add(entry, new List<AddressableAssetEntry>());
                        dependencyMap[entry].Add(depEntry);
                        depEntry.FlaggedDuringContentUpdateRestriction = true;
                    }
                }
            }
        }

        internal static void AddAllDependentScenesFromModifiedEntries(List<AddressableAssetEntry> modifiedEntries)
        {
            List<AddressableAssetEntry> entriesToAdd = new List<AddressableAssetEntry>();
            //If a scene has changed, all scenes that end up in the same bundle need to be marked as modified due to bundle dependencies
            foreach (AddressableAssetEntry entry in modifiedEntries)
            {
                if (entry.IsScene && !entriesToAdd.Contains(entry))
                {
                    switch (entry.parentGroup.GetSchema<BundledAssetGroupSchema>().BundleMode)
                    {
                        case BundledAssetGroupSchema.BundlePackingMode.PackTogether:
                            //Add every scene in the group to modified entries
                            foreach (AddressableAssetEntry sharedGroupEntry in entry.parentGroup.entries)
                            {
                                if (sharedGroupEntry.IsScene && !modifiedEntries.Contains(sharedGroupEntry))
                                {
                                    sharedGroupEntry.FlaggedDuringContentUpdateRestriction = true;
                                    entriesToAdd.Add(sharedGroupEntry);
                                }
                            }

                            break;

                        case BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel:
                            foreach (AddressableAssetEntry sharedGroupEntry in entry.parentGroup.entries)
                            {
                                //Check if one entry has 0 labels while the other contains labels.  The labels union check below will return true in this case.
                                //That is not the behavior we want.  So to avoid that, we check here first.
                                if (sharedGroupEntry.labels.Count == 0 ^ entry.labels.Count == 0)
                                    continue;

                                //Only add if labels are shared
                                if (sharedGroupEntry.IsScene && !modifiedEntries.Contains(sharedGroupEntry) && sharedGroupEntry.labels.Union(entry.labels).Any())
                                {
                                    sharedGroupEntry.FlaggedDuringContentUpdateRestriction = true;
                                    entriesToAdd.Add(sharedGroupEntry);
                                }
                            }

                            break;

                        case BundledAssetGroupSchema.BundlePackingMode.PackSeparately:
                            //Do nothing.  The scene will be in a different bundle.
                            break;

                        default:
                            break;
                    }
                }
            }

            modifiedEntries.AddRange(entriesToAdd);
        }

        private static void AddInvalidGroupsToLogMessage(StringBuilder builder, List<string> invalidGroupList,
            string headerMessage)
        {
            if (invalidGroupList.Count > 0)
            {
                builder.AppendFormat("{0} ({1} groups):\n", headerMessage, invalidGroupList.Count);
                int maxList = 15;
                for (int i = 0; i < invalidGroupList.Count; i++)
                {
                    if (i > maxList)
                    {
                        builder.AppendLine("...");
                        break;
                    }

                    builder.AppendLine("-" + invalidGroupList[i]);
                }

                builder.AppendLine("");
            }
        }

        /// <summary>
        /// Create a new AddressableAssetGroup with the items and mark it as remote.
        /// </summary>
        /// <param name="settings">The settings object.</param>
        /// <param name="items">The items to move.</param>
        /// <param name="groupName">The name of the new group.</param>
        public static void CreateContentUpdateGroup(AddressableAssetSettings settings, List<AddressableAssetEntry> items, string groupName)
        {
            var contentGroup = settings.CreateGroup(settings.FindUniqueGroupName(groupName), false, false, true, null);
            var schema = contentGroup.AddSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            contentGroup.AddSchema<ContentUpdateGroupSchema>().StaticContent = false;
            settings.MoveEntries(items, contentGroup);
        }

        /// <summary>
        /// Functor to filter AddressableAssetGroups during content update. If the functor returns false, the group is excluded from the update.
        /// </summary>
        public static Func<AddressableAssetGroup, bool> GroupFilterFunc = GroupFilter;

        internal static bool GroupFilter(AddressableAssetGroup g)
        {
            if (g == null)
                return false;
            if (!g.HasSchema<ContentUpdateGroupSchema>() || !g.GetSchema<ContentUpdateGroupSchema>().StaticContent)
                return false;
            if (!g.HasSchema<BundledAssetGroupSchema>() || !g.GetSchema<BundledAssetGroupSchema>().IncludeInBuild)
                return false;
            return true;
        }
    }
}
