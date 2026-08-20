#if ENABLE_CONTENT_DIRECTORIES
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Loading;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.U2D;

namespace UnityEditor.AddressableAssets.Tests
{
    public class ContentDirectorySchemaBuilderTests : AddressableAssetTestBase
    {
        ContentDirectorySchemaBuilder schemaBuilder;
        AddressablesDataBuilderInput input;
        AddressableAssetsBuildContext aaContext = new AddressableAssetsBuildContext();
        AddressablesPlayerBuildResult addressablesBuildResult;
        List<string> m_CreatedAssetPaths = new List<string>();
        List<string> m_CreatedAssetGuids = new List<string>();

        SpritePackerMode m_SavedSpritePackingMode;

        /// <summary>Texture / sprite atlas platform row for in-Editor packing (NamedBuildTarget.Editor on newer Unity).</summary>
        const string kEditorTexturePlatformName = "Editor";

        [SetUp]
        public void Setup()
        {
            m_SavedSpritePackingMode = EditorSettings.spritePackerMode;
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;

            schemaBuilder = new ContentDirectorySchemaBuilder();
            aaContext = new AddressableAssetsBuildContext();
            aaContext.Settings = Settings;
            // set in SchemaDriverBuildScript
            aaContext.providerTypes = new HashSet<Type>();
            aaContext.runtimeData = new UnityEngine.AddressableAssets.Initialization.ResourceManagerRuntimeData();
            aaContext.Settings.activeProfileId = Settings.profileSettings.GetProfileId("Default");

            input = new AddressablesDataBuilderInput(Settings);
            input.Logger = new BuildLog();
            input.SetAllValues(Settings, EditorUserBuildSettings.selectedBuildTargetGroup, EditorUserBuildSettings.activeBuildTarget, "1.0", false, new string[0]);

            addressablesBuildResult = new AddressablesPlayerBuildResult();
            m_CreatedAssetPaths.Clear();
            m_CreatedAssetGuids.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up any addressable entries first
            foreach (var guid in m_CreatedAssetGuids)
            {
                Settings.RemoveAssetEntry(guid);
            }

            // Then delete the assets
            foreach (var path in m_CreatedAssetPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }

            m_CreatedAssetGuids.Clear();
            m_CreatedAssetPaths.Clear();

            EditorSettings.spritePackerMode = m_SavedSpritePackingMode;
        }

        [Test]
        public void ContentDirectorySchemaBuilder_GeneratesLocationsWithCorrectEntries()
        {
            string groupName = "GenerateCatalogTestGroup";
            string buildPath = Path.Combine(TestFolder, "ContentDirectories");
            Settings.profileSettings.SetValue(Settings.activeProfileId, "Local.LoadPath", buildPath);

            Directory.CreateDirectory(buildPath);

            AddressableAssetGroup group = GetGroupWithEntry(groupName);
            group.AddSchema(CreateSchema());
            var contentDirectoryGroupSchema = group.GetSchema<ContentDirectoryGroupSchema>();
            schemaBuilder.Init(null, input, null, null);
            schemaBuilder.ProcessGroupSchema(aaContext, contentDirectoryGroupSchema);

            StubBuildManifest(buildPath);

            var locations = schemaBuilder.GenerateCatalogLocations(aaContext, addressablesBuildResult);

            Assert.AreEqual(1, locations.Count, "Expected exactly one catalog id.");
            Assert.IsTrue(locations.ContainsKey(contentDirectoryGroupSchema.CatalogId),
                $"Expected catalog id '{contentDirectoryGroupSchema.CatalogId}' in the returned map.");
            var entries = locations[contentDirectoryGroupSchema.CatalogId];
            Assert.AreEqual(1, entries.Count(e => e.InternalId == "test"),
                "The loadable address didn't make it into the catalog locations.");
            // The standalone ContentDirectory location is no longer emitted; the load path now
            // travels inside each entry's ContentDirectoryAssetData instead.
            Assert.AreEqual(0, entries.Count(e => e.InternalId == buildPath),
                "A standalone content directory location should no longer be emitted.");

            var assetData = entries[0].Data as ContentDirectoryAssetData;
            Assert.IsNotNull(assetData, "Entry data should be ContentDirectoryAssetData.");
            Assert.AreEqual(buildPath, assetData.LoadPath, "The content directory load path should be embedded in the entry data.");

            Directory.Delete(buildPath, true);
            File.Delete(buildPath + ".meta");
        }

        [Test]
        public void ContentDirectorySchemaBuilder_CanGenerateMultipleCatalogs()
        {

            string buildPath = Path.Combine(TestFolder, "ContentDirectories");
            Settings.profileSettings.SetValue(Settings.activeProfileId, "Local.LoadPath", buildPath);
            Directory.CreateDirectory(buildPath);

            AddressableAssetGroup group = GetGroupWithEntry("GroupInDefaultCatalog");
            group.AddSchema(CreateSchema());
            AddressableAssetGroup group2 = GetGroupWithEntry("GroupInSecondCatalog");
            group2.AddSchema(CreateSchema());

            var contentDirectoryGroupSchema = group.GetSchema<ContentDirectoryGroupSchema>();
            var contentDirectoryGroupSchema2 = group2.GetSchema<ContentDirectoryGroupSchema>();
            contentDirectoryGroupSchema2.CatalogId = "SecondCatalogId";

            schemaBuilder.Init(null, input, null, null);
            schemaBuilder.ProcessGroupSchema(aaContext, contentDirectoryGroupSchema);
            schemaBuilder.ProcessGroupSchema(aaContext, contentDirectoryGroupSchema2);

            StubBuildManifest(buildPath);

            var locations = schemaBuilder.GenerateCatalogLocations(aaContext, addressablesBuildResult);

            Assert.AreEqual(2, locations.Count, "Expected two distinct catalog ids.");
            Assert.IsTrue(locations.ContainsKey(contentDirectoryGroupSchema.CatalogId),
                $"Expected catalog id '{contentDirectoryGroupSchema.CatalogId}' in the returned map.");
            Assert.IsTrue(locations.ContainsKey(contentDirectoryGroupSchema2.CatalogId),
                $"Expected catalog id '{contentDirectoryGroupSchema2.CatalogId}' in the returned map.");

            Directory.Delete(buildPath, true);
            File.Delete(buildPath + ".meta");
        }

