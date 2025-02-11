using System;
using UnityEngine.Profiling;

namespace UnityEngine.ResourceManagement.Profiling
{
    /// <summary>
    /// The profiler emitter for Addressables data
    /// </summary>
    public class EngineEmitter : IProfilerEmitter
    {
        /// <summary>
        /// Checks if the profiler is enabled
        /// </summary>
        public bool IsEnabled
        {
            get => Profiler.enabled;
        }

        /// <summary>
        /// Emits the frame data for the profiler
        /// </summary>
        /// <param name="id">The GUID of the profiler</param>
        /// <param name="tag">tag indicating what type of data is being emitted</param>
        /// <param name="data">The data contents to send to the profiler</param>
        public void EmitFrameMetaData(Guid id, int tag, Array data)
        {
            Profiler.EmitFrameMetaData(id, tag, data);
        }

        /// <summary>
        /// Adds a callback to the MonoBehaviourCallbackHooks instance
        /// </summary>
        /// <param name="d">The callback to invoke</param>
        public void InitialiseCallbacks(Action<float> d)
        {
            MonoBehaviourCallbackHooks.Instance.OnLateUpdateDelegate += d;
        }
    }
}
