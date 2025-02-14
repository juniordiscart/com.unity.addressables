using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Settings.GroupSchemas
{
    /// <summary>
    /// Settings used for sorting Addressables groups
    /// </summary>
    public class AddressableAssetGroupSortSettings : ScriptableObject
    {
        const string DEFAULT_PATH = "Assets/AddressableAssetsData";
        const string DEFAULT_NAME = "AddressableAssetGroupSortSettings";
        static string DEFAULT_SETTING_PATH = $"{DEFAULT_PATH}/{DEFAULT_NAME}.asset";

        /// <summary>
        /// The group sort order
        /// </summary>
        [SerializeField]
        public string[] sortOrder;

        /// <summary>
        /// Creates, if needed, and returns the asset group sort settings for the project
        /// </summary>
        /// <param name="path">Desired path to put settings</param>
        /// <param name="settingName">Desired name for settings</param>
        /// <returns>scriptable object with sort settings</returns>
        public static AddressableAssetGroupSortSettings Create(string path = null, string settingName = null)
        {
            AddressableAssetGroupSortSettings settings;
            var assetPath = DEFAULT_SETTING_PATH;

            if (path != null && settingName != null)
            {
                assetPath = $"{path}/{settingName}.asset";
            }

            settings = AssetDatabase.LoadAssetAtPath<AddressableAssetGroupSortSettings>(assetPath);
            if (settings == null)
            {
                Directory.CreateDirectory(path != null ? path : DEFAULT_PATH);
                settings = CreateInstance<AddressableAssetGroupSortSettings>();
                AssetDatabase.CreateAsset(settings, assetPath);
                settings = AssetDatabase.LoadAssetAtPath<AddressableAssetGroupSortSettings>(assetPath);
                settings.sortOrder = Array.Empty<string>();
                EditorUtility.SetDirty(settings);
            }

            return settings;
        }

        /// <summary>
        /// Gets the asset group sort settings for the project
        /// </summary>
        /// <param name="settings">AddressableAssetSettings object</param>
        /// <returns>scriptable object with sort settings</returns>
        public static AddressableAssetGroupSortSettings GetSettings(AddressableAssetSettings settings)
        {
            return AddressableAssetGroupSortSettings.GetSettings(settings?.ConfigFolder, nameof(AddressableAssetGroupSortSettings));
        }

        /// <summary>
        /// Gets the asset group sort settings for the project
        /// </summary>
        /// <param name="path">Desired path to put settings</param>
        /// <param name="settingName">Desired name for settings</param>
        /// <returns>scriptable object with sort settings</returns>
        public static AddressableAssetGroupSortSettings GetSettings(string path = null, string settingName = null)
        {
            AddressableAssetGroupSortSettings settings;
            var assetPath = DEFAULT_SETTING_PATH;

            if (path != null && settingName != null)
            {
                assetPath = $"{path}/{settingName}.asset";
            }

            settings = AssetDatabase.LoadAssetAtPath<AddressableAssetGroupSortSettings>(assetPath);
            if (settings == null)
                return Create(path, settingName);
            return settings;
        }
    }
}
