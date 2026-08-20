#if ENABLE_CONTENT_DIRECTORIES
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.TestTools;
using UnityEngine.U2D;

namespace UnityEngine.AddressableAssets.ResourceProviders.Tests
{
    /// <summary>
    /// Runs the GroupAssetEntryProviderIntegrationTests suite against
    /// <see cref="NativeContentAssetEntryProvider"/>.
    ///
    /// Both fixtures build the same catalog (stamped with GroupAssetEntryProvider).
    /// At runtime, after AddressablesImpl is initialized, this fixture swaps the
    /// GroupAssetEntryProvider instance in ResourceManager.ResourceProviders for a
    /// NativeContentAssetEntryProvider that reports the same ProviderId so catalog
    /// references still resolve. This is intentionally fragile - it should be removed
    /// once the original provider is retired and NativeContentAssetEntryProvider becomes
    /// the catalog default.
    ///
    /// Tests defined here are specific to the new provider: they exercise code paths
    /// (synchronous WaitForCompletion, multi-Provide-per-frame batching) that the
    /// inherited suite does not reach.
    /// </summary>
    public class NativeContentAssetEntryProviderIntegrationTests : GroupAssetEntryProviderIntegrationTests
    {
        // Keys defined in the base fixture. Re-declared here as locals so this file is
        // self-contained; values must match the base.
        private const string PrefabKey = "test_prefab";
        private const string MultiSpriteTextureKey = "multi_sprite_texture";
        private const string SpriteTopLeftName = "sprite_topleft";
        private const string SpriteAtlasKey = "test_sprite_atlas";

        protected override IEnumerator InitAddressables()
        {
            yield return base.InitAddressables();
            SwapProviderForNativeContent();
        }

        void SwapProviderForNativeContent()
        {
            var providers = m_Addressables.ResourceManager.ResourceProviders;
            string targetProviderId = typeof(NativeContentAssetEntryProvider).FullName;

            for (int i = 0; i < providers.Count; i++)
            {
                if (providers[i] is NativeContentAssetEntryProvider existing && existing.ProviderId == targetProviderId)
                {
                    var native = new NativeContentAssetEntryProvider();
                    // Inherit the catalog-side ProviderId so location resolution still finds us.
                    native.Initialize(targetProviderId, "");
                    providers[i] = native;
                    return;
                }
            }

            Assert.Fail($"NativeContentAssetEntryProviderIntegrationTests setup: did not find a {nameof(NativeContentAssetEntryProvider)} to replace in ResourceManager.");
        }

        [UnityTest]
        public IEnumerator WaitForCompletion_Single_LoadsAsset()
        {
            // Exercises the WaitForSingle path: Flush + NativeLoadingSystem.WaitForLoadCompletion +
            // Drain inline. The inherited tests yield on the handle (Update-driven), so this
            // path is otherwise untested.
            var handle = m_Addressables.LoadAssetAsync<GameObject>(PrefabKey);
            handle.WaitForCompletion();

            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle.Status, "Synchronous load should succeed");
            Assert.IsNotNull(handle.Result, "Loaded asset should not be null");
            Assert.AreEqual(PrefabKey, handle.Result.name, "Loaded asset should have correct name");

            handle.Release();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ConcurrentLoads_BatchedInSingleFrame()
        {
            // Issue multiple Provides without yielding between them. They share the
            // ContentDirectory dependency, so once it's loaded, the entry Provides run
            // together in the same Update tick and are batched into a single LoadAsync
            // call. This exercises the staging buffer's accumulate-then-flush behavior
            // and Drain's per-handle dispatch back to the right ProvideHandle.
            string spriteAddress = $"{MultiSpriteTextureKey}[{SpriteTopLeftName}]";

            var prefabHandle = m_Addressables.LoadAssetAsync<GameObject>(PrefabKey);
            var atlasHandle = m_Addressables.LoadAssetAsync<SpriteAtlas>(SpriteAtlasKey);
            var spriteHandle = m_Addressables.LoadAssetAsync<Sprite>(spriteAddress);

            yield return prefabHandle;
            yield return atlasHandle;
            yield return spriteHandle;

            Assert.AreEqual(AsyncOperationStatus.Succeeded, prefabHandle.Status, "Prefab load should succeed");
            Assert.AreEqual(AsyncOperationStatus.Succeeded, atlasHandle.Status, "Atlas load should succeed");
            Assert.AreEqual(AsyncOperationStatus.Succeeded, spriteHandle.Status, "Sprite load should succeed");

            Assert.IsNotNull(prefabHandle.Result, "Prefab result should not be null");
            Assert.AreEqual(PrefabKey, prefabHandle.Result.name);

            Assert.IsNotNull(atlasHandle.Result, "Atlas result should not be null");
            Assert.IsInstanceOf<SpriteAtlas>(atlasHandle.Result);

            Assert.IsNotNull(spriteHandle.Result, "Sprite result should not be null");
            Assert.AreEqual(SpriteTopLeftName, spriteHandle.Result.name);

            prefabHandle.Release();
            atlasHandle.Release();
            spriteHandle.Release();
        }
    }
}
#endif
