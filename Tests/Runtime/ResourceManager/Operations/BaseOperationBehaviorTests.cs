using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Exceptions;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.TestTools;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Scripting;
using UnityEngine.TestTools.Constraints;

[assembly: Preserve]

namespace UnityEngine.ResourceManagement.Tests
{
    public class BaseOperationBehaviorTests
    {
        Action<AsyncOperationHandle, Exception> m_PrevHandler;
        ResourceManager m_RM;

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

        [SetUp]
        public void Setup()
        {
            m_RM = new ResourceManager();
            m_RM.CallbackHooksEnabled = false; // default for tests. disabled callback hooks. we will call update manually
        }

        [TearDown]
        public void TearDown()
        {
            Assert.Zero(m_RM.OperationCacheCount);
            m_RM.Dispose();
        }

        [Test]
        public void WhenReferenceCountReachesZero_DestroyCallbackInvoked()
        {
            var op = m_RM.CreateCompletedOperation<int>(1, string.Empty);
            int resultInDestroyCallback = 0;
            op.Destroyed += (x) => resultInDestroyCallback = x.Convert<int>().Result;
            op.Release();
            Assert.AreEqual(1, resultInDestroyCallback);
        }

        [Test]
        public void WhileCompletedCallbackIsDeferredOnCompletedOperation_ReferenceCountIsHeld()
        {
            var op = m_RM.CreateCompletedOperation<int>(1, string.Empty);
            int refCount = op.ReferenceCount;
            bool completedCalled = false;
            op.Completed += (x) => completedCalled = true; // callback is deferred to next update
            Assert.AreEqual(refCount + 1, op.ReferenceCount);
            m_RM.Update(0.0f);
            Assert.AreEqual(refCount, op.ReferenceCount);
            Assert.AreEqual(true, completedCalled);
            op.Release();
        }

        [Test]
        public void ReferenceCount_WhenHandleIsInvalid_ReturnsZero()
        {
            var handle = default(AsyncOperationHandle);
            Assert.IsFalse(handle.IsValid());
            Assert.AreEqual(0, handle.ReferenceCount);
        }

        [Test]
        public void ReferenceCount_WhenGenericHandleIsInvalid_ReturnsZero()
        {
            var handle = default(AsyncOperationHandle<int>);
            Assert.IsFalse(handle.IsValid());
            Assert.AreEqual(0, handle.ReferenceCount);
        }

        [Test]
        public void ReferenceCount_AfterHandleReleased_ReturnsZero()
        {
            var op = m_RM.CreateCompletedOperation<int>(1, string.Empty);
            Assert.IsTrue(op.IsValid());
            Assert.Greater(op.ReferenceCount, 0);
            op.Release();
            Assert.IsFalse(op.IsValid());
            Assert.AreEqual(0, op.ReferenceCount);
        }

        [Test]
        public void WhenInDestroyCallback_IncrementAndDecrementReferenceCount_Throws()
        {
            var op = m_RM.CreateCompletedOperation<int>(1, string.Empty);
            int resultInDestroyCallback = 0;
            Exception onInc = null;
            Exception onDec = null;
            op.Destroyed += (x) =>
            {
                try
                {
                    x.Acquire();
                }
                catch (Exception e)
                {
                    onInc = e;
                }

                try
                {
                    x.Release();
                }
                catch (Exception e)
                {
                    onDec = e;
                }

                resultInDestroyCallback = x.Convert<int>().Result;
            };
            op.Release();
            Assert.NotNull(onInc);
            Assert.NotNull(onDec);
        }

        class MockOperation<T> : AsyncOperationBase<T>
        {
            public Action ExecuteCallback = () => { };

            // Counts Destroy() invocations - a reliable way to detect a reference count hitting zero more
            // than once in a single completion pass. Unlike TearDown's Assert.Zero(m_RM.OperationCacheCount),
            // this works for these tests: MockOperation never uses a cache key, so it's never tracked there.
            public int DestroyCount;

            protected override void Execute()
            {
                ExecuteCallback();
            }

            protected override void Destroy()
            {
                DestroyCount++;
            }
        }

        [Test]
        public void WhenOperationHasDependency_ExecuteNotCalledUntilDependencyCompletes()
        {
            var op1 = new MockOperation<int>();
            var op2 = new MockOperation<int>();
            var handle1 = m_RM.StartOperation(op1, default(AsyncOperationHandle));
            op2.ExecuteCallback = () => { op2.Complete(0, true, string.Empty); };
            var handle2 = m_RM.StartOperation(op2, handle1);
            m_RM.Update(0.0f);
            Assert.AreEqual(false, handle2.IsDone);
            op1.Complete(0, true, null);
            Assert.AreEqual(true, handle2.IsDone);
            handle1.Release();
            handle2.Release();
        }

