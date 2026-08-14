#if ENABLE_CONTENT_DIRECTORIES
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Loading;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace UnityEngine.AddressableAssets.ResourceProviders.Tests
{
    [TestFixture]
    public class GroupRootAssetProviderTests
    {
        private GroupRootAssetProvider m_Provider;
        private Action<AsyncOperationHandle, Exception> exceptionHandler;


        [SetUp]
        public void Setup()
        {
            exceptionHandler = ResourceManager.ExceptionHandler;
            ResourceManager.ExceptionHandler = null;
            m_Provider = new GroupRootAssetProvider();
        }

        [TearDown]
        public void TearDown()
        {
            ResourceManager.ExceptionHandler = exceptionHandler;
        }

        // Mock IContentDirectoryResource for testing
        private class MockContentDirectoryResource : IContentDirectoryResource
        {
            private ContentDirectoryHandle m_Handle;

            public MockContentDirectoryResource(ContentDirectoryHandle handle)
            {
                m_Handle = handle;
            }

            public ContentDirectoryHandle GetContentDirectoryHandle()
            {
                return m_Handle;
            }
        }

        // Mock ProviderOperation for creating ProvideHandles
        private class MockProviderOperation : ProviderOperation<object>
        {
            private IResourceLocation m_Location;
            private List<object> m_Dependencies = new List<object>();
            private int m_ProvideHandleVersion = 1;
            private ResourceManager m_ResourceManager;

            public MockProviderOperation(IResourceLocation location, List<object> dependencies = null)
            {
                m_Location = location;
                if (dependencies != null)
                {
                    m_Dependencies = dependencies;
                }
                m_ResourceManager = new ResourceManager(); // Minimal ResourceManager just for ProvideHandle creation
                m_RM = m_ResourceManager;

                // Initialize m_DepOp to simulate dependency operations
                // Create completed operations for each dependency
                if (m_Dependencies != null && m_Dependencies.Count > 0)
                {
                    var depHandles = new List<AsyncOperationHandle>();
                    foreach (var dep in m_Dependencies)
                    {
                        var depOp = m_ResourceManager.CreateCompletedOperation<object>(dep, "");
                        depHandles.Add(depOp);
                    }

                    var depListOp = m_ResourceManager.CreateCompletedOperation<IList<AsyncOperationHandle>>(depHandles, "");
                    m_DepOp = depListOp;
                }
            }

            public new int ProvideHandleVersion => m_ProvideHandleVersion;
            public new IResourceLocation Location => m_Location;
            public new int DependencyCount => m_Dependencies?.Count ?? 0;

            public new void GetDependencies(IList<object> dstList)
            {
                dstList.Clear();
                if (m_Dependencies != null)
                {
                    foreach (var dep in m_Dependencies)
                    {
                        dstList.Add(dep);
                    }
                }
            }

            public new TDepObject GetDependency<TDepObject>(int index)
            {
                if (m_Dependencies == null || index >= m_Dependencies.Count)
                    throw new Exception("Cannot get dependency because no dependencies were available");
                return (TDepObject)m_Dependencies[index];
            }

            public new void SetProgressCallback(Func<float> callback) { }
            public new void SetDownloadProgressCallback(Func<DownloadStatus> callback) { }
            public new void SetWaitForCompletionCallback(Func<bool> callback) { }

            public new void ProviderCompleted<T>(T result, bool status, Exception e)
            {
                Complete((object)(T)(object)result, status, e);
            }

            public ResourceManager ResourceManager => m_ResourceManager;

            protected override void Execute()
            {
                // No-op for testing
            }
        }

        // Helper to create a ProvideHandle for testing
        private ProvideHandle CreateProvideHandle(
            string key,
            Type resourceType,
            IContentDirectoryResource contentDirectoryResource = null,
            IResourceLocation[] dependencies = null)
        {
            var location = new ResourceLocationBase(
                key,
                key,
                typeof(GroupRootAssetProvider).FullName,
                resourceType,
                dependencies ?? new IResourceLocation[0]);

            var dependenciesList = new List<object>();
            if (contentDirectoryResource != null)
            {
                dependenciesList.Add(contentDirectoryResource);
            }

            var mockOp = new MockProviderOperation(location, dependenciesList);
            // Internal constructor is accessible to tests in the same assembly
            return new ProvideHandle(mockOp.ResourceManager, mockOp);
        }

        [Test]
        public void Provide_WhenNoContentDirectoryResourceInDependencies_FailsWithException()
        {
            var handle = CreateProvideHandle("testKey", typeof(GameObject), null);

            var op = (AsyncOperationBase<object>)handle.InternalOp;


            // Call Provide directly
            m_Provider.Provide(handle);

            // The provider should have completed with an error
            Assert.IsTrue(op.IsDone);
            Assert.AreEqual(AsyncOperationStatus.Failed, op.Status);
            Assert.IsNotNull(op.OperationException);
            Assert.IsTrue(op.OperationException.Message.Contains("No valid ContentDirectoryResource found in dependencies"));
        }

        [Test]
        public void Provide_WhenContentDirectoryHandleIsInvalid_FailsWithException()
        {
            var invalidHandle = default(ContentDirectoryHandle);
            var mockCdr = new MockContentDirectoryResource(invalidHandle);
            var handle = CreateProvideHandle("testKey", typeof(GameObject), mockCdr);

            var op = (AsyncOperationBase<object>)handle.InternalOp;

            m_Provider.Provide(handle);

            Assert.IsTrue(op.IsDone);
            Assert.AreEqual(AsyncOperationStatus.Failed, op.Status);
            Assert.IsNotNull(op.OperationException);
            Assert.AreEqual("Invalid ContentDirectoryHandle found in ContentDirectoryResource.", op.OperationException.Message);
        }

        [Test]
        public void Release_ClearsRootAsset()
        {
            var location = new ResourceLocationBase(
                "testKey",
                "testKey",
                typeof(GroupRootAssetProvider).FullName,
                typeof(GameObject));

            var testObject = new GameObject("TestObject");

            // Release should not throw
            Assert.DoesNotThrow(() => m_Provider.Release(location, testObject));

            Object.DestroyImmediate(testObject);
        }

        [Test]
        public void GetContentDirectoryResourceFromDependencies_WhenDependenciesIsEmpty_ReturnsNull()
        {
            // Create a handle with empty dependencies
            var handle = CreateProvideHandle("testKey", typeof(GameObject), null);

            var op = (AsyncOperationBase<object>)handle.InternalOp;

            m_Provider.Provide(handle);

            // Should fail because no ContentDirectoryResource in dependencies
            Assert.IsTrue(op.IsDone);
            Assert.AreEqual(AsyncOperationStatus.Failed, op.Status);
            Assert.IsNotNull(op.OperationException);
            Assert.IsTrue(op.OperationException.Message.Contains("No valid ContentDirectoryResource found in dependencies"));
        }

        [Test]
        public void GetContentDirectoryResourceFromDependencies_WhenDependenciesContainsNonIContentDirectoryResource_ReturnsNull()
        {
            // Create a handle with a non-IContentDirectoryResource dependency
            var dependenciesList = new List<object> { "not a content directory resource" };
            var location = new ResourceLocationBase(
                "testKey",
                "testKey",
                typeof(GroupRootAssetProvider).FullName,
                typeof(GameObject),
                new IResourceLocation[0]);

            var mockOp = new MockProviderOperation(location, dependenciesList);
            // Internal constructor is accessible to tests in the same assembly
            var handle = new ProvideHandle(mockOp.ResourceManager, mockOp);

            // need to store this off before complete
            var op = (AsyncOperationBase<object>)handle.InternalOp;

            m_Provider.Provide(handle);

            // Should fail because dependency is not IContentDirectoryResource
            Assert.IsTrue(op.IsDone);
            Assert.AreEqual(AsyncOperationStatus.Failed, op.Status);
            Assert.IsNotNull(op.OperationException);
            Assert.IsTrue(op.OperationException.Message.Contains("No valid ContentDirectoryResource found in dependencies"));
        }
    }
}
#endif
