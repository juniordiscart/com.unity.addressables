using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UnityEngine.ResourceManagement.AsyncOperations
{
    /// <summary>
    /// The synchronous operation used to calculate the required download size of a key or set of keys. Encapsulated in an operation
    /// to simplify operation chaining.
    /// Internally this calls Caching.IsVersionCached so it generates more IO than it appears on the surface.
    /// </summary>
    internal class GetDownloadSizeOperation : AsyncOperationBase<long>
    {
        IEnumerable<IResourceLocation> m_Locations;
        bool m_Started;

        /// <summary>
        /// Initialize the operation with a unique set of Locations to check the download size for.
        /// </summary>
        /// <param name="locations">The unique set of IResource Locations that we'll use to calculate the required download size.</param>
        /// <param name="resourceManager">The current instance of the ResourceManager</param>
        public void Init(IEnumerable<IResourceLocation> locations, ResourceManager resourceManager)
        {
            m_Locations = locations;
            m_RM = resourceManager;
            m_Started = false;
        }

        void Calculate()
        {
            if (m_Started)
                return;
            m_Started = true;
            long size = 0;
            try {
                foreach (var location in m_Locations)
                {
                    var sizeData = location.Data as ILocationSizeData;
                    if (sizeData != null)
                        size += sizeData.ComputeSize(location, m_RM);
                }
            }
            catch(Exception e)
            {
                Complete(0, false, $"Error calculating download size: {e.ToString()}");
                return;
            }
            Complete(size, true, "");
        }

        /// <summary>
        /// Executes the synchronous op.
        /// </summary>
        protected override void Execute()
        {
            Calculate();
        }

        protected override bool InvokeWaitForCompletion()
        {
            Calculate();
            return true;
        }
    }
}