        [Test]
        public void ContentDirectorySchemaBuilder_AddsMultipleCatalogsToAAContextRuntimeData()
        {

            string buildPath = Path.Combine(TestFolder, "ContentDirectories");
            Directory.CreateDirectory(buildPath);

            AddressableAssetGroup group = GetGroupWithEntry("GroupInDefaultCatalog");
            group.AddSchema(CreateSchema());
            AddressableAssetGroup group2 = GetGroupWithEntry("GroupInSecondCatalog");
            group2.AddSchema(CreateSchema());

            var contentDirectoryGroupSchema = group.GetSchema<ContentDirectoryGroupSchema>();
            var contentDirectoryGroupSchema2 = group2.GetSchema<ContentDirectoryGroupSchema>();

            Settings.profileSettings.SetValue(Settings.activeProfileId, "Local.LoadPath", buildPath);
            contentDirectoryGroupSchema2.CatalogId = "SecondCatalogId";

            schemaBuilder.Init(null, input, null, null);
            Assert.IsEmpty(schemaBuilder.ProcessGroupSchema(aaContext, contentDirectoryGroupSchema), "Unable to process first schema.");
            Assert.IsEmpty(schemaBuilder.ProcessGroupSchema(aaContext, contentDirectoryGroupSchema2), "Unable to process second schema.");

            StubBuildManifest(buildPath);

            var locations = schemaBuilder.GenerateCatalogLocations(aaContext, addressablesBuildResult);

            // The builder returns one map entry per distinct catalog id; catalog file writing
            // (and thus CatalogLocations population) now happens in BuildScriptSchemaDriven.
            Assert.AreEqual(2, locations.Count,
                "Expected 2 distinct catalog ids in the returned map.");
            Assert.IsTrue(locations.ContainsKey(contentDirectoryGroupSchema.CatalogId),
                $"Expected catalog id '{contentDirectoryGroupSchema.CatalogId}' in the returned map.");
            Assert.IsTrue(locations.ContainsKey(contentDirectoryGroupSchema2.CatalogId),
                $"Expected catalog id '{contentDirectoryGroupSchema2.CatalogId}' in the returned map.");

            Directory.Delete(buildPath, true);
            File.Delete(buildPath + ".meta");
        }

        [Test]
        public void ContentDirectorySchemaBuilder_ProcessGroupSchema_CreatesRootAssetScriptableObjects()
        {
            string buildPath = Path.Combine(TestFolder, "ContentDirectories");
            Directory.CreateDirectory(buildPath);

            AddressableAssetGroup group = GetGroupWithEntry("TestGroup");
            group.AddSchema(CreateSchema());
            var contentDirectoryGroupSchema = group.GetSchema<ContentDirectoryGroupSchema>();

            schemaBuilder.Init(null, input, null, null);
            schemaBuilder.ProcessGroupSchema(aaContext, contentDirectoryGroupSchema);

            // RootAssetBuildPath: AddressableRootAsset.asset + meta = 2 files
            Assert.AreEqual(2, Directory.GetFiles(schemaBuilder.RootAssetBuildPath).Count());

            // Verify the root asset is an AddressableRootAsset
            var rootAssetFiles = Directory.GetFiles(schemaBuilder.RootAssetBuildPath, "*.asset");
            Assert.AreEqual(1, rootAssetFiles.Length, "Expected one root asset file");
            var rootAsset = AssetDatabase.LoadAssetAtPath<AddressableRootAsset>(rootAssetFiles[0]);
            Assert.IsNotNull(rootAsset, "Failed to load AddressableRootAsset");

            Directory.Delete(buildPath, true);
            File.Delete(buildPath + ".meta");
        }

