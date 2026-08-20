using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings
{
    using BuildCompression = UnityEngine.BuildCompression;

    /// <summary>
    /// Build settings for addressables.
    /// </summary>
    [Serializable]
    public class AddressableAssetBuildSettings
    {
        [FormerlySerializedAs("m_logResourceManagerExceptions")]
        [SerializeField]
        bool m_LogResourceManagerExceptions = true;

        /// <summary>
        /// When enabled, the Addressables.ResourceManager.ExceptionHandler is set to (op, ex) => Debug.LogException(ex);
        /// </summary>
        public bool LogResourceManagerExceptions
        {
            get { return m_LogResourceManagerExceptions; }
            set
            {
                if (m_LogResourceManagerExceptions != value)
                {
                    m_LogResourceManagerExceptions = value;
                    SetDirty();
                }
            }
        }

        /// <summary>
        /// //Specifies where to build asset bundles, this is usually a temporary folder (or a folder in the project).  Bundles are copied out of this location to their final destination.
        /// </summary>
        public string bundleBuildPath
        {
            get { return m_BundleBuildPath; }
            set
            {
                m_BundleBuildPath = value;
                SetDirty();
            }
        }

        [FormerlySerializedAs("m_bundleBuildPath")]
        [SerializeField]
        string m_BundleBuildPath = "Temp/com.unity.addressables/AssetBundles";

        Hash128 m_CurrentHash;
        internal Hash128 currentHash
        {
            get
            {
                if (!m_CurrentHash.isValid)
                    HashUtilities.ComputeHash128(ref m_LogResourceManagerExceptions, ref m_CurrentHash);
                return m_CurrentHash;
            }
        }

        [NonSerialized]
        AddressableAssetSettings m_Settings;

        void SetDirty()
        {
            m_CurrentHash = default;
            if (m_Settings != null)
                m_Settings.SetDirty(AddressableAssetSettings.ModificationEvent.BuildSettingsChanged, this, true, false);
        }

        internal void OnAfterDeserialize(AddressableAssetSettings settings)
        {
            m_Settings = settings;
        }

        internal void Validate(AddressableAssetSettings addressableAssetSettings)
        {
        }
    }
}
