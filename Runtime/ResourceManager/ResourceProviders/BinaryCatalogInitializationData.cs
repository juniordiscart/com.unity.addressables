#if !ENABLE_JSON_CATALOG
using System;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    /// <summary>
    /// Object used to specify custom values for the cache sizes used in Binary Catalogs.
    /// </summary>
    [Serializable]
    public class BinaryCatalogInitialization : IInitializableObject
    {
        /// <summary>
        /// Default size for binary storage buffer internal cache;
        /// </summary>
        public const int kDefaultBinaryStorageBufferCacheSize = 128;
        /// <summary>
        /// Default size of binary catalog location cache.
        /// </summary>
        public const int kCatalogLocationCacheSize = 32;

        static int s_BinaryStorageBufferCacheSize = kDefaultBinaryStorageBufferCacheSize;
        static int s_CatalogLocationCacheSize = kCatalogLocationCacheSize;

        /// <summary>
        /// This value controls the amount of memory used internally by the storage buffer of the binary catalog.
        /// Lower values will use less memory but may result in higher cpu usage as more data will be uncached and will need to be rebuilt.
        /// The default value is 128 and should work for most applications.
        /// </summary>
        public static int BinaryStorageBufferCacheSize => s_BinaryStorageBufferCacheSize;
        /// <summary>
        /// This value controls the number of ResourceLocations to keep in the internal cache for the binary catalog.
        /// Lower values will use less memory but may result in higher cpu usage as more data will be uncached and will need to be rebuilt.
        /// The default value is 32 and should work for most applications.
        /// </summary>
        public static int CatalogLocationCacheSize => s_CatalogLocationCacheSize;

        /// <summary>
        /// Reset the cache values to their defaults.
        /// </summary>
        static public void ResetToDefaults()
        {
            s_BinaryStorageBufferCacheSize = kDefaultBinaryStorageBufferCacheSize;
            s_CatalogLocationCacheSize = kCatalogLocationCacheSize;
        }

        /// <summary>
        /// Initialize the cache values from the serialized data.
        /// </summary>
        /// <param name="id">The object id.  This value is usually the type name.</param>
        /// <param name="dataStr">The serialized data.</param>
        /// <returns>Returns true if the initialization operation succeeded.</returns>
        public bool Initialize(string id, string dataStr)
        {
            try
            {
                var data = JsonUtility.FromJson< BinaryCatalogInitializationData>(dataStr);
                if (data != null)
                {
                    s_BinaryStorageBufferCacheSize = data.m_BinaryStorageBufferCacheSize;
                    s_CatalogLocationCacheSize = data.m_CatalogLocationCacheSize;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to initialize BinaryCatalog cache size - invalid data.");
                Debug.LogException(ex);
            }
            return true;
        }
        /// <summary>
        /// This is the async initialization.
        /// </summary>
        /// <param name="resourceManager">The current ResourceManager to create the operation handle with.</param>
        /// <param name="id">The object id.  This value is usually the type name.</param>
        /// <param name="dataStr">The serialized data.</param>
        /// <returns>Returns the operation handle.  This will always be a completed operation since the initialization can be done synchronously.</returns>
        public AsyncOperationHandle<bool> InitializeAsync(ResourceManager resourceManager, string id, string dataStr)
        {
            return resourceManager.CreateCompletedOperation(Initialize(id, dataStr), null);
        }
    }

    [Serializable]
    class BinaryCatalogInitializationData
    {
        [SerializeField]
        public int m_BinaryStorageBufferCacheSize = BinaryCatalogInitialization.BinaryStorageBufferCacheSize;
        [SerializeField]
        public int m_CatalogLocationCacheSize = BinaryCatalogInitialization.CatalogLocationCacheSize;
    }
}
#endif
