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
