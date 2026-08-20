using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;

namespace UnityEngine.ResourceManagement.Tests
{
    public class DelayedActionManagerTests
    {
        class DamTest
        {
            public bool methodInvoked;
            public int frameInvoked;
            public float timeInvoked;

            public void Method()
            {
                frameInvoked = Time.frameCount;
                timeInvoked = Time.unscaledTime;
                methodInvoked = true;
            }

            public void MethodWithParams(int p1, string p2, bool p3, float p4)
            {
                Assert.AreEqual(p1, 5);
                Assert.AreEqual(p2, "testValue");
                Assert.AreEqual(p3, true);
                Assert.AreEqual(p4, 3.14f);
            }
        }

        [UnityTest]
        public IEnumerator DelayedActionManagerInvokeSameFrame()
        {
            var testObj = new DamTest();
            int frameCalled = Time.frameCount;
            DelayedActionManager.AddAction((Action)testObj.Method);
            yield return null;
            Assert.AreEqual(frameCalled, testObj.frameInvoked);
        }

        [UnityTest]
        public IEnumerator DelayedActionManagerInvokeDelayed()
        {
            var testObj = new DamTest();
            float timeCalled = Time.unscaledTime;
            DelayedActionManager.AddAction((Action)testObj.Method, 2);
            while (!testObj.methodInvoked)
                yield return null;
            //make sure delay was at least 1 second (to account for test slowness)
            Assert.Greater(testObj.timeInvoked, timeCalled + 1);
        }

        [UnityTest]
        public IEnumerator DelayedActionManagerInvokeWithParameters()
        {
            var testObj = new DamTest();
            DelayedActionManager.AddAction((Action<int, string, bool, float>)testObj.MethodWithParams, 0, 5, "testValue", true, 3.14f);
            yield return null;
        }
    }

    public class LinkedListNodeCacheTests
    {
        LinkedListNodeCache<T> CreateCache<T>(int count)
        {
            var cache = new LinkedListNodeCache<T>();
            var temp = new List<LinkedListNode<T>>();
            for (int i = 0; i < count; i++)
                temp.Add(cache.Acquire(default(T)));
            Assert.AreEqual(count, cache.CreatedNodeCount);
            foreach (var t in temp)
                cache.Release(t);
            Assert.AreEqual(count, cache.CachedNodeCount);
            return cache;
        }

        void PopulateCache_AddRemove<T>()
        {
            var cache = CreateCache<T>(1);
            Assert.That(() => { cache.Release(cache.Acquire(default(T))); }, TestTools.Constraints.Is.Not.AllocatingGCMemory(), "GC Allocation detected");
            Assert.AreEqual(1, cache.CreatedNodeCount);
            Assert.AreEqual(1, cache.CachedNodeCount);
        }

        [Test]
        public void WhenRefTypeAndCacheNotEmpty_AddRemove_DoesNotAlloc()
        {
            PopulateCache_AddRemove<string>();
        }

        [Test]
        public void WhenValueTypeAndCacheNotEmpty_AddRemove_DoesNotAlloc()
        {
            PopulateCache_AddRemove<int>();
        }

        [Test]
        public void Release_ResetsValue()
        {
            var cache = new LinkedListNodeCache<string>();
            var node = cache.Acquire(null);
            Assert.IsNull(node.Value);
            node.Value = "TestString";
            cache.Release(node);
            Assert.IsNull(node.Value);
        }
    }

    public class DelegateListTests
    {
        [Test]
        public void WhenDelegateRemoved_DelegateIsNotInvoked()
        {
            var cache = new LinkedListNodeCache<Action<string>>();
            var delList = new DelegateList<string>(cache.Acquire, cache.Release);
            bool called = false;
            Action<string> del = s => { called = true; };
            delList.Add(del);
            delList.Remove(del);
            delList.Invoke(null);
            Assert.IsFalse(called);
            Assert.AreEqual(cache.CreatedNodeCount, cache.CreatedNodeCount);
        }

