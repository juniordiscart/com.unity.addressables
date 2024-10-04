using System;
using UnityEngine.Profiling;

namespace UnityEngine.ResourceManagement.Profiling
{

    public class EngineEmitter : IProfilerEmitter
    {
        public bool IsEnabled
        {
            get => Profiler.enabled;
        }

        public void EmitFrameMetaData(Guid id, int tag, Array data)
        {
            Profiler.EmitFrameMetaData(id, tag, data);
        }

        public void InitialiseCallbacks(Action<float> d)
        {
            MonoBehaviourCallbackHooks.Instance.OnLateUpdateDelegate += d;
        }
    }
}
