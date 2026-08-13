using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Input rule that aggregates assets selected directly or via JSON lists.
    /// </summary>
    [CreateAssetMenu(menuName = Constants.ContextMenus.InputRulesMenu + nameof(AssetSelectionInputRule))]
    public class AssetSelectionInputRule : InputRule
    {
        #region Fields
        /// <summary>
        /// Assets explicitly selected for inclusion in the addressables groups.
        /// </summary>
        [SerializeField]
        public List<UnityEngine.Object> m_SelectedAssets = new List<UnityEngine.Object>();

        /// <summary>
        /// JSON list of assets containing additional asset paths.
        /// </summary>
        [SerializeField]
        public List<TextAsset> m_JsonAssetLists = new List<TextAsset>();

        /// <summary>
        /// When true, include the current addressable assets in the input list.
        /// </summary>
        [SerializeField]
        public bool m_IncludeCurrentAddressables = false;
        #endregion

        #region Helper DTO
        [Serializable]
        private class StringArrayWrapper
        {
            public List<string> items;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Returns a set of asset paths based on selected assets and JSON lists.
        /// </summary>
        /// <returns>Asset paths to include.</returns>
        public override HashSet<string> GetIncludedAssets()
        {
            var result = new HashSet<string>(StringComparer.Ordinal);

            foreach (var asset in m_SelectedAssets)
            {
                if (asset == null) continue;
                string path = AssetDatabase.GetAssetPath(asset);
                if (!string.IsNullOrEmpty(path)) result.Add(path);
            }

            foreach (var jsonTextAsset in m_JsonAssetLists)
            {
                if (jsonTextAsset == null)
                    continue;

                try
                {
                    string raw = jsonTextAsset.text;
                    if (string.IsNullOrWhiteSpace(raw))
                        continue;

                    string wrapped = raw.TrimStart().StartsWith("[")
                        ? $"{{\"items\":{raw}}}"
                        : raw;

                    var parsed = JsonUtility.FromJson<StringArrayWrapper>(wrapped);

                    if (parsed?.items == null)
                        continue;

                    foreach (var p in parsed.items)
                    {
                        string path = (p ?? string.Empty).Trim();
                        if (string.IsNullOrEmpty(path))
                            continue;

                        var loaded = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                        if (loaded == null) continue;

                        if (AssetDatabase.IsValidFolder(path)) continue;

                        result.Add(path);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"[AutoGroupGenerator] Invalid JSON in '{jsonTextAsset.name}'. " +
                        $"Expected a simple array of asset paths (e.g. [\"Assets/X.prefab\"]). " +
                        $"This file will be skipped. Error: {ex.Message}",
                        jsonTextAsset
                    );
                }
            }

            if (m_IncludeCurrentAddressables)
            {
                result.UnionWith(AddressableUtil.GetExtendedAddressableEntries());
            }

            return result;
        }
        #endregion
    }
}