        [Test]
        public void WhenOperationIsSuccessfulButHasErrorMsg_FailsSilently_CompletesButExceptionHandlerIsCalled()
        {
            bool exceptionHandlerCalled = false;
            ResourceManager.ExceptionHandler += (h, ex) => exceptionHandlerCalled = true;

            var op = m_RM.CreateCompletedOperationInternal<int>(1, true, new Exception("An exception occured."));

            var status = AsyncOperationStatus.None;
            op.Completed += (x) => status = x.Status;

            // callbacks are deferred to next update
            m_RM.Update(0.0f);

            Assert.AreEqual(true, exceptionHandlerCalled);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, status);
            op.Release();
        }

        [UnityTest]
        public IEnumerator AsyncOperationHandle_TaskIsDelayedUntilAfterDelayedCompletedCallbacks()
        {
            var op = m_RM.CreateCompletedOperationInternal<int>(1, true, null);

            var status = AsyncOperationStatus.None;
            op.Completed += (x) => status = x.Status;
            var t = op.Task;
            Assert.IsFalse(t.IsCompleted);

            // callbacks are deferred to next update
            m_RM.Update(0.0f);

            // the Task may not yet have continues after at this point on the update,
            // give the Synchronization a little time with a yield
            yield return null;

            Assert.IsTrue(t.IsCompleted);
            op.Release();
        }

        [Test]
        public void AsyncOperationHandle_TaskIsCompletedWhenHandleIsCompleteWithoutDelayedCallbacks()
        {
            var op = m_RM.CreateCompletedOperationInternal<int>(1, true, null);
            var t = op.Task;
            Assert.IsTrue(t.IsCompleted);
            op.Release();
        }

        [Test]
        public void GetAwaiter_AlreadyDoneSuccessfulHandle_ResolvesSynchronously()
        {
            var op = m_RM.CreateCompletedOperationInternal<int>(42, true, null);

            async Task<int> AwaitIt() => await op;
            var task = AwaitIt();

            // The fast path in AsyncOperationHandle.ToAwaitable reads Status/Result directly, so an already-done
            // handle resolves synchronously - no m_RM.Update() needed, unlike subscribing to Completed directly.
            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual(42, task.Result);
            op.Release();
        }

        [Test]
        public void GetAwaiter_AlreadyDoneFailedHandle_ThrowsSynchronously()
        {
            var exception = new Exception("boom");
            var op = m_RM.CreateCompletedOperationWithException<int>(default, exception);

            async Task AwaitIt() => await op;
            var task = AwaitIt();

            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsFaulted);

            // Await throws an AsyncOperationHandleException<int> carrying the failed handle, with the
            // operation's original exception as InnerException - not the original exception directly.
            var handleEx = task.Exception.InnerException as AsyncOperationHandleException<int>;
            Assert.IsNotNull(handleEx);
            Assert.AreSame(exception, handleEx.InnerException);
            Assert.AreEqual(op, handleEx.Handle);

            // The failed op's Start()-time keep-alive reference isn't dropped synchronously in
            // Complete() - it's deferred to the next Update(), same as any other failed operation.
            // Drain that here so the final release below actually reaches a refcount of zero.
            m_RM.Update(0f);

