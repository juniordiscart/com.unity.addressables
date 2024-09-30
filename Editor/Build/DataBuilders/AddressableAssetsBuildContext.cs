using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    /// <summary>
    /// Interface for any Addressables specific context objects to be used in the Scriptable Build Pipeline context store
    /// </summary>
    public interface IAddressableAssetsBuildContext : IContextObject
    {
    }

    /// <summary>
    /// Simple context object for passing data through SBP, between different sections of Addressables code.
    /// </summary>
    public class AddressableAssetsBuildContext : IAddressableAssetsBuildContext
    {
        private AddressableAssetSettings m_Settings;

        /// <summary>
        /// The settings object to use.
        /// </summary>
        [Obsolete("Use Settings property instead.")]
        public AddressableAssetSettings settings;

        /// <summary>
        /// The settings object to use.
        /// </summary>
        public AddressableAssetSettings Settings
        {
            get
            {
                if (m_Settings == null && !string.IsNullOrEmpty(m_SettingsAssetPath))
                    m_Settings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(m_SettingsAssetPath);
                return m_Settings;
            }
            set
            {
                m_Settings = value;
                string guid;
                if (m_Settings != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_Settings, out guid, out long localId))
                    m_SettingsAssetPath = AssetDatabase.GUIDToAssetPath(guid);
                else
                    m_SettingsAssetPath = null;
            }
        }

        private string m_SettingsAssetPath;

        /// <summary>
        /// The time the build started
        /// </summary>
        public DateTime buildStartTime;

        /// <summary>
        /// The current runtime data being built.
        /// </summary>
        public ResourceManagerRuntimeData runtimeData;

        /// <summary>
        /// The list of catalog locations.
        /// </summary>
        public List<ContentCatalogDataEntry> locations;

        /// <summary>
        /// Mapping of bundles to asset groups.
        /// </summary>
        public Dictionary<string, string> bundleToAssetGroup;

        /// <summary>
        /// Mapping of asset group to bundles.
        /// </summary>
        public Dictionary<AddressableAssetGroup, List<string>> assetGroupToBundles;

        /// <summary>
        /// Set of provider types needed in this build.
        /// </summary>
        public HashSet<Type> providerTypes;

        /// <summary>
        /// The list of all AddressableAssetEntry objects.
        /// </summary>
        public List<AddressableAssetEntry> assetEntries;

        /// <summary>
        /// Mapping of AssetBundle to the direct dependencies.
        /// </summary>
        public Dictionary<string, List<string>> bundleToImmediateBundleDependencies;

        /// <summary>
        /// A mapping of AssetBundle to the full dependency tree, flattened into a single list.
        /// </summary>
        public Dictionary<string, List<string>> bundleToExpandedBundleDependencies;

        /// <summary>
        /// A mapping of Asset GUID's to resulting ContentCatalogDataEntry entries.
        /// </summary>
        internal Dictionary<GUID, List<ContentCatalogDataEntry>> GuidToCatalogLocation = null;

        private Dictionary<string, List<ContentCatalogDataEntry>> m_PrimaryKeyToDependers = null;

        internal Dictionary<string, List<ContentCatalogDataEntry>> PrimaryKeyToDependerLocations
        {
            get
            {
                if (m_PrimaryKeyToDependers != null)
                    return m_PrimaryKeyToDependers;
                if (locations == null || locations.Count == 0)
                {
                    Debug.LogError("Attempting to get Entries dependent on key, but currently no locations");
                    return new Dictionary<string, List<ContentCatalogDataEntry>>(0);
                }

                m_PrimaryKeyToDependers = new Dictionary<string, List<ContentCatalogDataEntry>>(locations.Count);
                foreach (ContentCatalogDataEntry location in locations)
                {
                    for (int i = 0; i < location.Dependencies.Count; ++i)
                    {
                        string dependencyKey = location.Dependencies[i] as string;
                        if (string.IsNullOrEmpty(dependencyKey))
                            continue;

                        if (!m_PrimaryKeyToDependers.TryGetValue(dependencyKey, out var dependers))
                        {
                            dependers = new List<ContentCatalogDataEntry>();
                            m_PrimaryKeyToDependers.Add(dependencyKey, dependers);
                        }

                        dependers.Add(location);
                    }
                }

                return m_PrimaryKeyToDependers;
            }
        }

        private Dictionary<string, ContentCatalogDataEntry> m_PrimaryKeyToLocation = null;

        internal Dictionary<string, ContentCatalogDataEntry> PrimaryKeyToLocation
        {
            get
            {
                if (m_PrimaryKeyToLocation != null)
                    return m_PrimaryKeyToLocation;
                if (locations == null || locations.Count == 0)
                {
                    Debug.LogError("Attempting to get Primary key to entries dependent on key, but currently no locations");
                    return new Dictionary<string, ContentCatalogDataEntry>();
                }

                m_PrimaryKeyToLocation = new Dictionary<string, ContentCatalogDataEntry>();
                foreach (var loc in locations)
                {
                    if (loc != null && loc.Keys[0] != null && loc.Keys[0] is string && !m_PrimaryKeyToLocation.ContainsKey((string)loc.Keys[0]))
                        m_PrimaryKeyToLocation[(string)loc.Keys[0]] = loc;
                }

                return m_PrimaryKeyToLocation;
            }
        }
    }
}
