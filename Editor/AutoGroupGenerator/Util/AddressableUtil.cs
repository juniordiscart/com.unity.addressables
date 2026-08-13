using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Utility helpers for interacting with the Addressables configuration.
    /// </summary>
    public static class AddressableUtil
    {
        #region Static Methods
        /// <summary>
        /// Finds the default Addressable group template from the project settings.
        /// </summary>
        /// <returns>The default group template.</returns>
        public static AddressableAssetGroupTemplate FindDefaultAddressableGroupTemplate()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                throw new("AddressableAssetSettings not found. Ensure Addressables are initialized.");
            }


            List<ScriptableObject> templates = settings.GroupTemplateObjects;

            if (templates == null || templates.Count == 0)
            {
                throw new("No group templates found in Addressable Settings.");
            }


            var defaultTemplate = templates[0];

            return defaultTemplate as AddressableAssetGroupTemplate;
        }

        /// <summary>
        /// Gets all Addressable asset entries from non-read-only groups.
        /// </summary>
        /// <returns>A list of addressable asset entries.</returns>
        public static List<AddressableAssetEntry> GetAddressableEntries()
        {
            var entries = new List<AddressableAssetEntry>();

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                return entries;
            }


            foreach (var group in settings.groups.Where(group => !group.ReadOnly))
            {
                if (group == null)
                    continue;

                entries.AddRange(group.entries);
            }

            return entries;
        }

        /// <summary>
        /// Gets addressable asset paths, expanding folders into their contained assets.
        /// </summary>
        /// <param name="includeFolderEntries">If true, include folder paths in the result.</param>
        /// <returns>A set of asset paths referenced by Addressables.</returns>
        public static HashSet<string> GetExtendedAddressableEntries(bool includeFolderEntries = false)
        {
            var result = new HashSet<string>();

            foreach (var entry in GetAddressableEntries())
            {
                if (entry == null)
                    continue;

                string entryAssetPath = entry.AssetPath;
                if (string.IsNullOrEmpty(entryAssetPath))
                    continue;

                bool isFolder = AssetDatabase.IsValidFolder(entryAssetPath);

                if (isFolder)
                {
                    if (includeFolderEntries)
                        result.Add(entryAssetPath);

                    var guidsUnderFolder = AssetDatabase.FindAssets(string.Empty, new[] { entryAssetPath });
                    foreach (var guid in guidsUnderFolder)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                        if (string.IsNullOrEmpty(assetPath))
                            continue;

                        if (AssetDatabase.IsValidFolder(assetPath))
                            continue;

                        result.Add(assetPath);
                    }
                }
                else
                {
                    result.Add(entryAssetPath);
                }
            }

            return result;
        }
        #endregion
    }
}
