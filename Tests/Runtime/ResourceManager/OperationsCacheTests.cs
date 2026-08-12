using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.ResourceManagement.Tests
{
    public class OperationsCacheTests
    {
        ResourceManager m_ResourceManager;

        [SetUp]
        public void Setup()
        {
            m_ResourceManager = new ResourceManager();
            m_ResourceManager.CallbackHooksEnabled = false; // default for tests. disabled callback hooks. we will call update manually
        }

        [TearDown]
        public void TearDown()
        {
            Assert.Zero(m_ResourceManager.OperationCacheCount);
            m_ResourceManager.Dispose();
        }

        [Test]
        public void OperationCache_CollisionsAreProperlyHandled()
        {
#if ENABLE_CACHING
            Assert.Zero(m_ResourceManager.OperationCacheCount);
            var provType = typeof(InstanceProvider);
            var loc1 = new TestResourceLocation("asset1", "asset1", provType.FullName, typeof(GameObject));
            var key1 = new LocationCacheKey(loc1, typeof(GameObject));
            var opType = typeof(TestOperation);
            m_ResourceManager.CreateOperation<TestOperation>(opType, opType.GetHashCode(), key1, null);

            Assert.AreEqual(1, m_ResourceManager.CachedOperationCount());
            Assert.IsTrue(m_ResourceManager.IsOperationCached(key1));

            var loc2 = new TestResourceLocation("asset2", "asset2", provType.FullName, typeof(GameObject));
            var key2 = new LocationCacheKey(loc2, typeof(GameObject));
            Assert.IsFalse(m_ResourceManager.IsOperationCached(key2));

            Assert.IsTrue(m_ResourceManager.RemoveOperationFromCache(key1));
#else
            Assert.Ignore("Caching not enabled.");
#endif
        }


        [Test]
        public void LocationCacheKey_Equals_ReturnsExpectedValue()
        {
            var d1 = new TestResourceLocation("d1", "dd1", "dp", typeof(AssetBundle));
            var d2 = new TestResourceLocation("d2", "dd2", "dp", typeof(AssetBundle));
            var d3 = new TestResourceLocation("d3", "dd3", "dp", typeof(AssetBundle));
            var t = typeof(GameObject);
            {
                //everything same, should return true
                var k1 = new LocationCacheKey(new TestResourceLocation("loc1", "x", "p", t, d1, d2), t);
                var k2 = new LocationCacheKey(new TestResourceLocation("loc1", "x", "p", t, d1, d2), t);
                Assert.IsTrue(k1.Equals(k2));
            }

            {
                //dependencies different, should return false
                var k1 = new LocationCacheKey(new TestResourceLocation("loc1", "x", "p", t, d1, d2), t);
                var k2 = new LocationCacheKey(new TestResourceLocation("loc1", "x", "p", t, d1, d2, d3), t);
                Assert.IsFalse(k1.Equals(k2));
            }


            {
                //dependencies same, ids different should return false
                var k1 = new LocationCacheKey(new TestResourceLocation("loc1", "x", "p", t, d1, d2), t);
                var k2 = new LocationCacheKey(new TestResourceLocation("loc1", "x2", "p", t, d1, d2), t);
                Assert.IsFalse(k1.Equals(k2));
            }


        }

        class TestOperation : AsyncOperationBase<GameObject>, ICachable
        {
            protected override void Execute()
            {
            }

            public IOperationCacheKey Key { get; set; }
        }

        class TestResourceLocation : IResourceLocation
        {
            public TestResourceLocation(string name, string id, string providerId, Type t, params IResourceLocation[] dependencies)
            {
                PrimaryKey = name;
                InternalId = id;
                ProviderId = providerId;
                Dependencies = new List<IResourceLocation>(dependencies);
                ResourceType = t == null ? typeof(object) : t;
            }

            public string InternalId { get; }
            public string ProviderId { get; }
            public IList<IResourceLocation> Dependencies { get; }

            public int Hash(Type type)
            {
                // Returning a constant hashcode to force collisions.
                return 1337;
            }

            public int DependencyHashCode => 0;
            public bool HasDependencies => Dependencies.Count > 0;
            public object Data { get; }
            public string PrimaryKey { get; }
            public Type ResourceType { get; }
        }

        [Test]
        public void Locations_WithDiffNames_LocationEquals_Returns_True()
        {
            var l1 = new ResourceLocationBase("a", "b", "c", typeof(Mesh));
            var l2 = new ResourceLocationBase("x", "b", "c", typeof(Mesh));
            Assert.IsTrue(LocationUtils.LocationEquals(l1, l2));
        }

        [Test]
        public void Locations_WithDiffIds_LocationEquals_Returns_False()
        {
            var l1 = new ResourceLocationBase("a", "b", "c", typeof(Mesh));
            var l2 = new ResourceLocationBase("a", "x", "c", typeof(Mesh));
            Assert.IsFalse(LocationUtils.LocationEquals(l1, l2));
        }

        [Test]
        public void Locations_WithDiffProvider_LocationEquals_Returns_False()
        {
            var l1 = new ResourceLocationBase("a", "b", "c", typeof(Mesh));
            var l2 = new ResourceLocationBase("a", "b", "x", typeof(Mesh));
            Assert.IsFalse(LocationUtils.LocationEquals(l1, l2));
        }

        [Test]
        public void Locations_WithDiffResourceTypes_LocationEquals_Returns_True()
        {
            var l1 = new ResourceLocationBase("a", "b", "c", typeof(Mesh));
            var l2 = new ResourceLocationBase("a", "b", "c", typeof(Material));
            Assert.IsFalse(LocationUtils.LocationEquals(l1, l2));
        }

        class ResourceLocatonTestSub : ResourceLocationBase
        {
            public ResourceLocatonTestSub(string n, string id, string pr, Type t) : base(n, id, pr, t)
            {
            }
        }

        [Test]
        public void Locations_WithDiffTypes_LocationEquals_Returns_True()
        {
            var l1 = new ResourceLocationBase("a", "b", "c", typeof(Mesh));
            var l2 = new ResourceLocatonTestSub("a", "b", "c", typeof(Mesh));
            Assert.IsTrue(LocationUtils.LocationEquals(l1, l2));
        }
    }
}