        [Test]
        public void WhenAddInsideInvoke_NewDelegatesAreCalled()
        {
            bool addedDelegateCalled = false;
            var delList = CreateDelegateList<string>();
            delList.Add(s => delList.Add(s2 => addedDelegateCalled = true));
            delList.Invoke(null);
            Assert.IsTrue(addedDelegateCalled);
        }

        [Test]
        public void WhenCleared_DelegateIsNotInvoked()
        {
            var delList = CreateDelegateList<string>();
            int invocationCount = 0;
            delList.Add(s => invocationCount++);
            delList.Clear();
            delList.Invoke(null);
            Assert.AreEqual(0, invocationCount);
        }

        [Test]
        public void DuringInvoke_CanRemoveNextDelegate()
        {
            var delList = CreateDelegateList<string>();
            bool del1Called = false;
            Action<string> del1 = s => { del1Called = true; };
            Action<string> del2 = s => delList.Remove(del1);
            delList.Add(del2);
            delList.Add(del1);
            delList.Invoke(null);
            Assert.IsFalse(del1Called);
        }

        DelegateList<T> CreateDelegateList<T>()
        {
            var cache = new LinkedListNodeCache<Action<T>>();
            return new DelegateList<T>(cache.Acquire, cache.Release);
        }

        void InvokeAllocTest<T>(T p)
        {
            var delList = CreateDelegateList<T>();
            delList.Add(s => { });
            Assert.That(() => { delList.Invoke(p); }, TestTools.Constraints.Is.Not.AllocatingGCMemory(), "GC Allocation detected");
        }

        [Test]
        public void DelegateNoGCWithRefType()
        {
            InvokeAllocTest<string>(null);
        }

        [Test]
        public void DelegateNoGCWithValueType()
        {
            InvokeAllocTest<int>(0);
        }

        static object[] KeyResultData =
        {
            new object[] {null, false, null, null},
            new object[] {"", false, null, null},
            new object[] {5, false, null, null},
            new object[] {"k", false, null, null},
            new object[] {"[k]", false, null, null},
            new object[] {"k]s[", false, null, null},
            new object[] {"k[s", false, null, null},
            new object[] {"[s]k", false, null, null},
            new object[] {"k]s", false, null, null},
            new object[] {"k[s]", true, "k", "s"},
            new object[] {"k[[s]", true, "k", "[s"},
            new object[] {"k[s[]", true, "k", "s["},
            new object[] {"k[s]]", true, "k", "s]"},
            new object[] {"k[]s]", true, "k", "]s"},
        };

        [TestCaseSource(nameof(KeyResultData))]
        public void ResourceManagerConfigExtractKeyAndSubKey_WhenPassedKey_ReturnsExpectedValue(object key, bool expectedReturn, string expectedMainKey, string expectedSubKey)
        {
            Assert.AreEqual(expectedReturn, ResourceManagerConfig.ExtractKeyAndSubKey(key, out string mainKey, out string subKey));
            Assert.AreEqual(expectedMainKey, mainKey);
            Assert.AreEqual(expectedSubKey, subKey);
        }

        [TestCase(RuntimePlatform.WebGLPlayer, false)]
        [TestCase(RuntimePlatform.OSXEditor, true)]
        public void CanIdentifyMultiThreadedPlatforms(RuntimePlatform platform, bool usesMultiThreading)
        {
            Assert.AreEqual(usesMultiThreading, PlatformUtilities.PlatformUsesMultiThreading(platform));
        }
    }

    [TestFixture]
    class AddressablesImplGetCatalogExtensionTests
    {
        static readonly TestCaseData[] k_Cases =
        {
            // HTTP URLs with query strings — the core fix case
            new TestCaseData("http://127.0.0.1/catalog.bin?param1=value1&param2=value2", ".bin"),
            new TestCaseData("http://127.0.0.1/catalog.json?param=value",                ".json"),
            new TestCaseData("http://127.0.0.1/catalog.bin?date=20240101",               ".bin"),
            // HTTP URLs without query strings
            new TestCaseData("http://127.0.0.1/catalog.bin",                             ".bin"),
            new TestCaseData("http://127.0.0.1/catalog.json",                            ".json"),
            // Relative file paths
            new TestCaseData("Assets/catalog.bin",                                       ".bin"),
            new TestCaseData("Assets/catalog.json",                                      ".json"),
            new TestCaseData("catalog.bin",                                               ".bin"),
            // Unsupported extensions — returned as-is; caller validates
            new TestCaseData("http://127.0.0.1/catalog.xyz?param=value",                 ".xyz"),
            new TestCaseData("catalog.xyz",                                               ".xyz"),
            // No extension at all
            new TestCaseData("http://127.0.0.1/catalog?param=value",                     ""),
            new TestCaseData("catalog",                                                   ""),
        };

