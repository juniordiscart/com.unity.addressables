using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using Unity.Collections;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.Diagnostics;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Profiling;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using TreeView = UnityEditor.IMGUI.Controls.TreeView;

namespace UnityEditor.AddressableAssets.Tests.Diagnostics.Profiler
{
    public class AddressablesProfilerDetailsViewTests
    {
        private static BuildLayout.ExplicitAsset m_TestAsset;
        AddressablesProfilerDetailsView m_AddressablesProfilerDetailsView;
        private TestProfiler testProfiler;

        [SetUp]
        public void SetUp()
        {
            // this is an implementation of the profiler interfaces to allow us to create profile events
            // and then retrieve them for testing
            testProfiler = new TestProfiler();
            ProfilerRuntime.m_profilerEmitter = testProfiler;
            ProfilerRuntime.Initialise();
            AddressablesProfilerDetailsView.m_frameDataStore = testProfiler;
            AddressablesProfilerViewController.LayoutsManager.ClearReports();
            m_AddressablesProfilerDetailsView = new AddressablesProfilerDetailsView(null);

            m_TestAsset = new BuildLayout.ExplicitAsset
            {
                AddressableName = "MainScene",
                AssetPath = "Assets/Scenes/MainScene.unity",
                Guid = new Hash128().ToString()
            };
        }

        [TearDown]
        public void TearDown()
        {
            m_AddressablesProfilerDetailsView.Dispose();
        }

        [Test]
        public void Test_GenerateContentData_EmptyFrameData()
        {
            AddressablesProfilerDetailsView.FrameData frameData = new AddressablesProfilerDetailsView.FrameData(1);
            List<GroupData> groupData = m_AddressablesProfilerDetailsView.GenerateContentDataForFrame(frameData);
            Assert.AreEqual(0, groupData.Count);
        }

        [Test]
        public void Test_GenerateContentData_ValidData()
        {
            Test_GenerateContentData_FrameData(true, true, true, true);
        }

        [Test]
        public void Test_GenerateContentData_MissingReferencedBundle()
        {
            LogAssert.Expect(LogType.Warning, "Asset Assets/AddressMe/brown.bmp referenced bundle folder_assets_assets/addressme_3876ae7ae6d93912555c4c93ef6e3fd7.bundle not loaded from build layout, attaching to parent scene_scenes_all_ea5d4d944831b5303deb66b664bd2e25.bundle");
            Test_GenerateContentData_FrameData(true, true, true, false);
        }

