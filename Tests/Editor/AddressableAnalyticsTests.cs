using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets.Tests;
using UnityEngine;

namespace Tests.Editor
{
    public class AddressableAnalyticsTests
    {
        private AddressablesDataBuilderInput builderInput;
        private AddressableAssetSettings testSettings;
        private List<AddressableAssetGroup> groupList;

        private int numberOfGroupsCompressedWithLZ4;
        private int numberOfGroupsCompressedWithLZMA;
        private int numberOfGroupsUncompressed;

        private int numberOfGroupsPackedTogether;
        private int numberOfGroupsPackedSeparately;
        private int numberOfGroupsPackedTogetherByLabel;

        private int numberOfGroups;

        private AddressableAssetGroup CreateGroupWithCompressionType(string groupName,
            BundledAssetGroupSchema.BundleCompressionMode compressionMode)
        {
            var group = ScriptableObject.CreateInstance<AddressableAssetGroup>();
            group.Name = "AddressableAnalyticsTestingGroup" + groupName;
            var schema = group.AddSchema<BundledAssetGroupSchema>();
            group.Schemas.Add(schema);
            schema.Compression = compressionMode;
            return group;
        }

        private void AddSchemaWithPackingAndBundleMode(AddressableAssetGroup group, BundledAssetGroupSchema.BundleCompressionMode compressionMode,
            BundledAssetGroupSchema.BundlePackingMode packingMode)
        {
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            schema.Compression = compressionMode;
            schema.BundleMode = packingMode;
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            //Initialize Settings Object
            testSettings = AddressableAssetSettings.Create("AddressableAnalyticsTests", "AddressableAnalyticsTestSettings", false, false);
            var buildScriptPackedMode = ScriptableObject.CreateInstance<BuildScriptPackedMode>();
            var buildScriptPackedPlayMode = ScriptableObject.CreateInstance<BuildScriptPackedPlayMode>();
            var buildScriptVirtualMode = ScriptableObject.CreateInstance<BuildScriptVirtualMode>();
            var buildScriptFastMode = ScriptableObject.CreateInstance<BuildScriptFastMode>();
            var customBuildScript = ScriptableObject.CreateInstance<BuildScriptTests.BuildScriptTestClass>();

            testSettings.AddDataBuilder(buildScriptPackedMode);
            testSettings.AddDataBuilder(buildScriptFastMode);
            testSettings.AddDataBuilder(buildScriptVirtualMode);
            testSettings.AddDataBuilder(buildScriptPackedPlayMode);
            testSettings.AddDataBuilder(customBuildScript);

            testSettings.ActivePlayerDataBuilderIndex = 0;
            testSettings.ActivePlayModeDataBuilderIndex = 0;

            var group0 = testSettings.CreateGroup("LZ4AndPackedTogether", false, false, false, null, typeof(BundledAssetGroupSchema));
            var group1 = testSettings.CreateGroup("LZ4AndPackedTogether2", false, false, false, null, typeof(BundledAssetGroupSchema));
            var group2 = testSettings.CreateGroup("LZMAAndPackedTogetherByLabel", false, false, false, null, typeof(BundledAssetGroupSchema));
            var group3 = testSettings.CreateGroup("LZMAAndPackedTogetherByLabel2", false, false, false, null, typeof(BundledAssetGroupSchema));
            var group4 = testSettings.CreateGroup("LZMAAndPackedTogetherByLabel3", false, false, false, null, typeof(BundledAssetGroupSchema));
            var group5 = testSettings.CreateGroup("UncompressedAndPackedSeparately", false, false, false, null, typeof(BundledAssetGroupSchema));
            //Create Groups with schemas

            AddSchemaWithPackingAndBundleMode(group0, BundledAssetGroupSchema.BundleCompressionMode.LZ4, BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            AddSchemaWithPackingAndBundleMode(group1, BundledAssetGroupSchema.BundleCompressionMode.LZ4, BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            AddSchemaWithPackingAndBundleMode(group2, BundledAssetGroupSchema.BundleCompressionMode.LZMA, BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel);
            AddSchemaWithPackingAndBundleMode(group3, BundledAssetGroupSchema.BundleCompressionMode.LZMA, BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel);
            AddSchemaWithPackingAndBundleMode(group4, BundledAssetGroupSchema.BundleCompressionMode.LZMA, BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel);
            AddSchemaWithPackingAndBundleMode(group5, BundledAssetGroupSchema.BundleCompressionMode.Uncompressed, BundledAssetGroupSchema.BundlePackingMode.PackSeparately);

            builderInput = new AddressablesDataBuilderInput(testSettings);
            numberOfGroupsUncompressed = 1;
            numberOfGroupsCompressedWithLZ4 = 2;
            numberOfGroupsCompressedWithLZMA = 3;

            numberOfGroupsPackedSeparately = 1;
            numberOfGroupsPackedTogether = 2;
            numberOfGroupsPackedTogetherByLabel = 3;

            numberOfGroups = 6;
        }

        [TearDown]
        public void RemoveAnyAssetEntriesCreated()
        {
            if (testSettings.groups.Sum(x => x.entries.Count) > 0)
                foreach (var group in testSettings.groups)
                    group.RemoveAssetEntries(new List<AddressableAssetEntry>(group.entries));
        }


        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            foreach (var group in testSettings.groups)
                group.ClearSchemas(true, false);
        }


        internal AddressableAssetBuildResult CreateTestResult(double duration, bool isPlayMode, string err, long bundleCount = 0)
        {
            if (isPlayMode)
                return CreatePlaymodeResult(duration, err);
            else
            {
                return CreatePlayerBuildResult(duration, err, bundleCount);
            }
        }

        /// <summary>
        /// Dummy constructor for analytics testing purposes
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="err"></param>
        /// <param name="bundleCount"></param>
        private AddressablesPlayerBuildResult CreatePlayerBuildResult(double duration, string err, long bundleCount)
        {
            var result = new AddressablesPlayerBuildResult();
            result.Duration = duration;
            result.Error = err;
            for (int i = 0; i < bundleCount; i++)
            {
                result.m_AssetBundleBuildResults.Add(null);
            }

            return result;
        }

        /// <summary>
        /// Dummy constructor for analytics testing purposes
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="err"></param>
        private AddressablesPlayModeBuildResult CreatePlaymodeResult(double duration, string err)
        {
            var result = new AddressablesPlayModeBuildResult();
            result.Duration = duration;
            result.Error = err;
            return result;
        }

        [Test]
        public void GenerateBuildData_CorrectlyAcquiresCompressionTypes()
        {
            builderInput.IsBuildAndRelease = false;
            builderInput.IsContentUpdateBuild = false;
            var buildData = AddressableAnalytics.GenerateBuildData(builderInput, CreateTestResult(0, false, null), AddressableAnalytics.BuildType.Inconclusive);

            Assert.NotNull(buildData, "GenerateBuildData should not return a null value.");
            Assert.AreEqual(numberOfGroups, buildData.NumberOfGroups, "GenerateBuildData incorrectly retrieves number of groups");
            Assert.AreEqual(numberOfGroupsUncompressed, buildData.NumberOfGroupsUncompressed, "GenerateBuildData incorrectly retrieves number of uncompressed groups");
            Assert.AreEqual(numberOfGroupsCompressedWithLZ4, buildData.NumberOfGroupsUsingLZ4, "GenerateBuildData incorrectly retrieves number of groups compressed using LZ4.");
            Assert.AreEqual(numberOfGroupsCompressedWithLZMA, buildData.NumberOfGroupsUsingLZMA, "GenerateBuildData incorrectly retrieves number of groups compressed using LZMA.");
        }

        [Test]
        public void GenerateBuildData_CorrectlyAcquiresBundlePackingMode()
        {
            builderInput.IsBuildAndRelease = false;
            builderInput.IsContentUpdateBuild = false;
            var buildData = AddressableAnalytics.GenerateBuildData(builderInput, CreateTestResult(0, false, null), AddressableAnalytics.BuildType.Inconclusive);

            Assert.NotNull(buildData, "GenerateBuildData should not return a null value.");
            Assert.AreEqual(numberOfGroups, buildData.NumberOfGroups, "GenerateBuildData incorrectly retrieves number of groups");
            Assert.AreEqual(numberOfGroupsPackedSeparately, buildData.NumberOfGroupsPackedSeparately, "GenerateBuildData incorrectly retrieves number of groups packed separately.");
            Assert.AreEqual(numberOfGroupsPackedTogether, buildData.NumberOfGroupsPackedTogether, "GenerateBuildData incorrectly retrieves number of groups packed together.");
            Assert.AreEqual(numberOfGroupsPackedTogetherByLabel, buildData.NumberOfGroupsPackedTogetherByLabel, "GenerateBuildData incorrectly retrieves number of groups packed together by label.");
        }

        [Test]
        public void GenerateBuildData_CorrectlyCalculatesNumberOfAddressableAssets_SingleGroup()
        {
            for (int i = 0; i < 100; i++)
                testSettings.groups[0].AddAssetEntry(testSettings.CreateEntry(System.Guid.NewGuid().ToString(), "AddressableAnalyticsTest" + i, testSettings.groups[0],
                    false, false));

            builderInput.IsBuildAndRelease = false;
            builderInput.IsContentUpdateBuild = false;
            var buildData = AddressableAnalytics.GenerateBuildData(builderInput, CreateTestResult(0, false, null), AddressableAnalytics.BuildType.Inconclusive);

            Assert.NotNull(buildData, "GenerateBuildData should not return a null value.");
            Assert.AreEqual(numberOfGroups, buildData.NumberOfGroups, "GenerateBuildData incorrectly retrieves number of groups");
            Assert.AreEqual(100, buildData.NumberOfAddressableAssets, "GenerateBuildData incorrectly retrieves number of AddressableAssets");
        }

        [Test]
        public void GenerateBuildData_CorrectlyCalculatesNumberOfAddressableAssets_MultipleGroups()
        {
            foreach (var group in testSettings.groups)
                for (int i = 0; i < 20; i++)
                    group.AddAssetEntry(testSettings.CreateEntry(System.Guid.NewGuid().ToString(),
                        "AddressableAnalyticsTest" + group.Name + i, group, false));

            builderInput.IsBuildAndRelease = false;
            builderInput.IsContentUpdateBuild = false;
            var buildData = AddressableAnalytics.GenerateBuildData(builderInput, CreateTestResult(0, false, null), AddressableAnalytics.BuildType.Inconclusive);
            Assert.NotNull(buildData, "GenerateBuildData should not return a null value.");
            Assert.AreEqual(numberOfGroups, buildData.NumberOfGroups, "GenerateBuildData incorrectly retrieves number of groups");
            Assert.AreEqual(numberOfGroups * 20, buildData.NumberOfAddressableAssets, "GenerateBuildData incorrectly retrieves number of AddressableAssets");
        }

        [Test]
        public void GenerateBuildData_CorrectlyRetrievesMinAndMaxAssetsInGroups()
        {
            var group0 = testSettings.groups[0];
            var group1 = testSettings.groups[1];

            for (int i = 0; i < 30; i++)
            {
                group0.AddAssetEntry(testSettings.CreateEntry(System.Guid.NewGuid().ToString(),
                    "AddressableAnalyticsTest" + group0.Name + i, group0, false));
            }

            for (int i = 0; i < 10; i++)
            {
                group1.AddAssetEntry(testSettings.CreateEntry(System.Guid.NewGuid().ToString(),
                    "AddressableAnalyticsTest" + group1.Name + i, group1, false));
            }

            for (int i = 2; i < 6; i++)
            {
                var group = testSettings.groups[i];
                group.AddAssetEntry(testSettings.CreateEntry(System.Guid.NewGuid().ToString(),
                    "AddressableAnalyticsTest" + group.Name + i, group, false));
            }

            builderInput.IsBuildAndRelease = false;
            builderInput.IsContentUpdateBuild = false;
            var buildData = AddressableAnalytics.GenerateBuildData(builderInput, CreateTestResult(0, false, null), AddressableAnalytics.BuildType.Inconclusive);
            Assert.NotNull(buildData, "GenerateBuildData should not return a null value.");
            Assert.AreEqual(30, buildData.MaxNumberOfAddressableAssetsInAGroup, "GenerateBuildData does not properly retrieve Max number of addressable assets in a group");
            Assert.AreEqual(1, buildData.MinNumberOfAddressableAssetsInAGroup, "GenerateBuildData does not properly retrieve min number of addressable assets in a group");
        }

        [Test]
        public void GenerateBuildData_CorrectlyRetrievesBuildAndReleaseValue()
        {
            builderInput.IsBuildAndRelease = true;
            builderInput.IsContentUpdateBuild = false;
            var buildData = AddressableAnalytics.GenerateBuildData(builderInput, CreateTestResult(0, false, null), AddressableAnalytics.BuildType.Inconclusive);
            Assert.IsTrue(buildData.BuildAndRelease, "Value of BuildAndRelease is not properly set");
        }

        [Test]
        public void GenerateBuildData_CorrectlyDeterminesBuildTypeFromInput()
        {
            builderInput.IsBuildAndRelease = false;
            builderInput.IsContentUpdateBuild = false;
            var buildData1 = AddressableAnalytics.GenerateBuildData(builderInput, CreateTestResult(0, false, null), AddressableAnalytics.BuildType.Inconclusive);
            Assert.AreEqual(-1, buildData1.IsIncrementalBuild, "BuildType not correctly set by analytics, ensure that an Inconclusive build is associated with an IsIncrementalBuild value of -1");
            var buildData2 = AddressableAnalytics.GenerateBuildData(builderInput, CreateTestResult(0, false, null), AddressableAnalytics.BuildType.CleanBuild);
            Assert.AreEqual(0, buildData2.IsIncrementalBuild, "BuildType not correctly set by analytics, ensure that a clean build is associated with an IsIncrementalBuild value of 0");
            var buildData3 = AddressableAnalytics.GenerateBuildData(builderInput, CreateTestResult(0, false, null), AddressableAnalytics.BuildType.IncrementalBuild);
            Assert.AreEqual(1, buildData3.IsIncrementalBuild, "BuildType not correctly set by analytics, ensure that a clean build is associated with an IsIncrementalBuild value of 1");
        }

        [Test]
        public void GenerateBuildData_OnlyIncludesBuildScriptNameInPlayModeBuild()
        {
            builderInput.IsBuildAndRelease = false;
            builderInput.IsContentUpdateBuild = false;
            var buildData = AddressableAnalytics.GenerateBuildData(builderInput, CreateTestResult(0, true, null), AddressableAnalytics.BuildType.Inconclusive);
            Assert.IsTrue(buildData.IsPlayModeBuild, "IsPlayModeBuild should be true for play mode builds");
            Assert.AreEqual(0, buildData.NumberOfGroups, "BuildData should be initialized to 0 for play mode builds.");
        }

        [Test]
        public void GenerateBuildData_PlayModeScriptIsCorrectlyRetrieved()
        {
            var oldActivePlayModeBuildScriptIndex = ProjectConfigData.ActivePlayModeIndex;
            try
            {
                //PackedMode
                ProjectConfigData.ActivePlayModeIndex = 1;
                builderInput.IsBuildAndRelease = false;
                builderInput.IsContentUpdateBuild = false;
                var buildData1 = AddressableAnalytics.GenerateBuildData(builderInput, CreateTestResult(0, true, null), AddressableAnalytics.BuildType.CleanBuild);
                Assert.IsTrue(buildData1.IsPlayModeBuild, "IsPlayModeBuild should be true for play mode builds");
                Assert.AreEqual(2, buildData1.BuildScript, "Fast Mode should correspond to a BuildData.BuildScript value of 2.");
                //FastMode
                ProjectConfigData.ActivePlayModeIndex = 2;
                var buildData2 = AddressableAnalytics.GenerateBuildData(builderInput, CreateTestResult(0, true, null), AddressableAnalytics.BuildType.CleanBuild);
                Assert.IsTrue(buildData2.IsPlayModeBuild, "IsPlayModeBuild should be true for play mode builds");
                Assert.AreEqual(3, buildData2.BuildScript, "Virtual mode should correspond to a BuildData.BuildScript value of 3.");
                //VirtualMode
                ProjectConfigData.ActivePlayModeIndex = 3;
                var buildData3 = AddressableAnalytics.GenerateBuildData(builderInput, CreateTestResult(0, true, null), AddressableAnalytics.BuildType.CleanBuild);
                Assert.IsTrue(buildData3.IsPlayModeBuild, "IsPlayModeBuild should be true for play mode builds");
                Assert.AreEqual(1, buildData3.BuildScript, "Packed PlayMode should correspond to a BuildData.BuildScript value of 1.");
                //Custom
                ProjectConfigData.ActivePlayModeIndex = 4;
                var buildData4 = AddressableAnalytics.GenerateBuildData(builderInput, CreateTestResult(0, true, null), AddressableAnalytics.BuildType.CleanBuild);
                Assert.IsTrue(buildData4.IsPlayModeBuild, "IsPlayModeBuild should be true for play mode builds");
                Assert.AreEqual(4, buildData4.BuildScript, "A custom build script should correspond to a BuildData.BuildScript value of 4.");
            }
            finally
            {
                ProjectConfigData.ActivePlayModeIndex = oldActivePlayModeBuildScriptIndex;
            }
        }

        [Test]
        public void GenerateBuildData_ProperlyCollectsDataIfNotPlayModeBuild()
        {
            var buildData1 = AddressableAnalytics.GenerateBuildData(builderInput, CreateTestResult(0, false, null), AddressableAnalytics.BuildType.CleanBuild);
            Assert.AreEqual(0, buildData1.BuildScript, "BuildData should correspond to a BuildData.BuildScript value of 0 because we're using packed mode.");
            Assert.IsFalse(buildData1.IsPlayModeBuild, "IsPlayModeBuild should be false on player builds.");
            Assert.AreNotEqual(0, buildData1.NumberOfGroups, "Data should be fully collected in player builds.");
        }

        [Test]
        public void GenerateUsageData_UsageEventTypesCorrectlyCorrespondToIntegerValues()
        {
            var usageDataGroups =
                AddressableAnalytics.GenerateUsageData(AddressableAnalytics.UsageEventType.OpenGroupsWindow);
            var usageDataProfiles =
                AddressableAnalytics.GenerateUsageData(AddressableAnalytics.UsageEventType.OpenProfilesWindow);
            var usageDataEventViewer =
                AddressableAnalytics.GenerateUsageData(AddressableAnalytics.UsageEventType.OpenEventViewerWindow);
            var usageDataAnalyze =
                AddressableAnalytics.GenerateUsageData(AddressableAnalytics.UsageEventType.OpenAnalyzeWindow);
            var usageDataHosting =
                AddressableAnalytics.GenerateUsageData(AddressableAnalytics.UsageEventType.OpenHostingWindow);

            Assert.AreEqual(0, usageDataGroups.UsageEventType, "Opening the groups window should correspond with an integer value of 0");
            Assert.AreEqual(1, usageDataProfiles.UsageEventType, "Opening the profiles window should correspond with an integer value of 1");
            Assert.AreEqual(2, usageDataEventViewer.UsageEventType, "Opening the event viewer window should correspond with an integer value of 2");
            Assert.AreEqual(3, usageDataAnalyze.UsageEventType, "Opening the analyze window should correspond with an integer value of 3");
            Assert.AreEqual(4, usageDataHosting.UsageEventType, "Opening the hosting window should correspond with an integer value of 4");
        }

        // This test is just to lock down behavior, please do not make any changes that would lead to this test failing as it will break analytics!
        // The analytics platform relies on each of these values being what they are, and any changes will lead to discontinuity in the data.
        [Test]
        public void EnsureCheckForContentUpdateRestrictionAndAnalyticsEnumMatch()
        {
            Assert.AreEqual((int)CheckForContentUpdateRestrictionsOptions.ListUpdatedAssetsWithRestrictions,
                (int)AddressableAnalytics.AnalyticsContentUpdateRestriction.ListUpdatedAssetsWithRestrictions,
                "The ListUpdatedAssetsWithRestrictions member of both enums should correspond to the same integer");
            Assert.AreEqual(0, (int)CheckForContentUpdateRestrictionsOptions.ListUpdatedAssetsWithRestrictions,
                "The ListUpdatedAssetsWithRestrictions member should correspond to 0 for consistency with the analytics platform.");

            Assert.AreEqual((int)CheckForContentUpdateRestrictionsOptions.FailBuild,
                (int)AddressableAnalytics.AnalyticsContentUpdateRestriction.FailBuild, "The FailBuild member of both enums should correspond to the same integer");
            Assert.AreEqual(1, (int)CheckForContentUpdateRestrictionsOptions.FailBuild, "The FailBuild member should correspond to 1 for consistency with the analytics platform.");

            Assert.AreEqual((int)CheckForContentUpdateRestrictionsOptions.Disabled,
                (int)AddressableAnalytics.AnalyticsContentUpdateRestriction.Disabled, "The disabled member of both enums should correspond to the same integer");
            Assert.AreEqual(2, (int)CheckForContentUpdateRestrictionsOptions.Disabled, "The Disabled member should correspond to 2 for consistency with the analytics platform.");

            Assert.AreEqual(-1, (int)AddressableAnalytics.AnalyticsContentUpdateRestriction.NotApplicable,
                "For non applicable data events, the AnalyticsContentUpdateRestriction enum should correspond to -1");
        }
    }
}
