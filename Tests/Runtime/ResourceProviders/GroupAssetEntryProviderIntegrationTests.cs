#if ENABLE_CONTENT_DIRECTORIES
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.TestTools;
using UnityEngine.U2D;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.U2D;
#endif

namespace UnityEngine.AddressableAssets.ResourceProviders.Tests
{
    /// <summary>
    /// Integration tests for Content Directory asset loading that validate end-to-end behavior
    /// including building Content Directory groups and loading assets at runtime via
    /// NativeContentAssetEntryProvider.
    ///
    /// Subclass this fixture to run additional provider-specific tests (see
    /// NativeContentAssetEntryProviderIntegrationTests for WaitForCompletion and batching tests).
    /// </summary>
    public class GroupAssetEntryProviderIntegrationTests : AddressablesTestFixture
    {
        protected override TestBuildScriptMode BuildScriptMode => TestBuildScriptMode.SchemaDriven;

        // Asset keys for testing
        private const string k_PrefabKey = "test_prefab";
        private const string k_MultiSpriteTextureKey = "multi_sprite_texture";
        private const string k_SpriteTopLeftName = "sprite_topleft";
        private const string k_SpriteBotRightName = "sprite_botright";
        private const string k_GroupName = "ContentDirectoryIntegrationTestGroup";

        // Sprite name in a single-sprite atlas matches the source texture's filename, so
        // CreateSpriteAtlasWithSprite uses k_AtlasSpriteName for both.
        private const string k_SpriteAtlasKey = "test_sprite_atlas";
        private const string k_AtlasSpriteName = "atlas_member_sprite";

        private string k_TextureGuidFile
        {
            get
            {
                return $"{GetGeneratedAssetsPath()}/GroupAssetEntryProviderTests_TextureGuid.txt";
            }
        }

        string SpriteTopLeftAddress => $"{k_MultiSpriteTextureKey}[{k_SpriteTopLeftName}]";
        string SpriteBotRightAddress => $"{k_MultiSpriteTextureKey}[{k_SpriteBotRightName}]";
        string AtlasSpriteAddress => $"{k_SpriteAtlasKey}[{k_AtlasSpriteName}]";

#if UNITY_EDITOR
        internal override void Setup(AddressableAssetSettings settings, string tempAssetFolder)
        {
            // Create group with ContentDirectoryGroupSchema
            AddressableAssetGroup cdGroup = settings.CreateGroup(
                k_GroupName,
                false, false, false, null,
                typeof(ContentDirectoryGroupSchema));

            // Configure the schema. CatalogId, schema BuildPath/LoadPath, and the temp
            // staging path all must be unique per fixture so derived fixtures (e.g.
            // NativeContentAssetEntryProviderIntegrationTests) don't share the same
            // global Library/com.unity.addressables/aa/<Platform> destination - the
            // second build's content/manifest hashes would otherwise replace the first
            // build's, and the runtime CAH lookup would miss for one of the fixtures.
            settings.profileSettings.SetValue(settings.activeProfileId, AddressableAssetSettings.kLocalBuildPath,
                $"{AddressableAssetSettings.kLocalBuildPathValue}/{m_UniqueTestName}");
            settings.profileSettings.SetValue(settings.activeProfileId, AddressableAssetSettings.kLocalLoadPath,
                $"{AddressableAssetSettings.kLocalLoadPathValue}/{m_UniqueTestName}");

            // Wipe stale outputs from prior runs. ContentDirectorySchemaBuilder.PostProcessDirectory
            // only copies a file when the destination doesn't already exist, so a stale
            // BuildManifestHash.txt at the Library destination would survive an archive
            // regeneration and the runtime CAH lookup would miss for the new content.
            string fixtureLibraryDir = Path.Combine(
                Addressables.BuildPath,
                EditorUserBuildSettings.activeBuildTarget.ToString(),
                m_UniqueTestName);
            if (Directory.Exists(fixtureLibraryDir))
                Directory.Delete(fixtureLibraryDir, recursive: true);

            var schema = cdGroup.GetSchema<ContentDirectoryGroupSchema>();
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            schema.CatalogId = $"test_integration_catalog_{m_UniqueTestName}";

            // Create test prefab
            string prefabPath = CreateAssetPath(tempAssetFolder, k_PrefabKey, ".prefab");
            string prefabGuid = CreatePrefab(prefabPath);
            var prefabEntry = settings.CreateOrMoveEntry(prefabGuid, cdGroup, false, false);
            prefabEntry.address = k_PrefabKey;

            // Create texture with multiple sprites (for subasset testing)
            string texturePath = CreateTextureWithMultipleSprites(tempAssetFolder);
            string textureGuid = AssetDatabase.AssetPathToGUID(texturePath);

            // Add the texture as an addressable entry
            var mainEntry = settings.CreateOrMoveEntry(textureGuid, cdGroup, false, false);
            mainEntry.address = k_MultiSpriteTextureKey;

            // Store the texture GUID to a file that survives domain reload
            File.WriteAllText(k_TextureGuidFile, textureGuid);

            string atlasPath = CreateSpriteAtlasWithSprite(tempAssetFolder, k_AtlasSpriteName);
            string atlasGuid = AssetDatabase.AssetPathToGUID(atlasPath);
            var atlasEntry = settings.CreateOrMoveEntry(atlasGuid, cdGroup, false, false);
            atlasEntry.address = k_SpriteAtlasKey;
        }

