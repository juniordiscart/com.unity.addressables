using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UnityEditor.AddressableAssets.Tests
{
    public abstract class AddressableAssetTestBase
    {
        protected const string k_TestConfigName = "AddressableAssetSettings.Tests";
        protected string TestFolder => $"Assets/{TestFolderName}";
        protected string TestFolderName => $"{GetType()}_Tests";
        protected string ConfigFolder => TestFolder + "/Config";
        protected string AddressablesFolder => $"Assets/AddressableAssetsData";

        protected string m_PackagePath;

        protected string GetAssetPath(string assetName)
        {
            return $"{TestFolder}/{assetName}";
        }

        private AddressableAssetSettings m_Settings;
        private AddressableAssetSettings m_PreviousDefaultSettings;
        private bool m_DidOverrideDefaultSettings;

        protected AddressableAssetSettings Settings
        {
            get
            {
                if (!m_Settings || m_Settings.groups.IsNullOrEmpty())
                    m_Settings = AddressableAssetSettings.Create(ConfigFolder, k_TestConfigName, true, PersistSettings);
                return m_Settings;
            }
        }

        protected string m_AssetGUID;
        protected string[] m_SceneGuids;

        protected virtual bool PersistSettings
        {
            get { return true; }
        }

        // Opt-in: makes AddressableAssetSettingsDefaultObject.Settings deterministically point at this
        // fixture's own Settings for the duration of its tests (see Init/Cleanup). Off by default because
        // the setter also live-registers this fixture's OnPostprocessAllAssets on the real
        // AssetPostprocessor pipeline (AddressableAssetSettingsDefaultObject.cs:96), which conflicts with
        // fixtures that themselves test that exact mechanism (e.g. AddressableAssetSettingsTests).
        protected virtual bool ManageDefaultSettings => false;

        [OneTimeSetUp]
        public void Init()
        {
            //TODO: Remove when NSImage warning issue on bokken is fixed
            Application.logMessageReceived += CheckLogForWarning;

            if (Directory.Exists(TestFolder))
            {
                Debug.Log($"{GetType()} (init) - deleting {TestFolder}");
                if (!AssetDatabase.DeleteAsset(TestFolder))
                    Directory.Delete(TestFolder);
            }

            if (Directory.Exists(AddressablesFolder))
            {
                Debug.Log($"{GetType()} (init) - deleting {AddressablesFolder}");
                if (!AssetDatabase.DeleteAsset(AddressablesFolder))
                    Directory.Delete(AddressablesFolder);
            }

            Debug.Log($"{GetType()} (init) - creating {TestFolder}");
            AssetDatabase.CreateFolder("Assets", TestFolderName);
            AssetDatabase.CreateFolder(TestFolder, "Config");

            Settings.labelTable.Clear();
            GameObject testObject = new GameObject("TestObject");
            GameObject testObject1 = new GameObject("TestObject 1");
            GameObject testObject2 = new GameObject("TestObject 2");

            PrefabUtility.SaveAsPrefabAsset(testObject, TestFolder + "/test.prefab");
            PrefabUtility.SaveAsPrefabAsset(testObject1, TestFolder + "/test 1.prefab");
            PrefabUtility.SaveAsPrefabAsset(testObject2, TestFolder + "/test 2.prefab");
            m_AssetGUID = AssetDatabase.AssetPathToGUID(TestFolder + "/test.prefab");

            string scene1Path = TestFolder + "/contentUpdateScene1.unity";
            string scene2Path = TestFolder + "/contentUpdateScene2.unity";
            string scene3Path = TestFolder + "/contentUpdateScene3.unity";

            Scene scene1 = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            EditorSceneManager.SaveScene(scene1, scene1Path);

            Scene scene2 = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            EditorSceneManager.SaveScene(scene2, scene2Path);

            Scene scene3 = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            EditorSceneManager.SaveScene(scene3, scene3Path);

            //Clear out the active scene so it doesn't affect tests
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

            m_SceneGuids = new string[]
            {
                AssetDatabase.AssetPathToGUID(scene1Path),
                AssetDatabase.AssetPathToGUID(scene2Path),
                AssetDatabase.AssetPathToGUID(scene3Path)
            };

            // Make the process-wide default settings object deterministic for fixtures that opt in via
            // ManageDefaultSettings: point it at THIS fixture's Settings so tests reading
            // AddressableAssetSettingsDefaultObject.Settings (e.g. via AddressableAssetUtility.IsPathValidForEntry)
            // don't depend on whatever the previously-run fixture left behind. Requires a persisted asset --
            // the setter refuses an in-memory (PersistSettings=false) settings object.
            if (ManageDefaultSettings && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(Settings)))
            {
                m_PreviousDefaultSettings = AddressableAssetSettingsDefaultObject.Settings;
                AddressableAssetSettingsDefaultObject.Settings = Settings;
                m_DidOverrideDefaultSettings = true;
            }

            OnInit();

            //TODO: Remove when NSImage warning issue on bokken is fixed
            //Removing here in the event we didn't recieve any messages during the setup, we can respond appropriately to
            //logs in the tests.
            Application.logMessageReceived -= CheckLogForWarning;
            if (resetFailingMessages)
                LogAssert.ignoreFailingMessages = false;

            // the way our package isolation tests work the tests are built into their own package
            // which means we have to load expected files from a different location
            m_PackagePath = AddressablesTestUtility.GetPackagePath() + "/Tests/Editor";
        }

        private bool resetFailingMessages = false;

        //TODO: Remove when NSImage warning issue on bokken is fixed
        private void CheckLogForWarning(string condition, string stackTrace, LogType type)
        {
            LogAssert.ignoreFailingMessages = true;
            resetFailingMessages = true;
        }

        protected virtual void OnInit()
        {
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            OnCleanup();

            if (m_DidOverrideDefaultSettings)
            {
                AddressablesAssetPostProcessor.OnPostProcess.Unregister(Settings.OnPostprocessAllAssets);
                AddressableAssetSettingsDefaultObject.Settings = m_PreviousDefaultSettings;
                m_DidOverrideDefaultSettings = false;
                m_PreviousDefaultSettings = null;
            }

            if (Directory.Exists(TestFolder))
            {
                Debug.Log($"{GetType()} - (cleanup) deleting {TestFolder}");
                AssetDatabase.DeleteAsset(TestFolder);
            }

            if (Directory.Exists(AddressablesFolder))
            {
                Debug.Log($"{GetType()} (init) - deleting {AddressablesFolder}");
                if (!AssetDatabase.DeleteAsset(AddressablesFolder))
                    Directory.Delete(AddressablesFolder);
            }

            EditorBuildSettings.RemoveConfigObject(k_TestConfigName);
        }

        protected virtual void OnCleanup()
        {
        }

        /// <summary>
        /// Temporarily sets <see cref="AddressableAssetSettings.EnableJsonCatalog"/> to <paramref name="value"/>,
        /// runs <paramref name="body"/>, then restores the previous value. Use in tests that exercise
        /// the build pipeline so the catalog format under test is controlled without permanently
        /// mutating the shared <see cref="Settings"/> instance.
        /// </summary>
        protected void WithEnableJsonCatalog(bool value, Action body)
        {
            bool prev = Settings.EnableJsonCatalog;
            Settings.EnableJsonCatalog = value;
            try { body(); }
            finally { Settings.EnableJsonCatalog = prev; }
        }

        protected string CreateAsset(string assetPath, string objectName = null)
        {
            if (string.IsNullOrEmpty(objectName))
                objectName = Path.GetFileNameWithoutExtension(assetPath);

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = objectName;
            //this is to ensure that bundles are different for every run.
            go.transform.localPosition = UnityEngine.Random.onUnitSphere;

            string directoryName = Path.GetDirectoryName(assetPath);
            CreateFolderDeep(directoryName);
            try
            {
                Assert.IsTrue(AssetDatabase.IsValidFolder(directoryName), "Attempting to save prefab to invalid Folder : " + directoryName);
                Assert.IsTrue(Directory.Exists(directoryName), "Folder is not in FileSystem, but is in ADB : " + directoryName);
                Assert.IsNotNull(go, "Attempting to save null GameObject to Prefab");
                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error while attempting to save prefab {objectName} to {assetPath} with Exception message {e.Message}");
                throw e;
            }

            UnityEngine.Object.DestroyImmediate(go, false);
            return AssetDatabase.AssetPathToGUID(assetPath);
        }

        protected string CreateFolderDeep(string path)
        {
            path = path.Replace('\\', '/');
            if (!path.StartsWith("Assets/", StringComparison.Ordinal))
                return null;

            if (Directory.Exists(path))
            {
                if (AssetDatabase.IsValidFolder(path))
                    return AssetDatabase.AssetPathToGUID(path);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                return AssetDatabase.AssetPathToGUID(path);
            }

            Directory.CreateDirectory(path);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.AssetPathToGUID(path);
        }

        protected void ReloadSettings()
        {
            m_Settings = null;
            EditorBuildSettings.RemoveConfigObject(AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName);
            EditorBuildSettings.TryGetConfigObject(AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, out m_Settings);
        }

        /// <summary>
        /// Discards the in-memory settings instance and reloads it from disk (used by serialization determinism tests).
        /// Also assigns <see cref="AddressableAssetSettingsDefaultObject.Settings"/> so code paths that read the global
        /// default use the same instance as this test base after reload.
        /// </summary>
        protected void ReloadSettingsAssetFromDisk()
        {
            if (m_Settings == null)
                throw new InvalidOperationException($"{nameof(ReloadSettingsAssetFromDisk)} requires Settings to exist.");

            string path = AssetDatabase.GetAssetPath(m_Settings);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            m_Settings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(path);
            AddressableAssetSettingsDefaultObject.Settings = m_Settings;
        }

        protected string GetExpectedPath(string filename)
        {
            return $"{m_PackagePath}/Expected/{filename}";
        }

        protected string GetFixturePath(string filename)
        {
            return $"{m_PackagePath}/Fixtures/{filename}";
        }

    }
}
