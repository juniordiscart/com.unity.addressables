using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Pool;

namespace UnityEditor.AddressableAssets.Build
{
    /// <summary>
    /// Use to contain files created during a build.
    /// </summary>
    public class FileRegistry
    {
        private readonly HashSet<string> m_FilePaths;

        // Stem -> full paths: O(1) lookup instead of the O(n^2) substring scans on thousands of bundles.
        private readonly Dictionary<string, HashSet<string>> m_FileNameToPaths = new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// Initializes a new file registry instance.
        /// </summary>
        public FileRegistry()
        {
            m_FilePaths = new HashSet<string>();
        }

        /// <summary>
        /// Retrieves all the stored file paths.
        /// </summary>
        /// <returns>Returns all file paths as an IEnumerable.</returns>
        public IEnumerable<string> GetFilePaths()
        {
            return new HashSet<string>(m_FilePaths);
        }

        /// <summary>
        /// Adds a file path to our set of file paths.
        /// </summary>
        /// <param name="path">The file path.</param>
        public void AddFile(string path)
        {
            if (m_FilePaths.Add(path))
            {
                var key = Path.GetFileNameWithoutExtension(path);
                if (!m_FileNameToPaths.TryGetValue(key, out var set))
                    m_FileNameToPaths[key] = set = new HashSet<string>();

                set.Add(path);
            }
        }

        /// <summary>
        /// Removes a file path from our set of file paths.
        /// </summary>
        /// <param name="path">The file path.</param>
        public void RemoveFile(string path)
        {
            if (m_FilePaths.Remove(path))
            {
                var key = Path.GetFileNameWithoutExtension(path);
                if (m_FileNameToPaths.TryGetValue(key, out var set))
                {
                    set.Remove(path);
                    if (set.Count == 0)
                        m_FileNameToPaths.Remove(key);
                }
            }
        }

        /// <summary>
        /// Given a bundle name, determine the file path for the bundle.
        /// </summary>
        /// <param name="bundleName">The name of the bundle.</param>
        /// <returns>The full file path. If several files share a stem, which one is returned is arbitrary</returns>
        public string GetFilePathForBundle(string bundleName)
        {
            bundleName = Path.GetFileNameWithoutExtension(bundleName);

            // Try bucket first
            if (m_FileNameToPaths.TryGetValue(bundleName, out var set))
                foreach (var path in set)
                    return path;

            // Fallback: substring match ("catalog" -> "catalog_<hash>.json").
            foreach (var entry in m_FilePaths)
                if (AddressableAssetUtility.StringContains(entry, bundleName, StringComparison.Ordinal))
                    return entry;

            return null;
        }

        /// <summary>
        /// Replace an entry in the File Registry with a new bundle name.
        /// </summary>
        /// <param name="bundleName">The bundle name to replace.</param>
        /// <param name="newFileRegistryEntry">The new file registry bundle name.</param>
        /// <returns>Returns true if a successful replacement occured.</returns>
        public bool ReplaceBundleEntry(string bundleName, string newFileRegistryEntry)
        {
            if (m_FilePaths.Contains(newFileRegistryEntry))
                return false;

            // Filter the stem bucket by the full bundleName: siblings share a stem (catalog.json vs
            // catalog.hash), so dumping it whole would wrongly delete them. Scan if the bucket misses.
            if (m_FilePaths.Contains(bundleName))
            {
                RemoveFile(bundleName);
            }
            else
            {
                using var _ = ListPool<string>.Get(out var toRemove);

                if (m_FileNameToPaths.TryGetValue(Path.GetFileNameWithoutExtension(bundleName), out var set))
                    foreach (var path in set)
                        if (AddressableAssetUtility.StringContains(path, bundleName, StringComparison.Ordinal))
                            toRemove.Add(path);

                if (toRemove.Count == 0)
                    foreach (var entry in m_FilePaths)
                        if (AddressableAssetUtility.StringContains(entry, bundleName, StringComparison.Ordinal))
                            toRemove.Add(entry);

                foreach (var path in toRemove)
                    RemoveFile(path);

            }

            // Original contract: always add the new entry and return true (caller's error log keys off this).
            AddFile(newFileRegistryEntry);
            return true;
        }

        /// <summary>
        /// Writes binary content to <paramref name="path"/> on disk and registers the path with
        /// this registry. Creates any intermediate directories as needed.
        /// </summary>
        /// <param name="path">Destination file path.</param>
        /// <param name="content">Binary content to write.</param>
        /// <returns><c>true</c> on success; <c>false</c> if an exception was thrown (logged via <see cref="UnityEngine.Debug"/>).</returns>
        public bool WriteAndAddFile(string path, byte[] content)
        {
            try
            {
                AddFile(path);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllBytes(path, content);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                RemoveFile(path);
                return false;
            }

        }

        /// <summary>
        /// Writes text content to <paramref name="path"/> on disk and registers the path with
        /// this registry. Creates any intermediate directories as needed.
        /// </summary>
        /// <param name="path">Destination file path.</param>
        /// <param name="content">Text content to write.</param>
        /// <returns><c>true</c> on success; <c>false</c> if an exception was thrown (logged via <see cref="UnityEngine.Debug"/>).</returns>
        public bool WriteAndAddFile(string path, string content)
        {
            try
            {
                AddFile(path);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(path, content);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                RemoveFile(path);
                return false;
            }
        }

    }
}
