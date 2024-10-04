using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Diagnostics;
using UnityEngine;
using UnityEngine.ResourceManagement.Profiling;
using static UnityEngine.ResourceManagement.Profiling.ProfilerRuntime;

namespace UnityEditor.AddressableAssets.Tests.Diagnostics.Profiler
{

    internal class TestProfiler : IProfilerEmitter, IFrameDataStore
    {
        // maybe have a key
        private Dictionary<string, List<CatalogFrameData>> m_catalogFrames = new();
        private Dictionary<string, List<BundleFrameData>> m_bundleFrames = new();
        private Dictionary<string, List<AssetFrameData>> m_assetFrames = new();
        private Dictionary<string, List<AssetFrameData>> m_sceneFrames = new();

        public int CurrentThread
        {
            get => Thread.CurrentContext.ContextID;
        }

        public bool IsEnabled { get; set; } = true;

        // public int CurrentFrame { get => Time.frameCount; }
        public int CurrentFrame { get; set; } = 0;

        private string GetKey()
        {
            return $"{CurrentFrame}-{CurrentThread}";
        }

        /*
        public void IncrementCurrentFrame()
        {
            // it would be cool if this hooked into the test frameworks async frame tools
            CurrentFrame += 1;
        }
        */

        // You can emit data for each type of tag in a given frame, but then you'll need
        // to increment the frame by setting CurrentFrame or calling IncrementCurrentFrame
        public void EmitFrameMetaData(Guid id, int tag, Array data)
        {
            if (!IsEnabled)
                return;
            Assert.AreEqual(kResourceManagerProfilerGuid, id, "TestProfiler only handles data for the Addressables ResourceManager");
            if (data.Length == 0)
            {
                return;
            }

            var key = GetKey();
            switch (tag)
            {
                case kCatalogTag:
                    m_catalogFrames.TryAdd(key, new List<CatalogFrameData>());
                    // Assert.False(m_catalogFrames.ContainsKey(GetKey()), "TestProfiler does not allow emitting data for the same frame/thread/catalog more than once. Increment CurrentFrame.");
                    m_catalogFrames[key].AddRange(data as CatalogFrameData[]);
                    break;
                case kBundleDataTag:
                    m_bundleFrames.TryAdd(key, new List<BundleFrameData>());
                    // Assert.False(m_bundleFrames.ContainsKey(GetKey()), "TestProfiler does not allow emitting data for the same frame/thread/bundle more than once. Increment CurrentFrame.");
                    m_bundleFrames[key].AddRange(data as BundleFrameData[]);
                    break;
                case kAssetDataTag:
                    m_assetFrames.TryAdd(key, new List<AssetFrameData>());
                    // Assert.False(m_assetFrames.ContainsKey(GetKey()), "TestProfiler does not allow emitting data for the same frame/thread/asset more than once. Increment CurrentFrame.");
                    m_assetFrames[key].AddRange(data as AssetFrameData[]);
                    break;
                case kSceneDataTag:
                    m_sceneFrames.TryAdd(key, new List<AssetFrameData>());
                    // Assert.False(m_catalogFrames.ContainsKey(GetKey()), "TestProfiler does not allow emitting data for the same frame/thread/scene more than once. Increment CurrentFrame.");
                    m_sceneFrames[key].AddRange(data as AssetFrameData[]);
                    break;
            }

        }

        public void InitialiseCallbacks(Action<float> onLateUpdateDelegate)
        {
            // make sure we get rid of the delegate if it has already been registered, although maybe we don't want to do this explicitly?
            MonoBehaviourCallbackHooks.Instance.OnLateUpdateDelegate -= onLateUpdateDelegate;
        }

        public FrameDataViewRef GetRawFrameDataView(int frameIndex, int threadIndex)
        {
            return new FrameDataViewRef
            {
                frameIndex = frameIndex,
                threadIndex = threadIndex,
                valid = true,
            };
        }

        public IEnumerable<T> GetFrameMetaData<T>(FrameDataViewRef rawFrameDatView, Guid id, int tag) where T : struct
        {
            var key = $"{rawFrameDatView.frameIndex}-{rawFrameDatView.threadIndex}";
            Assert.AreEqual(kResourceManagerProfilerGuid, id, "TestProfiler only handles data for the Addressables ResourceManager");
            switch (tag)
            {
                case kCatalogTag:
                    var catalogFrameData = m_catalogFrames.GetValueOrDefault(key, new List<CatalogFrameData>());
                    return catalogFrameData as IEnumerable<T>;
                case kBundleDataTag:
                    var bundleFrameData = m_bundleFrames.GetValueOrDefault(key, new List<BundleFrameData>());
                    return bundleFrameData as IEnumerable<T>;
                case kAssetDataTag:
                    var assetFrameData = m_assetFrames.GetValueOrDefault(key, new List<AssetFrameData>());
                    return assetFrameData as IEnumerable<T>;
                case kSceneDataTag:
                    var sceneFrameData = m_sceneFrames.GetValueOrDefault(key, new List<AssetFrameData>());
                    return sceneFrameData as IEnumerable<T>;
            }

            Assert.Fail($"unknown tag {tag}");
            return null;
        }
    }
}