        [Test, TestCaseSource(nameof(k_Cases))]
        public void GetCatalogExtension_ReturnsCorrectExtension(string path, string expected)
        {
            Assert.AreEqual(expected, CatalogUtilities.GetCatalogExtension(path));
        }
    }

    [TestFixture]
    class AddressablesImplGetHashFilePathTests
    {
        static readonly TestCaseData[] k_Cases =
        {
            // Query string with colon — the exact pattern that broke Path.ChangeExtension
            new TestCaseData("http://127.0.0.1/catalog.bin?param1=value1&param2=value2:date=number",
                             "http://127.0.0.1/catalog.hash?param1=value1&param2=value2:date=number"),
            // Regular query string
            new TestCaseData("http://127.0.0.1/catalog.json?param=value",
                             "http://127.0.0.1/catalog.hash?param=value"),
            // URL without query string
            new TestCaseData("http://127.0.0.1/catalog.bin",
                             "http://127.0.0.1/catalog.hash"),
            // Relative paths
            new TestCaseData("Assets/catalog.bin",  "Assets/catalog.hash"),
            new TestCaseData("catalog.json",         "catalog.hash"),
            // URL with no extension — ChangeExtension adds .hash
            new TestCaseData("http://127.0.0.1/catalog?param=value",
                             "http://127.0.0.1/catalog.hash?param=value"),
            new TestCaseData("catalog",              "catalog.hash"),
        };

        [Test, TestCaseSource(nameof(k_Cases))]
        public void GetHashFilePath_ReturnsCorrectHashPath(string catalogPath, string expected)
        {
            Assert.AreEqual(expected, CatalogUtilities.GetHashFilePath(catalogPath));
        }
    }

    [TestFixture]
    class GetCatalogFilePathTests
    {
        static readonly TestCaseData[] k_Cases =
        {
            // Simple extension swap: .hash → .bin / .json
            new TestCaseData("http://127.0.0.1/catalog.hash",   ".bin",  "http://127.0.0.1/catalog.bin"),
            new TestCaseData("http://127.0.0.1/catalog.hash",   ".json", "http://127.0.0.1/catalog.json"),
            // Relative and bare filenames
            new TestCaseData("Assets/catalog.hash", ".bin",  "Assets/catalog.bin"),
            new TestCaseData("catalog.hash",         ".json", "catalog.json"),
            // Query string is preserved
            new TestCaseData("http://127.0.0.1/catalog.hash?param=value", ".bin",
                             "http://127.0.0.1/catalog.bin?param=value"),
            // Query string with colon — the edge case that breaks Path.ChangeExtension directly
            new TestCaseData("http://127.0.0.1/catalog.hash?param1=value1&param2=value2:date=number", ".bin",
                             "http://127.0.0.1/catalog.bin?param1=value1&param2=value2:date=number"),
            // .hash appearing earlier in the path must NOT be touched (the original bug)
            new TestCaseData("http://h/my.hash.dir/catalog.hash",   ".bin",
                             "http://h/my.hash.dir/catalog.bin"),
            new TestCaseData("http://h/my.hash.dir/catalog.hash?q=1", ".json",
                             "http://h/my.hash.dir/catalog.json?q=1"),
        };

        [Test, TestCaseSource(nameof(k_Cases))]
        public void GetCatalogFilePath_ReturnsCorrectCatalogPath(string hashPath, string catalogExt, string expected)
        {
            Assert.AreEqual(expected, CatalogUtilities.GetCatalogFilePath(hashPath, catalogExt));
        }
    }
}
