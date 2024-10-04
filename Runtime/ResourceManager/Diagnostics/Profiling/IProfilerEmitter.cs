using System;

namespace UnityEngine.ResourceManagement.Profiling
{
    internal interface IProfilerEmitter
    {
        public bool IsEnabled { get; }
        public void EmitFrameMetaData(Guid id, int tag, Array data);

        public void InitialiseCallbacks(Action<float> onLateUpdateDelegate);
    }
}
