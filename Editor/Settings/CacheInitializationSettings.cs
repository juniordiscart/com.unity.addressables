using System;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings
{
    /// <summary>
    /// Asset container for <see cref="CacheInitializationData"/>.
    /// A user can create one of these assets using the context menu option at Create->Addressables->Initialization->Cache Initialization Settings.
    /// This object implements the <see cref="IObjectInitializationDataProvider"/> interface to create initialization data for the Addressables system.
    /// In the Addressables Settings window, there is a list of Initialization Objects that this asset must be added to in order to be included in the build.
    /// </summary>
    [CreateAssetMenu(fileName = "CacheInitializationSettings.asset", menuName = "Addressables/Initialization/Cache Initialization Settings")]
    public class CacheInitializationSettings : ScriptableObject, IObjectInitializationDataProvider
    {
        [FormerlySerializedAs("m_data")]
        [SerializeField]
        CacheInitializationData m_Data = new CacheInitializationData();

        /// <summary>
        /// Display name used in GUI for this object.
        /// </summary>
        public string Name
        {
            get { return "Asset Bundle Cache Settings"; }
        }

        /// <summary>
        /// The cache initialization data that will be serialized and applied during Addressables initialization.
        /// </summary>
        public CacheInitializationData Data
        {
            get { return m_Data; }
            set { m_Data = value; }
        }

        /// <summary>
        /// Create initialization data to be serialized into the Addressables runtime data.
        /// </summary>
        /// <returns>The serialized data for the initialization class and the data.</returns>
        public ObjectInitializationData CreateObjectInitializationData()
        {
            return ObjectInitializationData.CreateSerializedInitializationData<CacheInitialization>(typeof(CacheInitialization).Name, m_Data);
        }
    }
}
