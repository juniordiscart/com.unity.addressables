using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Helpers for querying the Unity asset database.
    /// </summary>
    public static class AssetDatabaseUtil
    {
        /// <summary>
        /// Finds and loads assets of the specified type.
        /// </summary>
        /// <typeparam name="T">Asset type to search for.</typeparam>
        /// <returns>Loaded assets of the requested type.</returns>
        public static List<T> FindAssetsOfType<T>() where T : UnityEngine.Object
        {
            var guids = FindAssetGuidsForType<T>();
            var assets = new List<T>(guids.Length);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                T asset = AssetDatabase.LoadAssetAtPath<T>(path);

                if (asset != null)
                {
                    assets.Add(asset);
                }
            }

            return assets;
        }

        /// <summary>
        /// Finds asset paths for assets of the specified type.
        /// </summary>
        /// <typeparam name="T">Asset type to search for.</typeparam>
        /// <returns>Project-relative asset paths.</returns>
        public static List<string> FindAssetPathsForType<T>() where T : UnityEngine.Object
        {
            var guids = FindAssetGuidsForType<T>();

            return guids.Select(AssetDatabase.GUIDToAssetPath).ToList();
        }

        /// <summary>
        /// Finds asset GUIDs for assets of the specified type.
        /// </summary>
        /// <typeparam name="T">Asset type to search for.</typeparam>
        /// <returns>Array of GUID strings.</returns>
        public static string[] FindAssetGuidsForType<T>() where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        }
    }
}
