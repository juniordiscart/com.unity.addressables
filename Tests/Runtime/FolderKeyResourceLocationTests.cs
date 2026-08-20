using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
#endif

namespace FolderKeyResourceLocationTests
{
    // Covers the "folder key" feature end-to-end through a real Addressables build: marking a
    // folder Addressable makes the folder's own address load every asset inside it in one call,
    // mirroring Resources.LoadAll. See AddressableAssetEntry.ParentFolderAddress and
    // BundledAssetGroupSchema.IncludeFolderKeysInCatalog.
    public abstract class FolderKeyResourceLocationTestsBase : AddressablesTestFixture
    {
        protected const string k_FolderAddress = "FolderKeyFolder";

        // Overridden by the "disabled" variant to prove the schema toggle actually gates the feature.
        protected virtual bool ExpectFolderKeyEnabled => true;

        // Overridden by the "exclude address" variant to prove that toggle drops individual
        // per-child addresses while the folder key keeps working.
        protected virtual bool IncludeAddressesForFolderChildren => true;

#if UNITY_EDITOR
        internal override void Setup(AddressableAssetSettings settings, string tempAssetFolder)
        {
            var schema = settings.DefaultGroup.GetSchema<BundledAssetGroupSchema>();
            schema.IncludeFolderKeysInCatalog = ExpectFolderKeyEnabled;
            schema.IncludeAddressesForFolderChildren = IncludeAddressesForFolderChildren;

            string folderPath = $"{tempAssetFolder}/{k_FolderAddress}";
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            CreatePrefab($"{folderPath}/go1.prefab");
            CreatePrefab($"{folderPath}/go2.prefab");

            var tex = new Texture2D(4, 4);
            string texPath = $"{folderPath}/tex1.png";
            File.WriteAllBytes(texPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceSynchronousImport);

            string folderGuid = AssetDatabase.AssetPathToGUID(folderPath);
            AddressableAssetEntry folder = settings.CreateOrMoveEntry(folderGuid, settings.DefaultGroup);
            folder.address = k_FolderAddress;
            folder.IsFolder = true;
        }
#endif

        [UnityTest]
        public IEnumerator LoadResourceLocationsAsync_WithFolderAddress_AnyType_ReturnsExpectedCount()
        {
            var handle = m_Addressables.LoadResourceLocationsAsync(k_FolderAddress, null);
            yield return handle;

            // 2 prefabs (GameObject) + 1 PNG (Texture2D) directly inside the folder.
            Assert.AreEqual(ExpectFolderKeyEnabled ? 3 : 0, handle.Result.Count);

            handle.Release();
        }

        [UnityTest]
        public IEnumerator LoadResourceLocationsAsync_WithFolderAddress_GameObjectType_ReturnsOnlyGameObjects()
        {
            var handle = m_Addressables.LoadResourceLocationsAsync(k_FolderAddress, typeof(GameObject));
            yield return handle;

            if (ExpectFolderKeyEnabled)
            {
                Assert.AreEqual(2, handle.Result.Count);
                Assert.IsTrue(handle.Result.All(l => l.ResourceType == typeof(GameObject)));
            }
            else
            {
                Assert.AreEqual(0, handle.Result.Count);
            }

            handle.Release();
        }

        [UnityTest]
        public IEnumerator LoadResourceLocationsAsync_WithIndividualChildAddress_FailsWhenExcludedFromCatalog()
        {
            // Proves the trade-off: when IncludeAddressesForFolderChildren is off, a child can no
            // longer be resolved by its own individual address -- only via the folder key (see
            // LoadResourceLocationsAsync_WithFolderAddress_AnyType_ReturnsExpectedCount above).
            var handle = m_Addressables.LoadResourceLocationsAsync($"{k_FolderAddress}/go1.prefab", typeof(GameObject));
            yield return handle;

            Assert.AreEqual(IncludeAddressesForFolderChildren ? 1 : 0, handle.Result.Count);

            handle.Release();
        }
    }

    public abstract class FolderKeyResourceLocationTests : FolderKeyResourceLocationTestsBase
    {
    }

    public abstract class FolderKeyDisabledResourceLocationTests : FolderKeyResourceLocationTestsBase
    {
        protected override bool ExpectFolderKeyEnabled => false;
    }

    public abstract class FolderKeyExcludeAddressResourceLocationTests : FolderKeyResourceLocationTestsBase
    {
        protected override bool IncludeAddressesForFolderChildren => false;
    }

#if UNITY_EDITOR
    class FolderKeyResourceLocationTests_FastMode : FolderKeyResourceLocationTests
    {
        protected override TestBuildScriptMode BuildScriptMode => TestBuildScriptMode.Fast;
    }

    class FolderKeyResourceLocationTests_PackedPlaymodeMode : FolderKeyResourceLocationTests
    {
        protected override TestBuildScriptMode BuildScriptMode => TestBuildScriptMode.PackedPlaymode;
    }

    class FolderKeyDisabledResourceLocationTests_FastMode : FolderKeyDisabledResourceLocationTests
    {
        protected override TestBuildScriptMode BuildScriptMode => TestBuildScriptMode.Fast;
    }

    class FolderKeyDisabledResourceLocationTests_PackedPlaymodeMode : FolderKeyDisabledResourceLocationTests
    {
        protected override TestBuildScriptMode BuildScriptMode => TestBuildScriptMode.PackedPlaymode;
    }

    class FolderKeyExcludeAddressResourceLocationTests_FastMode : FolderKeyExcludeAddressResourceLocationTests
    {
        protected override TestBuildScriptMode BuildScriptMode => TestBuildScriptMode.Fast;
    }

    class FolderKeyExcludeAddressResourceLocationTests_PackedPlaymodeMode : FolderKeyExcludeAddressResourceLocationTests
    {
        protected override TestBuildScriptMode BuildScriptMode => TestBuildScriptMode.PackedPlaymode;
    }
#endif

    [UnityPlatform(exclude = new[] {RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor})]
    class FolderKeyResourceLocationTests_PackedMode : FolderKeyResourceLocationTests
    {
        protected override TestBuildScriptMode BuildScriptMode => TestBuildScriptMode.Packed;
    }

    [UnityPlatform(exclude = new[] {RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor})]
    class FolderKeyDisabledResourceLocationTests_PackedMode : FolderKeyDisabledResourceLocationTests
    {
        protected override TestBuildScriptMode BuildScriptMode => TestBuildScriptMode.Packed;
    }

    [UnityPlatform(exclude = new[] {RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor})]
    class FolderKeyExcludeAddressResourceLocationTests_PackedMode : FolderKeyExcludeAddressResourceLocationTests
    {
        protected override TestBuildScriptMode BuildScriptMode => TestBuildScriptMode.Packed;
    }
}
