using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Exceptions;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.SceneManagement;
using System.IO;

#endif

namespace AddressableTests.AsyncTask
{
    public abstract class AsyncTaskTests : AddressablesTestFixture
    {
        const int loadCount = 100;
#if UNITY_EDITOR

        internal override void Setup(AddressableAssetSettings settings, string tempAssetFolder)
        {
            for(int i = 0 ; i < loadCount; i++)
            {
                var guid = CreateAsset( $"{GetGeneratedAssetsPath()}/test{i}.prefab");
                var entry = new AddressableAssetEntry(guid, i.ToString(), settings.DefaultGroup, false);
                entry.SetLabel("all", true, true, false);
                settings.DefaultGroup.AddAssetEntry(entry);
            }
        }
#endif
        [UnityTest]
        public IEnumerator AsyncTask_LoadAll_Separately()
        {
            yield return LoadAllImp(LoadAll);
        }

        [UnityTest]
        public IEnumerator AsyncTask_LoadAll_Batched()
        {
            yield return LoadAllImp(LoadAllBatch);
        }

        [UnityTest]
        public IEnumerator AsyncTask_LoadAll_Label()
        {
            yield return LoadAllImp(LoadAllLabel);
        }

        [UnityTest]
        public IEnumerator AsyncTask_LoadAll_Group_Operation()
        {
            yield return LoadAllImp(LoadAllGroupOp);
        }

        IEnumerator LoadAllImp(Func<AddressablesImpl, List<AsyncOperationHandle>, Task> func)
        {
            using AddressablesImpl impl = new AddressablesImpl(new DefaultAllocationStrategy());
            var op = impl.InitializeAsync(m_RuntimeSettingsPath);
            var task = op.Task;
            while (!task.IsCompleted)
                yield return null;
            yield return null; //need deferred callbacks to get called
            Stopwatch w = Stopwatch.StartNew();
            var handles = new List<AsyncOperationHandle>();
            var loadTask = func(impl, handles);
            while (!loadTask.IsCompleted)
                yield return null;
            UnityEngine.Debug.Log("Complete in " + w.ElapsedMilliseconds);
            foreach (var h in handles)
                h.Release();
            yield return new WaitForSecondsRealtime(.5f);
        }

        async Task LoadAll(AddressablesImpl impl, List<AsyncOperationHandle> handles)
        {
            for (int i = 0; i < loadCount; i++)
            {
                var h = impl.LoadAssetAsync<GameObject>(i.ToString());
                handles.Add(h);
                await h.Task;
            }
        }
        async Task LoadAllGroupOp(AddressablesImpl impl, List<AsyncOperationHandle> handles)
        {
            for (int i = 0; i < loadCount; i++)
            {
                var h = impl.LoadAssetAsync<GameObject>(i.ToString());
                handles.Add(h);
            }
            var gop = impl.ResourceManager.CreateGenericGroupOperation(handles);

            await gop.Task;
        }

        async Task LoadAllBatch(AddressablesImpl impl, List<AsyncOperationHandle> handles)
        {
            List<Task<GameObject>> tasks = new List<Task<GameObject>>(loadCount);
            for (int i = 0; i < loadCount; i++)
            {
                var h = impl.LoadAssetAsync<GameObject>(i.ToString());
                handles.Add(h);
                tasks.Add(h.Task);
            }
            await Task.WhenAll(tasks);
        }
        async Task LoadAllLabel(AddressablesImpl impl, List<AsyncOperationHandle> handles)
        {
            var h = impl.LoadAssetsAsync<GameObject>("all", null, true);
            handles.Add(h);
            await h.Task;
        }
        [UnityTest]
        public IEnumerator AsyncTask_MaintainsCorrectRefCountAfterCompletion()
        {
            using AddressablesImpl impl = new AddressablesImpl(new DefaultAllocationStrategy());
            var op = impl.InitializeAsync(m_RuntimeSettingsPath);
            var task = op.Task;
            while (!task.IsCompleted)
                yield return null;
            yield return null; //need deferred callbacks to get called
            Assert.IsFalse(op.IsValid());
        }

