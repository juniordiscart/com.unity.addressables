#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Profiling;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    // this is a wrapper to make it possible to pass around context for testing
    internal class FrameDataViewRef : IDisposable
    {
        public int frameIndex { get; set; }
        public int threadIndex { get; set; }
        public FrameDataView frameDataView { get; set; }

        public bool valid { get; set; }

        public FrameDataViewRef()
        {
        }

        public void Dispose()
        {
            frameDataView?.Dispose();
        }
    }
}
#endif
