using System;
using System.Collections.Generic;
using UnityEditorInternal;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal class EditorFrameDataStore : IFrameDataStore
    {
        public FrameDataViewRef GetRawFrameDataView(int frameIndex, int threadIndex)
        {
            var frameDataView = ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex);
            return new FrameDataViewRef
            {
                frameIndex = frameIndex,
                threadIndex = threadIndex,
                frameDataView = frameDataView,
                valid = frameDataView?.valid ?? false,
            };
        }

        public IEnumerable<T> GetFrameMetaData<T>(FrameDataViewRef rawFrameDataViewRef, Guid id, int tag) where T : struct
        {
            return rawFrameDataViewRef.frameDataView.GetFrameMetaData<T>(id, tag);
        }
    }
}
