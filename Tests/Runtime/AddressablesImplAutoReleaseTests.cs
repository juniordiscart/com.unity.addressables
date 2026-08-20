using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Exceptions;
using UnityEngine.ResourceManagement.Util;

namespace AddressableTests.AsyncTask
{
    public class AddressablesImplAutoReleaseTests
    {
        Action<AsyncOperationHandle, Exception> m_PrevHandler;

        // A prior test (or the real InitializeAsync path) may have installed
        // ResourceManager.ExceptionHandler = LogException as its static default.
        // This test intentionally completes an op with an exception, which would
        // otherwise route through that leaked handler and log an unhandled error.
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            m_PrevHandler = ResourceManager.ExceptionHandler;
            ResourceManager.ExceptionHandler = null;
        }

        [OneTimeTearDown]
        public void OneTimeTeardown()
        {
            ResourceManager.ExceptionHandler = m_PrevHandler;
        }

        [Test]
        public void AutoReleaseHandleOnTypelessCompletion_ThenAwaitFailure_ReleasesAllHandles()
        {
            // AutoReleaseHandleOnTypelessCompletion is the mechanism CheckForCatalogUpdates/UpdateCatalogs
            // use internally - a different code path than ReleaseHandleOnCompletion, so it needs its own
            // coverage: it must also mark HasReleaseOnCompletionRegistered, or ToAwaitable's failure-path
            // release would double-release the operation's reference.
            using var impl = new AddressablesImpl(new DefaultAllocationStrategy());
            var exception = new Exception("boom");
            var handle = impl.ResourceManager.CreateCompletedOperationWithException<int>(default, exception);
            impl.AutoReleaseHandleOnTypelessCompletion(handle);

            AsyncOperationHandleException<int> caught = null;

            async Task AwaitIt()
            {
                try
                {
                    await handle;
                }
                catch (AsyncOperationHandleException<int> e)
                {
                    caught = e;
                }
            }

            var task = AwaitIt();

            // The auto-release listener defers completion to the next Update - the await won't
            // resolve until then.
            Assert.IsFalse(task.IsCompleted);

            impl.ResourceManager.Update(0f);

            Assert.IsTrue(task.IsCompleted);
            Assert.IsNotNull(caught);

            // The auto-release listener already released `handle`'s reference; the
            // HasReleaseOnCompletionRegistered flag stopped ToAwaitable from releasing it again.
            // caught.Handle is a separate reference and keeps the op alive until it's released too.
            Assert.IsTrue(handle.IsValid());
            Assert.IsTrue(caught.Handle.IsValid());
            Assert.DoesNotThrow(() => caught.Handle.Release());
            Assert.IsFalse(handle.IsValid());
            Assert.IsFalse(caught.Handle.IsValid());
        }
    }
}
