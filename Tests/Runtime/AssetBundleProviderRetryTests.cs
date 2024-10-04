using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
#endif
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.TestTools;

namespace AddressableAssetsIntegrationTests
{
    public abstract class AssetBundleProviderRetryTests : AddressablesTestFixture
    {
        class CustomRetryAssetBundleProvider : AssetBundleProvider
        {
            public override bool ShouldRetryDownloadError(UnityWebRequestResult uwrResult)
            {
                if (uwrResult.Error == "Cannot connect to destination host")
                    return false;
                return true;
            }
        }

#if UNITY_EDITOR
        internal override void Setup(AddressableAssetSettings settings, string tempAssetFolder)
        {
            settings.AssetBundleProviderType = new SerializedType() { Value = typeof(CustomRetryAssetBundleProvider) };

            AddressableAssetGroup testGroup = settings.CreateGroup("AssetBundleProviderRetryTestsGroup", false, false, true,
              new List<AddressableAssetGroupSchema>(), typeof(BundledAssetGroupSchema));
            var schema = testGroup.GetSchema<BundledAssetGroupSchema>();
            schema.AssetBundleProviderType = settings.AssetBundleProviderType;

            string guid = CreatePrefab(tempAssetFolder + $"/testprefab.prefab");
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, testGroup);
            entry.address = "testprefab";
        }
#endif

        [UnityTest]
        [Platform(Exclude = "PS5")]
        public IEnumerator AssetBundleProviderRetry_WhenRetryCountIsZero_LogsUWRError()
        {
            var nonExistingPath = "http://127.0.0.1/non-existing-bundle";
            var loc = new ResourceLocationBase(nonExistingPath, nonExistingPath, typeof(CustomRetryAssetBundleProvider).FullName, typeof(AssetBundleResource));
            var options = new AssetBundleRequestOptions();
            options.RetryCount = 0;
            loc.Data = options;

            var h = m_Addressables.ResourceManager.ProvideResource<AssetBundleResource>(loc);
            yield return h;
            LogAssert.Expect(LogType.Error, new Regex($"RemoteProviderException : Unable to load asset bundle from : {nonExistingPath}\nUnityWebRequest result : ConnectionError : Cannot connect to destination host.*"));

            if (h.IsValid())
                h.Release();
        }

        [UnityTest]
        [Platform(Exclude = "PS5")]
        public IEnumerator AssetBundleProviderRetry_WhenRetryCountIsSet_LogsRetryError()
        {
            var nonExistingPath = "http://127.0.0.1/non-existing-bundle";
            var loc = new ResourceLocationBase(nonExistingPath, nonExistingPath, typeof(CustomRetryAssetBundleProvider).FullName, typeof(AssetBundleResource));
            var options = new AssetBundleRequestOptions();
            options.RetryCount = 3;
            loc.Data = options;

            var h = m_Addressables.ResourceManager.ProvideResource<AssetBundleResource>(loc);
            yield return h;
            LogAssert.Expect(LogType.Error, new Regex($".*Retry count set to {options.RetryCount} but cannot retry request due to error Cannot connect to destination host. To override use a custom AssetBundle provider.*"));

            if (h.IsValid())
                h.Release();
        }

#if UNITY_EDITOR
        class AssetBundleProviderRetryTests_PackedPlaymodeMode : AssetBundleProviderRetryTests
        {
            protected override TestBuildScriptMode BuildScriptMode
            {
                get { return TestBuildScriptMode.PackedPlaymode; }
            }
        }
#endif

        [UnityPlatform(exclude = new[] { RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor })]
        class AssetBundleProviderRetryTests_PackedMode : AssetBundleProviderRetryTests
        {
            protected override TestBuildScriptMode BuildScriptMode
            {
                get { return TestBuildScriptMode.Packed; }
            }
        }
    }
}