        private string CreateTextureWithMultipleSprites(string folder)
        {
            // Create a texture
            var texture = new Texture2D(32, 32);
            for (int x = 0; x < 32; x++)
                for (int y = 0; y < 32; y++)
                    texture.SetPixel(x, y, Color.white);
            texture.Apply();

            byte[] data = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);

            string texturePath = Path.Combine(folder, "multi_sprite.png");
            File.WriteAllBytes(texturePath, data);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            // Configure as multiple sprites
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;

#pragma warning disable 618
            importer.spritesheet = new SpriteMetaData[]
            {
                new SpriteMetaData() { name = k_SpriteTopLeftName, pivot = Vector2.zero, rect = new Rect(0, 16, 16, 16) },
                new SpriteMetaData() { name = k_SpriteBotRightName, pivot = Vector2.zero, rect = new Rect(16, 0, 16, 16) }
            };
#pragma warning restore 618

            importer.SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            return texturePath;
        }

        private string CreateSpriteAtlasWithSprite(string folder, string spriteName)
        {
            var texture = new Texture2D(32, 32);
            var data = ImageConversion.EncodeToPNG(texture);
            UnityEngine.Object.DestroyImmediate(texture);

            string texturePath = Path.Combine(folder, spriteName + ".png");
            File.WriteAllBytes(texturePath, data);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();

            string atlasPath = Path.Combine(folder, k_SpriteAtlasKey + ".spriteatlas");
            var sa = new SpriteAtlas();
            sa.Add(new[] { AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(texturePath) });
            AssetDatabase.CreateAsset(sa, atlasPath);
            SpriteAtlasUtility.PackAtlases(new SpriteAtlas[] { sa }, EditorUserBuildSettings.activeBuildTarget, false);
            SpriteAtlasUtility.CleanupAtlasPacking();
            return atlasPath;
        }

        protected override void RunBuilder(AddressableAssetSettings settings)
        {
            try
            {
                base.RunBuilder(settings);
            }
            catch (Exception ex)
            {
                Debug.LogError($"GroupAssetEntryProviderIntegrationTests: RunBuilder failed: {ex}");
                if (ex.InnerException != null)
                    Debug.LogError($"Inner exception: {ex.InnerException}");
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
                Debug.LogError(
                    $"GroupAssetEntryProviderIntegrationTests: InitializeAsync failed, status={op.Status}. {details}");
                Assert.Fail($"InitializeAsync failed: Status={op.Status}. OperationException: {details}");
            }

            OnRuntimeSetup();
            if (op.IsValid())
                op.Release();
        }

        [UnityTest]
        public IEnumerator LoadAsset_FromContentDirectory_Succeeds()
        {
            // Load a prefab from the Content Directory group
            var handle = m_Addressables.LoadAssetAsync<GameObject>(k_PrefabKey);
            yield return handle;

            // Verify successful load
            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle.Status, "Asset load should succeed");
            Assert.IsNotNull(handle.Result, "Loaded asset should not be null");
            Assert.AreEqual(k_PrefabKey, handle.Result.name, "Loaded asset should have correct name");

