using System;
using NUnit.Framework;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
#if ENABLE_CONTENT_DIRECTORIES
using UnityEngine.AddressableAssets.ResourceProviders;
#endif

namespace UnityEngine.ResourceManagement.ResourceProviders.Tests
{
    [TestFixture]
    public class SceneProviderTests
    {
        /// <summary>
        /// Verifies that providers return the correct type from SceneProvider::GetDependencyResourceType.
        /// </summary>
        [TestCase(typeof(TestResourceProvider), typeof(IAssetBundleResource))]
        [TestCase(typeof(CustomDependencyTypeProvider), typeof(Texture2D))]
        [TestCase(typeof(CustomNonBaseProvider), typeof(IAssetBundleResource))]
#if ENABLE_CONTENT_DIRECTORIES
        [TestCase(typeof(GroupRootAssetEntryProvider), typeof(Object))]
#endif
        public void DependencyResourceType_ResourceProviderBase_DefaultsToIAssetBundleResource(Type resourceProviderType, Type expectedDepType)
        {
            var rm = new ResourceManager();
            try
            {
                // instantiate the provider
                var provider = resourceProviderType.GetConstructor(Type.EmptyTypes)?.Invoke(null) as IResourceProvider;
                Assert.NotNull(provider);

                // add it to the resource manager
                rm.ResourceProviders.Add(provider);

                // create our location using that provider
                var location = new ResourceLocationBase("1", "internalid", provider.ProviderId, expectedDepType);

                // create a scene provider and get the dependency resource type for the location
                var sceneProvider = new SceneProvider();
                var actualDepType = sceneProvider.GetSceneDependencyResourceType(rm, location);
                Assert.AreEqual(expectedDepType, actualDepType);
            }
            finally
            {
                rm.Dispose();
            }
        }
    }

    /// <summary>
    /// Mock provider that inherits ResourceProviderBase (uses default DependencyResourceType).
    /// </summary>
    class TestResourceProvider : ResourceProviderBase
    {
        public override void Provide(ProvideHandle provideHandle)
        {
            provideHandle.Complete<object>(null, false, new Exception("Test provider"));
        }
    }

    /// <summary>
    /// Mock provider that inherits ResourceProviderBase and overrides DependencyResourceType.
    /// </summary>
    class CustomDependencyTypeProvider : ResourceProviderBase
    {
        public override Type SceneDependencyResourceType => typeof(Texture2D);

        public override void Provide(ProvideHandle provideHandle)
        {
            provideHandle.Complete<object>(null, false, new Exception("Test provider"));
        }
    }

    /// <summary>
    /// Mock provider that implements IResourceProvider directly (NOT ResourceProviderBase).
    /// This tests the fallback behavior when a provider doesn't have DependencyResourceType.
    /// </summary>
    class CustomNonBaseProvider : IResourceProvider
    {
        public string ProviderId => GetType().FullName;
        public ProviderBehaviourFlags BehaviourFlags => ProviderBehaviourFlags.None;
        public Type GetDefaultType(IResourceLocation location) => typeof(object);
        public bool CanProvide(Type type, IResourceLocation location) => true;
        public void Provide(ProvideHandle provideHandle) { }
        public void Release(IResourceLocation location, object asset) { }
    }
}
