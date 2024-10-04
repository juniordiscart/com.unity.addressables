using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.TestTools;
using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.ResourceProviders;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace AddressableAssetSettingsResourceLocationTests
{
    public abstract class AddressableAssetSettingsResourceLocationTests : AddressablesTestFixture
    {
        const string k_ValidKey = "key";
        const string k_InvalidKey = "[key]";

        const string k_FolderAddress = "Folder";
        const string k_SubFolderAddress = "Folder/subfolder.prefab";
        const string k_SceneSubFolderAddress = "Folder/subscene.unity";

#if UNITY_EDITOR
        //We're temporarily removing this setup since it's causing errors on Yamato
        internal override void Setup(AddressableAssetSettings settings, string tempAssetFolder)
        {

            string folderGuid = AssetDatabase.CreateFolder(tempAssetFolder, k_FolderAddress);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SaveScene(scene, $"{tempAssetFolder}/{k_FolderAddress}/subscene.unity");
            EditorSceneManager.CloseScene(scene, true);

            AssetDatabase.Refresh();

            GameObject testObject = new GameObject("TestObject");
            GameObject testObject2 = new GameObject("TestObject2");
            GameObject subFolderEntry = new GameObject("SubFolder");
            string path = tempAssetFolder + "/test.prefab";
            string path2 = tempAssetFolder + "/test2.prefab";
            string path3 = $"{tempAssetFolder}/{k_FolderAddress}/subfolder.prefab";
            PrefabUtility.SaveAsPrefabAsset(testObject, path);
            PrefabUtility.SaveAsPrefabAsset(testObject2, path2);
            PrefabUtility.SaveAsPrefabAsset(subFolderEntry, path3);
            string guid = AssetDatabase.AssetPathToGUID(path);
            string guid2 = AssetDatabase.AssetPathToGUID(path2);

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.address = k_ValidKey;

            entry = settings.CreateOrMoveEntry(guid2, settings.DefaultGroup);
            entry.m_Address = k_InvalidKey;

            AddressableAssetEntry folder = settings.CreateOrMoveEntry(folderGuid, settings.DefaultGroup);
            folder.address = k_FolderAddress;
            folder.IsFolder = true;
        }

        protected override void OnRuntimeSetup()
        {
            // Only keep AddressableAssetSettingsLocator
            List<IResourceLocator> locators = m_Addressables.ResourceLocators.ToList();
            foreach (IResourceLocator locator in locators)
            {
                if (locator.GetType() != typeof(AddressableAssetSettingsLocator))
                    m_Addressables.RemoveResourceLocator(locator);
            }
        }

        [Test]
        public void WhenKeyIsValid_AddressableAssetSettingsLocator_ReturnsLocations()
        {
            var res = m_Addressables.GetResourceLocations(k_ValidKey, typeof(GameObject), out IList<IResourceLocation> locs);
            Assert.IsTrue(res);
            Assert.IsNotNull(locs);
            Assert.AreEqual(1, locs.Count);
        }

        [UnityTest]
        public IEnumerator GetResourceLocations_IncludesFolderGameObjectSubEntries_NullType()
        {
            IList<IResourceLocation> locations = new List<IResourceLocation>();
            yield return m_Addressables.GetResourceLocations(k_SubFolderAddress, null, out locations);
            Assert.AreEqual(1, locations.Count);
        }

        [UnityTest]
        public IEnumerator GetResourceLocations_IncludesFolderGameObjectSubEntries_RuntimeType()
        {
            IList<IResourceLocation> locations = new List<IResourceLocation>();
            yield return m_Addressables.GetResourceLocations(k_SubFolderAddress, typeof(GameObject), out locations);
            Assert.AreEqual(1, locations.Count);
        }

        [UnityTest]
        public IEnumerator GetResourceLocations_IncludesFolderSceneSubEntries_NullType()
        {
            IList<IResourceLocation> locations = new List<IResourceLocation>();
            yield return m_Addressables.GetResourceLocations(k_SceneSubFolderAddress, null, out locations);
            Assert.AreEqual(1, locations.Count);
        }

        [UnityTest]
        public IEnumerator GetResourceLocations_IncludesFolderSceneSubEntries_RuntimeSceneType()
        {
            IList<IResourceLocation> locations = new List<IResourceLocation>();
            yield return m_Addressables.GetResourceLocations(k_SceneSubFolderAddress, typeof(SceneInstance), out locations);
            Assert.AreEqual(1, locations.Count);
        }

        [Test]
        public void WhenKeyHasSquareBrackets_AddressableAssetSettingsLocator_ThrowsExceptionAndReturnsNoLocations()
        {
            var res = m_Addressables.GetResourceLocations(k_InvalidKey, typeof(GameObject), out IList<IResourceLocation> locs);
            LogAssert.Expect(LogType.Error, $"Address '{k_InvalidKey}' cannot contain '[ ]'.");
            Assert.IsFalse(res);
            Assert.IsNull(locs);
        }

        [Test]
        public void ComparingTwoResourceLocations_ReturnsTrue_IfInternalIdAndResourceTypeMatch()
        {
            IResourceLocation loc1 = new ResourceLocationBase("1", "internalid", "provider", typeof(GameObject));
            IResourceLocation loc2 = new ResourceLocationBase("2", "internalid", "provider", typeof(GameObject));

            ResourceLocationComparer comparer = new ResourceLocationComparer();

            Assert.IsTrue(comparer.Equals(loc1, loc2));
        }

        [Test]
        public void ComparingTwoResourceLocations_ReturnsFalse_IfInternalIdOrResourceTypeDontMatch()
        {
            IResourceLocation loc1 = new ResourceLocationBase("1", "internalid", "provider", typeof(GameObject));
            IResourceLocation loc2 = new ResourceLocationBase("2", "internalid2", "provider", typeof(GameObject));
            IResourceLocation loc3 = new ResourceLocationBase("3", "internalid", "provider", typeof(Sprite));

            ResourceLocationComparer comparer = new ResourceLocationComparer();

            Assert.IsFalse(comparer.Equals(loc1, loc2));
            Assert.IsFalse(comparer.Equals(loc1, loc3));
        }

#endif
    }

#if UNITY_EDITOR
    [Ignore("Issues with folder creation on yamato")]
    class AddressableAssetSettingsResourceLocationTests_FastMode : AddressableAssetSettingsResourceLocationTests
    {
        protected override TestBuildScriptMode BuildScriptMode
        {
            get { return TestBuildScriptMode.Fast; }
        }
    }
#endif
}