            // ToAwaitable already released `op`'s own reference, but `op` still reads valid: IsValid()
            // reflects the operation's shared refcount, and handleEx.Handle's own dedicated reference is
            // still outstanding.
            Assert.IsTrue(op.IsValid());
            Assert.IsTrue(handleEx.Handle.IsValid());
            Assert.DoesNotThrow(() => handleEx.Handle.Release());
            Assert.IsFalse(op.IsValid());
            Assert.IsFalse(handleEx.Handle.IsValid());
        }

        [Test]
        public void GetAwaiter_AlreadyDoneHandleWithPendingCompletion_DefersUntilListenersRun()
        {
            var op = m_RM.CreateCompletedOperationInternal<int>(42, true, null);

            // Simulate a tracking listener (e.g. AddressablesImpl.TrackHandle) registered on this already-done
            // handle before it's awaited. Completed on a done handle still defers to the next update, so
            // ToAwaitable's fast path must not resolve synchronously while this listener is pending -
            // otherwise a caller could resume before the listener populates state it depends on.
            bool listenerRan = false;
            op.Completed += _ => listenerRan = true;

            async Task<int> AwaitIt() => await op;
            var task = AwaitIt();

            Assert.IsFalse(listenerRan);
            Assert.IsFalse(task.IsCompleted);

            m_RM.Update(0.0f);

            // Listeners run in registration order, so the tracking listener above has already run by the
            // time the await's continuation resolves.
            Assert.IsTrue(listenerRan);
            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual(42, task.Result);
            op.Release();
        }

        [Test]
        public void GetAwaiter_PendingSuccessfulOperation_ResolvesWhenCompleted()
        {
            var op = new MockOperation<int>();
            var handle = m_RM.StartOperation(op, default(AsyncOperationHandle));

            async Task<int> AwaitIt() => await handle;
            var task = AwaitIt();
            Assert.IsFalse(task.IsCompleted);

            // Success completions invoke the Completed event synchronously (not deferred), so the awaiter's
            // continuation - like Awaitable continuations generally - resumes synchronously, no update needed.
            op.Complete(7, true, null);
            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual(7, task.Result);
            handle.Release();
        }

        [Test]
        public void GetAwaiter_PendingOperationThatFails_ThrowsAfterDeferredUpdate()
        {
            var op = new MockOperation<int>();
            var handle = m_RM.StartOperation(op, default(AsyncOperationHandle));

            async Task AwaitIt() => await handle;
            var task = AwaitIt();
            Assert.IsFalse(task.IsCompleted);

            var exception = new Exception("boom");
            op.Complete(default, false, exception);
            // Failed completions are always deferred to the next ResourceManager update, even when the
            // Completed event already has a listener (see AsyncOperationBase.Complete's failure branch).
            Assert.IsFalse(task.IsCompleted);

            m_RM.Update(0.0f);
            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsFaulted);

            var handleEx = task.Exception.InnerException as AsyncOperationHandleException<int>;
            Assert.IsNotNull(handleEx);
            Assert.AreSame(exception, handleEx.InnerException);
            Assert.AreEqual(handle, handleEx.Handle);

            // ToAwaitable already released `handle`'s own reference, but `handle` still reads valid:
            // IsValid() reflects the shared refcount, and handleEx.Handle's own dedicated reference is
            // still outstanding.
            Assert.IsTrue(handle.IsValid());
            Assert.IsTrue(handleEx.Handle.IsValid());
            Assert.DoesNotThrow(() => handleEx.Handle.Release());
            Assert.IsFalse(handle.IsValid());
            Assert.IsFalse(handleEx.Handle.IsValid());
        }

        [Test]
        public void GetAwaiter_HandleWithAutoReleaseRegisteredFirst_StillCompletes()
        {
            var op = new MockOperation<int>();
            var handle = m_RM.StartOperation(op, default(AsyncOperationHandle));

            // Simulate autoReleaseHandle: true APIs (e.g. InitializeAsync) that register a release-on-complete
            // listener before the caller can await. Without its own acquired reference, ToAwaitable's
            // callback could run after this listener already released (and destroyed) the operation.
            handle.ReleaseHandleOnCompletion();

            async Task<int> AwaitIt() => await handle;
            var task = AwaitIt();
            Assert.IsFalse(task.IsCompleted);

            op.Complete(99, true, null);

            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual(99, task.Result);
            // No manual Release(): the auto-release listener and ToAwaitable's own acquire/release pair
            // together drain the reference count. TearDown's OperationCacheCount check doesn't cover this
            // (MockOperation never uses a cache key) - IsValid() below is the real check.
            Assert.IsFalse(handle.IsValid());
        }

        [Test]
        public void GetAwaiter_HandleWithAutoReleaseRegisteredFirst_FailurePathStillThrows()
        {
            var op = new MockOperation<int>();
            var handle = m_RM.StartOperation(op, default(AsyncOperationHandle));

            // See GetAwaiter_HandleWithAutoReleaseRegisteredFirst_StillCompletes: same autoReleaseHandle:
            // true simulation, but exercising the failure/deferred completion path instead.
            handle.ReleaseHandleOnCompletion();

            async Task AwaitIt() => await handle;
            var task = AwaitIt();
            Assert.IsFalse(task.IsCompleted);

            var exception = new Exception("boom");
            op.Complete(default, false, exception);
            // Failed completions are always deferred to the next ResourceManager update.
            Assert.IsFalse(task.IsCompleted);

            m_RM.Update(0.0f);

            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsFaulted);
            var handleEx = task.Exception.InnerException as AsyncOperationHandleException<int>;
            Assert.IsNotNull(handleEx);
            Assert.AreSame(exception, handleEx.InnerException);

            // Unlike the success path, on failure the acquired reference transfers to handleEx.Handle
            // instead of being auto-released - the listener only released `handle`'s own reference. The
            // operation survives until handleEx.Handle is released too.
            Assert.IsTrue(handleEx.Handle.IsValid());
            handleEx.Handle.Release();
            Assert.IsFalse(handleEx.Handle.IsValid());
        }

        [Test]
        public void GetAwaiter_HandleWithAutoReleaseRegisteredFirst_FailurePath_DestroysExactlyOnce()
        {
            var op = new MockOperation<int>();
            var handle = m_RM.StartOperation(op, default(AsyncOperationHandle));

            // Same autoReleaseHandle: true simulation, but this exercises the pattern our doc samples use in
            // catch blocks - releasing e.Handle directly. Regression: on the unfixed code, the auto-release
            // listener and this handler's finally-release both redeemed the same acquired reference, driving
            // the refcount to zero twice in one completion pass and double-destroying. Exactly one release
            // should win.
            handle.ReleaseHandleOnCompletion();

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
                    e.Handle.Release();
                }
            }

            var task = AwaitIt();
            var exception = new Exception("boom");

            Assert.DoesNotThrow(() =>
            {
                op.Complete(default, false, exception);
                m_RM.Update(0.0f);
            });

            Assert.IsTrue(task.IsCompleted);
            Assert.IsNotNull(caught);
            Assert.AreEqual(1, op.DestroyCount);
        }


        [Test]
        public void ToAwaitable_AutoReleaseHandle_CancelWhilePendingThenSucceeds_DestroysExactlyOnce()
        {
            // Regression: ToAwaitable(CancellationToken)'s cancellation callback used to release the caller's
            // own reference unconditionally, even with a release-on-completion listener already registered
            // (simulated below). If the token fired before completion, that listener would later release the
            // same reference again, double-destroying the operation. HasReleaseOnCompletionRegistered now
            // guards every self-release in ToAwaitable, not just the failure path.
            var op = new MockOperation<int>();
            var handle = m_RM.StartOperation(op, default(AsyncOperationHandle));
            handle.ReleaseHandleOnCompletion();

            using var cts = new CancellationTokenSource();
            async Task<int> AwaitIt() => await handle.ToAwaitable(cts.Token);
            var task = AwaitIt();

            cts.Cancel();
            Assert.IsTrue(task.IsCanceled);

            Assert.DoesNotThrow(() => op.Complete(42, true, null));
            Assert.AreEqual(1, op.DestroyCount);
        }

        [Test]
        public void ToAwaitable_AutoReleaseHandle_AlreadyCanceledToken_OnDeferredFailure_DestroysExactlyOnce()
        {
            // Same root cause as above, exercised via the already-canceled-at-call-time fast-exit branch
            // instead: the operation has already failed (its completion deferred to the next
            // ResourceManager.Update()) when ToAwaitable is called with an already-canceled token.
            var op = new MockOperation<int>();
            var handle = m_RM.StartOperation(op, default(AsyncOperationHandle));
            handle.ReleaseHandleOnCompletion();

            var exception = new Exception("boom");
            op.Complete(default, false, exception);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // No try/catch: an unhandled OperationCanceledException is what puts the async Task in the
            // Canceled state. Catching it would leave the task RanToCompletion, making IsCanceled false
            // below regardless of whether ToAwaitable actually canceled correctly.
            async Task AwaitIt() => await handle.ToAwaitable(cts.Token);

            var task = AwaitIt();
            Assert.IsTrue(task.IsCanceled);

            Assert.DoesNotThrow(() => m_RM.Update(0.0f));
            Assert.AreEqual(1, op.DestroyCount);
        }

        [Test]
        public void ToAwaitable_Typeless_AutoReleaseHandle_CancelWhilePendingThenSucceeds_DestroysExactlyOnce()
        {
            var op = new MockOperation<int>();
            var typedHandle = m_RM.StartOperation(op, default(AsyncOperationHandle));
            var handle = (AsyncOperationHandle)typedHandle; //< Typeless handle
            handle.ReleaseHandleOnCompletion();

            using var cts = new CancellationTokenSource();
            async Task AwaitIt() => await handle.ToAwaitable(cts.Token);
            var task = AwaitIt();

            cts.Cancel();
            Assert.IsTrue(task.IsCanceled);

            Assert.DoesNotThrow(() => op.Complete(42, true, null));
            Assert.AreEqual(1, op.DestroyCount);
        }

        [Test]
        public void ToAwaitable_Typeless_AutoReleaseHandle_AlreadyCanceledToken_OnDeferredFailure_DestroysExactlyOnce()
        {
            var op = new MockOperation<int>();
            var typedHandle = m_RM.StartOperation(op, default(AsyncOperationHandle));
            var handle = (AsyncOperationHandle)typedHandle; //< Typeless handle
            handle.ReleaseHandleOnCompletion();

            var exception = new Exception("boom");
            op.Complete(default, false, exception);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // No try/catch: see the generic overload's test above for why catching OperationCanceledException
            // here would swallow the Canceled task state and make IsCanceled false regardless of outcome.
            async Task AwaitIt() => await handle.ToAwaitable(cts.Token);

            var task = AwaitIt();
            Assert.IsTrue(task.IsCanceled);

            Assert.DoesNotThrow(() => m_RM.Update(0.0f));
            Assert.AreEqual(1, op.DestroyCount);
        }

        [Test]
        public void ToAwaitable_TokenAlreadyCanceled_ReturnsCanceledSynchronously_AndReleasesHandle()
        {
            var op = m_RM.CreateCompletedOperationInternal<int>(42, true, null);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            async Task<int> AwaitIt() => await op.ToAwaitable(cts.Token);
            var task = AwaitIt();

            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsCanceled);
            // Already-canceled tokens release the handle directly, bypassing the fast-path/Completed
            // machinery - no manual Release() needed; IsValid() below is the real check.
            Assert.IsFalse(op.IsValid());
        }

        [Test]
        public void ToAwaitable_CancelWhilePending_ThrowsOperationCanceled_AndReleases()
        {
            var op = new MockOperation<int>();
            var handle = m_RM.StartOperation(op, default(AsyncOperationHandle));
            using var cts = new CancellationTokenSource();

            async Task<int> AwaitIt() => await handle.ToAwaitable(cts.Token);
            var task = AwaitIt();
            Assert.IsFalse(task.IsCompleted);

            cts.Cancel();
            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsCanceled);

            // The abandoned load eventually completes - cancellation must not have destroyed the operation
            // mid-flight (it still holds its own "keep alive while running" self-reference), so this must not
            // throw or double-release.
            op.Complete(0, true, null);
            Assert.IsFalse(handle.IsValid());
        }

        [Test]
        public void ToAwaitable_CancelAfterSuccessfulCompletion_ResultObserved_ThenHandleReleased()
        {
            var op = new MockOperation<int>();
            var handle = m_RM.StartOperation(op, default(AsyncOperationHandle));
            using var cts = new CancellationTokenSource();

            async Task<int> AwaitIt() => await handle.ToAwaitable(cts.Token);
            var task = AwaitIt();

            op.Complete(5, true, null);
            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual(5, task.Result);
            Assert.IsTrue(handle.IsValid());

            // Cancelling after a successful completion still releases the handle - this is what lets a
            // lifetime-bound token (e.g. destroyCancellationToken) replace a manual cleanup call entirely,
            // even when the object is destroyed well after the load already finished.
            cts.Cancel();
            Assert.IsFalse(handle.IsValid());
        }

        [Test]
        public void ToAwaitable_CancelAfterCompletion_WhenCallerAlsoReleased_NoDoubleRelease()
        {
            var op = new MockOperation<int>();
            var handle = m_RM.StartOperation(op, default(AsyncOperationHandle));
            using var cts = new CancellationTokenSource();

            async Task<int> AwaitIt() => await handle.ToAwaitable(cts.Token);
            var task = AwaitIt();

            op.Complete(5, true, null);
            Assert.AreEqual(5, task.Result);

            // Caller releases in their own success path...
            handle.Release();
            Assert.IsFalse(handle.IsValid());

            // ...then the token fires later. Must be a silent no-op: whichever release ran first already
            // bumped the operation's Version, so the second one's IsValid() guard correctly sees it as stale.
            Assert.DoesNotThrow(() => cts.Cancel());
        }

        [Test]
        public void ToAwaitable_TokenNone_BehavesLikeParameterless()
        {
            var op = new MockOperation<int>();
            var handle = m_RM.StartOperation(op, default(AsyncOperationHandle));

            async Task<int> AwaitIt() => await handle.ToAwaitable(CancellationToken.None);
            var task = AwaitIt();
            Assert.IsFalse(task.IsCompleted);

            op.Complete(9, true, null);
            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual(9, task.Result);
            handle.Release();
        }

        [Test]
        public void ToAwaitable_AlreadyDoneNotCanceled_ThenCancel_ReleasesHandle()
        {
            var op = m_RM.CreateCompletedOperationInternal<int>(42, true, null);
            using var cts = new CancellationTokenSource();

            async Task<int> AwaitIt() => await op.ToAwaitable(cts.Token);
            var task = AwaitIt();

            // Fast path: resolves synchronously, same as the parameterless overload.
            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual(42, task.Result);
            Assert.IsTrue(op.IsValid());

            // The fast path still registers for a *later* cancellation, so relying purely on the token
            // (rather than a separate cleanup call) still releases the handle.
            cts.Cancel();
            Assert.IsFalse(op.IsValid());
        }

        [Test]
        public void ToAwaitable_CancelWhilePending_OnFailedOperation()
        {
            var op = new MockOperation<int>();
            var handle = m_RM.StartOperation(op, default(AsyncOperationHandle));
            using var cts = new CancellationTokenSource();

            async Task<int> AwaitIt() => await handle.ToAwaitable(cts.Token);
            var task = AwaitIt();

            cts.Cancel();
            Assert.IsTrue(task.IsCanceled);

            // Failure completions are deferred to the next ResourceManager update. The deferred path must
            // find the source already canceled (a guarded no-op) and must not throw.
            var exception = new Exception("boom");
            Assert.DoesNotThrow(() =>
            {
                op.Complete(default, false, exception);
                m_RM.Update(0.0f);
            });
            Assert.IsFalse(handle.IsValid());
        }

        [Test]
        public void ToAwaitable_Typeless_TokenAlreadyCanceled_ReleasesHandle()
        {
            var op = m_RM.CreateCompletedOperationInternal<int>(42, true, null);
            var handle = (AsyncOperationHandle)op; //< Typeless handle
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            async Task AwaitIt() => await handle.ToAwaitable(cts.Token);
            var task = AwaitIt();

            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsCanceled);
            Assert.IsFalse(handle.IsValid());
        }

        [Test]
        public void ToAwaitable_Typeless_CancelWhilePending_ThrowsOperationCanceled_AndReleases()
        {
            var op = new MockOperation<int>();
            var typedHandle = m_RM.StartOperation(op, default(AsyncOperationHandle));
            var handle = (AsyncOperationHandle)typedHandle; //< Typeless handle
            using var cts = new CancellationTokenSource();

            async Task AwaitIt() => await handle.ToAwaitable(cts.Token);
            var task = AwaitIt();
            Assert.IsFalse(task.IsCompleted);

            cts.Cancel();
            Assert.IsTrue(task.IsCanceled);

            op.Complete(0, true, null);
            Assert.IsFalse(handle.IsValid());
        }

        [Test]
        public void ToAwaitable_Typeless_CancelAfterSuccessfulCompletion_ThenHandleReleased()
        {
            var op = new MockOperation<int>();
            var typedHandle = m_RM.StartOperation(op, default(AsyncOperationHandle));
            var handle = (AsyncOperationHandle)typedHandle; //< Typeless handle
            using var cts = new CancellationTokenSource();

            async Task AwaitIt() => await handle.ToAwaitable(cts.Token);
            var task = AwaitIt();

            op.Complete(5, true, null);
            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(handle.IsValid());

            cts.Cancel();
            Assert.IsFalse(handle.IsValid());
        }

        [Test]
        public void ToAwaitable_Typeless_CancelAfterCompletion_WhenCallerAlsoReleased_NoDoubleRelease()
        {
            var op = new MockOperation<int>();
            var typedHandle = m_RM.StartOperation(op, default(AsyncOperationHandle));
            var handle = (AsyncOperationHandle)typedHandle; //< Typeless handle
            using var cts = new CancellationTokenSource();

            async Task AwaitIt() => await handle.ToAwaitable(cts.Token);
            var task = AwaitIt();

            op.Complete(5, true, null);
            handle.Release();
            Assert.IsFalse(handle.IsValid());

            Assert.DoesNotThrow(() => cts.Cancel());
        }

        [Test]
        public void GetAwaiter_Typeless_HandleWithAutoReleaseRegisteredFirst_FailurePath_DestroysExactlyOnce()
        {
            // Typeless mirror of GetAwaiter_HandleWithAutoReleaseRegisteredFirst_FailurePath_DestroysExactlyOnce.
            // This used to be exactly the misuse the old AwaitableFailureBehavior.ReleaseHandle opt-in warned
            // against; it's safe by default now that HasReleaseOnCompletionRegistered guards the release.
            var op = new MockOperation<int>();
            var typedHandle = m_RM.StartOperation(op, default(AsyncOperationHandle));
            var handle = (AsyncOperationHandle)typedHandle; //< Typeless handle
            handle.ReleaseHandleOnCompletion();

            AsyncOperationHandleException caught = null;

            async Task AwaitIt()
            {
                try
                {
                    await handle;
                }
                catch (AsyncOperationHandleException e)
                {
                    caught = e;
                    // Release the exception's own dedicated reference here - it's the last
                    // outstanding reference, so this is what actually drops the op to zero.
                    e.Handle.Release();
                }
            }

            var task = AwaitIt();
            var exception = new Exception("boom");

            Assert.DoesNotThrow(() =>
            {
                op.Complete(default, false, exception);
                m_RM.Update(0.0f);
            });

            Assert.IsTrue(task.IsCompleted);
            Assert.IsNotNull(caught);
            Assert.AreEqual(1, op.DestroyCount);
        }

        [Test]
        public void GetAwaiter_SameHandle_MultipleAwaitersAllResolve()
        {
            // Scope note: this only covers the parameterless overload's success path, the one case where
            // neither awaiter releases `handle` itself (see ToAwaitable()'s remarks). It does not show that
            // sharing one un-Acquire()'d handle across ToAwaitable(CancellationToken)/(MonoBehaviour) awaiters,
            // or across a failure, is safe - whichever awaiter releases first (a cancellation or the
            // auto-release-on-failure path) does so for every awaiter sharing it.
            var op = new MockOperation<int>();
            var handle = m_RM.StartOperation(op, default(AsyncOperationHandle));

            async Task<int> AwaitIt() => await handle;
            var taskA = AwaitIt();
            var taskB = AwaitIt();
            Assert.IsFalse(taskA.IsCompleted);
            Assert.IsFalse(taskB.IsCompleted);

            op.Complete(99, true, null);
            Assert.IsTrue(taskA.IsCompleted);
            Assert.IsTrue(taskB.IsCompleted);
            Assert.AreEqual(99, taskA.Result);
            Assert.AreEqual(99, taskB.Result);
            handle.Release();
        }

        [Test]
        public void GetAwaiter_ThenRelease_HandleBecomesInvalid()
        {
            var op = m_RM.CreateCompletedOperationInternal<int>(1, true, null);

            async Task AwaitIt() => await op;
            var task = AwaitIt();
            Assert.IsTrue(task.IsCompleted);

            op.Release();
            Assert.IsFalse(op.IsValid());
        }

        // TODO:
        // public void WhenOperationHasDependency_AndDependencyFails_DependentOpStillExecutes()

        // Bad derived class behavior
        // public void CustomOperation_WhenCompleteCalledBeforeStartOperation_ThrowsOperationDoesNotComplete
        // public void CustomOperation_WhenCompleteCalledMultipleTimes_Throws
        // public void CustomOperation_WhenProgressCallbackThrowsException_ErrorLoggedAndHandleReturnsZero
        // public void CustomOperation_WhenDestroyThrowsException_ErrorLogged
        // public void CustomOperation_WhenExecuteThrows_ErrorLoggedAndOperationSetAsFailed

        // TEST: Per operation update behavior

        // public void AsyncOperationHandle_WhenReleaseOnInvalidHandle_Throws
        // public void AsyncOperationHandle_WhenConvertToIncompatibleHandleType_Throws
        //

        [Test]
        public void AsyncOperationHandle_EventSubscriptions_UnsubscribingToNonSubbedEventsShouldHaveNoEffect()
        {
            var op = new MockOperation<int>();
            var handle = m_RM.StartOperation(op, default(AsyncOperationHandle));

            Assert.False(op.CompletedEventHasListeners);
            handle.Completed -= oph => { };
            Assert.False(op.CompletedEventHasListeners);

            Assert.False(op.DestroyedEventHasListeners);
            handle.Destroyed -= oph => { };
            Assert.False(op.DestroyedEventHasListeners);

            handle.Release();
        }

        internal class ManualDownloadPercentCompleteOperation : AsyncOperationBase<IAssetBundleResource>
        {
            public long m_bytesDownloaded = 0;
            public long m_totalBytes = 1024;
            public bool m_IsDone = false;

            protected override void Execute()
            {
            }

            public void CompleteNow()
            {
                m_bytesDownloaded = m_totalBytes;
                Complete(null, true, null);
            }

            internal override DownloadStatus GetDownloadStatus(HashSet<object> visited)
            {
                return new DownloadStatus() {DownloadedBytes = m_bytesDownloaded, TotalBytes = m_totalBytes, IsDone = m_IsDone};
            }
        }

        static void AssertExpectedDownloadStatus(DownloadStatus dls, long dl, long tot, float per)
        {
            Assert.AreEqual(dl, dls.DownloadedBytes);
            Assert.AreEqual(tot, dls.TotalBytes);
            Assert.AreEqual(per, dls.Percent);
        }

        [Test]
        public void DownloadStatusWithNoBytes_WithIsDoneFalse_Returns_PercentCompleteZero()
        {
            var dls = new DownloadStatus() {DownloadedBytes = 0, TotalBytes = 0, IsDone = false};
            Assert.AreEqual(0f, dls.Percent);
        }

        [Test]
        public void DownloadStatusWithNoBytes_WithIsDoneTrue_Returns_PercentCompleteOne()
        {
            var dls = new DownloadStatus() {DownloadedBytes = 0, TotalBytes = 0, IsDone = true};
            Assert.AreEqual(1f, dls.Percent);
        }

        [Test]
        public void GroupOperation_WithOpsThatImplementGetDownloadStatus_ComputesExpectedDownloadPercentComplete()
        {
            var ops = new List<AsyncOperationHandle>();
            var mdpco = new List<ManualDownloadPercentCompleteOperation>();
            for (int i = 0; i < 4; i++)
            {
                var o = m_RM.CreateOperation<ManualDownloadPercentCompleteOperation>(typeof(ManualDownloadPercentCompleteOperation), 1, null, null);
                o.Start(m_RM, default, null);
                mdpco.Add(o);
                ops.Add(new AsyncOperationHandle(o));
            }

            var gOp = m_RM.CreateGenericGroupOperation(ops, true);
            AssertExpectedDownloadStatus(gOp.GetDownloadStatus(), 0, 4096, 0);
            mdpco[0].m_bytesDownloaded = 512;
            AssertExpectedDownloadStatus(gOp.GetDownloadStatus(), 512, 4096, .125f);
            foreach (var o in mdpco)
                o.CompleteNow();
            AssertExpectedDownloadStatus(gOp.GetDownloadStatus(), 4096, 4096, 1f);
            gOp.Release();
        }

        [Test]
        public void ChainOperation_WithOpThatImplementGetDownloadStatus_ComputesExpectedDownloadPercentComplete()
        {
            var depOp = m_RM.CreateOperation<ManualDownloadPercentCompleteOperation>(typeof(ManualDownloadPercentCompleteOperation), 1, null, null);
            depOp.Start(m_RM, default, null);
            var chainOp = m_RM.CreateChainOperation<object>(new AsyncOperationHandle(depOp), s => m_RM.CreateCompletedOperationInternal<object>(null, true, null));

            AssertExpectedDownloadStatus(chainOp.GetDownloadStatus(), 0, 1024, 0f);
            depOp.m_bytesDownloaded = 512;
            AssertExpectedDownloadStatus(chainOp.GetDownloadStatus(), 512, 1024, .5f);
            depOp.CompleteNow();
            m_RM.Update(.1f);
            Assert.IsTrue(chainOp.IsDone);
            AssertExpectedDownloadStatus(chainOp.GetDownloadStatus(), 1024, 1024, 1f);
            chainOp.Release();
        }

        [Test]
        public void PercentComplete_ReturnsOne_WhenDependentOpIsCompleteAndNoDownloadStatus()
        {
            var baseOperation = m_RM.CreateChainOperation<AsyncOperationHandle>(
                new AsyncOperationHandle(new ManualPercentCompleteOperation(1f)),
                (obj) => { return new AsyncOperationHandle<AsyncOperationHandle>(); });

            Assert.AreEqual(1f, baseOperation.PercentComplete);
        }

        [Test]
        public void GroupOperation_WithDuplicateOpThatImplementGetDownloadStatus_DoesNotOverCountValues()
        {
            var ops = new List<AsyncOperationHandle>();
            var o = m_RM.CreateOperation<ManualDownloadPercentCompleteOperation>(typeof(ManualDownloadPercentCompleteOperation), 1, null, null);
            o.Start(m_RM, default, null);
            for (int i = 0; i < 4; i++)
                ops.Add(new AsyncOperationHandle(o));

            var gOp = m_RM.CreateGenericGroupOperation(ops, true);
            AssertExpectedDownloadStatus(gOp.GetDownloadStatus(), 0, 1024, 0);
            o.m_bytesDownloaded = 512;
            AssertExpectedDownloadStatus(gOp.GetDownloadStatus(), 512, 1024, .5f);
            o.CompleteNow();
            AssertExpectedDownloadStatus(gOp.GetDownloadStatus(), 1024, 1024, 1f);
            gOp.Release();
        }

        class TestOp : AsyncOperationBase<int>
        {
            protected override void Execute()
            {
                InvokeCompletionEvent();
            }
        }

        [Test]
        public void CompletionEvents_AreInvoked_InOrderAdded()
        {
            var op = new TestOp();
            int count = 0;
            op.Completed += o =>
            {
                Assert.AreEqual(0, count);
                count++;
            };
            op.CompletedTypeless += o =>
            {
                Assert.AreEqual(1, count);
                count++;
            };
            op.Completed += o =>
            {
                Assert.AreEqual(2, count);
                count++;
            };
            op.CompletedTypeless += o =>
            {
                Assert.AreEqual(3, count);
                count++;
            };
            op.Start(null, default, null);
            op.Complete(1, true, null);
        }


        [Test]
        public void ReleaseHandleOnCompletion_Typed()
        {
            var op = new TestOp();

            var handle = op.Handle;
            handle.ReleaseHandleOnCompletion();

            Assert.IsTrue(handle.IsValid());

            op.Start(null, default, null);

            Assert.IsTrue(handle.IsValid());

            op.Complete(1, true, null);

            Assert.IsFalse(handle.IsValid());
        }

        [Test]
        public void ReleaseHandleOnCompletion_Typeless()
        {
            var op = new TestOp();

            var handle = (AsyncOperationHandle)op.Handle; //< Typeless handle
            handle.ReleaseHandleOnCompletion();

            Assert.IsTrue(handle.IsValid());

            op.Start(null, default, null);

            Assert.IsTrue(handle.IsValid());

            op.Complete(1, true, null);

            Assert.IsFalse(handle.IsValid());
        }

        [Test]
        public void OperationIsEqual_OnAcquire()
        {
            var op = new TestOp();
            var handle = op.Handle; //< Typed handle
            var handle2 = m_RM.Acquire(handle);
            Assert.AreEqual(handle, handle2);
        }

#if false /// TODO: Future major revision to support non-void return for `AsyncOperationHandle ResourceManager::Acquire(AsyncOperationHandle)`
        [Test]
        public void OperationIsEqual_OnAcquire_Typeless()
        {
            var op = new TestOp();
            var handle = (AsyncOperationHandle)op.Handle; //< Typeless handle
            var handle2 = m_RM.Acquire(handle);
            Assert.AreEqual(handle, handle2);
        }
#endif

        [Test]
        public void WhenOperationIsReused_HasExecutedIsReset()
        {
            var op = new TestOp();
            op.Start(null, default, null);
            op.Complete(1, true, null);

            Assert.IsTrue(op.HasExecuted);
            var dep = new AsyncOperationHandle(new TestOp());
            op.Start(null, dep, null);
            Assert.IsFalse(op.HasExecuted);
        }
    }
}
