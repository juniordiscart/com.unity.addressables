using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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
            AddressablesImpl impl = new AddressablesImpl(new DefaultAllocationStrategy());
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
            AddressablesImpl impl = new AddressablesImpl(new DefaultAllocationStrategy());
            var op = impl.InitializeAsync(m_RuntimeSettingsPath);
            var task = op.Task;
            while (!task.IsCompleted)
                yield return null;
            yield return null; //need deferred callbacks to get called
            Assert.IsFalse(op.IsValid());
        }

        [UnityTest]
        [Ignore("Ignoring until task refactor is complete.")]
        public IEnumerator AsyncTask_DoesNotReturnNull_StressTest()
        {
            for (int i = 0; i < 100; i++)
            {
                AddressablesImpl impl = new AddressablesImpl(new DefaultAllocationStrategy());
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

    class AsyncTaskTests_VirtualMode : AsyncTaskTests
    {
        protected override TestBuildScriptMode BuildScriptMode
        {
            get { return TestBuildScriptMode.Virtual; }
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
