using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
#if (UNITY_EDITOR && ENABLE_CCD)
using Unity.Services.Ccd.Management.Models;
#endif
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.TestTools;

#if ENABLE_CCD
namespace AddressableTests.OptionalPackages.Ccd
{
    public abstract class CcdManagerTests : AddressablesTestFixture
    {
        protected const string m_devEnvironmentName = "development";
        protected const string m_devEnvironmentId = "76f53158-98b5-4d10-bea5-6479be10030f";
        protected const string m_prodEnvironmentName = "production";
        protected const string m_prodEnvironmentId = "a32d487d-9028-4b64-a3b6-470b221106cb";
        protected const string m_devBucketId = "56c671eb-59b9-49f2-8495-791a117c8e71";
        protected const string m_prodBucketId = "2f4bd5d5-102d-4564-9951-999a3f3bcffb";
        protected const string m_myTestBadge = "my-test-badge";
        protected const string m_DevProfileName = "Dev Profile";
        protected const string m_ProdProfileName = "Prod Profile";
        protected string m_expectedBadge;
        protected string m_expectedBucketId;
        protected string m_expectedEnvironmentName;
        protected bool m_expectedConfigured;


#if UNITY_EDITOR
        protected virtual string TestProfileName { get; }

        internal override void Setup(AddressableAssetSettings settings, string tempAssetFolder)
        {
            SetExpected();
            CreateGroups(settings, TestProfileName, m_expectedEnvironmentName);
            settings.activeProfileId = settings.profileSettings.GetProfileId(TestProfileName);
        }

        [SetUp]
        public void Setup()
        {
            SetExpected();
        }

        protected virtual void SetExpected()
        {
            m_expectedBadge = m_myTestBadge;
            m_expectedBucketId = m_devBucketId;
            m_expectedEnvironmentName = m_devEnvironmentName;
            m_expectedConfigured = true;
        }

        protected void CreateGroups(AddressableAssetSettings settings, string profileName, string envName)
        {
            var profileId = settings.profileSettings.AddProfile(profileName, null);
            settings.profileSettings.SetValue(profileId, AddressableAssetSettings.kRemoteBuildPath, $"{AddressableAssetSettings.kCCDBuildDataPath}/ManagedEnvironment/ManagedBucket/ManagedBadge");
            settings.profileSettings.SetValue(profileId, AddressableAssetSettings.kRemoteLoadPath, $"https://{CloudProjectSettings.projectId}.client-api.unity3dusercontent.com/client_api/v1/environments/{{CcdManager.EnvironmentName}}/buckets/{{CcdManager.BucketId}}/release_by_badge/{{CcdManager.Badge}}/entry_by_path/content/?path=");
            settings.profileSettings.CreateValue(ProfileDataSourceSettings.ENVIRONMENT_NAME, "");
            settings.profileSettings.SetValue(profileId, ProfileDataSourceSettings.ENVIRONMENT_NAME, envName);


            ProfileDataSourceSettings.GetSettings().environments.Add(new ProfileDataSourceSettings.Environment
            {
                name= m_devEnvironmentName,
                id = m_devEnvironmentId,
                isDefault = false,
                projectGenesisId = CloudProjectSettings.projectId,
                projectId = CloudProjectSettings.projectId
            });
            ProfileDataSourceSettings.GetSettings().environments.Add(new ProfileDataSourceSettings.Environment
            {
                name= m_prodEnvironmentName,
                id = m_prodEnvironmentId,
                isDefault = true,
                projectGenesisId = CloudProjectSettings.projectId,
                projectId = CloudProjectSettings.projectId
            });
            ProfileDataSourceSettings.GetSettings().profileGroupTypes.Add(CreateGroupType(m_devEnvironmentId, m_devEnvironmentName, m_devBucketId, "false", m_myTestBadge));
            ProfileDataSourceSettings.GetSettings().profileGroupTypes.Add(CreateGroupType(m_prodEnvironmentId, m_prodEnvironmentName, m_prodBucketId, "false", m_myTestBadge));
        }

        private ProfileGroupType CreateGroupType(string envId, string envName, string bucketId, string isPromotionOnly, string badgeName)
        {
            var bucketName = EditorUserBuildSettings.activeBuildTarget.ToString();

            var groupType =
                new ProfileGroupType($"CCD{ProfileGroupType.k_PrefixSeparator}{CloudProjectSettings.projectId}{ProfileGroupType.k_PrefixSeparator}{envId}{ProfileGroupType.k_PrefixSeparator}{bucketId}{ProfileGroupType.k_PrefixSeparator}{badgeName}");
            groupType.AddVariable(new ProfileGroupType.GroupTypeVariable($"{nameof(CcdBucket)}{nameof(CcdBucket.Name)}", bucketName));
            groupType.AddVariable(new ProfileGroupType.GroupTypeVariable($"{nameof(CcdBucket)}{nameof(CcdBucket.Id)}", bucketId));
            groupType.AddVariable(new ProfileGroupType.GroupTypeVariable($"{nameof(CcdBadge)}{nameof(CcdBadge.Name)}", badgeName));
            groupType.AddVariable(new ProfileGroupType.GroupTypeVariable(nameof(CcdBucket.Attributes.PromoteOnly), isPromotionOnly));

            //Adding environment stub here
            groupType.AddVariable(new ProfileGroupType.GroupTypeVariable($"{nameof(ProfileDataSourceSettings.Environment)}{nameof(ProfileDataSourceSettings.Environment.name)}", envName));
            groupType.AddVariable(new ProfileGroupType.GroupTypeVariable($"{nameof(ProfileDataSourceSettings.Environment)}{nameof(ProfileDataSourceSettings.Environment.id)}", envId));

            string buildPath = $"{AddressableAssetSettings.kCCDBuildDataPath}/{envName}/{bucketId}/latest";
            groupType.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kBuildPath, buildPath));

