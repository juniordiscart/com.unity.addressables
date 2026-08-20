using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.U2D;

namespace UnityEditor.AddressableAssets.Tests
{
    // Covers the "folder key" feature: marking a folder Addressable causes the folder's own
    // address to be added as an extra shared catalog key on every asset inside it, mirroring
    // how labels already work. See AddressableAssetEntry.ParentFolderAddress / CreateKeyList /
    // CreateCatalogEntries.
    public class AddressableAssetEntryFolderKeyTests : AddressableAssetTestBase
    {
        AddressableAssetGroup m_testGroup;

        protected override void OnInit()
        {
            m_testGroup = Settings.CreateGroup("folderKeyTestGroup", false, false, false, null, typeof(BundledAssetGroupSchema));
        }

        protected override void OnCleanup()
        {
            Settings.RemoveGroup(m_testGroup);
        }

        [Test]
        public void CreateCatalogEntries_WhenIncludeFolderKeysIsTrue_ChildrenGetFolderAddressAsKey()
        {
            string folderPath = GetAssetPath("FolderKeyTest1");
            string subFolderPath = folderPath + "/Sub";
            Directory.CreateDirectory(subFolderPath);

            CreateAsset(folderPath + "/top.prefab", "top");
            CreateAsset(subFolderPath + "/nested.prefab", "nested");

            AssetDatabase.ImportAsset(folderPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var folderGuid = AssetDatabase.AssetPathToGUID(folderPath);
            var folderEntry = Settings.CreateOrMoveEntry(folderGuid, m_testGroup, false);
            folderEntry.SetAddress("FolderKeyTest1");

            try
            {
                var childEntries = new List<AddressableAssetEntry>();
                folderEntry.GatherAllAssets(childEntries, false, true, true);
                Assert.AreEqual(2, childEntries.Count);

                foreach (var child in childEntries)
                {
                    var entries = new List<ContentCatalogDataEntry>();
                    var providerTypes = new HashSet<Type>();
                    child.CreateCatalogEntries(entries, false, "doesntMatter", null, null, null, providerTypes,
                        true, true, true, null, includeFolderKeys: true);
                    Assert.Greater(entries.Count, 0, $"No catalog entries created for {child.address}");
                    foreach (var e in entries)
                        CollectionAssert.Contains(e.Keys, "FolderKeyTest1", $"Folder key missing for {child.address}");
                }
            }
            finally
            {
                Settings.RemoveAssetEntry(folderPath);
                AssetDatabase.DeleteAsset(folderPath);
            }
        }

        [Test]
        public void CreateCatalogEntries_WhenIncludeFolderKeysIsFalse_ChildrenDoNotGetFolderAddressAsKey()
        {
            string folderPath = GetAssetPath("FolderKeyTest2");
            Directory.CreateDirectory(folderPath);
            CreateAsset(folderPath + "/top.prefab", "top");

            AssetDatabase.ImportAsset(folderPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var folderGuid = AssetDatabase.AssetPathToGUID(folderPath);
            var folderEntry = Settings.CreateOrMoveEntry(folderGuid, m_testGroup, false);
            folderEntry.SetAddress("FolderKeyTest2");

            try
            {
                var childEntries = new List<AddressableAssetEntry>();
                folderEntry.GatherAllAssets(childEntries, false, true, true);
                Assert.AreEqual(1, childEntries.Count);
                var child = childEntries[0];

                var entries = new List<ContentCatalogDataEntry>();
                var providerTypes = new HashSet<Type>();
                child.CreateCatalogEntries(entries, false, "doesntMatter", null, null, null, providerTypes,
                    true, true, true, null, includeFolderKeys: false);
                Assert.Greater(entries.Count, 0);
                foreach (var e in entries)
                    CollectionAssert.DoesNotContain(e.Keys, "FolderKeyTest2");
            }
            finally
            {
                Settings.RemoveAssetEntry(folderPath);
                AssetDatabase.DeleteAsset(folderPath);
            }
        }

        [Test]
        public void CreateCatalogEntries_WhenIncludeAddressesForFolderChildrenIsFalse_ChildLosesOwnAddressButKeepsGuidAndFolderKey()
        {
            string folderPath = GetAssetPath("FolderKeyTest4");
            Directory.CreateDirectory(folderPath);
            var childGuid = CreateAsset(folderPath + "/child1.prefab", "child1");

            AssetDatabase.ImportAsset(folderPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var folderGuid = AssetDatabase.AssetPathToGUID(folderPath);
            var folderEntry = Settings.CreateOrMoveEntry(folderGuid, m_testGroup, false);
            folderEntry.SetAddress("FolderKeyTest4");

            try
            {
                var childEntries = new List<AddressableAssetEntry>();
                folderEntry.GatherAllAssets(childEntries, false, true, true);
                Assert.AreEqual(1, childEntries.Count);
                var child = childEntries[0];
                string childAddress = child.address;

                var entries = new List<ContentCatalogDataEntry>();
                var providerTypes = new HashSet<Type>();
                child.CreateCatalogEntries(entries, false, "doesntMatter", null, null, null, providerTypes,
                    true, true, true, null, includeFolderKeys: true, includeAddressesForFolderChildren: false);
                Assert.Greater(entries.Count, 0);
                foreach (var e in entries)
                {
                    CollectionAssert.DoesNotContain(e.Keys, childAddress, "Child's own address should have been excluded.");
                    CollectionAssert.Contains(e.Keys, childGuid, "GUID should still be present -- only the address is excluded.");
                    CollectionAssert.Contains(e.Keys, "FolderKeyTest4", "Folder key should still be present.");
                    Assert.AreEqual(childGuid, e.Keys[0], "PrimaryKey/Keys[0] should fall back to the GUID when the address is excluded.");
                }
            }
            finally
            {
                Settings.RemoveAssetEntry(folderPath);
                AssetDatabase.DeleteAsset(folderPath);
            }
        }

        [Test]
        public void CreateCatalogEntries_WhenIncludeAddressesForFolderChildrenIsFalse_NonFolderEntryIsUnaffected()
        {
            var path = GetAssetPath("standaloneExclude.prefab");
            var guid = CreateAsset(path, "standaloneExclude");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var entry = Settings.CreateOrMoveEntry(guid, m_testGroup);
            entry.SetAddress("StandaloneExcludeAddress");

            try
            {
                var entries = new List<ContentCatalogDataEntry>();
                var providerTypes = new HashSet<Type>();
                entry.CreateCatalogEntries(entries, false, "doesntMatter", null, null, null, providerTypes,
                    true, true, true, null, includeFolderKeys: true, includeAddressesForFolderChildren: false);
                Assert.Greater(entries.Count, 0);
                foreach (var e in entries)
                {
                    CollectionAssert.Contains(e.Keys, "StandaloneExcludeAddress",
                        "Non-folder entry's address should be unaffected by the folder-child exclusion toggle.");
                    Assert.AreEqual("StandaloneExcludeAddress", e.Keys[0]);
                }
            }
            finally
            {
                Settings.RemoveAssetEntry(path);
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void ParentFolderAddress_ForNestedSubfolderEntry_ReturnsTopMarkedFolderAddress_NotIntermediate()
        {
            string folderPath = GetAssetPath("FolderKeyTest3");
            string subFolderPath = folderPath + "/Sub";
            Directory.CreateDirectory(subFolderPath);
            var nestedGuid = CreateAsset(subFolderPath + "/nested.prefab", "nested");

            AssetDatabase.ImportAsset(folderPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var folderGuid = AssetDatabase.AssetPathToGUID(folderPath);
            var topEntry = Settings.CreateOrMoveEntry(folderGuid, m_testGroup, false);
            topEntry.SetAddress("FolderKeyTest3");

            try
            {
                // Simulate the Groups window's one-level-at-a-time tree expansion: gather only
                // the immediate subfolder as a folder entry (recurseAll: false), then gather ITS
                // children separately. This is what produces a real ParentEntry chain longer
                // than one hop (GatherFolderEntries always sets ParentEntry to whichever entry
                // GatherAllAssets was invoked on).
                var topLevelEntries = new List<AddressableAssetEntry>();
                topEntry.GatherAllAssets(topLevelEntries, false, false, true);
                var subfolderEntry = topLevelEntries.Single(e => e.IsFolder);

                var subEntries = new List<AddressableAssetEntry>();
                subfolderEntry.GatherAllAssets(subEntries, false, false, true);
                var nestedEntry = subEntries.Single(e => e.guid == nestedGuid);

                Assert.AreEqual(subfolderEntry, nestedEntry.ParentEntry,
                    "Test setup assumption violated: nested entry should be a direct child of the subfolder entry.");
                Assert.AreEqual(topEntry.address, nestedEntry.ParentFolderAddress,
                    "ParentFolderAddress should walk to the top marked folder, not the intermediate subfolder.");
                Assert.AreNotEqual(subfolderEntry.address, nestedEntry.ParentFolderAddress);
            }
            finally
            {
                Settings.RemoveAssetEntry(folderPath);
                AssetDatabase.DeleteAsset(folderPath);
            }
        }

        [Test]
        public void ParentFolderAddress_ForNonSubAssetEntry_IsNull_AndCreateKeyListAddsNoSpuriousKey()
        {
            var path = GetAssetPath("standalone.prefab");
            var guid = CreateAsset(path, "standalone");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var entry = Settings.CreateOrMoveEntry(guid, m_testGroup);
            entry.SetAddress("StandaloneAddress");

            try
            {
                Assert.IsNull(entry.ParentFolderAddress);
                var keys = entry.CreateKeyList(true, true, true, true);
                // Address should only appear once even with folder-key inclusion enabled, since
                // there is no owning folder.
                Assert.AreEqual(1, keys.Count(k => (string)k == "StandaloneAddress"));
            }
            finally
            {
                Settings.RemoveAssetEntry(path);
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void CreateKeyList_WhenFolderKeyEqualsChildAddress_DoesNotDuplicateKey()
        {
            // Structural edge case: force ParentFolderAddress == address by wiring up a
            // standalone entry's ParentEntry directly. This configuration cannot arise from
            // normal folder gathering (a child's address always has the folder's address as a
            // strict prefix plus a relative path), but CreateKeyList must not double the key if
            // it ever does.
            var path = GetAssetPath("edgeCase.prefab");
            var guid = CreateAsset(path, "edgeCase");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var folderStandIn = new AddressableAssetEntry("fakeFolderGuid", "SameAddress", m_testGroup, true);
            // the fake guid resolves to no AssetPath, so force the folder flag
            // (ParentFolderAddress returns null for non-folder roots)
            folderStandIn.IsFolder = true;
            var entry = Settings.CreateOrMoveEntry(guid, m_testGroup);
            entry.SetAddress("SameAddress");
            entry.ParentEntry = folderStandIn;

            try
            {
                Assert.AreEqual("SameAddress", entry.ParentFolderAddress);
                var keys = entry.CreateKeyList(true, false, false, true);
                Assert.AreEqual(1, keys.Count(k => (string)k == "SameAddress"));
            }
            finally
            {
                entry.ParentEntry = null;
                Settings.RemoveAssetEntry(path);
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void ParentFolderAddress_ForSpriteInTopLevelAtlas_ReturnsNull_AndKeepsOwnAddress()
        {
            // Sprites gathered from a SpriteAtlas get ParentEntry = the atlas entry. When the
            // atlas is marked addressable directly (not sitting inside an addressable folder),
            // the root of the ParentEntry chain is not a folder, so no folder key must appear.
            var atlasPath = GetAssetPath("folderKeyAtlas.spriteatlas");
            var atlas = new SpriteAtlas();
            AssetDatabase.CreateAsset(atlas, atlasPath);

            var texturePath = GetAssetPath("folderKeySprite.png");
            File.WriteAllBytes(texturePath, Texture2D.whiteTexture.EncodeToPNG());
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();

            SpriteAtlasExtensions.Add(atlas, new[] {AssetDatabase.LoadAssetAtPath<Texture>(texturePath)});
            SpriteAtlasUtility.PackAtlases(new[] {atlas}, EditorUserBuildSettings.activeBuildTarget, false);

            var atlasEntry = Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(atlasPath), m_testGroup);
            atlasEntry.SetAddress("topAtlas");

            try
            {
                var gathered = new List<AddressableAssetEntry>();
                atlasEntry.GatherAllAssets(gathered, true, true, true);
                var spriteEntry = gathered.Single(e => e.IsSubAsset && e.ParentEntry == atlasEntry);

                Assert.IsNull(spriteEntry.ParentFolderAddress,
                    "The root of the ParentEntry chain is the atlas, not a folder, so ParentFolderAddress must be null.");

                var keys = spriteEntry.CreateKeyList(true, false, false, true, includeAddressesForFolderChildren: false);
                CollectionAssert.Contains(keys, spriteEntry.address,
                    "A sprite that is not a folder child must keep its own address even when includeAddressesForFolderChildren is off.");
                CollectionAssert.DoesNotContain(keys, atlasEntry.address,
                    "The atlas address must not be added to the sprite's keys as a spurious folder key.");
            }
            finally
            {
                Settings.RemoveAssetEntry(atlasEntry.guid);
                AssetDatabase.DeleteAsset(atlasPath);
                AssetDatabase.DeleteAsset(texturePath);
            }
        }

        [Test]
        public void ParentFolderAddress_ForSubObjectOfTopLevelAsset_ReturnsNull_AndKeepsOwnAddress()
        {
            // Sub-object entries get ParentEntry = the main asset entry. When the main asset is
            // marked addressable directly, the root of the chain is not a folder.
            var path = GetAssetPath("folderKeySubObjects.asset");
            AssetDatabase.CreateAsset(UnityEngine.AddressableAssets.Tests.TestObject.Create("main"), path);
            AssetDatabase.AddObjectToAsset(UnityEngine.AddressableAssets.Tests.TestObject2.Create("sub"), path);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var mainEntry = Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), m_testGroup);
            mainEntry.SetAddress("topAsset");

            try
            {
                var gathered = new List<AddressableAssetEntry>();
                mainEntry.GatherAllAssets(gathered, true, true, true);
                var subEntry = gathered.Single(e => e.IsSubAsset && e.ParentEntry == mainEntry);

                Assert.IsNull(subEntry.ParentFolderAddress,
                    "The root of the ParentEntry chain is the main asset, not a folder, so ParentFolderAddress must be null.");

                var keys = subEntry.CreateKeyList(true, false, false, true, includeAddressesForFolderChildren: false);
                CollectionAssert.Contains(keys, subEntry.address,
                    "A sub-object that is not a folder child must keep its own address even when includeAddressesForFolderChildren is off.");
                CollectionAssert.DoesNotContain(keys, mainEntry.address,
                    "The main asset address must not be added to the sub-object's keys as a spurious folder key.");
            }
            finally
            {
                Settings.RemoveAssetEntry(mainEntry.guid);
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void IsFolder_ComputesLazily_WithoutGathering()
        {
            // IsFolder must be correct even before any gather pass runs (it used to default to
            // false until GatherAllAssets set it, e.g. right after a domain reload).
            string folderPath = GetAssetPath("FolderKeyLazyIsFolder");
            Directory.CreateDirectory(folderPath);
            var assetGuid = CreateAsset(folderPath + "/lazy.prefab", "lazy");
            AssetDatabase.ImportAsset(folderPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var folderEntry = Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(folderPath), m_testGroup);
            var assetEntry = Settings.CreateOrMoveEntry(assetGuid, m_testGroup);

            try
            {
                Assert.IsTrue(folderEntry.IsFolder, "A folder entry must report IsFolder even when no gather pass has set the flag.");
                Assert.IsFalse(assetEntry.IsFolder, "A non-folder entry must not report IsFolder.");
            }
            finally
            {
                Settings.RemoveAssetEntry(assetGuid);
                Settings.RemoveAssetEntry(folderEntry.guid);
                AssetDatabase.DeleteAsset(folderPath);
            }
        }
    }
}
