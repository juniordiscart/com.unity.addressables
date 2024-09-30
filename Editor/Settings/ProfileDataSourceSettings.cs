using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Text;
using System.Net.Http;
using UnityEditor.AddressableAssets.Build;

#if (ENABLE_CCD && UNITY_2019_4_OR_NEWER)
using Unity.Services.Core;
using Unity.Services.Ccd.Management;
using Unity.Services.Ccd.Management.Http;
using Unity.Services.Ccd.Management.Models;
using UnityEditor.AddressableAssets.Build;
#endif

namespace UnityEditor.AddressableAssets.Settings
{
    /// <summary>
    /// Scriptable Object that holds data source setting information for the profile data source dropdown window
    /// </summary>
    public class ProfileDataSourceSettings : ScriptableObject, ISerializationCallbackReceiver
    {
        const string DEFAULT_PATH = "Assets/AddressableAssetsData";
        const string DEFAULT_NAME = "ProfileDataSourceSettings";
        const string CONTENT_RANGE_HEADER = "Content-Range";
        static string DEFAULT_SETTING_PATH = $"{DEFAULT_PATH}/{DEFAULT_NAME}.asset";
        internal const string ENVIRONMENT_NAME = "EnvironmentName";

#if ENABLE_CCD
        internal const string MANAGED_ENVIRONMENT = "ManagedEnvironment";
        internal const string MANAGED_BUCKET = "ManagedBucket";
        internal const string MANAGED_BADGE = "ManagedBadge";
#endif

        // paths
        internal static string m_CloudEnvironment = "production";
        internal static string m_GenesisBasePath = "https://api-staging.unity.com";
        internal static string m_CcdClientBasePath = ".client-api-stg.unity3dusercontent.com";
        internal static string m_DashboardBasePath = "https://staging.dashboard.unity3d.com";
        internal static string m_ServicesBasePath = "https://staging.services.unity.com";

        [InitializeOnLoadMethod]
        internal static void InitializeCloudEnvironment()
        {
            var cloudEnvironment = GetCloudEnvironment();
            switch (cloudEnvironment)
            {
                case "staging":
#if ENABLE_CCD
                    CcdManagement.SetBasePath("https://staging.services.unity.com");
#endif
                    m_GenesisBasePath = "https://api-staging.unity.com";
                    m_CcdClientBasePath = ".client-api-stg.unity3dusercontent.com";
                    m_DashboardBasePath = "https://staging.dashboard.unity3d.com";
                    m_ServicesBasePath = "https://staging.services.unity.com";
                    break;
                default:
#if ENABLE_CCD
                    CcdManagement.SetBasePath("https://services.unity.com");
#endif
                    m_GenesisBasePath = "https://api.unity.com";
                    m_CcdClientBasePath = ".client-api.unity3dusercontent.com";
                    m_DashboardBasePath = "https://dashboard.unity3d.com";
                    m_ServicesBasePath = "https://services.unity.com";
                    break;
            }
        }

        const string EnvironmentArg = "-cloudEnvironment";