        [UnityTest]
        public IEnumerator AsyncTask_Await_Then_ReleaseByObject_FreesHandle()
        {
            using AddressablesImpl impl = new AddressablesImpl(new DefaultAllocationStrategy());
            var initOp = impl.InitializeAsync(m_RuntimeSettingsPath);
            var initTask = initOp.Task;
            while (!initTask.IsCompleted)
                yield return null;
            yield return null; //need deferred callbacks to get called

            var handle = impl.LoadAssetsAsync<GameObject>("all", null, true);
            var task = AwaitThenRelease(impl, handle);
            while (!task.IsCompleted)
                yield return null;

            // Releasing the exact IList<GameObject> the await yielded looks the handle up via
            // AddressablesImpl.m_resultToHandle and releases it - no stored handle field needed.
            Assert.IsFalse(handle.IsValid());
        }

        async Task AwaitThenRelease(AddressablesImpl impl, AsyncOperationHandle<IList<GameObject>> handle)
        {
            var result = await handle;
            impl.Release(result);
        }

        [UnityTest]
        public IEnumerator AsyncTask_Await_FailedLoad_Throws()
        {
            using AddressablesImpl impl = new AddressablesImpl(new DefaultAllocationStrategy());
            var initOp = impl.InitializeAsync(m_RuntimeSettingsPath);
            var initTask = initOp.Task;
            while (!initTask.IsCompleted)
                yield return null;
            yield return null; //need deferred callbacks to get called

            Exception caught = null;
            Task task;
            using (new IgnoreFailingLogMessage())
            {
                task = AwaitBadKey(impl, e => caught = e);
                while (!task.IsCompleted)
                    yield return null;
            }

            Assert.IsNotNull(caught, "Expected await on a failed load to throw.");

            // The thrown exception must carry the failed handle so a caller who only awaited
            // (and never stored the handle separately) can still inspect it and release it.
            var handleEx = caught as AsyncOperationHandleException<GameObject>;
            Assert.IsNotNull(handleEx, $"Expected AsyncOperationHandleException<GameObject>, got {caught.GetType()}.");
            Assert.IsTrue(handleEx.Handle.IsValid());
            Assert.AreEqual(AsyncOperationStatus.Failed, handleEx.Handle.Status);
            handleEx.Handle.Release();
        }

        async Task AwaitBadKey(AddressablesImpl impl, Action<Exception> onError)
        {
            try
            {
                // Unlike handle.Task (which resolves to null on failure and never faults), await on the
                // handle throws an AsyncOperationHandleException wrapping the operation's OperationException.
                await impl.LoadAssetAsync<GameObject>("this_key_does_not_exist_in_any_group");
            }
            catch (Exception e)
            {
                onError(e);
            }
        }


        [UnityTest]
        public IEnumerator AsyncTask_Await_PartialFailure_ExposesResultOnException()
        {
            using AddressablesImpl impl = new AddressablesImpl(new DefaultAllocationStrategy());
            var initOp = impl.InitializeAsync(m_RuntimeSettingsPath);
            var initTask = initOp.Task;
            while (!initTask.IsCompleted)
                yield return null;
            yield return null; //need deferred callbacks to get called

            // Simulate the partial-success contract of LoadAssetsAsync(..., releaseDependenciesOnFailure: false):
            // Status is Failed, but Result is a non-null list with successful items alongside failed (null) slots.
            var partialResult = new List<GameObject> {new GameObject("loaded"), null};

            AsyncOperationHandleException<IList<GameObject>> caught = null;
            Task task;
            using (new IgnoreFailingLogMessage())
            {
                // CreateCompletedOperationWithException completes (and logs via the ResourceManager's
                // default exception handler) synchronously, so it must run inside this ignore-block too.
                var partialHandle = impl.ResourceManager.CreateCompletedOperationWithException<IList<GameObject>>(
                    partialResult, new ResourceManagerException("Partial success in ProvideResources."));

                task = AwaitPartialFailure(partialHandle, e => caught = e);
                while (!task.IsCompleted)
                    yield return null;
            }

            Assert.IsNotNull(caught, "Expected await on a partially-failed load to throw.");

            // A caller who only awaited (and never stored partialHandle separately) can still reach
            // the partial result and release it through the exception's Handle.
            Assert.AreSame(partialResult, caught.Handle.Result);
            Assert.AreEqual(AsyncOperationStatus.Failed, caught.Handle.Status);

            UnityEngine.Object.Destroy(partialResult[0]);
            caught.Handle.Release();
        }

