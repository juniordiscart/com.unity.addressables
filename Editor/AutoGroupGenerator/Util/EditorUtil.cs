using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Utility methods for editor-specific operations.
    /// </summary>
    public static class EditorUtil
    {
        /// <summary>
        /// Reveals the persistent data folder in the OS file explorer.
        /// </summary>
        public static void LocatePersistentDataFolder()
        {
            try
            {
                var fullPath = Path.GetFullPath(Constants.FilePaths.PersistentDataFolder);
                FileUtils.EnsureDirectoryExist(fullPath);
                EditorUtility.RevealInFinder(fullPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"{e.Message}");
            }
        }

        /// <summary>
        /// Forces a garbage collection and unloads unused editor assets.
        /// </summary>
        public static void UnloadUnusedEditorMemory()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                EditorUtility.UnloadUnusedAssetsImmediate(true);
            }
            catch { /* best-effort */ }
        }
    }
}