        ContentDirectoryGroupSchema CreateSchema()
        {
            ContentDirectoryGroupSchema contentDirectoryGroupSchema = ScriptableObject.CreateInstance<ContentDirectoryGroupSchema>();
            contentDirectoryGroupSchema.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalBuildPath);
            contentDirectoryGroupSchema.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);
            return contentDirectoryGroupSchema;
        }

        void StubBuildManifest(string buildPath)
        {
            // for right now we need this for hashing, replace with real method.
            var manifestPath = Path.Combine(buildPath, "BuildManifest.json");
            File.WriteAllText(manifestPath, "{}");
            input.Registry.AddFile(manifestPath);
        }

        AddressableAssetGroup GetGroupWithEntry(string groupName = "TestGroup")
        {
            ContentCatalogDataEntry entry = new ContentCatalogDataEntry(
                typeof(Loadable<GameObject>),
                "test",
                typeof(NativeContentAssetEntryProvider).FullName,
                new List<string>() { "test" });

            aaContext.locations = new List<ContentCatalogDataEntry>();
            aaContext.locations.Add(entry);

            AddressableAssetGroup group = ScriptableObject.CreateInstance<AddressableAssetGroup>();
            var guid = GUID.Generate();
            group.Initialize(Settings, groupName, guid.ToString(), false);
            AddressableAssetEntry addressableEntry = new AddressableAssetEntry("dummy", "test", null, false);
            addressableEntry.SetCachedPath(TestFolder + "/test.prefab");
            group.AddAssetEntry(addressableEntry);
            return group;
        }

        [Test]
        public void ProcessGroupSchema_WhenIncludeFolderKeysIsTrue_ChildrenGetFolderAddressAsKey()
        {
            // Use a build path unique to this test (rather than the "ContentDirectories" path
            // shared by other tests in this file) so cleanup here can't race with, or be raced
            // by, unrelated tests writing/deleting that shared folder.
            string buildPath = Path.Combine(TestFolder, "ContentDirectories_FolderKeyTrue");
            Settings.profileSettings.SetValue(Settings.activeProfileId, "Local.LoadPath", buildPath);
            Directory.CreateDirectory(buildPath);

            string folderPath = GetAssetPath("FolderKeyCDTest1");
            Directory.CreateDirectory(folderPath);
            CreateAsset(folderPath + "/child1.prefab", "child1");
            CreateAsset(folderPath + "/child2.prefab", "child2");
            AssetDatabase.ImportAsset(folderPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            // Folder gathering (AddressablesFileEnumeration.EnumerateAddressableFolder) needs the
            // group to be registered with Settings.groups, unlike plain file entries -- so use
            // Settings.CreateGroup rather than a raw unregistered ScriptableObject instance.
            AddressableAssetGroup group = Settings.CreateGroup("FolderKeyCDGroup1", false, false, false, null, typeof(ContentDirectoryGroupSchema));
            var schema = group.GetSchema<ContentDirectoryGroupSchema>();
            schema.IncludeFolderKeysInCatalog = true;

            var folderGuid = AssetDatabase.AssetPathToGUID(folderPath);
            var folderEntry = Settings.CreateOrMoveEntry(folderGuid, group, false, false);
            folderEntry.address = "FolderKeyCDTest1";

            try
            {
                schemaBuilder.Init(null, input, null, null);
                string result = schemaBuilder.ProcessGroupSchema(aaContext, schema);
                Assert.IsEmpty(result, $"ProcessGroupSchema failed: {result}");

                StubBuildManifest(buildPath);
                var locationsMap = schemaBuilder.GenerateCatalogLocations(aaContext, addressablesBuildResult);
                var entries = locationsMap[schema.CatalogId];

                Assert.AreEqual(2, entries.Count, "Expected exactly one catalog entry per child prefab.");
                foreach (var e in entries)
                    CollectionAssert.Contains(e.Keys, "FolderKeyCDTest1", $"Folder key missing for entry '{e.InternalId}'");
            }
            finally
            {
                Settings.RemoveAssetEntry(folderPath);
                AssetDatabase.DeleteAsset(folderPath);
                Settings.RemoveGroup(group);
                if (Directory.Exists(buildPath))
                    AssetDatabase.DeleteAsset(buildPath);
            }
        }

        [Test]
        public void ProcessGroupSchema_WhenIncludeAddressesForFolderChildrenIsFalse_ChildLosesOwnAddressButKeepsGuidAndFolderKey()
        {
            string buildPath = Path.Combine(TestFolder, "ContentDirectories_ExcludeAddress");
            Settings.profileSettings.SetValue(Settings.activeProfileId, "Local.LoadPath", buildPath);
            Directory.CreateDirectory(buildPath);

            string folderPath = GetAssetPath("FolderKeyCDTest3");
            Directory.CreateDirectory(folderPath);
            var childGuid = CreateAsset(folderPath + "/child1.prefab", "child1");
            AssetDatabase.ImportAsset(folderPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            AddressableAssetGroup group = Settings.CreateGroup("FolderKeyCDGroup3", false, false, false, null, typeof(ContentDirectoryGroupSchema));
            var schema = group.GetSchema<ContentDirectoryGroupSchema>();
            schema.IncludeFolderKeysInCatalog = true;
            schema.IncludeAddressesForFolderChildren = false;

            var folderGuid = AssetDatabase.AssetPathToGUID(folderPath);
            var folderEntry = Settings.CreateOrMoveEntry(folderGuid, group, false, false);
            folderEntry.address = "FolderKeyCDTest3";

            try
            {
                schemaBuilder.Init(null, input, null, null);
                string result = schemaBuilder.ProcessGroupSchema(aaContext, schema);
                Assert.IsEmpty(result, $"ProcessGroupSchema failed: {result}");

                StubBuildManifest(buildPath);
                var locationsMap = schemaBuilder.GenerateCatalogLocations(aaContext, addressablesBuildResult);
                var entries = locationsMap[schema.CatalogId];

                Assert.AreEqual(1, entries.Count);
                var entry = entries[0];
                CollectionAssert.DoesNotContain(entry.Keys, "FolderKeyCDTest3/child1.prefab", "Child's own address should have been excluded.");
                CollectionAssert.Contains(entry.Keys, childGuid, "GUID should still be present -- only the address is excluded.");
                CollectionAssert.Contains(entry.Keys, "FolderKeyCDTest3", "Folder key should still be present.");
            }
            finally
            {
                Settings.RemoveAssetEntry(folderPath);
                AssetDatabase.DeleteAsset(folderPath);
                Settings.RemoveGroup(group);
                if (Directory.Exists(buildPath))
                    AssetDatabase.DeleteAsset(buildPath);
            }
        }

        [Test]
        public void ProcessGroupSchema_WhenIncludeFolderKeysIsFalse_ChildrenDoNotGetFolderAddressAsKey()
        {
            string buildPath = Path.Combine(TestFolder, "ContentDirectories_FolderKeyFalse");
            Settings.profileSettings.SetValue(Settings.activeProfileId, "Local.LoadPath", buildPath);
            Directory.CreateDirectory(buildPath);

            string folderPath = GetAssetPath("FolderKeyCDTest2");
            Directory.CreateDirectory(folderPath);
            CreateAsset(folderPath + "/child1.prefab", "child1");
            AssetDatabase.ImportAsset(folderPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            AddressableAssetGroup group = Settings.CreateGroup("FolderKeyCDGroup2", false, false, false, null, typeof(ContentDirectoryGroupSchema));
            var schema = group.GetSchema<ContentDirectoryGroupSchema>();
            schema.IncludeFolderKeysInCatalog = false;

            var folderGuid = AssetDatabase.AssetPathToGUID(folderPath);
            var folderEntry = Settings.CreateOrMoveEntry(folderGuid, group, false, false);
            folderEntry.address = "FolderKeyCDTest2";

            try
            {
                schemaBuilder.Init(null, input, null, null);
                string result = schemaBuilder.ProcessGroupSchema(aaContext, schema);
                Assert.IsEmpty(result, $"ProcessGroupSchema failed: {result}");

                StubBuildManifest(buildPath);
                var locationsMap = schemaBuilder.GenerateCatalogLocations(aaContext, addressablesBuildResult);
                var entries = locationsMap[schema.CatalogId];

                Assert.AreEqual(1, entries.Count);
                CollectionAssert.DoesNotContain(entries[0].Keys, "FolderKeyCDTest2");
            }
            finally
            {
                Settings.RemoveAssetEntry(folderPath);
                AssetDatabase.DeleteAsset(folderPath);
                Settings.RemoveGroup(group);
                if (Directory.Exists(buildPath))
                    AssetDatabase.DeleteAsset(buildPath);
            }
        }

        [Test]
        public void ProcessGroupSchema_WhenIncludeLabelsInCatalogIsFalse_LabelsAreExcludedFromKeys()
        {
            // ContentDirectoryGroupSchema previously had no IncludeLabelsInCatalog toggle at all
            // (labels were always included); this proves the newly-added toggle actually gates them.
            string buildPath = Path.Combine(TestFolder, "ContentDirectories_LabelToggle");
            Settings.profileSettings.SetValue(Settings.activeProfileId, "Local.LoadPath", buildPath);
            Directory.CreateDirectory(buildPath);

            string assetPath = GetAssetPath("labelledAsset.prefab");
            var guid = CreateAsset(assetPath, "labelledAsset");

            AddressableAssetGroup group = ScriptableObject.CreateInstance<AddressableAssetGroup>();
            group.Initialize(Settings, "LabelToggleGroup", GUID.Generate().ToString(), false);
            group.AddSchema(CreateSchema());
            var schema = group.GetSchema<ContentDirectoryGroupSchema>();
            schema.IncludeLabelsInCatalog = false;

            var entry = Settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = "labelledAsset";
            entry.SetLabel("myLabel", true, true, true);

            try
            {
                schemaBuilder.Init(null, input, null, null);
                string result = schemaBuilder.ProcessGroupSchema(aaContext, schema);
                Assert.IsEmpty(result, $"ProcessGroupSchema failed: {result}");

                StubBuildManifest(buildPath);
                var locationsMap = schemaBuilder.GenerateCatalogLocations(aaContext, addressablesBuildResult);
                var entries = locationsMap[schema.CatalogId];

                Assert.AreEqual(1, entries.Count);
                CollectionAssert.DoesNotContain(entries[0].Keys, "myLabel");
            }
            finally
            {
                Settings.RemoveLabel("myLabel");
                Settings.RemoveAssetEntry(guid);
                AssetDatabase.DeleteAsset(assetPath);
                if (Directory.Exists(buildPath))
                    AssetDatabase.DeleteAsset(buildPath);
            }
        }

        [Test]
        public void ProcessGroupSchema_TextureWithMultipleSprites_CreatesEntriesForSubassets()
        {
            // Setup - create a texture with multiple sprites
            string texturePath = CreateTextureWithMultipleSprites("testMultiSprite");
            string textureGuid = AssetDatabase.AssetPathToGUID(texturePath);
            m_CreatedAssetGuids.Add(textureGuid);

            // Create group and add the texture as addressable
            AddressableAssetGroup group = ScriptableObject.CreateInstance<AddressableAssetGroup>();
            group.Initialize(Settings, "MultiSpriteTestGroup", GUID.Generate().ToString(), false);

            var mainEntry = Settings.CreateOrMoveEntry(textureGuid, group, false, false);
            mainEntry.address = textureGuid;

            // Gather all entries including subassets
            var allEntries = new List<AddressableAssetEntry>();
            mainEntry.GatherAllAssets(allEntries, true, true, true);

            // Verify we have subasset entries
            var subAssetEntries = allEntries.Where(e => e.IsSubAsset).ToList();
            Assert.IsTrue(subAssetEntries.Count >= 2, $"Expected at least 2 sprite subassets, but found {subAssetEntries.Count}");

            // Verify each subasset entry has the correct TargetAsset (not the parent texture)
            foreach (var subEntry in subAssetEntries)
            {
                Assert.IsNotNull(subEntry.TargetAsset, $"TargetAsset should not be null for subasset entry {subEntry.address}");
                Assert.IsInstanceOf<Sprite>(subEntry.TargetAsset, $"TargetAsset for sprite subasset should be a Sprite, not {subEntry.TargetAsset.GetType().Name}");
                Assert.AreNotEqual(subEntry.MainAsset, subEntry.TargetAsset, "TargetAsset should be different from MainAsset for subassets");
            }

            // Process with schema builder
            group.AddSchema(CreateSchema());
            var schema = group.GetSchema<ContentDirectoryGroupSchema>();

            schemaBuilder.Init(null, input, null, null);
            string result = schemaBuilder.ProcessGroupSchema(aaContext, schema);
            Assert.IsEmpty(result, $"ProcessGroupSchema failed: {result}");

            // Load the generated AddressableRootAsset and verify it was created
            var rootAssetFiles = Directory.GetFiles(schemaBuilder.RootAssetBuildPath, "*.asset");
            Assert.AreEqual(1, rootAssetFiles.Length, "Expected one root asset file");

            var rootAsset = AssetDatabase.LoadAssetAtPath<AddressableRootAsset>(rootAssetFiles[0]);
            Assert.IsNotNull(rootAsset, "Failed to load AddressableRootAsset");

            // Verify the root asset has entries by checking that we can retrieve LoadableObjectIds
            // for non-zero IDs (IDs are assigned starting from 1 in the current implementation)
            int validIdCount = 0;
            for (int i = 0; i <= 10; i++)
            {
                var loadableObjId = rootAsset.GetLoadableObjectId(i);
                if (loadableObjId != default)
                    validIdCount++;
            }
            // We should have at least main + 2 subassets = 3 entries
            Assert.GreaterOrEqual(validIdCount, 3, $"Expected at least 3 valid LoadableObjectIds (main + 2 sprites), found {validIdCount}");
        }

        [Test]
        public void ProcessGroupSchema_SpriteAtlas_CreatesEntriesForAtlasSprites()
        {
            // macOS trunk may emit Assert "[Assert] Image invalid format!" during atlas pack/import or later asset
            // refresh while formats are still valid; Edit Mode treats unexpected logs as failures (see LogAssert).
            bool prevIgnoreFailing = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                // Setup - create a sprite atlas with sprites
                string atlasPath = CreateSpriteAtlasWithSprites("testAtlas");
                string atlasGuid = AssetDatabase.AssetPathToGUID(atlasPath);
                m_CreatedAssetGuids.Add(atlasGuid);

                // Create group and add the atlas as addressable
                AddressableAssetGroup group = ScriptableObject.CreateInstance<AddressableAssetGroup>();
                group.Initialize(Settings, "SpriteAtlasTestGroup", GUID.Generate().ToString(), false);

                var mainEntry = Settings.CreateOrMoveEntry(atlasGuid, group, false, false);
                mainEntry.address = "testAtlas";

                // Gather all entries including subassets
                var allEntries = new List<AddressableAssetEntry>();
                mainEntry.GatherAllAssets(allEntries, true, true, true);

                // Verify we have subasset entries for the atlas sprites
                var subAssetEntries = allEntries.Where(e => e.IsSubAsset).ToList();
                Assert.IsTrue(subAssetEntries.Count >= 1, $"Expected at least 1 sprite subasset from atlas, but found {subAssetEntries.Count}");

                // Verify each subasset entry references a sprite
                foreach (var subEntry in subAssetEntries)
                {
                    Assert.IsNotNull(subEntry.TargetAsset, $"TargetAsset should not be null for atlas sprite entry {subEntry.address}");
                    Assert.IsInstanceOf<Sprite>(subEntry.TargetAsset, $"TargetAsset for atlas sprite should be a Sprite, not {subEntry.TargetAsset?.GetType().Name}");
                }

                // Process with schema builder
                group.AddSchema(CreateSchema());
                var schema = group.GetSchema<ContentDirectoryGroupSchema>();

                schemaBuilder.Init(null, input, null, null);
                string result = schemaBuilder.ProcessGroupSchema(aaContext, schema);
                Assert.IsEmpty(result, $"ProcessGroupSchema failed: {result}");

                // Load the generated AddressableRootAsset
                var rootAssetFiles = Directory.GetFiles(schemaBuilder.RootAssetBuildPath, "*.asset");
                Assert.AreEqual(1, rootAssetFiles.Length, "Expected one root asset file");

                var rootAsset = AssetDatabase.LoadAssetAtPath<AddressableRootAsset>(rootAssetFiles[0]);
                Assert.IsNotNull(rootAsset, "Failed to load AddressableRootAsset");

                // Verify the root asset has at least one valid entry
                var loadableObjId = rootAsset.GetLoadableObjectId(0);
                Assert.AreNotEqual(default(LoadableObjectId), loadableObjId, "Expected at least one valid LoadableObjectId for the atlas");
            }
            finally
            {
                LogAssert.ignoreFailingMessages = prevIgnoreFailing;
            }
        }


        [Test]
        public void ProcessGroupSchema_SpriteAtlasInFolder_SpritesGetFolderAddressAsKey()
        {
            bool prevIgnoreFailing = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            string buildPath = Path.Combine(TestFolder, "ContentDirectories_AtlasFolderKey");
            AddressableAssetGroup group = null;
            string folderPath = GetAssetPath("FolderKeyCDAtlasTest1");
            try
            {
                Settings.profileSettings.SetValue(Settings.activeProfileId, "Local.LoadPath", buildPath);
                Directory.CreateDirectory(buildPath);

                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                CreateSpriteAtlasWithSprites("FolderKeyCDAtlasTest1/testAtlas");

                group = Settings.CreateGroup("FolderKeyCDAtlasGroup1", false, false, false, null, typeof(ContentDirectoryGroupSchema));
                var schema = group.GetSchema<ContentDirectoryGroupSchema>();
                schema.IncludeFolderKeysInCatalog = true;

                var folderGuid = AssetDatabase.AssetPathToGUID(folderPath);
                var folderEntry = Settings.CreateOrMoveEntry(folderGuid, group, false, false);
                folderEntry.address = "FolderKeyCDAtlasTest1";

                schemaBuilder.Init(null, input, null, null);
                string result = schemaBuilder.ProcessGroupSchema(aaContext, schema);
                Assert.IsEmpty(result, $"ProcessGroupSchema failed: {result}");

                StubBuildManifest(buildPath);
                var locationsMap = schemaBuilder.GenerateCatalogLocations(aaContext, addressablesBuildResult);
                var entries = locationsMap[schema.CatalogId];

                var spriteEntries = entries.Where(e => e.ResourceType == typeof(Sprite)).ToList();
                Assert.IsNotEmpty(spriteEntries, "Expected catalog entries for the atlas's sprites.");
                foreach (var e in spriteEntries)
                {
                    CollectionAssert.Contains(e.Keys, "FolderKeyCDAtlasTest1", $"Folder key missing for atlas sprite '{e.InternalId}'");
                    CollectionAssert.Contains(e.Keys, e.InternalId, $"Sprite's own address should still be a key by default for '{e.InternalId}'");
                }
            }
            finally
            {
                LogAssert.ignoreFailingMessages = prevIgnoreFailing;
                Settings.RemoveAssetEntry(folderPath);
                AssetDatabase.DeleteAsset(folderPath);
                if (group != null)
                    Settings.RemoveGroup(group);
                if (Directory.Exists(buildPath))
                    AssetDatabase.DeleteAsset(buildPath);
            }
        }

        [Test]
        public void ProcessGroupSchema_SpriteAtlasInFolder_WhenIncludeAddressesForFolderChildrenIsFalse_SpriteLosesOwnAddressButKeepsFolderKey()
        {
            bool prevIgnoreFailing = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            string buildPath = Path.Combine(TestFolder, "ContentDirectories_AtlasExcludeAddress");
            AddressableAssetGroup group = null;
            string folderPath = GetAssetPath("FolderKeyCDAtlasTest2");
            try
            {
                Settings.profileSettings.SetValue(Settings.activeProfileId, "Local.LoadPath", buildPath);
                Directory.CreateDirectory(buildPath);

                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                CreateSpriteAtlasWithSprites("FolderKeyCDAtlasTest2/testAtlas");

                group = Settings.CreateGroup("FolderKeyCDAtlasGroup2", false, false, false, null, typeof(ContentDirectoryGroupSchema));
                var schema = group.GetSchema<ContentDirectoryGroupSchema>();
                schema.IncludeFolderKeysInCatalog = true;
                schema.IncludeAddressesForFolderChildren = false;

                var folderGuid = AssetDatabase.AssetPathToGUID(folderPath);
                var folderEntry = Settings.CreateOrMoveEntry(folderGuid, group, false, false);
                folderEntry.address = "FolderKeyCDAtlasTest2";

                schemaBuilder.Init(null, input, null, null);
                string result = schemaBuilder.ProcessGroupSchema(aaContext, schema);
                Assert.IsEmpty(result, $"ProcessGroupSchema failed: {result}");

                StubBuildManifest(buildPath);
                var locationsMap = schemaBuilder.GenerateCatalogLocations(aaContext, addressablesBuildResult);
                var entries = locationsMap[schema.CatalogId];

                var spriteEntries = entries.Where(e => e.ResourceType == typeof(Sprite)).ToList();
                Assert.IsNotEmpty(spriteEntries, "Expected catalog entries for the atlas's sprites.");
                foreach (var e in spriteEntries)
                {
                    CollectionAssert.DoesNotContain(e.Keys, e.InternalId, $"Sprite's own address should have been excluded for '{e.InternalId}'");
                    CollectionAssert.Contains(e.Keys, "FolderKeyCDAtlasTest2", $"Folder key should still be present for atlas sprite '{e.InternalId}'");
                }
            }
            finally
            {
                LogAssert.ignoreFailingMessages = prevIgnoreFailing;
                Settings.RemoveAssetEntry(folderPath);
                AssetDatabase.DeleteAsset(folderPath);
                if (group != null)
                    Settings.RemoveGroup(group);
                if (Directory.Exists(buildPath))
                    AssetDatabase.DeleteAsset(buildPath);
            }
        }

        string CreateTextureWithMultipleSprites(string name)
        {
            // Create a texture
            var texture = new Texture2D(32, 32);
            for (int x = 0; x < 32; x++)
                for (int y = 0; y < 32; y++)
                    texture.SetPixel(x, y, Color.white);
            texture.Apply();

            byte[] data = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);

            string texturePath = GetAssetPath($"{name}.png");
            File.WriteAllBytes(texturePath, data);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            m_CreatedAssetPaths.Add(texturePath);

            // Configure as multiple sprites
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;

#pragma warning disable 618
            importer.spritesheet = new SpriteMetaData[]
            {
                new SpriteMetaData() { name = "sprite_topleft", pivot = Vector2.zero, rect = new Rect(0, 16, 16, 16) },
                new SpriteMetaData() { name = "sprite_botright", pivot = Vector2.zero, rect = new Rect(16, 0, 16, 16) }
            };
#pragma warning restore 618

            importer.SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            return texturePath;
        }

        /// <summary>
        /// Native packing resolves the atlas pixel format via SpriteAtlas::DetermineFormatFromTextureCompression; if no
        /// overridden row applies for the active BuildTargetPlatform (and fallbacks), finalFormat can stay kTexFormatNone
        /// and Image-based packing aborts (Linux Editor/batch). Editor + Default + active player rows must all be
        /// explicit uncompressed RGBA; ignorePlatformSupport avoids rare platform-support substitution edge cases.
        /// </summary>
        static void ConfigureSpriteAtlasPlatformRowForPacking(SpriteAtlas spriteAtlas, string serializedBuildTarget)
        {
            var ps = spriteAtlas.GetPlatformSettings(serializedBuildTarget);
            ps.overridden = true;
            ps.maxTextureSize = 2048;
            ps.textureCompression = TextureImporterCompression.Uncompressed;
            ps.format = TextureImporterFormat.RGBA32;
            ps.crunchedCompression = false;
            ps.allowsAlphaSplitting = false;
            ps.ignorePlatformSupport = true;
            spriteAtlas.SetPlatformSettings(ps);
        }

        static void ConfigureTextureImporterPlatformRowForAtlasSource(TextureImporter importer, string serializedBuildTarget)
        {
            var ps = importer.GetPlatformTextureSettings(serializedBuildTarget);
            ps.overridden = true;
            ps.maxTextureSize = 2048;
            ps.textureCompression = TextureImporterCompression.Uncompressed;
            ps.format = TextureImporterFormat.RGBA32;
            ps.crunchedCompression = false;
            ps.allowsAlphaSplitting = false;
            ps.ignorePlatformSupport = true;
            importer.SetPlatformTextureSettings(ps);
        }

        string CreateSpriteAtlasWithSprites(string name)
        {
            // PackAtlases uses Image-based CPU packing; the atlas texture format must resolve to an uncompressed format
            // (see SpriteAtlas::DetermineFormatFromTextureCompression). Linux CI also needs a real sprite source: a 1x1
            // whiteTexture PNG can fail sprite texture extraction ("Image invalid format!") and leave finalFormat invalid
            // (TextureFormat -1 / GraphicsFormat None), which then aborts the Editor during packing or shutdown.
            string texturePath = CreateAtlasSourceSpriteTexture($"{name}_sourceTexture");
            string atlasPath = GetAssetPath($"{name}.spriteatlas");
            var spriteAtlas = new SpriteAtlas();
            AssetDatabase.CreateAsset(spriteAtlas, atlasPath);
            m_CreatedAssetPaths.Add(atlasPath);

            // Prefer the Sprite sub-asset so packing uses the same object path as typical authoring (texture-only can
            // behave differently during shared texture extraction on some targets).
            UnityEngine.Object packable = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            foreach (var sub in AssetDatabase.LoadAllAssetRepresentationsAtPath(texturePath))
            {
                if (sub is Sprite)
                {
                    packable = sub;
                    break;
                }
            }

            SpriteAtlasExtensions.Add(spriteAtlas, new[] { packable });

            const string defaultTexturePlatform = "DefaultTexturePlatform";
            ConfigureSpriteAtlasPlatformRowForPacking(spriteAtlas, defaultTexturePlatform);
            ConfigureSpriteAtlasPlatformRowForPacking(spriteAtlas, Application.platform.ToString());

            var atlasTextureSettings = spriteAtlas.GetTextureSettings();
            atlasTextureSettings.readable = false;
            atlasTextureSettings.generateMipMaps = false;
            atlasTextureSettings.sRGB = true;
            spriteAtlas.SetTextureSettings(atlasTextureSettings);

            EditorUtility.SetDirty(spriteAtlas);
            AssetDatabase.SaveAssets();

            SpriteAtlasUtility.PackAtlases(new SpriteAtlas[] { spriteAtlas }, EditorUserBuildSettings.activeBuildTarget, false);
            SpriteAtlasUtility.CleanupAtlasPacking();

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            return atlasPath;
        }

        string CreateAtlasSourceSpriteTexture(string baseName)
        {
            var texture = new Texture2D(32, 32);
            for (int x = 0; x < 32; x++)
                for (int y = 0; y < 32; y++)
                    texture.SetPixel(x, y, Color.white);
            texture.Apply();

            byte[] data = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);

            string texturePath = GetAssetPath($"{baseName}.png");
            File.WriteAllBytes(texturePath, data);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            m_CreatedAssetPaths.Add(texturePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            ConfigureTextureImporterPlatformRowForAtlasSource(importer, "DefaultTexturePlatform");
            ConfigureTextureImporterPlatformRowForAtlasSource(importer, Application.platform.ToString());

            importer.SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            return texturePath;
        }

        [Test]
        public void Build_ThrowsWhenDisableWriteTypeTreeAndStripUnityVersionBothEnabled()
        {
            string buildPath = Path.Combine(TestFolder, "ContentDirectories");
            Settings.profileSettings.SetValue(Settings.activeProfileId, "Local.LoadPath", buildPath);
            Directory.CreateDirectory(buildPath);

            AddressableAssetGroup group = GetGroupWithEntry();
            group.AddSchema(CreateSchema());
            ContentDirectoryGroupSchema contentDirectoryGroupSchema = group.GetSchema<ContentDirectoryGroupSchema>();
            schemaBuilder.Init(aaContext, input, null, null);
            schemaBuilder.ProcessGroupSchema(aaContext, contentDirectoryGroupSchema);
            group.AddSchema(contentDirectoryGroupSchema);

            bool originalDisable = Settings.DisableWriteTypeTree;
            bool originalStrip = Settings.StripUnityVersion;
            try
            {
                Settings.DisableWriteTypeTree = true;
                Settings.StripUnityVersion = true;

                var ex = Assert.Throws<InvalidOperationException>(() => schemaBuilder.Build(aaContext, addressablesBuildResult));
                StringAssert.Contains("DisableWriteTypeTree", ex.Message);
                StringAssert.Contains("StripUnityVersionFromBundleBuild", ex.Message);
                StringAssert.Contains("Content Directory", ex.Message);
            }
            finally
            {
                Settings.DisableWriteTypeTree = originalDisable;
                Settings.StripUnityVersion = originalStrip;

                if (Directory.Exists(buildPath))
                    Directory.Delete(buildPath, true);
                if (File.Exists(buildPath + ".meta"))
                    File.Delete(buildPath + ".meta");
            }
        }

        [Test]
        public void ProcessGroupSchema_ProcessesAssetsAndScenes()
        {
            // Use a build path unique to this test so cleanup here can't race with, or be
            // raced by, unrelated tests writing/deleting the shared "ContentDirectories" folder.
            string buildPath = Path.Combine(TestFolder, "ContentDirectories_AssetsAndScenes");
            Settings.profileSettings.SetValue(Settings.activeProfileId, "Local.LoadPath", buildPath);
            Directory.CreateDirectory(buildPath);

            // Create group and schema
            AddressableAssetGroup group = Settings.CreateGroup("ProcessTestGroup", false, false, false, null, typeof(ContentDirectoryGroupSchema));
            group.Initialize(Settings, "ProcessTestGroup", GUID.Generate().ToString(), false);
            ContentDirectoryGroupSchema schema = group.GetSchema<ContentDirectoryGroupSchema>();

            // Create prefab asset
            string prefabPath = CreateAsset(GetAssetPath("testPrefab.prefab"), "testPrefab");
            m_CreatedAssetGuids.Add(prefabPath);
            var prefabEntry = Settings.CreateOrMoveEntry(prefabPath, group, false, false);
            prefabEntry.address = "testPrefab";

            // Create texture asset
            string texturePath = CreateTextureWithMultipleSprites("testTexture");
            string textureGuid = AssetDatabase.AssetPathToGUID(texturePath);
            m_CreatedAssetGuids.Add(textureGuid);
            var textureEntry = Settings.CreateOrMoveEntry(textureGuid, group, false, false);
            textureEntry.address = "testTexture";

            // Create two scenes
            string scenePathA = GetAssetPath("testSceneA.unity");
            Scene sceneA = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            EditorSceneManager.SaveScene(sceneA, scenePathA);
            m_CreatedAssetPaths.Add(scenePathA);
            string sceneGuidA = AssetDatabase.AssetPathToGUID(scenePathA);
            m_CreatedAssetGuids.Add(sceneGuidA);
            var sceneEntryA = Settings.CreateOrMoveEntry(sceneGuidA, group, false, false);
            sceneEntryA.address = "testSceneA";

            string scenePathB = GetAssetPath("testSceneB.unity");
            Scene sceneB = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            EditorSceneManager.SaveScene(sceneB, scenePathB);
            m_CreatedAssetPaths.Add(scenePathB);
            string sceneGuidB = AssetDatabase.AssetPathToGUID(scenePathB);
            m_CreatedAssetGuids.Add(sceneGuidB);
            var sceneEntryB = Settings.CreateOrMoveEntry(sceneGuidB, group, false, false);
            sceneEntryB.address = "testSceneB";

            // Process with schema builder
            schemaBuilder.Init(null, input, null, null);
            string result = schemaBuilder.ProcessGroupSchema(aaContext, schema);
            Assert.IsEmpty(result, $"ProcessGroupSchema failed: {result}");

            // Load the generated AddressableRootAsset
            var rootAssetFiles = Directory.GetFiles(schemaBuilder.RootAssetBuildPath, "*.asset");
            Assert.AreEqual(1, rootAssetFiles.Length, "Expected one root asset file");
            var rootAsset = AssetDatabase.LoadAssetAtPath<AddressableRootAsset>(rootAssetFiles[0]);
            Assert.IsNotNull(rootAsset, "Failed to load AddressableRootAsset");

            // Verify the root asset has entries by counting valid LoadableObjectIds
            // (assets including texture subassets)
            int validAssetIdCount = 0;
            for (int i = 0; i <= 20; i++)
            {
                var loadableObjId = rootAsset.GetLoadableObjectId(i);
                if (loadableObjId != default)
                    validAssetIdCount++;
            }

            // We should have at least prefab + texture + 2 sprite subassets = 4 entries
            Assert.GreaterOrEqual(validAssetIdCount, 4, $"Expected at least 4 valid LoadableObjectIds, found {validAssetIdCount}");

            // Verify the root asset has scene entries
            int validSceneIdCount = 0;
            for (int i = 0; i <= 10; i++)
            {
                var loadableSceneId = rootAsset.GetLoadableSceneId(i);
                if (loadableSceneId != default)
                    validSceneIdCount++;
            }
            Assert.GreaterOrEqual(validSceneIdCount, 2, $"Expected at least 2 valid LoadableSceneIds, found {validSceneIdCount}");

            // Verify each catalog entry's AssetId/SceneId and that the two scenes get distinct indices.
            StubBuildManifest(buildPath);
            var locations = schemaBuilder.GenerateCatalogLocations(aaContext, addressablesBuildResult);
            var entries = locations[schema.CatalogId];

            var prefabData = entries.Single(e => e.InternalId == "testPrefab").Data as ContentDirectoryAssetData;
            Assert.AreEqual(-1, prefabData.SceneId, "Asset entry should have SceneId=-1 (not applicable).");
            Assert.GreaterOrEqual(prefabData.AssetId, 0);

            var sceneDataA = entries.Single(e => e.InternalId == "testSceneA").Data as ContentDirectoryAssetData;
            var sceneDataB = entries.Single(e => e.InternalId == "testSceneB").Data as ContentDirectoryAssetData;
            Assert.AreEqual(-1, sceneDataA.AssetId, "Scene entry should have AssetId=-1 (not applicable).");
            Assert.AreEqual(-1, sceneDataB.AssetId, "Scene entry should have AssetId=-1 (not applicable).");
            Assert.GreaterOrEqual(sceneDataA.SceneId, 0);
            Assert.GreaterOrEqual(sceneDataB.SceneId, 0);
            Assert.AreNotEqual(sceneDataA.SceneId, sceneDataB.SceneId, "Distinct scenes must get distinct SceneId indices.");

            // Each SceneId resolves to a distinct, valid LoadableSceneId.
            var loadableSceneA = rootAsset.GetLoadableSceneId(sceneDataA.SceneId);
            var loadableSceneB = rootAsset.GetLoadableSceneId(sceneDataB.SceneId);
            Assert.AreNotEqual(default(LoadableSceneId), loadableSceneA);
            Assert.AreNotEqual(default(LoadableSceneId), loadableSceneB);
            Assert.AreNotEqual(loadableSceneA, loadableSceneB);

            Directory.Delete(buildPath, true);
            if (File.Exists(buildPath + ".meta"))
                File.Delete(buildPath + ".meta");
        }
    }
}
#endif
