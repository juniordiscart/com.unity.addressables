#if ENABLE_CONTENT_DIRECTORIES
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Loading;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
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
        ExtractDataTask extractData = new ExtractDataTask();
        List<CachedAssetState> carryOverCachedState = new List<CachedAssetState>();
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

            extractData = new ExtractDataTask();
            carryOverCachedState.Clear();
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

#if ENABLE_JSON_CATALOG
        [Test]
        public void ContentDirectorySchemaBuilder_GeneratesContentCatalogWithCorrectLocations()
        {
            ContentDirectoryGroupSchema contentDirectoryGroupSchema = CreateSchema();

            string groupName = "GenerateCatalogTestGroup";
            string buildPath = Path.Combine(TestFolder, "ContentDirectories");
            Settings.profileSettings.SetValue(Settings.activeProfileId, "Local.LoadPath", buildPath);

            Directory.CreateDirectory(buildPath);

            AddressableAssetGroup group = GetGroupWithEntry(groupName);
            schemaBuilder.Init(null, null);
            schemaBuilder.ProcessGroupSchema(contentDirectoryGroupSchema, group, aaContext);
            group.AddSchema(contentDirectoryGroupSchema);

            StubBuildManifest(buildPath);

            var catalogs = schemaBuilder.GenerateCatalogs(input, aaContext, addressablesBuildResult);

            Assert.AreEqual(1, catalogs[0].InternalIds.Where(id => id == "test").Count(), "The loadable address didn't make it into the catalog locations.");
            Assert.AreEqual(1, catalogs[0].InternalIds.Where(id => id == $"{groupName}_RootAsset").Count(), "The Addressables root asset didn't make it into the catalog.");
            Assert.AreEqual(1, catalogs[0].InternalIds.Where(id => id == buildPath).Count(), "The content directory key didn't make it into the catalog.");

            Assert.AreEqual(1, catalogs.Count);
            Assert.AreEqual(contentDirectoryGroupSchema.CatalogId, catalogs[0].ProviderId);

            Directory.Delete(buildPath, true);
            File.Delete(buildPath + ".meta");
        }
