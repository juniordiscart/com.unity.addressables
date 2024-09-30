using System;


namespace UnityEngine.AddressableAssets.ResourceLocators
{
#if ENABLE_CCD
    /// <summary>
    /// This is an internal class used as an intermediary data store from editor time to runtime
    /// </summary>
    [Serializable]
    internal class CcdManagedData
    {
        /// <summary>
        /// Denotes what state the config is in.
        /// </summary>
        public enum ConfigState
        {
            /// <summary>
            /// Config has not been modified.
            /// </summary>
            None,
            /// <summary>
            /// Config should use default values according to CCD opinionated workflow.
            /// </summary>
            Default,
            /// <summary>
            /// The config has been overriden externally.
            /// </summary>
            Override
        };

        /// <summary>
        /// Id of the Environment to store
        /// </summary>
        public string EnvironmentId;

        /// <summary>
        /// Name of the Environment to store
        /// </summary>
        public string EnvironmentName;

        /// <summary>
        /// Id of the Bucket to store
        /// </summary>
        public string BucketId;

        /// <summary>
        /// Name of the Badge to store
        /// </summary>
        public string Badge;

        /// <summary>
        /// The current state of the config
        /// </summary>
        public ConfigState State;

        /// <summary>
        /// Constructor for CcdManagedData
        /// </summary>
        public CcdManagedData()
        {
            State = ConfigState.None;
        }

        /// <summary>
        /// Determines if the CcdManagedData has been configured
        /// </summary>
        /// <returns>True if all fields have been set. False, otherwise.</returns>
        public bool IsConfigured()
        {
            return !string.IsNullOrEmpty(EnvironmentId) && !string.IsNullOrEmpty(BucketId) && !string.IsNullOrEmpty(Badge);
        }
    }
#endif
}