            // Cleanup
            handle.Release();
        }

        [UnityTest]
        public IEnumerator LoadAsset_FromContentDirectory_MultipleTimes_Succeeds()
        {
            // Validates that NativeContentAssetEntryProvider returns the same native object
            // when the same location is provided again while still held.
            // 1. Load asset (native load issued, SingleEntry stored with refCount=1)
            // 2. Clear operation cache (forces Provide() to be called again on next load)
            // 3. Load same asset again (TryCompleteFromLoaded finds the entry, bumps refCount to 2,
            //    and completes synchronously — no second native load issued)

            // First load - issues a native load and stores a SingleEntry
            var handle = m_Addressables.LoadAssetAsync<GameObject>(k_PrefabKey);
            yield return handle;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle.Status, "First load should succeed");
            Assert.IsNotNull(handle.Result, "First load result should not be null");

            // Clear the operation cache to force Provide() to be called again.
            // (Normally the second load would return the cached ResourceManager operation.)
            m_Addressables.GetResourceLocations(k_PrefabKey, typeof(GameObject), out var locations);
            m_Addressables.ResourceManager.RemoveOperationFromCache(new LocationCacheKey(locations[0], typeof(GameObject)));

            // Second load - Provide() is called again; TryCompleteFromLoaded should return the
            // already-resident object without issuing a second native load.
            var handle2 = m_Addressables.LoadAssetAsync<GameObject>(k_PrefabKey);
            yield return handle2;

            // Verify successful load
            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle2.Status, "Second load should succeed");
            Assert.IsNotNull(handle2.Result, "Second load result should not be null");
            Assert.AreEqual(k_PrefabKey, handle2.Result.name, "Loaded asset should have correct name");
            Assert.AreSame(handle.Result, handle2.Result, "Both loads should return the same cached object");

            // Cleanup
            handle.Release();
            handle2.Release();
        }

        [UnityTest]
        public IEnumerator LoadAsset_WhenAlreadyLoaded_DoesNotThrowError()
        {
            // First load
            var handle1 = m_Addressables.LoadAssetAsync<GameObject>(k_PrefabKey);
            yield return handle1;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle1.Status, "First load should succeed");
            Assert.IsNotNull(handle1.Result, "First load result should not be null");

            // Second load of same asset (operation cache hit; ResourceManager returns the
            // cached operation without calling Provide again — this is the fast path).
            var handle2 = m_Addressables.LoadAssetAsync<GameObject>(k_PrefabKey);
            yield return handle2;

            // Both should succeed
            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle2.Status, "Second load should succeed");
            Assert.IsNotNull(handle2.Result, "Second load result should not be null");
            Assert.AreSame(handle1.Result, handle2.Result, "Both loads should return the same instance");

            // Cleanup
            handle1.Release();
            handle2.Release();
        }

        [UnityTest]
        public IEnumerator LoadSpriteList_FromMultiSpriteTexture_MultipleTimes_Succeeds()
        {
            // Validates that NativeContentAssetEntryProvider's list path returns the same native
            // objects when the same location is provided again while still held.
            // 1. Load IList<Sprite> (native loads issued, ListEntry stored with refCount=1)
            // 2. Clear operation cache (forces HandleListRequest / Provide() to be called again)
            // 3. Load same location again (TryCompleteListFromLoaded finds the entry, bumps
            //    refCount to 2, and completes synchronously — no second native loads issued)

            // First load - issues native loads and stores a ListEntry
            var handle1 = m_Addressables.LoadAssetAsync<IList<Sprite>>(k_MultiSpriteTextureKey);
            yield return handle1;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle1.Status, "First list load should succeed");
            Assert.IsNotNull(handle1.Result, "First list result should not be null");
            Assert.Greater(handle1.Result.Count, 0, "First list result should contain sprites");

            // Clear the operation cache to force HandleListRequest to be called again.
            m_Addressables.GetResourceLocations(k_MultiSpriteTextureKey, typeof(IList<Sprite>), out var locations);
            m_Addressables.ResourceManager.RemoveOperationFromCache(new LocationCacheKey(locations[0], typeof(IList<Sprite>)));

            // Second load - TryCompleteListFromLoaded should return the already-resident sprites.
            var handle2 = m_Addressables.LoadAssetAsync<IList<Sprite>>(k_MultiSpriteTextureKey);
            yield return handle2;

            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle2.Status, "Second list load should succeed");
            Assert.IsNotNull(handle2.Result, "Second list result should not be null");
            Assert.AreEqual(handle1.Result.Count, handle2.Result.Count, "Both loads should return the same number of sprites");
            for (int i = 0; i < handle1.Result.Count; i++)
                Assert.AreSame(handle1.Result[i], handle2.Result[i], $"Sprite at index {i} should be the same instance");

            // Cleanup
            handle1.Release();
            handle2.Release();
        }

        [UnityTest]
        public IEnumerator LoadAsset_MultipleSequentialLoads_MaintainsReferenceCount()
        {
            // Load the same asset multiple times
            var handle1 = m_Addressables.LoadAssetAsync<GameObject>(k_PrefabKey);
            yield return handle1;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle1.Status);

            var handle2 = m_Addressables.LoadAssetAsync<GameObject>(k_PrefabKey);
            yield return handle2;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle2.Status);

            var handle3 = m_Addressables.LoadAssetAsync<GameObject>(k_PrefabKey);
            yield return handle3;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle3.Status);

            // All should reference the same object
            Assert.AreSame(handle1.Result, handle2.Result, "Handles 1 and 2 should reference same object");
            Assert.AreSame(handle2.Result, handle3.Result, "Handles 2 and 3 should reference same object");

            // Release in reverse order
            handle3.Release();
            handle2.Release();
            handle1.Release();
        }

        [UnityTest]
        public IEnumerator ContentDirectoryMount_RemainsMountedAfterAllAssetsReleased()
        {
            // Validates the data-driven mount: each entry carries its Content Directory load path
            // in its ContentDirectoryAssetData (no standalone catalog dependency), and the providers
            // mount the directory once and keep it mounted for the lifetime of the system (it is
            // unmounted en masse by AddressablesImpl.Dispose, not when individual assets release).
            // Loading, fully releasing, and reloading from the same directory must keep working.
            var prefabHandle = m_Addressables.LoadAssetAsync<GameObject>(k_PrefabKey);
            yield return prefabHandle;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, prefabHandle.Status, "First asset load should succeed");

            var atlasHandle = m_Addressables.LoadAssetAsync<SpriteAtlas>(k_SpriteAtlasKey);
            yield return atlasHandle;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, atlasHandle.Status, "Second asset load should succeed");

            // Release every asset loaded from the directory. The directory stays mounted.
            prefabHandle.Release();
            atlasHandle.Release();
            yield return null;

            // A fresh load from the same Content Directory must still succeed.
            var prefabHandle2 = m_Addressables.LoadAssetAsync<GameObject>(k_PrefabKey);
            yield return prefabHandle2;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, prefabHandle2.Status,
                "Load should still succeed; the Content Directory stays mounted until the system is disposed");
            Assert.IsNotNull(prefabHandle2.Result, "Reloaded asset should not be null");
            Assert.AreEqual(k_PrefabKey, prefabHandle2.Result.name);

            prefabHandle2.Release();
        }

        [UnityTest]
        public IEnumerator LoadAsset_InvalidKey_Fails()
        {
            // Expect the error log for invalid key
            LogAssert.Expect(LogType.Error, new Regex(".*InvalidKeyException.*No Location found for Key=nonexistent_key.*"));

            // Try to load an asset with a non-existent key
            var handle = m_Addressables.LoadAssetAsync<GameObject>("nonexistent_key");
            yield return handle;

            // Should fail
            Assert.AreEqual(AsyncOperationStatus.Failed, handle.Status, "Load with invalid key should fail");
            Assert.IsNull(handle.Result, "Result should be null for failed load");

            // Cleanup
            if (handle.IsValid())
                handle.Release();
        }

        [UnityTest]
        public IEnumerator LoadSprite_FromMultiSpriteTexture_LoadsCorrectSubasset()
        {
            // Load the top-left sprite subasset
            var handleTopLeft = m_Addressables.LoadAssetAsync<Sprite>(SpriteTopLeftAddress);
            yield return handleTopLeft;

            Assert.AreEqual(AsyncOperationStatus.Succeeded, handleTopLeft.Status, "Top-left sprite load should succeed");
            Assert.IsNotNull(handleTopLeft.Result, "Top-left sprite should not be null");
            Assert.IsInstanceOf<Sprite>(handleTopLeft.Result, "Result should be a Sprite");
            Assert.AreEqual(k_SpriteTopLeftName, handleTopLeft.Result.name, "Sprite should have correct name");

            // Load the bottom-right sprite subasset
            var handleBotRight = m_Addressables.LoadAssetAsync<Sprite>(SpriteBotRightAddress);
            yield return handleBotRight;

            Assert.AreEqual(AsyncOperationStatus.Succeeded, handleBotRight.Status, "Bottom-right sprite load should succeed");
            Assert.IsNotNull(handleBotRight.Result, "Bottom-right sprite should not be null");
            Assert.IsInstanceOf<Sprite>(handleBotRight.Result, "Result should be a Sprite");
            Assert.AreEqual(k_SpriteBotRightName, handleBotRight.Result.name, "Sprite should have correct name");

            // Verify they are different sprites
            Assert.AreNotSame(handleTopLeft.Result, handleBotRight.Result, "Should load different sprite subassets");

            // Cleanup
            handleTopLeft.Release();
            handleBotRight.Release();
        }

        [UnityTest]
        public IEnumerator LoadSprite_WhenAlreadyLoaded_DoesNotThrowError()
        {
            // First load of sprite subasset
            var handle1 = m_Addressables.LoadAssetAsync<Sprite>(SpriteTopLeftAddress);
            yield return handle1;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle1.Status, "First sprite load should succeed");

            // Second load of same sprite (tests already-loaded Loadable for subassets)
            var handle2 = m_Addressables.LoadAssetAsync<Sprite>(SpriteTopLeftAddress);
            yield return handle2;

            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle2.Status, "Second sprite load should succeed");
            Assert.AreSame(handle1.Result, handle2.Result, "Both loads should return the same sprite instance");

            // Cleanup
            handle1.Release();
            handle2.Release();
        }

        [UnityTest]
        public IEnumerator LoadSpriteAtlas_FromContentDirectory_Succeeds()
        {
            var op = m_Addressables.LoadAssetAsync<SpriteAtlas>(k_SpriteAtlasKey);
            yield return op;
            Assert.IsNotNull(op.Result);
            Assert.AreEqual(typeof(SpriteAtlas), op.Result.GetType());
            op.Release();
        }

        // Regression: ContentDirectorySchemaBuilder used to emit atlas member sprites with
        // ResourceType = typeof(AddressableAssetEntry), an editor-only type.
        [UnityTest]
        public IEnumerator LoadSpriteFromAtlas_FromContentDirectory_LoadsCorrectSprite()
        {
            var op = m_Addressables.LoadAssetAsync<Sprite>(AtlasSpriteAddress);
            yield return op;
            Assert.IsNotNull(op.Result);
            Assert.AreEqual(typeof(Sprite), op.Result.GetType());
            op.Release();
        }