#endif

        [Test]
        public void ContentDirectorySchemaBuilder_CanGenerateMultipleCatalogs()
        {
            ContentDirectoryGroupSchema contentDirectoryGroupSchema = CreateSchema();
            ContentDirectoryGroupSchema contentDirectoryGroupSchema2 = CreateSchema();

            string buildPath = Path.Combine(TestFolder, "ContentDirectories");
            Settings.profileSettings.SetValue(Settings.activeProfileId, "Local.LoadPath", buildPath);

            contentDirectoryGroupSchema2.CatalogId = "SecondCatalogId";

            Directory.CreateDirectory(buildPath);

            AddressableAssetGroup group = GetGroupWithEntry();
            AddressableAssetGroup group2 = GetGroupWithEntry();

            schemaBuilder.Init(null, null);
            schemaBuilder.ProcessGroupSchema(contentDirectoryGroupSchema, group, aaContext);
            group.AddSchema(contentDirectoryGroupSchema);
            schemaBuilder.ProcessGroupSchema(contentDirectoryGroupSchema2, group2, aaContext);
            group2.AddSchema(contentDirectoryGroupSchema2);

            StubBuildManifest(buildPath);

            var catalogs = schemaBuilder.GenerateCatalogs(input, aaContext, addressablesBuildResult);

            Assert.AreEqual(2, catalogs.Count);

            // sort so we get the same values back each time, this is done internall in SchemaDriverBuildScript
            aaContext.runtimeData.CatalogLocations.Sort((a, b) => string.Compare(a.InternalId, b.InternalId, StringComparison.Ordinal));

            Assert.AreEqual(contentDirectoryGroupSchema.CatalogId, catalogs[0].ProviderId);
            Assert.AreEqual(contentDirectoryGroupSchema2.CatalogId, catalogs[1].ProviderId);

            Directory.Delete(buildPath, true);
            File.Delete(buildPath + ".meta");
        }

        [Test]
        public void ContentDirectorySchemaBuilder_AddsMultipleCatalogsToAAContextRuntimeData()
        {
            ContentDirectoryGroupSchema contentDirectoryGroupSchema = CreateSchema();
            ContentDirectoryGroupSchema contentDirectoryGroupSchema2 = CreateSchema();

            string buildPath = Path.Combine(TestFolder, "ContentDirectories");
            Settings.profileSettings.SetValue(Settings.activeProfileId, "Local.LoadPath", buildPath);
            contentDirectoryGroupSchema2.CatalogId = "SecondCatalogId";

            Directory.CreateDirectory(buildPath);

            AddressableAssetGroup group = GetGroupWithEntry();
            group.AddSchema(contentDirectoryGroupSchema);
            AddressableAssetGroup group2 = GetGroupWithEntry();
            group2.AddSchema(contentDirectoryGroupSchema2);

            schemaBuilder.Init(null, null);
            Assert.IsEmpty(schemaBuilder.ProcessGroupSchema(contentDirectoryGroupSchema, group, aaContext), "Unable to process first schema.");
            Assert.IsEmpty(schemaBuilder.ProcessGroupSchema(contentDirectoryGroupSchema2, group2, aaContext), "Unable to process second schema.");

            StubBuildManifest(buildPath);

            var catalogs = schemaBuilder.GenerateCatalogs(input, aaContext, addressablesBuildResult);

            int catalogCount = aaContext.runtimeData.CatalogLocations.Count;
            Assert.AreEqual(2, catalogCount, $"We're expecting 2 catalogs to be in the runtimeData, but there were {catalogCount}");

            // sort so we get the same values back each time, this is done internall in SchemaDriverBuildScript
            aaContext.runtimeData.CatalogLocations.Sort((a, b) => string.Compare(a.InternalId, b.InternalId, StringComparison.Ordinal));

            Assert.AreEqual(contentDirectoryGroupSchema.CatalogId, aaContext.runtimeData.CatalogLocations[0].Keys[0]);
            Assert.AreEqual(contentDirectoryGroupSchema2.CatalogId, aaContext.runtimeData.CatalogLocations[1].Keys[0]);

            Directory.Delete(buildPath, true);
            File.Delete(buildPath + ".meta");
        }

        [Test]
        public void ContentDirectorySchemaBuilder_ProcessGroupSchema_CreatesRootAssetScriptableObjects()
        {
            ContentDirectoryGroupSchema contentDirectoryGroupSchema = CreateSchema();
            ContentDirectoryGroupSchema contentDirectoryGroupSchema2 = CreateSchema();

            string buildPath = Path.Combine(TestFolder, "ContentDirectories");
            Directory.CreateDirectory(buildPath);

            AddressableAssetGroup group = GetGroupWithEntry();

            schemaBuilder.Init(null, null);
            schemaBuilder.ProcessGroupSchema(contentDirectoryGroupSchema, group, aaContext);

            //2 files because it should be the scriptable object and the meta file
            Assert.AreEqual(2, Directory.GetFiles(schemaBuilder.RootAssetBuildPath).Count());

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
                typeof(GroupRootAssetProvider).FullName,
                new List<string>() { "test" });

            aaContext.locations = new List<ContentCatalogDataEntry>();
            aaContext.locations.Add(entry);

            AddressableAssetGroup group = ScriptableObject.CreateInstance<AddressableAssetGroup>();
            var guid = new GUID();
            group.Initialize(Settings, groupName, guid.ToString(), false);
            AddressableAssetEntry addressableEntry = new AddressableAssetEntry("dummy", "test", null, false);
            addressableEntry.SetCachedPath(TestFolder + "/test.prefab");
            group.AddAssetEntry(addressableEntry);
            return group;
        }

        [Test]
        public void ProcessGroupSchema_TextureWithMultipleSprites_CreatesLoadableInfoForSubassets()
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
            ContentDirectoryGroupSchema schema = CreateSchema();
            group.AddSchema(schema);

            schemaBuilder.Init(null, null);
            string result = schemaBuilder.ProcessGroupSchema(schema, group, aaContext);
            Assert.IsEmpty(result, $"ProcessGroupSchema failed: {result}");

            // Load the generated GroupRootAsset and verify it contains entries for the sprites
            var rootAssetFiles = Directory.GetFiles(schemaBuilder.RootAssetBuildPath, "*.asset");
            Assert.AreEqual(1, rootAssetFiles.Length, "Expected one root asset file");

            var rootAsset = AssetDatabase.LoadAssetAtPath<GroupRootAsset>(rootAssetFiles[0]);
            Assert.IsNotNull(rootAsset, "Failed to load GroupRootAsset");

            // Verify sprite entries exist in the root asset and reference the correct subasset
            foreach (var subEntry in subAssetEntries)
            {
                var loadableInfo = rootAsset.GetLoadableInfo(subEntry.address, typeof(Sprite));
                Assert.IsNotNull(loadableInfo, $"LoadableInfo not found for sprite subasset {subEntry.address}");
                Assert.AreEqual(typeof(Sprite), loadableInfo.type, $"LoadableInfo type should be Sprite for {subEntry.address}");

                // Verify the loadable actually references the sprite subasset, not the parent texture
                var loadedAsset = loadableInfo.loadable.Load();
                Assert.IsInstanceOf<Sprite>(loadedAsset,
                    $"Loadable should reference the Sprite subasset, not {loadedAsset?.GetType().Name}");
                Assert.AreEqual(subEntry.TargetAsset, loadedAsset,
                    $"Loadable should reference the exact sprite subasset '{subEntry.address}'");
            }
        }

        [Test]
        public void ProcessGroupSchema_SpriteAtlas_CreatesLoadableInfoForAtlasSprites()
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
                ContentDirectoryGroupSchema schema = CreateSchema();
                group.AddSchema(schema);

                schemaBuilder.Init(null, null);
                string result = schemaBuilder.ProcessGroupSchema(schema, group, aaContext);
                Assert.IsEmpty(result, $"ProcessGroupSchema failed: {result}");

                // Load the generated GroupRootAsset
                var rootAssetFiles = Directory.GetFiles(schemaBuilder.RootAssetBuildPath, "*.asset");
                Assert.AreEqual(1, rootAssetFiles.Length, "Expected one root asset file");

                var rootAsset = AssetDatabase.LoadAssetAtPath<GroupRootAsset>(rootAssetFiles[0]);
                Assert.IsNotNull(rootAsset, "Failed to load GroupRootAsset");

                // Verify the main atlas entry exists and references the correct asset
                var atlasLoadableInfo = rootAsset.GetLoadableInfo("testAtlas", typeof(SpriteAtlas));
                Assert.IsNotNull(atlasLoadableInfo, "LoadableInfo not found for SpriteAtlas");

                // Verify the loadable actually references the SpriteAtlas
                var loadedAtlas = atlasLoadableInfo.loadable.Load();
                Assert.IsInstanceOf<SpriteAtlas>(loadedAtlas,
                    $"Loadable should reference the SpriteAtlas, not {loadedAtlas?.GetType().Name}");
            }
            finally
            {
                LogAssert.ignoreFailingMessages = prevIgnoreFailing;
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
        public void ProcessGroupSchema_LabelsAreSortedAlphabetically_ForMultipleEntries()
        {
            // Create group and schema
            AddressableAssetGroup group = Settings.CreateGroup("LabelSortTestGroup", false, false, false, null, typeof(ContentDirectoryGroupSchema));
            group.Initialize(Settings, "LabelSortTestGroup", GUID.Generate().ToString(), false);
            ContentDirectoryGroupSchema schema = group.GetSchema<ContentDirectoryGroupSchema>();

            var sortedAssetLabels = new List<string> { "apple", "middle", "zebra" };
            var sortedSceneLabels = new List<string> { "scene-a", "scene-m", "scene-z" };

            // Register labels (in Settings) first, in non-alphabetical order
            foreach(var label in sortedAssetLabels)
                Settings.AddLabel(label);
            foreach(var label in sortedSceneLabels)
                Settings.AddLabel(label);

            // Create prefab asset with labels in non-alphabetical order
            string prefabPath = CreateAsset(GetAssetPath("testPrefab.prefab"), "testPrefab");
            m_CreatedAssetGuids.Add(prefabPath);
            var prefabEntry = Settings.CreateOrMoveEntry(prefabPath, group, false, false);
            prefabEntry.address = "testPrefab";
            prefabEntry.SetLabel(sortedAssetLabels[2], true);
            prefabEntry.SetLabel(sortedAssetLabels[0], true);
            prefabEntry.SetLabel(sortedAssetLabels[1], true);

            // Create texture asset with labels in different non-alphabetical order
            string texturePath = CreateTextureWithMultipleSprites("testTexture");
            string textureGuid = AssetDatabase.AssetPathToGUID(texturePath);
            m_CreatedAssetGuids.Add(textureGuid);
            var textureEntry = Settings.CreateOrMoveEntry(textureGuid, group, false, false);
            textureEntry.address = "testTexture";
            textureEntry.SetLabel(sortedAssetLabels[1], true);
            textureEntry.SetLabel(sortedAssetLabels[2], true);
            textureEntry.SetLabel(sortedAssetLabels[0], true);

            // Create scene asset with labels in yet another non-alphabetical order
            string scenePath = GetAssetPath("testScene.unity");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            EditorSceneManager.SaveScene(scene, scenePath);
            m_CreatedAssetPaths.Add(scenePath);

            string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
            m_CreatedAssetGuids.Add(sceneGuid);
            var sceneEntry = Settings.CreateOrMoveEntry(sceneGuid, group, false, false);
            sceneEntry.address = "testScene";
            sceneEntry.SetLabel(sortedSceneLabels[2], true);
            sceneEntry.SetLabel(sortedSceneLabels[0], true);
            sceneEntry.SetLabel(sortedSceneLabels[1], true);

            // Process with schema builder
            schemaBuilder.Init(null, null);
            string result = schemaBuilder.ProcessGroupSchema(schema, group, aaContext);
            Assert.IsEmpty(result, $"ProcessGroupSchema failed: {result}");

            // Load the generated GroupRootAsset
            var rootAssetFiles = Directory.GetFiles(schemaBuilder.RootAssetBuildPath, "*.asset");
            Assert.AreEqual(1, rootAssetFiles.Length, "Expected one root asset file");
            var rootAsset = AssetDatabase.LoadAssetAtPath<GroupRootAsset>(rootAssetFiles[0]);
            Assert.IsNotNull(rootAsset, "Failed to load GroupRootAsset");

            // Verify labels are sorted for prefab
            var prefabLoadableInfo = rootAsset.GetLoadableInfo("testPrefab", typeof(GameObject));
            Assert.IsNotNull(prefabLoadableInfo, "LoadableInfo not found for prefab");
            CollectionAssert.AreEqual(sortedAssetLabels, prefabLoadableInfo.labels,
                "Prefab labels should be sorted alphabetically");

            // Verify labels are sorted for texture
            var textureLoadableInfo = rootAsset.GetLoadableInfo("testTexture", typeof(Texture2D));
            Assert.IsNotNull(textureLoadableInfo, "LoadableInfo not found for texture");
            CollectionAssert.AreEqual(sortedAssetLabels, textureLoadableInfo.labels,
                "Texture labels should be sorted alphabetically");

            // Verify labels are sorted for scene
            var sceneLoadableInfo = rootAsset.GetLoadableInfo("testScene", typeof(SceneInstance));
            Assert.IsNotNull(sceneLoadableInfo, "LoadableInfo not found for scene");
            CollectionAssert.AreEqual(sortedSceneLabels, sceneLoadableInfo.labels,
                "Scene labels should be sorted alphabetically");
        }
    }
}
#endif
