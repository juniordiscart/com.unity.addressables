#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.Diagnostics;
using UnityEngine;
using UnityEngine.ResourceManagement.Profiling;

namespace UnityEditor.AddressableAssets.Tests.Editor.OptionalPackages.Diagnostics.Profiler
{
    public class AddressablesProfilerDetailsViewTests
    {
        private static BuildLayout.ExplicitAsset m_TestAsset;
        AddressablesProfilerDetailsView m_AddressablesProfilerDetailsView;

        [SetUp]
        public void SetUp()
        {
            m_AddressablesProfilerDetailsView = new AddressablesProfilerDetailsView(null);
            // this is broken. Can we use Moq? What do we want to achieve here?

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
    }
}
#endif