#if UNITY_EDITOR
        //Making this an editor only test since we rely on a file written during setup.
        [UnityTest]
        public IEnumerator LoadSprite_UsingGuid_FromMultiSpriteTexture_LoadsCorrectSubasset()
        {
            // Get the texture GUID from the file written during Setup
            Assert.IsTrue(File.Exists(k_TextureGuidFile), "Texture GUID file should exist");
            string textureGuid = File.ReadAllText(k_TextureGuidFile).Trim();
            Assert.IsFalse(string.IsNullOrEmpty(textureGuid), "Texture GUID should not be empty");

            // Build sprite keys using GUID instead of address
            string topLeftSpriteKey = $"{textureGuid}[{k_SpriteTopLeftName}]";
            string bottomRightSpriteKey = $"{textureGuid}[{k_SpriteBotRightName}]";

            // Load the top-left sprite subasset using GUID
            var handleTopLeft = m_Addressables.LoadAssetAsync<Sprite>(topLeftSpriteKey);
            yield return handleTopLeft;

            Assert.AreEqual(AsyncOperationStatus.Succeeded, handleTopLeft.Status, "Top-left sprite load should succeed");
            Assert.IsNotNull(handleTopLeft.Result, "Top-left sprite should not be null");
            Assert.IsInstanceOf<Sprite>(handleTopLeft.Result, "Result should be a Sprite");
            Assert.AreEqual(k_SpriteTopLeftName, handleTopLeft.Result.name, "Sprite should have correct name");

            // Load the bottom-right sprite subasset using GUID
            var handleBotRight = m_Addressables.LoadAssetAsync<Sprite>(bottomRightSpriteKey);
            yield return handleBotRight;

            Assert.AreEqual(AsyncOperationStatus.Succeeded, handleBotRight.Status, "Bottom-right sprite load should succeed");
            Assert.IsNotNull(handleBotRight.Result, "Bottom-right sprite should not be null");
            Assert.IsInstanceOf<Sprite>(handleBotRight.Result, "Result should be a Sprite");
            Assert.AreEqual(k_SpriteBotRightName, handleBotRight.Result.name, "Sprite should have correct name");

            // Verify they are different sprites
            Assert.AreNotSame(handleTopLeft.Result, handleBotRight.Result, "Should load different sprite subassets");

            // Cleanup
            handleTopLeft.Release();
            handleBotRight.Release();
        }
#endif //UNITY_EDITOR
    }
}
#endif
