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
                    if (result != null && result.Error != "")
                    {
                        Debug.LogError(result.Error);
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

        public async Task<bool> VerifyBuildVersion(AddressablesDataBuilderInput input)
        {
            if (string.IsNullOrWhiteSpace(input.AddressableSettings.OverridePlayerVersion))
            {
                Addressables.LogWarning("<b>When using CCD it is recommended that you set a 'Player Version Override' in Addressables Settings.</b> You can have it use the Player build version by setting it to [UnityEditor.PlayerSettings.bundleVersion].");
                Addressables.LogWarning("Documentation on how to disable this warning is available in the example DisableBuildWarnings.cs.");
            }
            return true;
        }

        /// <summary>
        /// Update the CCD data source settings.
        /// </summary>
        /// <param name="input">Addressables data builder context</param>
        /// <returns></returns>
        public async Task<bool> RefreshDataSources(AddressablesDataBuilderInput input)
        {
            try
            {
                var projectId = CloudProjectSettings.projectId;
                await ProfileDataSourceSettings.UpdateCCDDataSourcesAsync(projectId, true);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
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
            if (input.AddressableSettings == null)
            {
                string error;
                if (EditorApplication.isUpdating)
                    error = "Addressable Asset Settings does not exist.  EditorApplication.isUpdating was true.";
                else if (EditorApplication.isCompiling)
                    error = "Addressable Asset Settings does not exist.  EditorApplication.isCompiling was true.";
                else
                    error = "Addressable Asset Settings does not exist.  Failed to create.";
                Debug.LogError(error);
                return false;
            }

            if (!hasRemoteGroups(input.AddressableSettings))
            {
                Debug.LogWarning("No Addressable Asset Groups have been marked remote or the current profile is not using CCD.");
                if (input.AddressableSettings.BuildRemoteCatalog)
                {
                    Debug.LogWarning("A remote catalog will be built without any remote Asset Bundles.");
                }

            }

            // Reclean directory before every build
            if (Directory.Exists(AddressableAssetSettings.kCCDBuildDataPath))
            {
                Directory.Delete(AddressableAssetSettings.kCCDBuildDataPath, true);
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
                var foundGroupType = GetGroupType(settings, schema);
                if (isCCDGroup(foundGroupType))
                {
                    return true;
                }
            }
            return false;
        }

        internal bool isCCDGroup(ProfileGroupType groupType)
        {

            if (groupType == null)
            {
                return false;
            }
            if (IsUsingManager(groupType))
            {
                return true;
            }
            return groupType.GroupTypePrefix.StartsWith("CCD");
        }

        internal async Task<bool> verifyTargetBucket(AddressableAssetSettings settings, AddressableAssetGroup group, BundledAssetGroupSchema schema)
        {
            AddressableAssetSettings.NullifyBundleFileIds(group);

            // if not using the manager try to lookup the bucket and verify it's not promotion only
            var existingGroupType = GetDefaultGroupType(ProfileDataSourceSettings.GetSettings());
            if (!IsUsingManager(settings, schema))
            {
                if (existingGroupType != null)
                {
                    var promotionOnly = IsPromotionOnlyBucket(settings, schema);
                    if (promotionOnly)
                    {
                        Debug.LogError("Cannot upload to Promotion Only bucket.");
                        return false;
                    }
                }
                return true;
            }

            // CcdManagedData.ConfigState.Override means it has been overriden by the customer at build time
            if (settings.m_CcdManagedData.State == CcdManagedData.ConfigState.Override)
            {
                return true;
            }

            // Target bucket should only be verified if Automatic profile type
            settings.m_CcdManagedData.EnvironmentId = ProfileDataSourceSettings.GetSettings().currentEnvironment.id;
            settings.m_CcdManagedData.EnvironmentName = ProfileDataSourceSettings.GetSettings().currentEnvironment.name;

            // existing automatic bucket loaded from cache
            if (existingGroupType != null)
            {
                var promotionOnly = IsPromotionOnlyBucket(settings, schema);
                if (promotionOnly)
                {
                    Debug.LogError("Cannot upload to Promotion Only bucket.");
                    return false;
                }
                settings.m_CcdManagedData.BucketId = existingGroupType
                    .GetVariableBySuffix($"{nameof(CcdBucket)}{nameof(CcdBucket.Id)}").Value;
                settings.m_CcdManagedData.Badge = existingGroupType
                    .GetVariableBySuffix($"{nameof(CcdBadge)}{nameof(CcdBadge.Name)}").Value;
            }
            // otherwise try to create
            else
            {
                try
                {
                    var api = CcdManagement.Instance;
                    var createdBucket = await CreateManagedBucket(api, ProfileDataSourceSettings.GetSettings().currentEnvironment.id,
                        EditorUserBuildSettings.activeBuildTarget.ToString());
                    if (createdBucket.Attributes.PromoteOnly)
                    {
                        Debug.LogError("Cannot upload to Promotion Only bucket.");
                        return false;
                    }
                    settings.m_CcdManagedData.BucketId = createdBucket.Id.ToString();
                    settings.m_CcdManagedData.Badge = "latest";
                }
                catch (Exception e)
                {
                    Debug.LogError(e.ToString());
                    return false;
                }
            }

            return true;
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
                Debug.LogWarning("Not downloading content state because 'Build Remote Catalog' is not checked in Addressable Asset Settings. This will disable content updates");
                return true;
            }

            var settings = input.AddressableSettings;
            var groupType = getRemoteCatalogGroupType(settings);
            if (groupType == null)
            {
                Debug.LogError("Could not find remote catalog paths");
                return false;
            }

            if (!this.isCCDGroup(groupType))
            {
                Debug.LogError("Content state could not be downloaded as the remote catalog is not targeting CCD");
                return false;
            }

            try
            {
                SetEnvironmentId(settings, groupType);
                var bucketId = GetBucketId(settings, groupType);
                if (bucketId == null)
                {
                    Debug.LogError("Content state could not be downloaded as no bucket was specified. This is populated for managed profiles in the VerifyBucket event.");
                    return false;
                }
                var api = CcdManagement.Instance;
                var ccdEntry = await GetEntryByPath(api, new Guid(bucketId), k_ContentStatePath);
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
                Debug.LogError(e.ToString());
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
                Debug.LogWarning(
                    "Skipping upload and release as no remote content was found to upload. Ensure you have at least one content group's 'Build & Load Path' set to Remote.");
                return false;
            }

            try
            {
                //Getting files
                Debug.Log("Creating and uploading entries");
                var startDirectory = new DirectoryInfo(AddressableAssetSettings.kCCDBuildDataPath);
                var buildData = CreateData(startDirectory);


                //Creating a release for each bucket
                var defaultEnvironmentId = ProfileDataSourceSettings.GetSettings().currentEnvironment.id;

                await UploadAndRelease(CcdManagement.Instance, input.AddressableSettings, defaultEnvironmentId, buildData);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                return false;
            }

            return true;
        }

        private ProfileGroupType getRemoteCatalogGroupType(AddressableAssetSettings settings)
        {
            var buildPath = settings.RemoteCatalogBuildPath;
            var loadPath = settings.RemoteCatalogLoadPath;
            var groupType = GetGroupType(settings, buildPath, loadPath);
            return groupType;

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
                Debug.LogWarning("Not uploading content state, because 'Build Remote Catalog' is not checked in Addressable Asset Settings. This will disable content updates");
                return true;
            }

            var settings = input.AddressableSettings;
            var groupType = getRemoteCatalogGroupType(settings);
            if (groupType == null)
            {
                Debug.LogError("Could not find remote catalog paths");
                return false;
            }

            if (!this.isCCDGroup(groupType))
            {
                Debug.LogError("Content state could not be uploaded as the remote catalog is not targeting CCD");
                return false;
            }

            try
            {
                SetEnvironmentId(settings, groupType);
                var bucketId = GetBucketId(settings, groupType);
                if (bucketId == null)
                {
                    Debug.LogError("Content state could not be uploaded as no bucket was specified. This is populated for managed profiles in the VerifyBucket event.");
                    return false;
                }
                var api = CcdManagement.Instance;

                var contentStatePath = Path.Combine(settings.GetContentStateBuildPath(), k_ContentStatePath);
                if (!File.Exists(contentStatePath))
                {
                    Debug.LogError($"Content state file is missing {contentStatePath}");
                    return false;
                }
                var contentHash = AddressableAssetUtility.GetMd5Hash(contentStatePath);

                using (var stream = File.OpenRead(contentStatePath))
                {

                    var entryModelOptions = new EntryModelOptions(k_ContentStatePath, contentHash, (int)stream.Length)
                    {
                        UpdateIfExists = true
                    };
                    var createdEntry = await api.CreateOrUpdateEntryByPathAsync(
                            new EntryByPathOptions(new Guid(bucketId), k_ContentStatePath),
                            entryModelOptions);

                    var uploadContentOptions = new UploadContentOptions(
                        new Guid(bucketId), createdEntry.Entryid, stream);
                    await api.UploadContentAsync(uploadContentOptions);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                return false;
            }
            return true;
        }

        internal bool IsPromotionOnlyBucket(AddressableAssetSettings settings, BundledAssetGroupSchema schema)
        {
            if (schema != null)
            {
                var foundGroupType = GetGroupType(settings, schema);
                if (foundGroupType != null && foundGroupType.GroupTypePrefix.StartsWith("CCD"))
                {
                    if (bool.Parse(foundGroupType.GetVariableBySuffix(nameof(CcdBucket.Attributes.PromoteOnly)).Value))
                    {
                        Debug.LogError("Cannot upload to Promotion Only bucket.");
                        return true;
                    }
                }

            }
            return false;
        }

        internal ProfileGroupType GetGroupType(AddressableAssetSettings settings, BundledAssetGroupSchema schema)
        {

            return GetGroupType(settings, schema.BuildPath, schema.LoadPath);
        }

        internal ProfileGroupType GetGroupType(AddressableAssetSettings settings, ProfileValueReference buildPath, ProfileValueReference loadPath)
        {
            // we need the "unresolved" value since we're tring to match it to its original type
            Debug.Log("Loading from active profile id " + settings.activeProfileId);
            var buildPathValue = settings.profileSettings.GetValueById(settings.activeProfileId, buildPath.Id);
            var loadPathValue = settings.profileSettings.GetValueById(settings.activeProfileId, loadPath.Id);
            Debug.Log(loadPathValue);
            if (buildPathValue == null || loadPathValue == null)
            {
                return null;
            }

            var groupType = new ProfileGroupType("temp");
            groupType.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kBuildPath, buildPathValue));
            groupType.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kLoadPath, loadPathValue));
            return ProfileDataSourceSettings.GetSettings().FindGroupType(groupType);
        }


        internal bool IsUsingManager(AddressableAssetSettings settings, BundledAssetGroupSchema schema)
        {
            var groupType = GetGroupType(settings, schema);
            return IsUsingManager(groupType);
        }

        internal bool IsUsingManager(ProfileGroupType groupType)
        {
            if (groupType == null)
            {
                return false;
            }
            return groupType.GroupTypePrefix == AddressableAssetSettings.CcdManagerGroupTypePrefix;
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

            CcdManagement.SetEnvironmentId(environmentId);
        }

        internal string GetBucketId(AddressableAssetSettings settings, ProfileGroupType groupType)
        {
            if (!IsUsingManager(groupType))
            {
                // if not using the manager load the bucketID from the group type
                return groupType.GetVariableBySuffix($"{nameof(CcdBucket)}{nameof(CcdBucket.Id)}").Value;
            }
            if (settings.m_CcdManagedData != null)
            {
                return settings.m_CcdManagedData.BucketId;
            }
            return null;
        }

        static ProfileGroupType GetDefaultGroupType(ProfileDataSourceSettings dataSourceSettings)
        {
            //Find existing bucketId
            var groupTypes = dataSourceSettings.GetGroupTypesByPrefix(string.Join(
                ProfileGroupType.k_PrefixSeparator.ToString(), "CCD", dataSourceSettings.currentEnvironment.projectGenesisId,
                dataSourceSettings.currentEnvironment.id));
            return groupTypes.FirstOrDefault(gt =>
                gt.GetVariableBySuffix($"{nameof(CcdBucket)}{nameof(CcdBucket.Name)}").Value ==
                EditorUserBuildSettings.activeBuildTarget.ToString());
        }

        async Task<CcdBucket> CreateManagedBucket(ICcdManagementServiceSdk api, string envId, string bucketName)
        {
            CcdBucket ccdBucket;
            try
            {
                CcdManagement.SetEnvironmentId(envId);
                ccdBucket = await api.CreateBucketAsync(
                    new CreateBucketOptions(bucketName));
            }
            catch (CcdManagementException e)
            {
                if (e.ErrorCode == CcdManagementErrorCodes.AlreadyExists)
                {
                    var buckets = await ProfileDataSourceSettings.GetAllBucketsAsync(envId);
                    ccdBucket = buckets.First(bucket =>
                        bucket.Value.Name == EditorUserBuildSettings.activeBuildTarget.ToString()).Value;
                }
                else
                {
                    throw;
                }
            }
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
            Debug.Log(description);
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
            Debug.Log($"[{progress}] {message}");
#endif
        }

        // Do not use Addressable.Log in this method as it may not be executed on the main thread
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
                        CcdManagement.SetEnvironmentId(defaultEnvironmentId);
                    }

                    foreach (var bucket in env.Buckets)
                    {
                        Guid bucketId;
                        var bucketIdString = bucket.Name == ProfileDataSourceSettings.MANAGED_BUCKET
                            ? settings.m_CcdManagedData.BucketId
                            : bucket.Name;
                        if (String.IsNullOrEmpty(bucketIdString))
                        {
                            Debug.LogError($"Invalid bucket ID for {bucket.Name}");
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
                                    var createdEntry = await api.CreateOrUpdateEntryByPathAsync(new EntryByPathOptions(bucketId, entryPath),
                                            entryModelOptions).ConfigureAwait(false);
                                    Debug.Log($"Created Entry {entryPath}");

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
                            Debug.Log("Creating release.");
                            var release = await api.CreateReleaseAsync(new CreateReleaseOptions(bucketId)
                            {
                                Entries = entries,
                                Notes = $"Automated release created for {badge.Name}"
                            }).ConfigureAwait(false);
                            Debug.Log($"Release {release.Releaseid} created.");

                            //Don't update latest badge (as it always updates)
                            if (badge.Name != "latest")
                            {
                                ReportProgress(progressId, total, "Updating badge");
                                Debug.Log("Updating badge.");
                                var badgeRes = await api.AssignBadgeAsync(new AssignBadgeOptions(bucketId, badge.Name, release.Releaseid))
                                    .ConfigureAwait(false);
                                Debug.Log($"Badge {badgeRes.Name} updated.");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
            finally
            {
                RemoveProgress(progressId);
            }
        }

    }
}
#endif

