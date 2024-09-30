#if ENABLE_CCD
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Services.Ccd.Management;
using Unity.Services.Ccd.Management.Models;
using Unity.Services.Core;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace UnityEditor.AddressableAssets.Build
{
    /// <summary>
    /// CCD Events used for building Addressables content with CCD.
    /// </summary>
    public class CcdBuildEvents
    {
        static CcdBuildEvents s_Instance;

        /// <summary>
        /// The static instance of CcdBuildEvents.
        /// </summary>
        public static CcdBuildEvents Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new CcdBuildEvents();
                    s_Instance.RegisterNewBuildEvents();
                    s_Instance.RegisterUpdateBuildEvents();
                }
                return s_Instance;
            }
        }

        const string k_ContentStatePath = "addressables_content_state.bin";

        internal void RegisterNewBuildEvents()
        {
            OnPreBuildEvents += s_Instance.VerifyBuildVersion;
            OnPreBuildEvents += s_Instance.RefreshDataSources;
            OnPreBuildEvents += s_Instance.VerifyTargetBucket;
            OnPostBuildEvents += s_Instance.UploadContentState;
            OnPostBuildEvents += s_Instance.UploadAndRelease;
        }

        internal void RegisterUpdateBuildEvents()
        {
            OnPreUpdateEvents += s_Instance.VerifyBuildVersion;
            OnPreUpdateEvents += s_Instance.RefreshDataSources;
            OnPreUpdateEvents += s_Instance.DownloadContentStateBin;
            OnPreUpdateEvents += s_Instance.VerifyTargetBucket;
            OnPostUpdateEvents += s_Instance.UploadContentState;
            OnPostUpdateEvents += s_Instance.UploadAndRelease;
        }

        /// <summary>
        /// Pre Addressables build event.
        /// </summary>
        public delegate Task<bool> PreEvent(AddressablesDataBuilderInput input);

        /// <summary>
        /// Pre new build events.
        /// Default events:
        /// <see cref="RefreshDataSources"/>
        /// <see cref="VerifyTargetBucket"/>
        /// </summary>
        public static event PreEvent OnPreBuildEvents;

        /// <summary>
        /// Pre update build events.
        /// Default events:
        /// <see cref="RefreshDataSources"/>
        /// <see cref="DownloadContentStateBin"/>
        /// <see cref="VerifyTargetBucket"/>
        /// </summary>
        public static event PreEvent OnPreUpdateEvents;

        /// <summary>
        /// Post Addressables build event.
        /// </summary>
        public delegate Task<bool> PostEvent(AddressablesDataBuilderInput input,
            AddressablesPlayerBuildResult result);

        /// <summary>
        /// Post new build events.
        /// Default events:
        /// <see cref="UploadContentState"/>
        /// <see cref="UploadAndRelease"/>
        /// </summary>
        public static event PostEvent OnPostBuildEvents;

        /// <summary>
        /// Post update build events.
        /// Default events:
        /// <see cref="UploadContentState"/>
        /// <see cref="UploadAndRelease"/>
        /// </summary>
        public static event PostEvent OnPostUpdateEvents;

        internal async Task<bool> OnPreEvent(bool isUpdate, AddressablesDataBuilderInput input)
        {
            if (isUpdate)
            {
                return await InvokePreEvent(OnPreUpdateEvents, input);
            }
            return await InvokePreEvent(OnPreBuildEvents, input);
        }

        internal async Task<bool> InvokePreEvent(PreEvent events, AddressablesDataBuilderInput input)
        {
            if (events == null)
            {

                return true;
            }

            var total = events.GetInvocationList().Length;
            for (var i = 0; i < total; i++)
            {
                var e = (PreEvent)events.GetInvocationList()[i];
                var shouldContinue = await e.Invoke(input);
                if (!shouldContinue)
                {
                    return false;
                }
            }
            return true;
        }

        internal async Task<bool> OnPostEvent(bool isUpdate, AddressablesDataBuilderInput input,
            AddressablesPlayerBuildResult result)
        {
            if (isUpdate)
            {
                return await InvokePostEvent(OnPostUpdateEvents, input, result);
            }
            return await InvokePostEvent(OnPostBuildEvents, input, result);
        }

        internal async Task<bool> InvokePostEvent(PostEvent events, AddressablesDataBuilderInput input,
            AddressablesPlayerBuildResult result)
        {
            if (events == null)
                return true;

            var total = events.GetInvocationList().Length;
            for (var i = 0; i < total; i++)
            {
                var e = (PostEvent)events.GetInvocationList()[i];
                var shouldContinue = await e.Invoke(input, result);
                if (!shouldContinue)
                {
                    // if a post-build step adds an error we have to log it manually
                    if (result != null && !string.IsNullOrEmpty(result.Error))
                    {
                        Addressables.LogError(result.Error);
                    }

                    return false;
                }
            }

            return true;

        }

        /// <summary>
        /// Prepend an event to the pre new build events.
        /// </summary>
        /// <param name="newEvent">Pre build event</param>
        public static void PrependPreBuildEvent(PreEvent newEvent)
        {
            Delegate[] oldEvents = OnPreBuildEvents?.GetInvocationList();
            OnPreBuildEvents = newEvent;
            if (oldEvents != null)
                foreach (var t in oldEvents)
                {
                    OnPreBuildEvents += (PreEvent)(t);
                }
        }

        /// <summary>
        /// Prepend an event to the post new build events.
        /// </summary>
        /// <param name="newEvent">Post build event</param>
        public static void PrependPostBuildEvent(PostEvent newEvent)
        {
            Delegate[] oldEvents = OnPostBuildEvents?.GetInvocationList();
            OnPostBuildEvents = newEvent;
            if (oldEvents != null)
                foreach (var t in oldEvents)
                {
                    OnPostBuildEvents += (PostEvent)(t);
                }
        }

        /// <summary>
        /// Prepend an event to the pre update build events.
        /// </summary>
        /// <param name="newEvent">Pre build event</param>
        public static void PrependPreUpdateEvent(PreEvent newEvent)
        {
            Delegate[] oldEvents = OnPreUpdateEvents?.GetInvocationList();
            OnPreUpdateEvents = newEvent;
            if (oldEvents != null)
                foreach (var t in oldEvents)
                {
                    OnPreUpdateEvents += (PreEvent)(t);
                }
        }

        /// <summary>
        /// Prepend an event to the post update build events.
        /// </summary>
        /// <param name="newEvent">Post build event</param>
        public static void PrependPostUpdateEvent(PostEvent newEvent)
        {
            Delegate[] oldEvents = OnPostUpdateEvents?.GetInvocationList();
            OnPostUpdateEvents = newEvent;
            if (oldEvents != null)
                foreach (var t in oldEvents)
                {
                    OnPostUpdateEvents += (PostEvent)(t);
                }
        }

        internal void ConfigureCcdManagement(AddressableAssetSettings settings, string environmentId)
        {
            CcdManagement.SetEnvironmentId(environmentId);
#if CCD_REQUEST_LOGGING
            CcdManagement.LogRequests = settings.CCDLogRequests;
            CcdManagement.LogRequestHeaders = settings.CCDLogRequestHeaders;
#endif
        }

        public Task<bool> VerifyBuildVersion(AddressablesDataBuilderInput input)
        {
            if (string.IsNullOrWhiteSpace(input.AddressableSettings.OverridePlayerVersion))
            {
                Addressables.LogWarning("<b>When using CCD it is recommended that you set a 'Player Version Override' in Addressables Settings.</b> You can have it use the Player build version by setting it to [UnityEditor.PlayerSettings.bundleVersion].");
                Addressables.LogWarning("Documentation on how to disable this warning is available in the example DisableBuildWarnings.cs.");
            }
            return Task.FromResult(true);
        }

        /// <summary>
        /// Update the CCD data source settings.
        /// </summary>
        /// <param name="input">Addressables data builder context</param>
        /// <returns></returns>
        public async Task<bool> RefreshDataSources(AddressablesDataBuilderInput input)
        {
            return await RefreshDataSources();
        }
        internal async Task<bool> RefreshDataSources()
        {
            try

            {
                var projectId = CloudProjectSettings.projectId;
                await ProfileDataSourceSettings.UpdateCCDDataSourcesAsync(projectId, true);
            }
            catch (Exception e)
            {
                Addressables.LogError(e.ToString());
                return false;
            }
            return true;
        }

        internal async Task<bool> LoopGroups(AddressableAssetSettings settings, Func<AddressableAssetSettings, AddressableAssetGroup, BundledAssetGroupSchema, Task<bool>> action)
        {
            var tasks = new List<Task<bool>>();
            foreach (var group in settings.groups)
            {
                if (group == null)
                {
                    continue;
                }

                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null)
                {
                    continue;
                }
                tasks.Add(action(settings, group, schema));
            }
            var results = await Task.WhenAll(tasks.ToArray());
            foreach (var result in results)
            {
                // if any fail, all fail
                if (result == false)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Verify that the targeted CCD bucket exists or create it.
        /// </summary>
        /// <param name="input">Addressables data builder context</param>
        /// <returns></returns>
        public async Task<bool> VerifyTargetBucket(AddressablesDataBuilderInput input)
        {
            try
            {
                if (input.AddressableSettings == null)
                {
                    string error;
                    if (EditorApplication.isUpdating)
                        error = "Addressable Asset Settings does not exist.  EditorApplication.isUpdating was true.";
                    else if (EditorApplication.isCompiling)
                        error = "Addressable Asset Settings does not exist.  EditorApplication.isCompiling was true.";
                    else
                        error = "Addressable Asset Settings does not exist.  Failed to create.";
                    Addressables.LogError(error);
                    return false;
                }

                if (!hasRemoteGroups(input.AddressableSettings))
                {
                    Addressables.LogWarning("No Addressable Asset Groups have been marked remote or the current profile is not using CCD.");
                    if (input.AddressableSettings.BuildRemoteCatalog)
                    {
                        Addressables.LogWarning("A remote catalog will be built without any remote Asset Bundles.");
                    }
                }

                if (input.AddressableSettings.BuildRemoteCatalog)
                {
                    var dataSource = getRemoteCatalogDataSource(input.AddressableSettings);
                    var success = await verifyTargetBucket(input.AddressableSettings, "Remote Catalog", dataSource);
                    if (!success)
                    {
                        return false;
                    }
                }

                // Reclean directory before every build
                if (Directory.Exists(AddressableAssetSettings.kCCDBuildDataPath))
                {
                    Directory.Delete(AddressableAssetSettings.kCCDBuildDataPath, true);
                }
            }
            catch (Exception e)
            {
                Addressables.LogError($"Unable to verify target bucket: {e.Message}");
                return false;
            }

            return await LoopGroups(input.AddressableSettings, verifyTargetBucket);
        }

        internal bool hasRemoteGroups(AddressableAssetSettings settings)
        {
            foreach (var group in settings.groups)
            {
                if (group == null)
                {
                    continue;
                }

                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null)
                {
                    continue;
                }
                var dataSource = GetDataSource(settings, schema);
                if (isCCDGroup(dataSource))
                {
                    return true;
                }
            }
            return false;
        }

        internal bool isCCDGroup(ProfileGroupType dataSource)
        {

            if (dataSource == null)
            {
                return false;
            }
            if (IsUsingManager(dataSource))
            {
                return true;
            }
            return dataSource.GroupTypePrefix.StartsWith("CCD");
        }

        internal async Task<bool> verifyTargetBucket(AddressableAssetSettings settings, AddressableAssetGroup group, BundledAssetGroupSchema schema)
        {
                AddressableAssetSettings.NullifyBundleFileIds(group);
                var dataSource = GetDataSource(settings, schema);
                return await verifyTargetBucket(settings, group.Name, dataSource);

        }
        internal async Task<bool> verifyTargetBucket(AddressableAssetSettings settings, string groupName, ProfileGroupType dataSource)
        {
            try
            {

                // if not using the manager try to lookup the bucket and verify it's not promotion only
                if (!IsUsingManager(dataSource))
                {
                    if (dataSource == null)
                    {
                        return true;
                    }
                    var promotionOnly = IsPromotionOnlyBucket(dataSource);
                    if (promotionOnly)
                    {
                        Addressables.LogError("Cannot upload to Promotion Only bucket.");
                        return false;
                    }
                    return true;
                }

                // CcdManagedData.ConfigState.Override means it has been overriden by the customer at build time
                if (settings.m_CcdManagedData.State == CcdManagedData.ConfigState.Override)
                {
                    return true;
                }

                if (settings.m_CcdManagedData.IsConfigured())
                {
                    // this has been configured by a previous run
                    return true;
                }


                // existing automatic bucket loaded from cache
                var bucketIdVariable = dataSource
                    .GetVariableBySuffix($"{nameof(CcdBucket)}{nameof(CcdBucket.Id)}");
                if (bucketIdVariable != null)
                {
                    var promotionOnly = IsPromotionOnlyBucket(dataSource);
                    if (promotionOnly)
                    {
                        Debug.LogError("Cannot upload to Promotion Only bucket.");
                        return false;
                    }

                    PopulateCcdManagedData(settings, settings.activeProfileId);
                    return true;
                }

                // otherwise try to create
                var environmentId = ProfileDataSourceSettings.GetSettings().GetEnvironmentId(settings.profileSettings, settings.activeProfileId);
                CcdManagement.SetEnvironmentId(environmentId); // this should be getting the value from the active profile
                var ccdBucket = await CreateManagedBucket(EditorUserBuildSettings.activeBuildTarget.ToString());
                if (ccdBucket == null) {
                    // the bucket already exists, we shouldn't be here if refresh was called
                    ccdBucket = await GetExistingManagedBucket();
                }
                var environmentName = ProfileDataSourceSettings.GetSettings().GetEnvironmentName(settings.profileSettings, settings.activeProfileId);
                ProfileDataSourceSettings.AddGroupTypeForRemoteBucket(CloudProjectSettings.projectId, environmentId, environmentName, ccdBucket, new List<CcdBadge>());
                PopulateCcdManagedData(settings, settings.activeProfileId);

                // I should put this value into the data source list
                return true;
            }
            catch (Exception e)
            {
                Addressables.LogError($"Unable to verify target bucket for {groupName}: {e.Message}");
                return false;
            }
        }

        public void PopulateCcdManagedData(AddressableAssetSettings settings, string profileId)
        {
            // reset the state data
            settings.m_CcdManagedData = new CcdManagedData();
            var buildPath = settings.profileSettings.GetVariableId(AddressableAssetSettings.kRemoteBuildPath);
            var loadPath = settings.profileSettings.GetVariableId(AddressableAssetSettings.kRemoteLoadPath);
            if (buildPath == null || loadPath == null)
            {
                Addressables.Log($"Not populating CCD managed data. No remote paths are configured for profile {settings.profileSettings.GetProfileName(profileId)}.");
                return;
            }

            var dataSource = GetDataSource(settings, buildPath, loadPath);
            if (dataSource == null)
            {
                Addressables.Log($"Not populating CCD managed data. Data source not found. Try refreshing data sources in the profile window.");
                return;
            }

            if (!IsUsingManager(dataSource))
            {
                return;
            }

            var bucketIdVariable = dataSource
                .GetVariableBySuffix($"{nameof(CcdBucket)}{nameof(CcdBucket.Id)}");
            if (bucketIdVariable == null)
            {
                Addressables.Log("Not populating CCD managed data. No bucket ID found. Try refreshing data sources in the profile window.");
                return;
            }
            settings.m_CcdManagedData.BucketId = bucketIdVariable.Value;
            settings.m_CcdManagedData.Badge = dataSource
                .GetVariableBySuffix($"{nameof(CcdBadge)}{nameof(CcdBadge.Name)}").Value;


            // Target bucket should only be verified if Automatic profile type
            settings.m_CcdManagedData.EnvironmentId = ProfileDataSourceSettings.GetSettings().GetEnvironmentId(settings.profileSettings, profileId);
            settings.m_CcdManagedData.EnvironmentName = ProfileDataSourceSettings.GetSettings().GetEnvironmentName(settings.profileSettings, profileId);
        }

        /// <summary>
        /// Download addressables_content_state.bin from the CCD managed bucket.
        /// </summary>
        /// <param name="input">Addressables data builder context</param>
        /// <returns></returns>
        public async Task<bool> DownloadContentStateBin(AddressablesDataBuilderInput input)
        {
            if (!input.AddressableSettings.BuildRemoteCatalog)
            {
                Addressables.LogWarning("Not downloading content state because 'Build Remote Catalog' is not checked in Addressable Asset Settings. This will disable content updates");
                return true;
            }

            var settings = input.AddressableSettings;
            var dataSource = getRemoteCatalogDataSource(settings);
            if (dataSource == null)
            {
                Addressables.LogError("Could not find remote catalog paths. Ensure your profile's remote catalog load path is configured for CCD and that the bucket exists.");
                return false;
            }

            if (!this.isCCDGroup(dataSource))
            {
                Addressables.LogError("Content state could not be downloaded as the remote catalog is not targeting CCD");
                return false;
            }

            try
            {
                SetEnvironmentId(settings, dataSource);
                var bucketId = GetBucketId(settings, dataSource);
                if (bucketId == null)
                {
                    Addressables.LogError("Content state could not be downloaded as no bucket was specified. This is populated for managed profiles in the VerifyTargetBucket event.");
                    return false;
                }
                var api = CcdManagement.Instance;
                CcdEntry ccdEntry;
                try
                {
                    ccdEntry = await GetEntryByPath(api, new Guid(bucketId), k_ContentStatePath);
                }
                catch (Exception e)
                {
                    Addressables.LogError($"Unable to get entry for content state {k_ContentStatePath}: {e.Message}");
                    return false;

                }

                if (ccdEntry != null)
                {
                    var contentStream = await api.GetContentAsync(new EntryOptions(new Guid(bucketId), ccdEntry.Entryid));

                    var contentStatePath = Path.Combine(settings.GetContentStateBuildPath(), k_ContentStatePath);
                    if (!Directory.Exists(contentStatePath))
                        Directory.CreateDirectory(Path.GetDirectoryName(contentStatePath));
                    else if (File.Exists(contentStatePath))
                        File.Delete(contentStatePath);

                    using (var fileStream = File.Create(contentStatePath))
                    {
                        contentStream.CopyTo(fileStream);
                    }
                }
            }
            catch (Exception e)
            {
                Addressables.LogError($"Unable to upload content state {k_ContentStatePath}: {e.Message}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Upload content to the CCD managed bucket and create a release.
        /// </summary>
        /// <param name="input">Addressables data builder context</param>
        /// <param name="result">Addressables build result</param>
        /// <returns></returns>
        public async Task<bool> UploadAndRelease(AddressablesDataBuilderInput input,
            AddressablesPlayerBuildResult result)
        {
            // Verify files exist that need uploading
            var foundRemoteContent = result.FileRegistry?.GetFilePaths()
                .Any(path => path.StartsWith(AddressableAssetSettings.kCCDBuildDataPath)) == true;

            if (!foundRemoteContent)
            {
                Addressables.LogWarning(
                    "Skipping upload and release as no remote content was found to upload. Ensure you have at least one content group's 'Build & Load Path' set to Remote.");
                return false;
            }

            try
            {
                //Getting files
                Addressables.Log("Creating and uploading entries");
                var startDirectory = new DirectoryInfo(AddressableAssetSettings.kCCDBuildDataPath);
                var buildData = CreateData(startDirectory);


                //Creating a release for each bucket
                var defaultEnvironmentId = ProfileDataSourceSettings.GetSettings().GetEnvironmentId(input.AddressableSettings.profileSettings, input.AddressableSettings.activeProfileId);

                await UploadAndRelease(CcdManagement.Instance, input.AddressableSettings, defaultEnvironmentId, buildData);
            }
            catch (Exception e)
            {
                Addressables.LogError(e.ToString());
                return false;
            }

            return true;
        }

        private ProfileGroupType getRemoteCatalogDataSource(AddressableAssetSettings settings)
        {
            var buildPath = settings.RemoteCatalogBuildPath;
            var loadPath = settings.RemoteCatalogLoadPath;
            return GetDataSource(settings, buildPath.Id, loadPath.Id);
        }


        /// <summary>
        /// Upload addressables_content_state.bin to the CCD managed bucket.
        /// </summary>
        /// <param name="input">Addressables data builder context</param>
        /// <param name="result">Addressables build result</param>
        /// <returns></returns>
        public async Task<bool> UploadContentState(AddressablesDataBuilderInput input,
            AddressablesPlayerBuildResult result)
        {

            if (!input.AddressableSettings.BuildRemoteCatalog)
            {
                Addressables.LogWarning("Not uploading content state, because 'Build Remote Catalog' is not checked in Addressable Asset Settings. This will disable content updates");
                return true;
            }

            var settings = input.AddressableSettings;
            var dataSource = getRemoteCatalogDataSource(settings);
            if (dataSource == null)
            {
                Addressables.LogError("Could not find remote catalog paths. Ensure your profile's remote catalog load path is configured for CCD and that the bucket exists.");
                return false;
            }

            if (!this.isCCDGroup(dataSource))
            {
                Addressables.LogError("Content state could not be uploaded as the remote catalog is not targeting CCD");
                return false;
            }

            try
            {
                SetEnvironmentId(settings, dataSource);
                var bucketId = GetBucketId(settings, dataSource);
                if (bucketId == null)
                {
                    Addressables.LogError("Content state could not be uploaded as no bucket was specified. This is populated for managed profiles in the VerifyBucket event.");
                    return false;
                }
                var api = CcdManagement.Instance;

                var contentStatePath = Path.Combine(settings.GetContentStateBuildPath(), k_ContentStatePath);
                if (!File.Exists(contentStatePath))
                {
                    Addressables.LogError($"Content state file is missing {contentStatePath}");
                    return false;
                }
                var contentHash = AddressableAssetUtility.GetMd5Hash(contentStatePath);

                using (var stream = File.OpenRead(contentStatePath))
                {

                    var entryModelOptions = new EntryModelOptions(k_ContentStatePath, contentHash, (int)stream.Length)
                    {
                        UpdateIfExists = true
                    };
                    CcdEntry createdEntry;
                    try
                    {
                        createdEntry = await api.CreateOrUpdateEntryByPathAsync(
                            new EntryByPathOptions(new Guid(bucketId), k_ContentStatePath),
                            entryModelOptions);
                    }
                    catch (Exception e)
                    {
                        Addressables.LogError($"Unable to create entry for content state: {e.Message}");
                        return false;
                    }

                    try
                    {
                        var uploadContentOptions = new UploadContentOptions(
                            new Guid(bucketId), createdEntry.Entryid, stream);
                        await api.UploadContentAsync(uploadContentOptions);
                    }
                    catch (Exception e)
                    {
                        Addressables.LogError($"Unable to upload content state: {e.Message}");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Addressables.LogError(e.ToString());
                return false;
            }
            return true;
        }

        internal bool IsPromotionOnlyBucket(ProfileGroupType dataSource)
        {
            if (dataSource != null && dataSource.GroupTypePrefix.StartsWith("CCD"))
            {
                if (bool.Parse(dataSource.GetVariableBySuffix(nameof(CcdBucket.Attributes.PromoteOnly)).Value))
                {
                    Addressables.LogError("Cannot upload to Promotion Only bucket.");
                    return true;
                }
            }
            return false;
        }

        internal ProfileGroupType GetDataSource(AddressableAssetSettings settings, BundledAssetGroupSchema schema)
        {
            return GetDataSource(settings, schema.BuildPath.Id,  schema.LoadPath.Id);
        }

        internal ProfileGroupType GetDataSource(AddressableAssetSettings settings, string buildPathId, string loadPathId)
        {
            var groupType = GetGroupType(settings, buildPathId, loadPathId);
            if (!IsUsingManager(groupType))
            {
                return groupType;
            }

            var environmentId = ProfileDataSourceSettings.GetSettings().GetEnvironmentId(settings.profileSettings, settings.activeProfileId);
            // if we haven't setup an automatic group since refresh we do it here
            IEnumerable<ProfileGroupType> groupTypes = ProfileDataSourceSettings.GetSettings().GetGroupTypesByPrefix(string.Join(
                ProfileGroupType.k_PrefixSeparator.ToString(), "CCD", CloudProjectSettings.projectId,
                ProfileDataSourceSettings.GetSettings().GetEnvironmentId(settings.profileSettings, settings.activeProfileId)));
            // if we have setup an automatic group we load it here
            groupTypes = groupTypes.Concat(ProfileDataSourceSettings.GetSettings().GetGroupTypesByPrefix(AddressableAssetSettings.CcdManagerGroupTypePrefix));
            var automaticGroupType = groupTypes.FirstOrDefault(gt =>
                gt.GetVariableBySuffix($"{nameof(CcdBucket)}{nameof(CcdBucket.Name)}")?.Value == EditorUserBuildSettings.activeBuildTarget.ToString()
                && gt.GetVariableBySuffix($"{nameof(ProfileDataSourceSettings.Environment)}{nameof(ProfileDataSourceSettings.Environment.id)}")?.Value == environmentId);
            if (automaticGroupType == null)
            {
                // the bucket does not yet exist
                return groupType;
            }

            // set this value so we can check with IsUsingManager
            automaticGroupType.GroupTypePrefix = AddressableAssetSettings.CcdManagerGroupTypePrefix;
            return automaticGroupType;
        }

        internal ProfileGroupType GetGroupType(AddressableAssetSettings settings, string buildPathId, string loadPathId)
        {
            // This data is populated in the RefreshDataSources event
            // we need the "unresolved" value since we're tring to match it to its original type
            var buildPathValue = settings.profileSettings.GetValueById(settings.activeProfileId, buildPathId);
            var loadPathValue = settings.profileSettings.GetValueById(settings.activeProfileId, loadPathId);
            if (buildPathValue == null || loadPathValue == null)
            {
                return null;
            }

            var tempGroupType = new ProfileGroupType("temp");
            tempGroupType.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kBuildPath, buildPathValue));
            tempGroupType.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kLoadPath, loadPathValue));
            return ProfileDataSourceSettings.GetSettings().FindGroupType(tempGroupType);
        }


        internal bool IsUsingManager(ProfileGroupType dataSource)
        {
            if (dataSource == null)
            {
                return false;
            }
            return dataSource.GroupTypePrefix == AddressableAssetSettings.CcdManagerGroupTypePrefix;
        }

        internal void SetEnvironmentId(AddressableAssetSettings settings, ProfileGroupType groupType)
        {
            string environmentId = null;
            if (!IsUsingManager(groupType))
            {
                // if not using the manager load the bucketID from the group type
                environmentId = groupType.GetVariableBySuffix($"{nameof(ProfileDataSourceSettings.Environment)}{nameof(ProfileDataSourceSettings.Environment.id)}").Value;
            }
            else if (settings.m_CcdManagedData != null)
            {
                environmentId = settings.m_CcdManagedData.EnvironmentId;
            }

            if (environmentId == null)
            {
                throw new Exception("unable to determine environment ID.");
            }

            ConfigureCcdManagement(settings, environmentId);
        }

        internal string GetBucketId(AddressableAssetSettings settings, ProfileGroupType dataSource)
        {
            if (!IsUsingManager(dataSource))
            {
                // if not using the manager load the bucketID from the group type
                return dataSource.GetVariableBySuffix($"{nameof(CcdBucket)}{nameof(CcdBucket.Id)}").Value;
            }
            if (settings.m_CcdManagedData != null)
            {
                return settings.m_CcdManagedData.BucketId;
            }
            return null;
        }

        async Task<CcdBucket> CreateManagedBucket(string bucketName)
        {
            CcdBucket ccdBucket;
            try
            {
                ccdBucket = await CcdManagement.Instance.CreateBucketAsync(
                    new CreateBucketOptions(bucketName));
            }
            catch (CcdManagementException e)
            {
                if (e.ErrorCode == CcdManagementErrorCodes.AlreadyExists)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
            return ccdBucket;
        }

        async Task<CcdBucket> GetExistingManagedBucket()
        {
            var buckets = await ProfileDataSourceSettings.GetAllBucketsAsync();
            var ccdBucket = buckets.First(bucket =>
                bucket.Value.Name == EditorUserBuildSettings.activeBuildTarget.ToString()).Value;
            return ccdBucket;
        }

        async Task<CcdEntry> GetEntryByPath(ICcdManagementServiceSdk api, Guid bucketId, string path)
        {
            CcdEntry ccdEntry = null;
            try
            {
                ccdEntry = await api.GetEntryByPathAsync(new EntryByPathOptions(bucketId, path));
            }
            catch (CcdManagementException e)
            {
                if (e.ErrorCode != CommonErrorCodes.NotFound)
                {
                    throw;
                }
            }
            return ccdEntry;
        }

        CcdBuildDataFolder CreateData(DirectoryInfo startDirectory)
        {
            var buildDataFolder = new CcdBuildDataFolder
            {
                Name = AddressableAssetSettings.kCCDBuildDataPath,
                Location = startDirectory.FullName
            };
            buildDataFolder.GetChildren(startDirectory);
            return buildDataFolder;
        }

        int StartProgress(string description)
        {
#if UNITY_2020_1_OR_NEWER
            return Progress.Start("CCD", description, Progress.Options.Managed);
#else
            Addressables.Log(description);
            return -1;
#endif
        }

        void RemoveProgress(int progressId)
        {
#if UNITY_2020_1_OR_NEWER
            Progress.Remove(progressId);
#endif
        }

        void ReportProgress(int progressId, float progress, string message)
        {
#if UNITY_2020_1_OR_NEWER
            Progress.Report(progressId, progress, message);
#else
            Addressables.Log($"[{progress}] {message}");
#endif
        }

        async Task UploadAndRelease(ICcdManagementServiceSdk api, AddressableAssetSettings settings, string defaultEnvironmentId, CcdBuildDataFolder buildData)
        {
            var progressId = StartProgress("Upload and Release");
            try
            {
                foreach (var env in buildData.Environments)
                {

                    CcdManagement.SetEnvironmentId(env.Name);

                    if (env.Name == ProfileDataSourceSettings.MANAGED_ENVIRONMENT)
                    {
                        ConfigureCcdManagement(settings, defaultEnvironmentId);
                    }

                    foreach (var bucket in env.Buckets)
                    {
                        Guid bucketId;
                        var bucketIdString = bucket.Name == ProfileDataSourceSettings.MANAGED_BUCKET
                            ? settings.m_CcdManagedData.BucketId
                            : bucket.Name;
                        if (String.IsNullOrEmpty(bucketIdString))
                        {
                            Addressables.LogError($"Invalid bucket ID for {bucket.Name}");
                            continue;
                        }
                        bucketId = Guid.Parse(bucketIdString);

                        foreach (var badge in bucket.Badges)
                        {
                            if (badge.Name == ProfileDataSourceSettings.MANAGED_BADGE)
                            {
                                badge.Name = "latest";
                            }
                            var entries = new List<CcdReleaseEntryCreate>();
                            var total = badge.Files.Count();
                            for (var i = 0; i < total; i++)
                            {
                                var file = badge.Files[i];
                                var contentHash = AddressableAssetUtility.GetMd5Hash(file.FullName);
                                using (var stream = File.OpenRead(file.FullName))
                                {
                                    var entryPath = file.Name;
                                    var entryModelOptions = new EntryModelOptions(entryPath, contentHash, (int)stream.Length)
                                    {
                                        UpdateIfExists = true
                                    };
                                    ReportProgress(progressId, (i + 1) / total, $"Creating Entry {entryPath}");
                                    CcdEntry createdEntry;
                                    try
                                    {
                                        createdEntry = await api.CreateOrUpdateEntryByPathAsync(new EntryByPathOptions(bucketId, entryPath),
                                            entryModelOptions).ConfigureAwait(false);
                                    }
                                    catch (Exception e)
                                    {
                                        throw new Exception($"Unable to create entry for {entryPath}: {e.Message}", e);
                                    }

                                    Addressables.Log($"Created Entry {entryPath}");

                                    ReportProgress(progressId, (i + 1) / total, $"Uploading Entry {entryPath}");
                                    var uploadContentOptions = new UploadContentOptions(bucketId, createdEntry.Entryid, stream);
                                    await api.UploadContentAsync(uploadContentOptions).ConfigureAwait(false);

                                    ReportProgress(progressId, (i + 1) / total, $"Uploaded Entry {entryPath}");
                                    entries.Add(new CcdReleaseEntryCreate(createdEntry.Entryid, createdEntry.CurrentVersionid));
                                }
                            }

                            // Add content_sate.bin to release if present
                            var contentStateEntry = await GetEntryByPath(api, bucketId, k_ContentStatePath);
                            if (contentStateEntry != null)
                                entries.Add(new CcdReleaseEntryCreate(contentStateEntry.Entryid, contentStateEntry.CurrentVersionid));

                            //Creating release
                            ReportProgress(progressId, total, "Creating release");
                            Addressables.Log("Creating release.");
                            var release = await api.CreateReleaseAsync(new CreateReleaseOptions(bucketId)
                            {
                                Entries = entries,
                                Notes = $"Automated release created for {badge.Name}"
                            }).ConfigureAwait(false);
                            Addressables.Log($"Release {release.Releaseid} created.");

                            //Don't update latest badge (as it always updates)
                            if (badge.Name != "latest")
                            {
                                ReportProgress(progressId, total, "Updating badge");
                                Addressables.Log("Updating badge.");
                                var badgeRes = await api.AssignBadgeAsync(new AssignBadgeOptions(bucketId, badge.Name, release.Releaseid))
                                    .ConfigureAwait(false);
                                Addressables.Log($"Badge {badgeRes.Name} updated.");
                            }
                        }
                    }
                }
            }
            finally
            {
                RemoveProgress(progressId);
            }
        }

    }
}
#endif

