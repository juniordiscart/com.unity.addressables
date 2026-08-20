using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;

namespace UnityEngine.ResourceManagement.Tests
{
    public class ToAwaitableMonoBehaviourTests
    {
        ResourceManager m_RM;
        GameObject m_GameObject;

        [SetUp]
        public void Setup()
        {
            m_RM = new ResourceManager();
            m_RM.CallbackHooksEnabled = false;
            m_GameObject = new GameObject(nameof(ToAwaitableMonoBehaviourTests));
        }

        [TearDown]
        public void TearDown()
        {
            if (m_GameObject != null)
                Object.Destroy(m_GameObject);
            Assert.Zero(m_RM.OperationCacheCount);
            m_RM.Dispose();
        }

        class MockOperation<T> : AsyncOperationBase<T>
        {
            protected override void Execute()
            {
            }
        }

        class TestBehaviour : MonoBehaviour
        {
        }

        // Covers only the MonoBehaviour -> destroyCancellationToken wiring and destruction while pending.
        // The full cancellation matrix (already-canceled, cancel-after-success, double-release, failure)
        // is covered deterministically against the pure CancellationToken overload in
        // BaseOperationBehaviorTests.ToAwaitable_* instead, since that doesn't need a real GameObject.
        [UnityTest]
        public IEnumerator ToAwaitable_MonoBehaviour_DestroyedWhilePending_CancelsAndReleases()
        {
            var op = new MockOperation<int>();
            var handle = m_RM.StartOperation(op, default(AsyncOperationHandle));
            var behaviour = m_GameObject.AddComponent<TestBehaviour>();

            bool canceled = false;

            async Task AwaitIt()
            {
                try
                {
                    await handle.ToAwaitable(behaviour);
                }
                catch (OperationCanceledException)
                {
                    canceled = true;
                }
            }

            var task = AwaitIt();
            Assert.IsFalse(task.IsCompleted);

            Object.Destroy(m_GameObject);
            m_GameObject = null; // already destroyed; TearDown should not destroy it again

            // destroyCancellationToken cancels as part of the object's teardown, which Unity defers to the
            // end of the frame - give it a frame to actually fire.
            yield return null;

            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(canceled);

            // Cancellation only released the caller's own reference and the awaiter's bookkeeping
            // reference - the op's Start()-time keep-alive self-reference is untouched, since the op is
            // still "running" until it completes. The handle stays valid until that happens.
            Assert.IsTrue(handle.IsValid());

            // The abandoned operation eventually completes - must not throw or double-release, since it
            // still holds its own "keep alive while running" self-reference independent of the cancellation.
            // Completing it drops that last reference, destroying the op.
            Assert.DoesNotThrow(() => op.Complete(0, true, null));
            Assert.IsFalse(handle.IsValid());
        }
    }
}
