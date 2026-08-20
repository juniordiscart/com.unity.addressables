#if ENABLE_CONTENT_DIRECTORIES
using NUnit.Framework;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
#endif

namespace UnityEngine.AddressableAssets.ResourceProviders.Tests
{
    /// <summary>
    /// Integration tests for Content Directory scene loading: builds a CD group with two
    /// scenes plus a regular asset and asserts each address resolves to its own content.
    /// </summary>
    public class ContentDirectorySceneTests : AddressablesTestFixture
    {
        protected override TestBuildScriptMode BuildScriptMode => TestBuildScriptMode.SchemaDriven;

        const string k_GroupName = "ContentDirectorySceneTestGroup";
        const string k_SceneKeyA = "cd_scene_a";
        const string k_SceneKeyB = "cd_scene_b";
        const string k_PrefabKey = "cd_scene_test_prefab";

#if UNITY_EDITOR
        static string CreateSceneAsset(string assetPath, string markerObjectName)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject(markerObjectName);
            EditorSceneManager.SaveScene(scene, assetPath);
            return AssetDatabase.AssetPathToGUID(scene.path);
        }

        internal override void Setup(AddressableAssetSettings settings, string tempAssetFolder)
        {
            // Create Content Directory group
            AddressableAssetGroup cdGroup = settings.CreateGroup(
                k_GroupName, false, false, false, null, typeof(ContentDirectoryGroupSchema));

            settings.profileSettings.SetValue(settings.activeProfileId, AddressableAssetSettings.kLocalBuildPath,
                $"{AddressableAssetSettings.kLocalBuildPathValue}/{m_UniqueTestName}");
            settings.profileSettings.SetValue(settings.activeProfileId, AddressableAssetSettings.kLocalLoadPath,
                $"{AddressableAssetSettings.kLocalLoadPathValue}/{m_UniqueTestName}");

            string fixtureLibraryDir = Path.Combine(
                Addressables.BuildPath,
                EditorUserBuildSettings.activeBuildTarget.ToString(),
                m_UniqueTestName);
            if (Directory.Exists(fixtureLibraryDir))
                Directory.Delete(fixtureLibraryDir, recursive: true);

            var schema = cdGroup.GetSchema<ContentDirectoryGroupSchema>();
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            schema.CatalogId = $"test_scene_catalog_{m_UniqueTestName}";

            // Create two test scenes
            string scenePathA = CreateAssetPath(tempAssetFolder, k_SceneKeyA, ".unity");
            string sceneGuidA = CreateSceneAsset(scenePathA, "MarkerA");
            var sceneEntryA = settings.CreateOrMoveEntry(sceneGuidA, cdGroup, false, false);
            sceneEntryA.address = k_SceneKeyA;

            string scenePathB = CreateAssetPath(tempAssetFolder, k_SceneKeyB, ".unity");
            string sceneGuidB = CreateSceneAsset(scenePathB, "MarkerB");
            var sceneEntryB = settings.CreateOrMoveEntry(sceneGuidB, cdGroup, false, false);
            sceneEntryB.address = k_SceneKeyB;

            // Create test prefab
            string prefabPath = CreateAssetPath(tempAssetFolder, k_PrefabKey, ".prefab");
            string prefabGuid = CreatePrefab(prefabPath);
            var prefabEntry = settings.CreateOrMoveEntry(prefabGuid, cdGroup, false, false);
            prefabEntry.address = k_PrefabKey;
        }

        protected override void RunBuilder(AddressableAssetSettings settings)
        {
            try
            {
                base.RunBuilder(settings);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"ContentDirectorySceneTests: RunBuilder failed: {ex}");
                Assert.Fail($"Addressables build (RunBuilder) failed (see Console). {ex}");
            }
        }
#endif

        protected override IEnumerator InitAddressables()
        {
            var op = m_Addressables.InitializeAsync(m_RuntimeSettingsPath, null, false);
            yield return op;
            if (op.Status != AsyncOperationStatus.Succeeded)
            {
                var details = op.OperationException?.ToString() ?? "(no OperationException on handle)";
                Assert.Fail($"InitializeAsync failed: Status={op.Status}. OperationException: {details}");
            }

            OnRuntimeSetup();
            if (op.IsValid())
                op.Release();
        }

        [UnityTest]
        public IEnumerator LoadSceneAsync_LoadsCorrectSceneForEachKey()
        {
            var opA = m_Addressables.LoadSceneAsync(k_SceneKeyA, new LoadSceneParameters(LoadSceneMode.Additive));
            yield return opA;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, opA.Status, "Loading scene A should succeed.");
            Assert.AreEqual(k_SceneKeyA, opA.Result.Scene.name, "Address A should load scene A, not whichever scene got index 0.");

            var opB = m_Addressables.LoadSceneAsync(k_SceneKeyB, new LoadSceneParameters(LoadSceneMode.Additive));
            yield return opB;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, opB.Status, "Loading scene B should succeed.");
            Assert.AreEqual(k_SceneKeyB, opB.Result.Scene.name, "Address B should load scene B, not scene A.");

            Assert.AreNotEqual(opA.Result.Scene.handle, opB.Result.Scene.handle, "The two addresses must resolve to two distinct scene instances.");

            yield return UnloadSceneFromHandler(opA, m_Addressables);
            yield return UnloadSceneFromHandler(opB, m_Addressables);
        }

        [UnityTest]
        public IEnumerator LoadAssetAsync_OnSceneKey_FailsWithException()
        {
            LogAssert.Expect(LogType.Error, new Regex(".*catalog entry is a scene, not a regular asset.*"));

            var handle = m_Addressables.LoadAssetAsync<object>(k_SceneKeyA);
            yield return handle;

            Assert.AreEqual(AsyncOperationStatus.Failed, handle.Status, "Loading a scene entry as a regular asset must fail, not silently return asset index 0.");
            Assert.IsNotNull(handle.OperationException);
            StringAssert.Contains("catalog entry is a scene, not a regular asset", handle.OperationException.Message);
        }
    }
}
#endif