        private void Test_GenerateContentData_FrameData(bool loadTextureBundle, bool loadSceneBundle, bool loadLocalAssets, bool loadFolderBundle)
        {
            AddressablesProfilerViewController.LayoutsManager.LoadManualReport(AddressablesTestUtility.GetPackagePath() + "/Tests/Editor/Fixtures/buildlayout1.json");
            AddressablesProfilerViewController.LayoutsManager.AddActiveLayout("3e22efc5f159401a15e7d27179b7cc4c"); // this is the value of BuildResultHash in buildlayout1.json
            // this creates the build report
            // var builder = new BuildLayoutBuilder().AddGroup("Remote Content").AddBundle("a.bundle").AddAsset("test.asset", "https://example.com/a.bundle").AddToLayoutManager();

            // texture bundle
            if (loadTextureBundle)
            {
                var bundleEvents = new ProfilerEventBuilder(testProfiler).SetBundleName("c75e210455b9b7f32a8e902db35e6ee3")
                    .SendBundleEvent(1, ContentStatus.Loading)
                    .SendBundleEvent(2, ContentStatus.Active)
                    .SendBundleEvent(3, ContentStatus.Released);

            }

            // scene bundle
            if (loadSceneBundle)
            {
                var bundleEvents2 = new ProfilerEventBuilder(testProfiler).SetBundleName("79f2518976c47599628aad579c012657")
                    .SendBundleEvent(1, ContentStatus.Loading)
                    .SendBundleEvent(2, ContentStatus.Active)
                    .SendBundleEvent(3, ContentStatus.Released);
            }

            // default local assets
            if (loadLocalAssets)
            {
                var bundleEvents3 = new ProfilerEventBuilder(testProfiler).SetBundleName("0113602827489280bf434abd9b49426c")
                    .SendBundleEvent(1, ContentStatus.Loading)
                    .SendBundleEvent(2, ContentStatus.Active)
                    .SendBundleEvent(3, ContentStatus.Released);
            }

            // folder bundle
            if (loadFolderBundle)
            {

                var bundleEvents4 = new ProfilerEventBuilder(testProfiler).SetBundleName("6801756afe332320683f01526bb77e3d")
                    .SendBundleEvent(1, ContentStatus.Loading)
                    .SendBundleEvent(2, ContentStatus.Active)
                    .SendBundleEvent(3, ContentStatus.Released);
            }

            var sceneEvents = new ProfilerEventBuilder(testProfiler)
                .SetAssetLocation("Assets/Scenes/SampleScene.unity", "79f2518976c47599628aad579c012657") // FIXME, should this take a builder so it can look this up for us?
                .SendSceneEvent(1, ContentStatus.Loading)
                .SendSceneEvent(2, ContentStatus.Active)
                .SendSceneEvent(3, ContentStatus.Released);
            var assetEvents = new ProfilerEventBuilder(testProfiler)
                .SetAssetLocation("Assets/khaki.png", "a1dd26be86668e2320052da88bcb6d39") // FIXME, should this take a builder so it can look this up for us?
                .SendAssetEvent(1, ContentStatus.Loading)
                .SendAssetEvent(2, ContentStatus.Active); // assets do not have an explicit released event
            // so this is an internal prefab, I need one with external references
            var embeddedPrefabEvents = new ProfilerEventBuilder(testProfiler)
                .SetAssetLocation("Assets/Prefabs/YellowCube.prefab", "c75e210455b9b7f32a8e902db35e6ee3") // FIXME, should this take a builder so it can look this up for us?
                .SendAssetEvent(1, ContentStatus.Loading)
                .SendAssetEvent(2, ContentStatus.Active); // assets do not have an explicit released event
            var referencePrefabEvents = new ProfilerEventBuilder(testProfiler)
                .SetAssetLocation("Assets/Prefabs/Canvas.prefab", "c75e210455b9b7f32a8e902db35e6ee3") // FIXME, should this take a builder so it can look this up for us?
                .SendAssetEvent(1, ContentStatus.Loading)
                .SendAssetEvent(2, ContentStatus.Active); // assets do not have an explicit released event

            // this simulates clicking each frame in the UI, retrieving data and verifying what is returned
            for (int i = 1; i <= 2; i++)
            {
                AddressablesProfilerDetailsView.FrameData frameData = new AddressablesProfilerDetailsView.FrameData(i);
                List<GroupData> groupData = m_AddressablesProfilerDetailsView.GenerateContentDataForFrame(frameData);
                Assert.AreEqual("Prefabs", groupData[0].Name);
                Assert.GreaterOrEqual(groupData[0].Children.Count, 1);
                Assert.AreEqual("prefabs_assets_texture_8d87ce5cdbde7dec1bdded52918590d0.bundle", groupData[0].Children[0].Name);
                foreach (var child in groupData[0].Children)
                {
                    if (child is AssetData)
                    {
                        assetEvents.VerifyFrameStatus(i, child.Status);
                    }
                }
            }
        }

