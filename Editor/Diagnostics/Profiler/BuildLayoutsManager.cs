#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER

using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.Profiling;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal class BuildLayoutsManager
    {
        private class ActiveLayout
        {
            public Hash128 BuildResultHash;

            private readonly Dictionary<int, BuildLayout.Bundle> m_BundleMap = new Dictionary<int, BuildLayout.Bundle>();
            private readonly Dictionary<int, Dictionary<int,BuildLayout.ExplicitAsset>> m_BundleAssetsMap = new Dictionary<int, Dictionary<int, BuildLayout.ExplicitAsset>>();

            public ActiveLayout(BuildLayout layout)
            {
                layout.ReadFull();
                foreach (BuildLayout.Bundle bundle in BuildLayoutHelpers.EnumerateBundles(layout))
                {
                    int bundleCode = bundle.InternalName.GetHashCode();
                    m_BundleMap.Add(bundleCode, bundle);
                    Dictionary<int, BuildLayout.ExplicitAsset> assetMap = new Dictionary<int, BuildLayout.ExplicitAsset>(bundle.AssetCount);
                    foreach (BuildLayout.ExplicitAsset asset in BuildLayoutHelpers.EnumerateAssets(bundle))
                    {
                        int assetCode = asset.InternalId.GetHashCode();
                        assetMap[assetCode] = asset;
                    }

                    m_BundleAssetsMap[bundleCode] = assetMap;
                }
            }

            public BuildLayout.Bundle GetBundle(int bundleCode)
            {
                if (!m_BundleMap.TryGetValue(bundleCode, out BuildLayout.Bundle value))
                    return null;
                return value;
            }

            public BuildLayout.ExplicitAsset GetAsset(int bundleCode, int assetCode)
            {
                if (!m_BundleAssetsMap.TryGetValue(bundleCode, out Dictionary<int, BuildLayout.ExplicitAsset> assetMap))
                    return null;
                if (!assetMap.TryGetValue(assetCode, out BuildLayout.ExplicitAsset value))
                    return null;
                return value;
            }
        }

        private Dictionary<Hash128, BuildLayout> m_BuildLayouts = new Dictionary<Hash128, BuildLayout>();
        private readonly List<ActiveLayout> m_ActiveLayouts = new List<ActiveLayout>();

        public void LoadReports()
        {
            m_BuildLayouts.Clear();
            m_ActiveLayouts.Clear();

            if (Directory.Exists(Addressables.BuildReportPath))
            {
                foreach (string file in Directory.EnumerateFiles(Addressables.BuildReportPath, "*.json", SearchOption.TopDirectoryOnly))
                {
                    if (!TryLoadLayoutAtPath(file, out BuildLayout layout))
                        continue;

                    Hash128 buildHash = Hash128.Parse(layout.BuildResultHash);
                    if (m_BuildLayouts.TryGetValue(buildHash, out var other))
                    {
                        // doesn't really matter, just means multiple with the same results exist, get the latest anyway
                        int comp = layout.Header.BuildStart.CompareTo(other.Header.BuildStart);
                        if (comp > 0)
                        {
                            m_BuildLayouts[buildHash].Close();
                            m_BuildLayouts[buildHash] = layout;
                        }
                    }
                    else
                        m_BuildLayouts.Add(buildHash, layout);
                }
            }
        }

        public bool LoadManualReport(string path)
        {
            if (!TryLoadLayoutAtPath(path, out BuildLayout layout, true))
                return false;

            // set a save for the build hash
            EditorPrefs.SetString("com.unity.addressabes.reportFilePath_" + layout.BuildResultHash, path);
            m_BuildLayouts[Hash128.Parse(layout.BuildResultHash)] = layout;
            return true;
        }

        private bool TryLoadLayoutAtPath(string path, out BuildLayout layoutOut, bool logErrors = false)
        {
            layoutOut = BuildLayout.Open(path);
            if (layoutOut == null)
                return false;
            if (!layoutOut.ReadHeader())
                return false;
            if (!string.IsNullOrEmpty(layoutOut.Header.BuildError))
                return false;
            Hash128 hash = Hash128.Parse(layoutOut.BuildResultHash);
            if (!hash.isValid)
            {
                if (logErrors)
                    Debug.LogError($"Could not load build report at {path}. Missing BuildResultHash");
                return false;
            }
            return true;
        }

        public HashSet<Hash128> SetActiveReportsAndGetMissingBuildHashes(NativeArray<CatalogFrameData> loadedRuntimeCatalogs)
        {
            HashSet<ActiveLayout> oldActives = new HashSet<ActiveLayout>(m_ActiveLayouts);
            HashSet<Hash128> missingBuildHashes = new HashSet<Hash128>();
            for (int i = 0; i < loadedRuntimeCatalogs.Length; ++i)
            {
                var recordedHash = loadedRuntimeCatalogs[i].BuildResultHash;
                var layout = m_ActiveLayouts.Find(activeLayout => activeLayout.BuildResultHash == recordedHash);
                if (layout != null)
                {
                    oldActives.Remove(layout);
                    continue;
                }

                if (m_BuildLayouts.TryGetValue(recordedHash, out BuildLayout newActiveLayout))
                    m_ActiveLayouts.Add(new ActiveLayout(newActiveLayout));
                else
                {
                    // try get from saved manual path
                    BuildLayout buildLayout = null;
                    if (EditorPrefs.HasKey("com.unity.addressabes.reportFilePath_" + recordedHash.ToString()))
                    {
                        string path = EditorPrefs.GetString("com.unity.addressabes.reportFilePath_" + recordedHash.ToString());
                        if (TryLoadLayoutAtPath(path, out buildLayout))
                        {
                            m_BuildLayouts[Hash128.Parse(buildLayout.BuildResultHash)] = buildLayout;
                            m_ActiveLayouts.Add(new ActiveLayout(buildLayout));
                        }
                    }
                    if (buildLayout == null)
                        missingBuildHashes.Add(recordedHash);
                }
            }

            foreach (ActiveLayout oldActive in oldActives)
                m_ActiveLayouts.Remove(oldActive);

            if (missingBuildHashes.Count == 0)
                return null;
            return missingBuildHashes;
        }

        public BuildLayout.Bundle GetBundle(int bundleCode)
        {
            BuildLayout.Bundle value;
            foreach (ActiveLayout activeLayout in m_ActiveLayouts)
            {
                value = activeLayout.GetBundle(bundleCode);
                if (value != null)
                    return value;
            }

            return null;
        }

        public BuildLayout.ExplicitAsset GetAsset(int bundleCode, int assetCode)
        {
            BuildLayout.ExplicitAsset value = null;
            foreach (ActiveLayout activeLayout in m_ActiveLayouts)
            {
                value = activeLayout.GetAsset(bundleCode, assetCode);
                if (value != null)
                {
                    // this should be fixed in layout gen bug fix
                    // keeping bundle assignment to handle any previous data
                    if (value.Bundle == null)
                        value.Bundle = value.File.Bundle;
                    return value;
                }
            }

            return null;
        }
    }
}

#endif
