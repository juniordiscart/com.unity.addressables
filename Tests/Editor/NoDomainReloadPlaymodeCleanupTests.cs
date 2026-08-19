using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.TestTools;

namespace UnityEditor.AddressableAssets.Tests
{
    public class NoDomainReloadPlaymodeCleanupTests
    {
        // Non-routable IP — TCP connections hang without immediately failing,
        // keeping the web request in-flight long enough to enter play mode.
        const string k_RemoteUrl = "http://10.255.255.1/fake-bundle.bundle";

        const string k_TempPath = "NoDomainReloadPlaymodeCleanupTests";
        const string k_AssetKey = "editmodeloaded";

        // SessionState default when Addressables.kAddressablesRuntimeDataPath was never set.
        const string k_NoSavedRuntimeDataPath = "__NoDomainReloadPlaymodeCleanupTests_NoSavedRuntimeDataPath__";

        bool m_SavedEnterPlayModeOptionsEnabled;
        EnterPlayModeOptions m_SavedEnterPlayModeOptions;
        InsecureHttpOption m_SavedHttpOption;
        string m_SavedRuntimeDataPath;
        bool m_WarnsOnEditorUsage;

        string GetPath(string a) => $"Assets/{k_TempPath}/{a}";

        string CreateAsset(string assetName, string path)
        {
            AssetDatabase.CreateAsset(UnityEngine.AddressableAssets.Tests.TestObject.Create(assetName), path);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            return AssetDatabase.AssetPathToGUID(path);
        }

        string BuildTestBundle()
        {
            if (AssetDatabase.IsValidFolder($"Assets/{k_TempPath}"))
            {
                AssetDatabase.DeleteAsset($"Assets/{k_TempPath}");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            AssetDatabase.CreateFolder("Assets", k_TempPath);
            var settings =
                AddressableAssetSettings.Create(GetPath("Settings"), "NoDomainReloadPlaymodeCleanupTests", true, true);

            settings.CreateOrMoveEntry(CreateAsset(k_AssetKey, GetPath($"{k_AssetKey}.asset")),
                settings.DefaultGroup, false, false).address = k_AssetKey;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            AddressablesDataBuilderInput inputs = new AddressablesDataBuilderInput(settings);
            // Unique filename prevents output collision with other fixtures using the default "settings.json".
            inputs.RuntimeSettingsFilename = $"settings{k_TempPath}.json";
            AddressableAssetSettings.BuildPlayerContent(out var buildResult, inputs);

            AssetDatabase.DeleteAsset($"Assets/{k_TempPath}");
            AssetDatabase.DeleteAsset($"Assets/AddressableAssetsData");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            return buildResult.OutputPath;
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_SavedEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            m_SavedEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
            m_SavedHttpOption = PlayerSettings.insecureHttpOption;
            m_WarnsOnEditorUsage = Addressables.WarnOnAddressablesUsageOutsidePlaymode;

            var settings = BuildTestBundle();
            m_SavedRuntimeDataPath =
                SessionState.GetString(Addressables.kAddressablesRuntimeDataPath, k_NoSavedRuntimeDataPath);
            SessionState.SetString(Addressables.kAddressablesRuntimeDataPath, settings);

            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions =
                EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            Addressables.WarnOnAddressablesUsageOutsidePlaymode = true;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            EditorSettings.enterPlayModeOptionsEnabled = m_SavedEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = m_SavedEnterPlayModeOptions;
            PlayerSettings.insecureHttpOption = m_SavedHttpOption;
            if (m_SavedRuntimeDataPath == k_NoSavedRuntimeDataPath)
                SessionState.EraseString(Addressables.kAddressablesRuntimeDataPath);
            else
                SessionState.SetString(Addressables.kAddressablesRuntimeDataPath, m_SavedRuntimeDataPath);
            Addressables.WarnOnAddressablesUsageOutsidePlaymode = m_WarnsOnEditorUsage;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (EditorApplication.isPlaying)
                yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator EnteringPlayMode_WithAnActiveWebRequestAssetBundle_CancelsIt()
        {
            // Setup
            var location = new ResourceLocationBase(
                k_RemoteUrl, k_RemoteUrl,
                typeof(AssetBundleProvider).FullName,
                null);
            location.Data = new AssetBundleRequestOptions { Timeout = 5 };

            Addressables.InitializeAsync().WaitForCompletion();

            // Start the load in edit mode; do NOT yield — leave the web request in-flight.
            var operation = Addressables.LoadAssetAsync<AssetBundleResource>(location);

            // Skip a frame so the WebRequest is processed and started.
            yield return null;

            Assert.IsTrue(
                AssetBundleProvider.LoadingRemoteBundles.ContainsKey(k_RemoteUrl),
                "Expected the remote URL to be listed in LoadingRemoteBundles while the bundle load runs in edit mode.");
            Assert.AreEqual(1, WebRequestQueue.s_ActiveRequests.Count,
                "Expected exactly one active web request after starting the remote bundle load in edit mode.");

            // Disabling the check on the log given other tests in the suite can make the log tracker unable to see it when entering playmode...
            LogAssert.Expect(LogType.Warning,
                "The Addressables class was used outside playmode and loaded references might be invalidated when entering playmode. " +
                "Ensure that your Editor scripts are registered to EditorApplication.playModeStateChanged " +
                "to reload what is needed. This warning can be turned off in Preferences/Addressables.");
            // LogAssert.ignoreFailingMessages = true;

            // Act
            yield return new EnterPlayMode();
            // LogAssert.ignoreFailingMessages = false;

            // Assert
            Assert.AreEqual(0, AssetBundleProvider.LoadingRemoteBundles.Count,
                "LoadingRemoteBundles should be empty after entering play mode.");

            Assert.AreEqual(0, WebRequestQueue.s_ActiveRequests.Count,
                "Active web requests should be aborted when entering play mode.");

            Assert.That(operation.Status, Is.EqualTo(AsyncOperationStatus.Failed),
                "Load operation should fail after play mode transition aborts the in-flight web request.");

            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator EnteringPlayMode_WithALoadedAssetBundleHandle_UnloadsIt()
        {
            // Setup
            Addressables.InitializeAsync().WaitForCompletion();
            var handle = Addressables.LoadAssetAsync<UnityEngine.AddressableAssets.Tests.TestObject>(k_AssetKey);
            handle.WaitForCompletion();

            Assert.That(AssetBundle.GetAllLoadedAssetBundles().Count(),
                Is.GreaterThan(0),
                "Expected a loaded AssetBundle before entering play mode.");

            // Disabling the check on the log given other tests in the suite can make the log tracker unable to see it when entering playmode...
            LogAssert.Expect(LogType.Warning,
                "The Addressables class was used outside playmode and loaded references might be invalidated when entering playmode. " +
                "Ensure that your Editor scripts are registered to EditorApplication.playModeStateChanged " +
                "to reload what is needed. This warning can be turned off in Preferences/Addressables.");
            // LogAssert.ignoreFailingMessages = true;

            // Act
            yield return new EnterPlayMode();
            // LogAssert.ignoreFailingMessages = false;

            // Assert
            Assert.IsFalse(handle.IsValid(),
                "Handle should have been released by Dispose() on the Addressables context.");

            Assert.AreEqual(0, AssetBundle.GetAllLoadedAssetBundles().Count(),
                "No AssetBundles should remain loaded after entering play mode.");

            yield return new ExitPlayMode();
        }
    }
}