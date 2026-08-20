#if ENABLE_CONTENT_DIRECTORIES
using NUnit.Framework;
using Unity.Loading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceProviders;

namespace UnityEditor.AddressableAssets.Tests
{
    /// <summary>
    /// Tests the object and scene ids returned by <see cref="AddressableRootAsset"/>.
    /// </summary>
    public class AddressableRootAssetTests
    {
        const string k_TempFolder = "Assets/AddressableRootAssetTests_Temp";

        AddressableRootAsset m_RootAsset;

        static string s_PreviousActiveScenePath;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            s_PreviousActiveScenePath = EditorSceneManager.GetActiveScene().path;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (!string.IsNullOrEmpty(s_PreviousActiveScenePath) &&
                EditorSceneManager.GetActiveScene().path != s_PreviousActiveScenePath)
            {
                EditorSceneManager.OpenScene(s_PreviousActiveScenePath, OpenSceneMode.Single);
            }
        }

        [SetUp]
        public void SetUp()
        {
            m_RootAsset = ScriptableObject.CreateInstance<AddressableRootAsset>();
            if (!AssetDatabase.IsValidFolder(k_TempFolder))
                AssetDatabase.CreateFolder("Assets", "AddressableRootAssetTests_Temp");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_RootAsset);
            if (AssetDatabase.IsValidFolder(k_TempFolder))
                AssetDatabase.DeleteAsset(k_TempFolder);
        }

        static LoadableSceneId CreateTestSceneId(string sceneName)
        {
            string path = $"{k_TempFolder}/{sceneName}.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
            return LoadableSceneIdEditorUtility.CreateLoadableSceneId(path);
        }

        static LoadableObjectId CreateTestObjectId(string assetName)
        {
            string path = $"{k_TempFolder}/{assetName}.asset";
            var asset = ScriptableObject.CreateInstance<ScriptableObject>();
            AssetDatabase.CreateAsset(asset, path);
            return LoadableObjectIdEditorUtility.CreateLoadableObjectId(asset.GetEntityId());
        }

        [Test]
        public void GetLoadableSceneId_NegativeId_ReturnsDefault()
        {
            m_RootAsset.AddScene(CreateTestSceneId("dummy"));

            Assert.AreEqual(default(LoadableSceneId), m_RootAsset.GetLoadableSceneId(-1));
        }

        [Test]
        public void GetLoadableObjectId_NegativeId_ReturnsDefault()
        {
            m_RootAsset.AddAsset(CreateTestObjectId("dummy"));

            Assert.AreEqual(default(LoadableObjectId), m_RootAsset.GetLoadableObjectId(-1));
        }

        [Test]
        public void GetLoadableSceneId_OutOfRangeId_ReturnsDefault()
        {
            Assert.AreEqual(default(LoadableSceneId), m_RootAsset.GetLoadableSceneId(5));
        }

        [Test]
        public void GetLoadableObjectId_OutOfRangeId_ReturnsDefault()
        {
            Assert.AreEqual(default(LoadableObjectId), m_RootAsset.GetLoadableObjectId(5));
        }

        [Test]
        public void AddScene_ThenGetLoadableSceneId_ReturnsMatchingId()
        {
            var sceneId = CreateTestSceneId("dummy");
            int index = m_RootAsset.AddScene(sceneId);

            Assert.AreEqual(sceneId, m_RootAsset.GetLoadableSceneId(index));
        }

        [Test]
        public void AddAsset_ThenGetLoadableObjectId_ReturnsMatchingId()
        {
            var objectId = CreateTestObjectId("dummy");
            int index = m_RootAsset.AddAsset(objectId);

            Assert.AreEqual(objectId, m_RootAsset.GetLoadableObjectId(index));
        }

        [Test]
        public void AddScene_TwoScenes_ReturnsDistinctIndicesThatResolveIndependently()
        {
            var sceneIdA = CreateTestSceneId("dummyA");
            var sceneIdB = CreateTestSceneId("dummyB");
            int indexA = m_RootAsset.AddScene(sceneIdA);
            int indexB = m_RootAsset.AddScene(sceneIdB);

            Assert.AreNotEqual(indexA, indexB);
            Assert.AreEqual(sceneIdA, m_RootAsset.GetLoadableSceneId(indexA));
            Assert.AreEqual(sceneIdB, m_RootAsset.GetLoadableSceneId(indexB));
        }
    }
}
#endif
