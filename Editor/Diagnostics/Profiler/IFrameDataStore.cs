#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor.Profiling;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal interface IFrameDataStore
    {
        public FrameDataViewRef GetRawFrameDataView(int frameIndex, int threadIndex);
        public IEnumerable<T> GetFrameMetaData<T>(FrameDataViewRef frameDataViewRef, Guid id, int tag) where T : struct;
    }
}
#endif
