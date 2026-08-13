#if !ENABLE_JSON_CATALOG
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace UnityEditor.AddressableAssets
{
    /// <summary>
    /// Object to hold cache settings for the binary catalog.  Create an asset of this type using the Create menu (Addressables/Initialization/Binary Catalog Initialization Settings) and then add it to the Initialization Objects of the Addressables system settings object.
    /// </summary>
    [CreateAssetMenu(fileName = "BinaryCatalogInitialization.asset", menuName = "Addressables/Initialization/Binary Catalog Initialization Settings")]
    public class BinaryCatalogInitializationSettings : ScriptableObject, IObjectInitializationDataProvider
    {

        /// <summary>
        /// This value controls the number of ResourceLocations to keep in the internal cache for the binary catalog.
        /// Lower values will use less memory but may result in higher cpu usage as more data will be uncached and will need to be rebuilt.
        /// The default value is 32 and should work for most applications.
        /// </summary>
        public int CatalogLocationCacheSize = BinaryCatalogInitialization.CatalogLocationCacheSize;

        /// <summary>
        /// This value controls the amount of memory used internally by the storage buffer of the binary catalog.
        /// Lower values will use less memory but may result in higher cpu usage as more data will be uncached and will need to be rebuilt.
        /// The default value is 128 and should work for most applications.
        /// </summary>
        public int BinaryStorageBufferCacheSize = BinaryCatalogInitialization.BinaryStorageBufferCacheSize;

        /// <summary>
        /// Name of the settings object.
        /// </summary>
        public string Name => "Binary Catalog Settings";

        /// <summary>
        /// Creates the initialization data.
        /// </summary>
        /// <returns>The created initialization data object.  This will be serialized into the Addressables runtime settings and used to initialize the cache settings for the binary catalogs.</returns>
        public ObjectInitializationData CreateObjectInitializationData()
        {
            return ObjectInitializationData.CreateSerializedInitializationData<BinaryCatalogInitialization>(typeof(BinaryCatalogInitialization).Name, new BinaryCatalogInitializationData { m_BinaryStorageBufferCacheSize = BinaryStorageBufferCacheSize, m_CatalogLocationCacheSize = CatalogLocationCacheSize });
        }
    }
}
#endif
