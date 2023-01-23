#if ENABLE_CCD
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Ccd.Management;
using Unity.Services.Ccd.Management.Models;
using Unity.Services.Core;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
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
        public static CcdBuildEvents Instance { 
            get {
                if (s_Instance == null) {
                    s_Instance = new CcdBuildEvents();
                    s_Instance.RegisterNewBuildEvents();
                    s_Instance.RegisterUpdateBuildEvents();
                }
                return s_Instance;
            }
        }
        
        const string k_ContentStatePath = "addressables_content_state.bin";

        internal void RegisterNewBuildEvents() {
            OnPreBuildEvents += s_Instance.RefreshDataSources;
            OnPreBuildEvents += s_Instance.VerifyTargetBucket;
            OnPostBuildEvents += s_Instance.UploadContentState;
            OnPostBuildEvents += s_Instance.UploadAndRelease;
        }

        internal void RegisterUpdateBuildEvents() {
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
            if (isUpdate) {
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
                var shouldContinue =  await e.Invoke(input);
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
            if (isUpdate) {
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
                var e = (PostEvent) events.GetInvocationList()[i];
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
                    OnPreBuildEvents += (PreEvent) (t);
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
                    OnPostBuildEvents += (PostEvent) (t);
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
                    OnPreUpdateEvents += (PreEvent) (t);
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
                    OnPostUpdateEvents += (PostEvent) (t);
                }
        }

        /// <summary>
        /// Update the CCD data source settings.
        /// </summary>
        /// <param name="input">Addressables data builder context</param>
        /// <returns></returns>
        public async Task<bool> RefreshDataSources(AddressablesDataBuilderInput input) 
        {
	        try {
                await ProfileDataSourceSettings.UpdateCCDDataSourcesAsync(CloudProjectSettings.projectId, true);
            } catch(Exception e) {
                Debug.LogError(e.ToString());
                return false;
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
            var settings = input.AddressableSettings;
            var dataSourceSettings = ProfileDataSourceSettings.GetSettings();

            if (settings == null)
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

            AddressableAssetSettings.NullifyBundleFileIds(settings);

            //Processing groups, checking for promotion buckets
            var promotionOnly = GroupsContainPromotionOnlyBucket(settings);
            if (promotionOnly)
            {
                Debug.LogError("Cannot upload to Promotion Only bucket.");
                return false;
            }

            //Reclean directory before every build
            if (Directory.Exists(AddressableAssetSettings.kCCDBuildDataPath))
            {
                Directory.Delete(AddressableAssetSettings.kCCDBuildDataPath, true);
            }


            //CcdManagedData should only be configured if ConfigState is Default
            if (settings.m_CcdManagedData.State == CcdManagedData.ConfigState.Default)
            {
                var existingGroupType = GetDefaultGroupType(dataSourceSettings);
                settings.m_CcdManagedData.EnvironmentId = dataSourceSettings.currentEnvironment.id;
                settings.m_CcdManagedData.EnvironmentName = dataSourceSettings.currentEnvironment.name;

                if (existingGroupType != null)
                {
                    settings.m_CcdManagedData.BucketId = existingGroupType
                        .GetVariableBySuffix($"{nameof(CcdBucket)}{nameof(CcdBucket.Id)}").Value;
                    settings.m_CcdManagedData.Badge = existingGroupType
                        .GetVariableBySuffix($"{nameof(CcdBadge)}{nameof(CcdBadge.Name)}").Value;

                }
                else
                {
                    try
                    {
                        var api = CcdManagement.Instance;
                        var createdBucket = await CreateManagedBucket(api, dataSourceSettings.currentEnvironment.id,
                            EditorUserBuildSettings.activeBuildTarget.ToString());
                        settings.m_CcdManagedData.BucketId = createdBucket.Id.ToString();
                        settings.m_CcdManagedData.Badge = "latest";
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.ToString());
                        return false;
                    }
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
            try
            {
                var settings = input.AddressableSettings;
                var api = CcdManagement.Instance;
                var ccdEntry = await GetEntryByPath(api, new Guid(settings.m_CcdManagedData.BucketId), k_ContentStatePath);
                if (ccdEntry != null)
                {
                    var contentStream = await api.GetContentAsync(new EntryOptions(new Guid(settings.m_CcdManagedData.BucketId),  ccdEntry.Entryid));
                    
                    var contentStatePath = Path.Combine(settings.GetContentStateBuildPath(), k_ContentStatePath);
                    if (!Directory.Exists(contentStatePath))
                        Directory.CreateDirectory(Path.GetDirectoryName(contentStatePath));
                    else if (File.Exists(contentStatePath))
                        File.Delete(contentStatePath);
            
                    using var fileStream = File.Create(contentStatePath);
                    contentStream.CopyTo(fileStream);
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
        
        /// <summary>
        /// Upload addressables_content_state.bin to the CCD managed bucket.
        /// </summary>
        /// <param name="input">Addressables data builder context</param>
        /// <param name="result">Addressables build result</param>
        /// <returns></returns>
        public async Task<bool> UploadContentState(AddressablesDataBuilderInput input,
            AddressablesPlayerBuildResult result) 
        {
            try
            {
                var settings = input.AddressableSettings;
                var api = CcdManagement.Instance;
                
                var contentStatePath = Path.Combine(settings.GetContentStateBuildPath(), k_ContentStatePath);
                var contentHash = AddressableAssetUtility.GetMd5Hash(contentStatePath);
                
                using var stream = File.OpenRead(contentStatePath);

                var entryModelOptions = new EntryModelOptions(k_ContentStatePath, contentHash, (int)stream.Length)
                {
                    UpdateIfExists = true
                };
                var createdEntry = await api.CreateOrUpdateEntryByPathAsync(
                        new EntryByPathOptions(new Guid(settings.m_CcdManagedData.BucketId), k_ContentStatePath),
                        entryModelOptions).ConfigureAwait(false);
                
                var uploadContentOptions = new UploadContentOptions(
                    new Guid(settings.m_CcdManagedData.BucketId), createdEntry.Entryid, stream);
                await api.UploadContentAsync(uploadContentOptions).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                return false;
            }
            return true;
        }

        static bool GroupsContainPromotionOnlyBucket(AddressableAssetSettings settings)
        {
            foreach (var group in settings.groups)
            {
                if (group == null)
                    continue;

                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema != null)
                {
                    var buildPath = schema.BuildPath.GetValue(settings);
                    var loadPath = schema.LoadPath.GetValue(settings);
                    var groupType = new ProfileGroupType("temp");
                    groupType.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kBuildPath, buildPath));
                    groupType.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kLoadPath, loadPath));
                    var foundGroupType = ProfileDataSourceSettings.GetSettings().FindGroupType(groupType);
                    
                    if (foundGroupType != null && foundGroupType.GroupTypePrefix.StartsWith("CCD"))
                    {
                        if (bool.Parse(foundGroupType.GetVariableBySuffix(nameof(CcdBucket.Attributes.PromoteOnly)).Value))
                        {
                            Debug.LogError("Cannot upload to Promotion Only bucket.");
                            return true;
                        }
                    }

                }
            }
            return false;
        }
        
        static ProfileGroupType GetDefaultGroupType(ProfileDataSourceSettings dataSourceSettings)
        {
            //Find existing bucketId
            var groupTypes = dataSourceSettings.GetGroupTypesByPrefix(string.Join(
                ProfileGroupType.k_PrefixSeparator.ToString(), "CCD", CloudProjectSettings.projectId,
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
            Progress.Report (progressId, progress, message);
#else
            Debug.Log($"[{progress}] {message}");
#endif
        }

        // Do not use Addressable.Log in this method as it may not be executed on the main thread
        async Task UploadAndRelease(ICcdManagementServiceSdk api, AddressableAssetSettings settings, string defaultEnvironmentId, CcdBuildDataFolder buildData)
        {
            var progressId = StartProgress("Upload and Release");
            try {
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
                        bucketId = Guid.Parse(bucket.Name == ProfileDataSourceSettings.MANAGED_BUCKET
                            ? settings.m_CcdManagedData.BucketId
                            : bucket.Name);

                        foreach (var badge in bucket.Badges)
                        {
                            if (badge.Name == ProfileDataSourceSettings.MANAGED_BADGE)
                            {
                                badge.Name = "latest";
                            }
                            var entries = new List<CcdReleaseEntryCreate>();
                            var total = badge.Files.Count();
                            for(var i = 0; i < total; i++)
                            {
                                var file = badge.Files[i];
                                var contentHash = AddressableAssetUtility.GetMd5Hash(file.FullName);
                                using var stream = File.OpenRead(file.FullName);
                                var entryPath = file.Name;
                                var entryModelOptions = new EntryModelOptions(entryPath, contentHash, (int)stream.Length)
                                {
                                    UpdateIfExists = true
                                };
                                ReportProgress (progressId, (i + 1) / total, $"Creating Entry {entryPath}");
                                var createdEntry = await api.CreateOrUpdateEntryByPathAsync(new EntryByPathOptions(bucketId, entryPath),
                                        entryModelOptions).ConfigureAwait(false);
                                Debug.Log("Created Entry");
                                    
                                ReportProgress (progressId, (i + 1) / total, $"Uploading Entry {entryPath}");
                                var uploadContentOptions = new UploadContentOptions(bucketId, createdEntry.Entryid, stream);
                                await api.UploadContentAsync(uploadContentOptions).ConfigureAwait(false);
            
                                ReportProgress (progressId, (i + 1) / total, $"Uploaded Entry {entryPath}");
                                entries.Add(new CcdReleaseEntryCreate(createdEntry.Entryid, createdEntry.CurrentVersionid));
                            }
                            
                            // Add content_sate.bin to release if present
                            var contentStateEntry = await GetEntryByPath(api, bucketId, k_ContentStatePath);
                            if (contentStateEntry != null) 
                                entries.Add(new CcdReleaseEntryCreate(contentStateEntry.Entryid, contentStateEntry.CurrentVersionid));
                            
                            //Creating release
                            ReportProgress (progressId, total, "Creating release");
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
                                ReportProgress (progressId, total, "Updating badge");
                                Debug.Log("Updating badge.");
                                var badgeRes = await api.AssignBadgeAsync(new AssignBadgeOptions(bucketId, badge.Name, release.Releaseid))
                                    .ConfigureAwait(false);
                                Debug.Log($"Badge {badgeRes.Name} updated.");
                            }
                        }
                    }
                }
            } catch (Exception e)
            {
                Debug.LogError(e.ToString());
            } finally {
                 RemoveProgress(progressId);
            }
        }

    }
}
#endif

