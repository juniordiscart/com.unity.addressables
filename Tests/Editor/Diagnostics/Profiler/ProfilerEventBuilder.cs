using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Profiling;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace UnityEditor.AddressableAssets.Tests.Diagnostics.Profiler
{
    internal class ProfilerEventBuilder
    {
        private TestProfiler m_TestProfiler;
        private ProvideHandle m_Handle;
        private AsyncOperationHandle<SceneInstance> m_SceneHandle;
        private AssetBundleRequestOptions m_Opts;
        private Dictionary<int, ContentStatus> m_FrameStatusMap = new Dictionary<int, ContentStatus>();

        public ProfilerEventBuilder(TestProfiler testProfiler)
        {
            m_TestProfiler = testProfiler;
            m_Handle = new ProvideHandle(Addressables.ResourceManager, new TestOp());
            m_SceneHandle = new AsyncOperationHandle<SceneInstance>(new TestSceneOp());
            m_Opts = new AssetBundleRequestOptions();
        }

        public ProfilerEventBuilder SetOperationStatus(AsyncOperationStatus status)
        {
            ((TestOp)m_Handle.InternalOp).Status = status;
            return this;
        }

        public ProfilerEventBuilder SetBundleName(string bundleName)
        {
            m_Opts.BundleName = bundleName;
            return this;
        }

        public ProfilerEventBuilder SetAssetLocation(string addressableName, string bundleName)
        {
            var location = new ProfilerResourceLocation();
            location.InternalId = addressableName;
            location.Dependencies = new List<IResourceLocation>();
            location.Dependencies.Add(new ProfilerResourceLocation
            {
                Data = new AssetBundleRequestOptions{BundleName = bundleName}, // fixme, strip off bundle name or tone up the api
            });

            ((TestOp)m_Handle.InternalOp).Location = location;
            return this;
        }

        public ProfilerEventBuilder SendBundleEvent(int frame, ContentStatus status, BundleSource source = BundleSource.Download)
        {
            m_FrameStatusMap[frame] = status;
            m_TestProfiler.CurrentFrame = frame;
            if (status == ContentStatus.Released)
            {
                ProfilerRuntime.BundleReleased(m_Opts.BundleName);
            }
            else
            {
                ProfilerRuntime.AddBundleOperation(m_Handle, m_Opts, status, source);
            }
            ProfilerRuntime.PushToProfilerStream();
            return this;
        }
        public ProfilerEventBuilder SendAssetEvent(int frame, ContentStatus status)
        {
            m_FrameStatusMap[frame] = status;
            m_TestProfiler.CurrentFrame = frame;
            ProfilerRuntime.AddAssetOperation(m_Handle, status);

            ProfilerRuntime.PushToProfilerStream();
            return this;
        }

        public ProfilerEventBuilder SendSceneEvent(int frame, ContentStatus status)
        {
            m_FrameStatusMap[frame] = status;
            m_TestProfiler.CurrentFrame = frame;
            if (status == ContentStatus.Released)
            {
                ProfilerRuntime.SceneReleased(m_SceneHandle);
            }
            else
            {
                ProfilerRuntime.AddSceneOperation(m_SceneHandle, m_Handle.Location, status);
            }

            ProfilerRuntime.PushToProfilerStream();
            return this;
        }

        public ProfilerEventBuilder VerifyFrameStatus(int frame, ContentStatus actualStatus)
        {
            if(!m_FrameStatusMap.ContainsKey(frame))
            {
                return this;
            }

            Assert.AreEqual(m_FrameStatusMap[frame], actualStatus, $"For frame {frame}, expected status {m_FrameStatusMap[frame]} but got {actualStatus}");
            return this;
        }
    }

    internal class TestSceneOp : AsyncOperationBase<SceneInstance>, IAsyncOperation
    {
        protected override void Execute()
        {
            throw new NotImplementedException();
        }
    }

    internal class TestOp : IGenericProviderOperation, IAsyncOperation
    {
        public void Init(ResourceManager rm, IResourceProvider provider, IResourceLocation location, AsyncOperationHandle<IList<AsyncOperationHandle>> depOp)
        {
            throw new NotImplementedException();
        }

        public void Init(ResourceManager rm, IResourceProvider provider, IResourceLocation location, AsyncOperationHandle<IList<AsyncOperationHandle>> depOp, bool releaseDependenciesOnFailure)
        {
            throw new NotImplementedException();
        }

        public int ProvideHandleVersion { get; }
        public IResourceLocation Location { get; set; }
        public int DependencyCount { get; }

        public void GetDependencies(IList<object> dstList)
        {
            throw new NotImplementedException();
        }

        public TDepObject GetDependency<TDepObject>(int index)
        {
            throw new NotImplementedException();
        }

        public void SetProgressCallback(Func<float> callback)
        {
            throw new NotImplementedException();
        }

        public void ProviderCompleted<T>(T result, bool status, Exception e)
        {
            throw new NotImplementedException();
        }

        public Type RequestedType { get; }

        public void SetDownloadProgressCallback(Func<DownloadStatus> callback)
        {
            throw new NotImplementedException();
        }

        public void SetWaitForCompletionCallback(Func<bool> callback)
        {
            throw new NotImplementedException();
        }

        public object GetResultAsObject()
        {
            throw new NotImplementedException();
        }

        public Type ResultType { get; }
        public int Version { get; }
        public string DebugName { get; }

        public void DecrementReferenceCount()
        {
            throw new NotImplementedException();
        }

        public void IncrementReferenceCount()
        {
            throw new NotImplementedException();
        }

        public int ReferenceCount { get; }
        public float PercentComplete { get; }

        public DownloadStatus GetDownloadStatus(HashSet<object> visited)
        {
            throw new NotImplementedException();
        }

        public AsyncOperationStatus Status { get; set; }
        public Exception OperationException { get; }
        public bool IsDone { get; }
        public Action<IAsyncOperation> OnDestroy { get; set; }

        public void GetDependencies(List<AsyncOperationHandle> deps)
        {
            throw new NotImplementedException();
        }

        public bool IsRunning { get; }
        public event Action<AsyncOperationHandle> CompletedTypeless;
        public event Action<AsyncOperationHandle> Destroyed;

        public void InvokeCompletionEvent()
        {
            throw new NotImplementedException();
        }

        public Task<object> Task { get; }

        public void Start(ResourceManager rm, AsyncOperationHandle dependency, DelegateList<float> updateCallbacks)
        {
            throw new NotImplementedException();
        }

        public AsyncOperationHandle Handle { get; }

        public void WaitForCompletion()
        {
            throw new NotImplementedException();
        }
    }

    internal class ProfilerResourceLocation : IResourceLocation {
        public string InternalId { get; set; }
        public string ProviderId { get; set; }
        public IList<IResourceLocation> Dependencies { get; set; }
        public int Hash(Type resultType)
        {
            throw new NotImplementedException();
        }

        public int DependencyHashCode { get; set; }
        public bool HasDependencies { get; set; }
        public object Data { get; set; }
        public string PrimaryKey { get; set; }
        public Type ResourceType { get; set; }
    }
}