        internal static string GetCloudEnvironment()
        {
            try
            {
                var commandLineArgs = System.Environment.GetCommandLineArgs();
                var cloudEnvironmentIndex = Array.IndexOf(commandLineArgs, EnvironmentArg);

                if (cloudEnvironmentIndex >= 0 && cloudEnvironmentIndex <= commandLineArgs.Length - 2)
                {
                    return commandLineArgs[cloudEnvironmentIndex + 1];
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return null;
        }


        /// <summary>
        /// Group types that exist within the settings object
        /// </summary>
        [SerializeField]
        public List<ProfileGroupType> profileGroupTypes = new List<ProfileGroupType>();

        [SerializeField]
        internal List<Environment> environments = new List<Environment>();

        [SerializeField]
        private Environment currentEnvironment;

        /// <summary>
        /// Creates, if needed, and returns the profile data source settings for the project
        /// </summary>
        /// <param name="path">Desired path to put settings</param>
        /// <param name="settingName">Desired name for settings</param>
        /// <returns></returns>
        public static ProfileDataSourceSettings Create(string path = null, string settingName = null)
        {
            ProfileDataSourceSettings aa;
            var assetPath = DEFAULT_SETTING_PATH;

            if (path != null && settingName != null)
            {
                assetPath = $"{path}/{settingName}.asset";
            }

            aa = AssetDatabase.LoadAssetAtPath<ProfileDataSourceSettings>(assetPath);
            if (aa == null)
            {
                Directory.CreateDirectory(path != null ? path : DEFAULT_PATH);
                aa = CreateInstance<ProfileDataSourceSettings>();
                AssetDatabase.CreateAsset(aa, assetPath);
                aa = AssetDatabase.LoadAssetAtPath<ProfileDataSourceSettings>(assetPath);
                aa.profileGroupTypes = CreateDefaultGroupTypes();
                EditorUtility.SetDirty(aa);
            }

            return aa;
        }

        /// <summary>
        /// Gets the profile data source settings for the project
        /// </summary>
        /// <param name="path"></param>
        /// <param name="settingName"></param>
        /// <returns></returns>
        public static ProfileDataSourceSettings GetSettings(string path = null, string settingName = null)
        {
            ProfileDataSourceSettings aa;
            var assetPath = DEFAULT_SETTING_PATH;

            if (path != null && settingName != null)
            {
                assetPath = $"{path}/{settingName}.asset";
            }

            aa = AssetDatabase.LoadAssetAtPath<ProfileDataSourceSettings>(assetPath);
            if (aa == null)
                return Create();
            return aa;
        }

        /// <summary>
        /// Creates a list of default group types that are automatically added on ProfileDataSourceSettings object creation
        /// </summary>
        /// <returns>List of ProfileGroupTypes: Built-In and Editor Hosted</returns>
        public static List<ProfileGroupType> CreateDefaultGroupTypes() => new List<ProfileGroupType>
        {
            CreateBuiltInGroupType(),
            CreateEditorHostedGroupType(),
#if ENABLE_CCD
            CreateCcdManagerGroupType()
#endif
        };

        static ProfileGroupType CreateBuiltInGroupType()
        {
            ProfileGroupType defaultBuiltIn = new ProfileGroupType(AddressableAssetSettings.LocalGroupTypePrefix);
            defaultBuiltIn.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kBuildPath, AddressableAssetSettings.kLocalBuildPathValue));
            defaultBuiltIn.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kLoadPath, AddressableAssetSettings.kLocalLoadPathValue));
            return defaultBuiltIn;
        }

        static ProfileGroupType CreateEditorHostedGroupType()
        {
            ProfileGroupType defaultRemote = new ProfileGroupType(AddressableAssetSettings.EditorHostedGroupTypePrefix);
            defaultRemote.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kBuildPath, AddressableAssetSettings.kRemoteBuildPathValue));
            defaultRemote.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kLoadPath, AddressableAssetSettings.RemoteLoadPathValue));
            return defaultRemote;
        }

#if ENABLE_CCD
        static ProfileGroupType CreateCcdManagerGroupType()
        {
            string buildPath = $"{AddressableAssetSettings.kCCDBuildDataPath}/{MANAGED_ENVIRONMENT}/{MANAGED_BUCKET}/{MANAGED_BADGE}";
            string loadPath =
 $"https://{CloudProjectSettings.projectId}{m_CcdClientBasePath}/client_api/v1/environments/{{CcdManager.EnvironmentName}}/buckets/{{CcdManager.BucketId}}/release_by_badge/{{CcdManager.Badge}}/entry_by_path/content/?path=";
            ProfileGroupType defaultCcdManager = new ProfileGroupType(AddressableAssetSettings.CcdManagerGroupTypePrefix);
            defaultCcdManager.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kBuildPath, buildPath));
            defaultCcdManager.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kLoadPath, loadPath));
            defaultCcdManager.AddVariable(new ProfileGroupType.GroupTypeVariable(ENVIRONMENT_NAME, "production"));
            return defaultCcdManager;
        }