        [Test]
        public void Test_GetAssetDataForFrameData_HasData()
        {
            AssetFrameData frameData = new AssetFrameData
            {
                AssetCode = 1,
                BundleCode = 10,
                Status = ContentStatus.Downloading
            };
            var bundleCodeToData = GetBundleCodeToData();
            var retVal = m_AddressablesProfilerDetailsView.GetAssetDataForFrameData(m_TestAsset, frameData, bundleCodeToData);
            Assert.AreEqual(ContentStatus.Downloading, retVal.Status);
            Assert.AreEqual(bundleCodeToData[10], retVal.Parent);
            Assert.AreEqual(m_TestAsset.AssetPath, retVal.AssetPath);
            Assert.AreEqual(m_TestAsset.AddressableName, retVal.Name);
            Assert.AreEqual(m_TestAsset.Guid, retVal.AssetGuid);
        }

        [Test]
        public void Test_GetAssetDataForFrameData_NoData()
        {
            // the BundleCode is used to lookup the asset frame data, so since these
            // don't match we expect to get null
            AssetFrameData frameData = new AssetFrameData
            {
                AssetCode = 1,
                BundleCode = 20,
                Status = ContentStatus.Downloading
            };
            var retVal = m_AddressablesProfilerDetailsView.GetAssetDataForFrameData(m_TestAsset, frameData, GetBundleCodeToData());
            Assert.IsNull(retVal);
        }

        private Dictionary<int, BundleData> GetBundleCodeToData() => new()
        {
            { 10, new BundleData(new BuildLayout.Bundle(), new BundleFrameData()) }
        };

        [Test]
        [TestCase(null)]
        [TestCase("Assets/Cube.prefab")]
        public void Test_ExplicitAssetsHaveUniqueTreeViewIDs(string assetPath1)
        {
            // These two GUID strings evaluate to the same hashcode.
            var cube = new BuildLayout.ExplicitAsset
            {
                AssetPath = assetPath1,
                Guid = "bf1daffab47bbb34587f194ac0dcc1bf"
            };

            var sphere = new BuildLayout.ExplicitAsset
            {
                AssetPath = "Assets/Sphere.prefab",
                Guid = "61a57bae3cb0cec499b6e6b8db8f4008"
            };

            var cubeData = new AssetData(cube);
            var sphereData = new AssetData(sphere);

            Assert.AreNotEqual(cubeData.TreeViewID, sphereData.TreeViewID, "Explicit assets should have unique TreeViewIds.");
        }

        [Test]
        public void TestHasValues()
        {
            testProfiler.CurrentFrame = 1;
            testProfiler.EmitFrameMetaData(ProfilerRuntime.kResourceManagerProfilerGuid, ProfilerRuntime.kCatalogTag, new CatalogFrameData[] {});
            testProfiler.CurrentFrame = 2;
            testProfiler.EmitFrameMetaData(ProfilerRuntime.kResourceManagerProfilerGuid, ProfilerRuntime.kCatalogTag, new CatalogFrameData[] {new CatalogFrameData()});
            testProfiler.CurrentFrame = 3;
            testProfiler.EmitFrameMetaData(ProfilerRuntime.kResourceManagerProfilerGuid, ProfilerRuntime.kCatalogTag, new CatalogFrameData[] {new CatalogFrameData(), new CatalogFrameData()});

            AddressablesProfilerDetailsView.FrameData frameData = new AddressablesProfilerDetailsView.FrameData(1);
            Assert.False(frameData.HasValues);
            AssertEnumerableSize(frameData.CatalogValues, 0);
            frameData = new AddressablesProfilerDetailsView.FrameData(2);
            Assert.True(frameData.HasValues);
            AssertEnumerableSize(frameData.CatalogValues, 1);
            frameData = new AddressablesProfilerDetailsView.FrameData(3);
            Assert.True(frameData.HasValues);
            AssertEnumerableSize(frameData.CatalogValues, 2);
        }

        private void AssertEnumerableSize(IEnumerable<CatalogFrameData> values, int expected)
        {
            int valueCount = 0;
            foreach (var data in values)
            {
                valueCount++;
            }
            Assert.AreEqual(expected, valueCount);
        }
    }
}
