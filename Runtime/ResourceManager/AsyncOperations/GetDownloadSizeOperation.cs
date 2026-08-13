using System.Collections;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UnityEngine.ResourceManagement.AsyncOperations
{
    /// <summary>
    /// The async operation used to calculate the required download size of a key or set of keys. Runs as part of a coroutine
    /// </summary>
    internal class GetDownloadSizeOperation : AsyncOperationBase<long>
    {
        IEnumerable<IResourceLocation> m_Locations;
        Coroutine m_AsyncCalculation;

        /// <summary>
        /// Initialize the operation with a unique set of Locations to check the download size for.
        /// </summary>
        /// <param name="locations">The unique set of IResource Locations that we'll use to calculate the required download size.</param>
        /// <param name="resourceManager">The current instance of the ResourceManager</param>
        public void Init(IEnumerable<IResourceLocation> locations, ResourceManager resourceManager)
        {
            m_Locations = locations;
            m_RM = resourceManager;
        }

        IEnumerator Calculate()
        {
            long size = 0;
            foreach(var location in m_Locations)
            {
                var sizeData = location.Data as ILocationSizeData;
                if (sizeData != null)
                {
                    size += sizeData.ComputeSize(location, m_RM);
                    yield return null;
                }
            }
            Complete(size, true, "");
        }

        void CalculateSync()
        {
            long size = 0;
            foreach (var location in m_Locations)
            {
                var sizeData = location.Data as ILocationSizeData;
                if (sizeData != null)
                    size += sizeData.ComputeSize(location, m_RM);
            }

            Complete(size, true, "");
        }

        /// <summary>
        /// Executes the async op. We run the GetDownloadSize calculations as part of a coroutine
        /// </summary>
        protected override void Execute()
        {
            m_AsyncCalculation = MonoBehaviourCallbackHooks.Instance.StartCoroutine(Calculate());
        }

        protected override bool InvokeWaitForCompletion()
        {
            MonoBehaviourCallbackHooks.Instance.StopCoroutine(m_AsyncCalculation);
            CalculateSync();
            return true;
        }
    }
}