            string loadPath =
                $"https://{CloudProjectSettings.projectId}{ProfileDataSourceSettings.m_CcdClientBasePath}/client_api/v1/environments/{envName}/buckets/{bucketId}/release_by_badge/latest/entry_by_path/content/?path=";
            groupType.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kLoadPath, loadPath));
            return groupType;
        }
#endif

        internal static void ResetCcdManager()
        {
            CcdManager.BucketId = null;
            CcdManager.Badge = null;
            CcdManager.EnvironmentName = null;
            Assert.IsFalse(CcdManager.IsConfigured());
        }

        [TearDown]
        public void TearDown()
        {
            ResetCcdManager();
            ProfileDataSourceSettings.GetSettings().profileGroupTypes.Clear();
        }

        protected override IEnumerator InitAddressables()
        {
            // NOOP - we need to do this in our tests
            return null;
        }



        [UnityTest]
        public IEnumerator TestDefault()
        {
            yield return m_Addressables.InitializeAsync(m_RuntimeSettingsPath, null, false);
            Debug.Log($"{CcdManager.Badge} {CcdManager.BucketId} {CcdManager.EnvironmentName}");
            Assert.AreEqual(m_expectedBadge, CcdManager.Badge);
            Assert.AreEqual(m_expectedBucketId, CcdManager.BucketId);
            Assert.AreEqual(m_expectedEnvironmentName, CcdManager.EnvironmentName);
            Assert.AreEqual(m_expectedConfigured, CcdManager.IsConfigured());
        }



    }

#if UNITY_EDITOR
    // Fast mode currently doesn't set the CcdManager values so this test does not work, it makes sense
    // as it uses the AssetDatabase rather than actual AssetBundles, but I still wonder about the ramifications
    // of not setting this.
    // class CcdManagerTests_FastMode : CcdManagerTests
    // {
    //     protected override TestBuildScriptMode BuildScriptMode
    //     {
    //         get { return TestBuildScriptMode.Fast; }
    //     }
    // }

    class CcdManagerTests_PackedPlaymodeMode_DevProfile : CcdManagerTests
    {
        protected override TestBuildScriptMode BuildScriptMode
        {
            get { return TestBuildScriptMode.PackedPlaymode; }
        }

        protected override string TestProfileName => m_DevProfileName;
    }

    class CcdManagerTests_PackedPlaymodeMode_ProdProfile : CcdManagerTests
    {
        protected override TestBuildScriptMode BuildScriptMode
        {
            get { return TestBuildScriptMode.PackedPlaymode; }
        }

        // override SetExpected
        protected override void SetExpected()
        {
            m_expectedBadge = m_myTestBadge;
            m_expectedBucketId = m_prodBucketId;
            m_expectedEnvironmentName = m_prodEnvironmentName;
            m_expectedConfigured = true;
        }

        protected override string TestProfileName => m_ProdProfileName;
    }
#endif

    [UnityPlatform(exclude = new[] {RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor})]
    class CcdManagerTests_PackedMode_DevProfile : CcdManagerTests
    {
        protected override TestBuildScriptMode BuildScriptMode
        {
            get { return TestBuildScriptMode.Packed; }
        }
    }

    class CcdManagerTests_NoBuild
    {
        [TearDown]
        public void TearDown()
        {
            CcdManagerTests.ResetCcdManager();
        }
        public class CcdManagerTestCase
        {
            public string EnvironmentName;
            public string BucketId;
            public string Badge;
            public bool IsConfigured;
        }

        static CcdManagerTestCase[] testCases = new []
        {
            new CcdManagerTestCase {  EnvironmentName = "", BucketId = "", Badge = "", IsConfigured = false },
            new CcdManagerTestCase {  EnvironmentName = "production", BucketId = "", Badge = "", IsConfigured = false },
            new CcdManagerTestCase {  EnvironmentName = "production", BucketId = "96797d04-6a18-4924-bda8-4c508537d009", Badge = "", IsConfigured = false },
            new CcdManagerTestCase {  EnvironmentName = "production", BucketId = "96797d04-6a18-4924-bda8-4c508537d009", Badge = "latest", IsConfigured = true },
            new CcdManagerTestCase {  EnvironmentName = null, BucketId = null, Badge = null, IsConfigured = false },
            new CcdManagerTestCase {  EnvironmentName = "production", BucketId = null, Badge = null, IsConfigured = false },
            new CcdManagerTestCase {  EnvironmentName = "production", BucketId = "96797d04-6a18-4924-bda8-4c508537d009", Badge = null, IsConfigured = false },
            new CcdManagerTestCase {  EnvironmentName = "production", BucketId = "96797d04-6a18-4924-bda8-4c508537d009", Badge = "latest", IsConfigured = true },
        };
        [Test]
        public void TestIsConfigured([ValueSource(nameof(testCases))] CcdManagerTestCase testCase)
        {
            CcdManager.EnvironmentName = testCase.EnvironmentName;
            CcdManager.BucketId = testCase.BucketId;
            CcdManager.Badge = testCase.Badge;
            Assert.AreEqual(testCase.IsConfigured, CcdManager.IsConfigured());
        }
    }
}
#endif