#endif

        /// <summary>
        /// Given a valid profileGroupType, searches the settings and returns, if exists, the profile group type
        /// </summary>
        /// <param name="groupType"></param>
        /// <returns>ProfileGroupType if found, null otherwise</returns>
        public ProfileGroupType FindGroupType(ProfileGroupType groupType)
        {
            ProfileGroupType result = null;
            if (!groupType.IsValidGroupType())
            {
                throw new ArgumentException("Group Type is not valid. Group Type must include a build path and load path variables");
            }

            var buildPath = groupType.GetVariableBySuffix(AddressableAssetSettings.kBuildPath);
            var loadPath = groupType.GetVariableBySuffix(AddressableAssetSettings.kLoadPath);
            foreach (ProfileGroupType settingsGroupType in profileGroupTypes)
            {
                var foundBuildPath = settingsGroupType.ContainsVariable(buildPath);
                var foundLoadPath = settingsGroupType.ContainsVariable(loadPath);
                if (foundBuildPath && foundLoadPath)
                {
                    result = settingsGroupType;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Retrieves a list of ProfileGroupType that matches the given prefix
        /// </summary>
        /// <param name="prefix">prefix to search by</param>
        /// <returns>List of ProfileGroupType</returns>
        public List<ProfileGroupType> GetGroupTypesByPrefix(string prefix)
        {
            return profileGroupTypes.Where((groupType) => groupType.GroupTypePrefix.StartsWith(prefix, StringComparison.Ordinal)).ToList();
        }

#if (ENABLE_CCD && UNITY_2019_4_OR_NEWER)
        /// <summary>
        /// Updates the CCD buckets and badges with the data source settings
        /// </summary>
        /// <param name="projectId">Project Id connected to Unity Services</param>
        /// <param name="showInfoLog">Whether or not to show debug logs or not</param>
        /// <returns>List of ProfileGroupType</returns>
        public static async Task<List<ProfileGroupType>> UpdateCCDDataSourcesAsync(string projectId, bool showInfoLog)
        {
            var settings = GetSettings();

            if (showInfoLog)
            {
                Addressables.Log("Syncing CCD Environments, Buckets, and Badges.");
            }

            settings.profileGroupTypes.Clear();

            var environments = await GetEnvironments();

            if (showInfoLog)
            {
                EditorUtility.DisplayProgressBar("Syncing Profile Data Sources", "Fetching Environments", 0);
                Addressables.Log($"Successfully fetched {environments.Count} environments.");
            }
            settings.profileGroupTypes.AddRange(CreateDefaultGroupTypes());

            try
            {
                var envProgress = 1;
                foreach (var environment in environments)
                {
                    CcdBuildEvents.Instance.ConfigureCcdManagement(AddressableAssetSettingsDefaultObject.Settings, environment.id);
                    var bucketDictionary = await GetAllBucketsAsync();
                    var bucketProgress = 1;
                    foreach (var kvp in bucketDictionary)
                    {

                        var bucket = kvp.Value;
                        if (showInfoLog)
                        {
                            EditorUtility.DisplayProgressBar($"Syncing Environment: {environment.name} ({envProgress} of {environments.Count})", $"Loading {bucket.Name}", (bucketProgress / (float)bucketDictionary.Count));
                        }

                        var badges = await GetAllBadgesAsync(bucket.Id.ToString());
                        AddGroupTypeForRemoteBucket(projectId, environment.id, environment.name, bucket, badges);
                        bucketProgress++;
                    }
                    envProgress++;
                }

                settings.environments = environments.ToList();
                if (showInfoLog) Addressables.Log("Successfully synced CCD Buckets and Badges.");
                EditorUtility.SetDirty(settings);
                AddressableAssetUtility.OpenAssetIfUsingVCIntegration(settings);
            }
            catch (CcdManagementException e)
            {
                throw e;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return settings.profileGroupTypes;
        }

        internal static void AddGroupTypeForRemoteBucket(string projectId, string environmentId, string environmentName, CcdBucket bucket, List<CcdBadge> badges)
        {
            var settings = GetSettings();
            if (badges.Count == 0) badges.Add(new CcdBadge(name: "latest"));
            foreach (var badge in badges)
            {
                var groupType =
                    new ProfileGroupType(
                        $"CCD{ProfileGroupType.k_PrefixSeparator}{projectId}{ProfileGroupType.k_PrefixSeparator}{environmentId}{ProfileGroupType.k_PrefixSeparator}{bucket.Id}{ProfileGroupType.k_PrefixSeparator}{badge.Name}");
                groupType.AddVariable(new ProfileGroupType.GroupTypeVariable($"{nameof(CcdBucket)}{nameof(CcdBucket.Name)}", bucket.Name));
                groupType.AddVariable(new ProfileGroupType.GroupTypeVariable($"{nameof(CcdBucket)}{nameof(CcdBucket.Id)}", bucket.Id.ToString()));
                groupType.AddVariable(new ProfileGroupType.GroupTypeVariable($"{nameof(CcdBadge)}{nameof(CcdBadge.Name)}", badge.Name));
                groupType.AddVariable(new ProfileGroupType.GroupTypeVariable(nameof(CcdBucket.Attributes.PromoteOnly), bucket.Attributes.PromoteOnly.ToString()));

                //Adding environment stub here
                groupType.AddVariable(new ProfileGroupType.GroupTypeVariable($"{nameof(Environment)}{nameof(Environment.name)}", environmentName));
                groupType.AddVariable(new ProfileGroupType.GroupTypeVariable($"{nameof(Environment)}{nameof(Environment.id)}", environmentId));

                string buildPath = $"{AddressableAssetSettings.kCCDBuildDataPath}/{environmentId}/{bucket.Id}/{badge.Name}";
                groupType.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kBuildPath, buildPath));

                string loadPath =
                    $"https://{projectId}{m_CcdClientBasePath}/client_api/v1/environments/{environmentName}/buckets/{bucket.Id}/release_by_badge/{badge.Name}/entry_by_path/content/?path=";
                groupType.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kLoadPath, loadPath));

                settings.profileGroupTypes.Add(groupType);
            }
    }

        internal static async Task<Dictionary<Guid, CcdBucket>> GetAllBucketsAsync()
        {
            int page = 1;
            bool loop = true;
            List<CcdBucket> buckets = new List<CcdBucket>();
            do
            {
                try
                {
                    var listBuckets = await CcdManagement.Instance.ListBucketsAsync(new PageOptions()
                    {
                        Page = page
                    });
                    buckets.AddRange(listBuckets);
                    page++;
                }
                catch (CcdManagementException e)
                {
                    if (e.ErrorCode == CcdManagementErrorCodes.OutOfRange)
                    {
                        loop = false;
                    }
                    else if (e.ErrorCode == CommonErrorCodes.Forbidden)
                    {
                        throw new CcdManagementException(e.ErrorCode, "Unactivated Org. Please activate your organization via the Unity Dashboard");
                    }
                    else
                    {
                        throw e;
                    }
                }
            } while (loop);
            return buckets.ToDictionary(kvp => kvp.Id, kvp => kvp);
        }

        internal static async Task<List<CcdBadge>> GetAllBadgesAsync(string bucketId)
        {
            int page = 1;
            bool loop = true;
            List<CcdBadge> badges = new List<CcdBadge>();
            do
            {
                try
                {
                    var listBadges = await CcdManagement.Instance.ListBadgesAsync(Guid.Parse(bucketId), new PageOptions()
                    {
                        Page = page
                    });
                    badges.AddRange(listBadges);
                    page++;
                }
                catch (CcdManagementException e)
                {
                    if (e.ErrorCode == CcdManagementErrorCodes.OutOfRange)
                    {
                        loop = false;
                    }
                    else
                    {
                        throw e;
                    }
                }
            } while (loop);
            return badges;
        }

		internal static async Task<List<Environment>> GetEnvironments()
        {

            var projectId = CloudProjectSettings.projectId;
            var authToken = await GetAuthToken();
            using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", authToken);
                var response = await client.GetAsync(String.Format("{0}/api/unity/legacy/v1/projects/{1}/environments", m_ServicesBasePath, projectId));
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(response.ReasonPhrase);
                }
                var data = await response.Content.ReadAsStringAsync();
                var envs = JsonUtility.FromJson<Environments>(data);
                var envList = envs.results.ToList();
                return envList;
            }
        }

        private static async Task<string> GetAuthToken()
        {
            using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
            {
                var jsonString = JsonUtility.ToJson(new Token() { token = CloudProjectSettings.accessToken });
                var url = $"{m_ServicesBasePath}/api/auth/v1/genesis-token-exchange/unity/";
                var clientResponse = await client.PostAsync(url, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                if (!clientResponse.IsSuccessStatusCode)
                {
                    throw new Exception(clientResponse.ReasonPhrase);
                }
                var token = JsonUtility.FromJson<Token>(await clientResponse.Content.ReadAsStringAsync()).token;
                return $"Bearer {token}";
            }
        }

        internal void SetEnvironmentById(AddressableAssetProfileSettings profileSettings, string profileId, string environmentId)
        {
            var environmentName = profileSettings.CreateValue(ENVIRONMENT_NAME, "");

            Environment env = environments.FirstOrDefault(x => x.id == environmentId);
            if (env != null)
            {
                profileSettings.SetValue(profileId, ENVIRONMENT_NAME, env.name);
            }
            else
            {
                throw new Exception($"Unable to find environment by id {environmentId}");
            }
        }

        internal string GetEnvironmentName(AddressableAssetProfileSettings profileSettings, string profileId)
        {
            var profileEnvironmentName = profileSettings.GetValueByName(profileId, ENVIRONMENT_NAME);
            if (profileEnvironmentName != null)
            {
                return profileEnvironmentName;
            }

            // this is here for backwards compatability
            if (currentEnvironment != null && !String.IsNullOrEmpty(currentEnvironment.name))
            {
                profileSettings.CreateValue(ENVIRONMENT_NAME, "");
                profileSettings.SetValue(profileId, ENVIRONMENT_NAME, currentEnvironment.name);
                return currentEnvironment.name;
            }
            throw new Exception($"Unable to find environment for profile {profileSettings.GetProfile(profileId).profileName}.");
        }

        internal string GetEnvironmentId(AddressableAssetProfileSettings profileSettings, string profileId)
        {
            var environmentName = GetEnvironmentName(profileSettings, profileId);
            Environment env = environments.Where(x => x.name == environmentName).FirstOrDefault();
            if (env == null)
            {
                throw new Exception($"Unable to find remote environment {environmentName}.");
            }

            return env.id;
        }
#endif
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Ensure static Group types have the correct string
            // Local
            var types = GetGroupTypesByPrefix(AddressableAssetSettings.LocalGroupTypePrefix);
            if (types == null || types.Count == 0)
                profileGroupTypes.Add(CreateBuiltInGroupType());
            else
            {
                types[0].AddOrUpdateVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kBuildPath,
                    AddressableAssetSettings.kLocalBuildPathValue));
                types[0].AddOrUpdateVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kLoadPath,
                    AddressableAssetSettings.kLocalLoadPathValue));
            }

            // Editor Hosted
            types = GetGroupTypesByPrefix(AddressableAssetSettings.EditorHostedGroupTypePrefix);
            if (types.Count == 0)
                profileGroupTypes.Add(CreateEditorHostedGroupType());
            else
            {
                types[0].AddOrUpdateVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kBuildPath,
                    AddressableAssetSettings.kRemoteBuildPathValue));
                types[0].AddOrUpdateVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kLoadPath,
                    AddressableAssetSettings.RemoteLoadPathValue));
            }
        }

        /// <summary>
        /// Access Token
        /// </summary>
        private class Token
        {
            [SerializeField]
            public string token;
        }

        /// <summary>
        /// Environment Wrapper Object
        /// </summary>
        internal class Environments
        {
            [SerializeField]
            public List<Environment> results;
        }

        /// <summary>
        /// Identity API Environment Object
        /// </summary>
        [Serializable]
        internal class Environment
        {
            [SerializeField]
            public string id;

            [SerializeField]
            public string projectId;

            [SerializeField]
            public string projectGenesisId;

            [SerializeField]
            public string name;

            [SerializeField]
            public bool isDefault;
        }
    }
}