        async Task AwaitPartialFailure(AsyncOperationHandle<IList<GameObject>> handle, Action<AsyncOperationHandleException<IList<GameObject>>> onError)
        {
            try
            {
                await handle;
            }
            catch (AsyncOperationHandleException<IList<GameObject>> e)
            {
                onError(e);
            }
        }

        [UnityTest]
        public IEnumerator AsyncTask_GetAwaiter_IsInstanceMember_ResolvesResult()
        {
            using AddressablesImpl impl = new AddressablesImpl(new DefaultAllocationStrategy());
            var initOp = impl.InitializeAsync(m_RuntimeSettingsPath);
            var initTask = initOp.Task;
            while (!initTask.IsCompleted)
                yield return null;
            yield return null; //need deferred callbacks to get called

            var handle = impl.LoadAssetAsync<GameObject>("0");

            // Drive the awaiter directly (not via `await`) to confirm GetAwaiter() is a public instance
            // member, not just an extension method needing a using directive to resolve.
            var awaiter = handle.GetAwaiter();
            while (!awaiter.IsCompleted)
                yield return null;

            Assert.IsNotNull(awaiter.GetResult());
            impl.Release(handle);
        }

        [UnityTest]
        public IEnumerator AsyncTask_ReleaseByObject_OnCopiedList_Warns()
        {
            using AddressablesImpl impl = new AddressablesImpl(new DefaultAllocationStrategy());
            var initOp = impl.InitializeAsync(m_RuntimeSettingsPath);
            var initTask = initOp.Task;
            while (!initTask.IsCompleted)
                yield return null;
            yield return null; //need deferred callbacks to get called

            var handle = impl.LoadAssetsAsync<GameObject>("all", null, true);

            LogAssert.Expect(LogType.Error, "Addressables.Release was called on an object that Addressables was not previously aware of.  Thus nothing is being released");
            var task = AwaitAndReleaseCopy(impl, handle);
            while (!task.IsCompleted)
                yield return null;

            // The copy is a different object instance (reference-equality key), so the release-by-object
            // lookup fails and the original handle is untouched - it must still be released explicitly.
            Assert.IsTrue(handle.IsValid());
            handle.Release();
        }

        async Task AwaitAndReleaseCopy(AddressablesImpl impl, AsyncOperationHandle<IList<GameObject>> handle)
        {
            var result = await handle;
            var copy = new List<GameObject>(result);
            impl.Release(copy);
        }

        [UnityTest]
        [Ignore("Ignoring until task refactor is complete.")]
        public IEnumerator AsyncTask_DoesNotReturnNull_StressTest()
        {
            for (int i = 0; i < 100; i++)
            {
                using AddressablesImpl impl = new AddressablesImpl(new DefaultAllocationStrategy());
                var op = impl.InitializeAsync(m_RuntimeSettingsPath);
                var task = op.Task;
                while (!task.IsCompleted)
                    yield return null;
                var result = op.Task.Result;
                yield return null; //need deferred callbacks to get called
                Assert.IsNotNull(op.Task.Result, $"task.Result is null! For task number [{i}]");
                op.Release();
            }
        }
    }
#if UNITY_EDITOR
    class AsyncTaskTests_FastMode : AsyncTaskTests
    {
        protected override TestBuildScriptMode BuildScriptMode
        {
            get { return TestBuildScriptMode.Fast; }
        }
    }

    class AsyncTaskTests_PackedPlaymodeMode : AsyncTaskTests
    {
        protected override TestBuildScriptMode BuildScriptMode
        {
            get { return TestBuildScriptMode.PackedPlaymode; }
        }
    }
#endif

    [UnityPlatform(exclude = new[] {RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor})]
    class AsyncTaskTests_PackedMode : AsyncTaskTests
    {
        protected override TestBuildScriptMode BuildScriptMode
        {
            get { return TestBuildScriptMode.Packed; }
        }
    }
}
