using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets.BuildReportVisualizer;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Progress;

/// <summary>
/// Helper object that hooks into EditorApplication update to lazy load icon assets
/// </summary>
internal class IconLazyLoad
{
    /// <summary>
    /// Represents a request to load an icon for either a TreeViewItem or an Image element
    /// </summary>
    internal struct IconRequest
    {
        public AssetEntryTreeViewItem TreeViewItem
        {
            get => m_TreeViewItem;
        }

        AssetEntryTreeViewItem m_TreeViewItem;
        Image m_Icon;
        string m_AssetPath;

        /// <summary>
        /// Creates an icon request for a UI Image element
        /// </summary>
        /// <param name="icon">The Image element to set the icon on</param>
        /// <param name="assetPath">Path to the asset whose icon should be loaded</param>
        public IconRequest(Image icon, string assetPath)
        {
            m_TreeViewItem = null;
            m_Icon = icon;
            m_AssetPath = assetPath;
        }

        /// <summary>
        /// Creates an icon request for a TreeViewItem
        /// </summary>
        /// <param name="treeViewItem">The TreeViewItem to set the icon on</param>
        /// <param name="assetPath">Path to the asset whose icon should be loaded</param>
        public IconRequest(AssetEntryTreeViewItem treeViewItem, string assetPath)
        {
            m_TreeViewItem = treeViewItem;
            m_Icon = null;
            m_AssetPath = assetPath;
        }

        /// <summary>
        /// Executes the icon load request, setting the icon on either the Image or TreeViewItem
        /// </summary>
        public void Execute()
        {
            if(m_Icon != null)
                m_Icon.image = BuildReportUtility.GetIcon(m_AssetPath);
            if(m_TreeViewItem != null)
                m_TreeViewItem.assetIcon = BuildReportUtility.GetIcon(m_AssetPath) as Texture2D;
        }
    }

    internal static class PrefabIcons
    {
        private static Texture s_prefabIcon;
        private static Texture s_prefabVariantIcon;

        /// <summary>
        /// Determine if a prefab is a Prefab Variant, without loading it as a full object
        /// Useful for cases where a user may have a large prefab, which shouldn't be loaded just to determine icons
        /// </summary>
        /// <param name="path">Path to the prefab</param>
        /// <returns>
        /// True if the prefab is a Prefab Variant.
        /// False if the path is not a prefab or a variant
        /// </returns>
        internal static bool IsPrefabVariant(string path)
        {
            if (path == null)
                return false;
            if (!path.EndsWith(".prefab"))
                return false;
            // See comment below about this being the best way to do this without incurring a full deserialization (expensive)
            using var reader = new StreamReader(path);
            // Skip to line 3
            for (var i = 0; i < 3; i++)
            {
                if (reader.ReadLine() == null)
                    return false;
            }
            var prefabType = reader.ReadLine();
            return prefabType != null && prefabType.StartsWith("PrefabInstance");
        }

        /// <summary>
        /// Gets the appropriate icon for a prefab, checking if it's a variant and returning the correct icon
        /// </summary>
        /// <param name="prefabPath">Path to the prefab asset</param>
        /// <returns>The prefab icon texture, or null if the path is invalid</returns>
        public static Texture2D PrefabIcon(string prefabPath)
        {
            if (!prefabPath.EndsWith(".prefab"))
            {
                Debug.LogError($"Attempting to get a prefab icon for an asset that isn't a prefab. Path provided is not a prefab: {prefabPath}.");
                return null;
            }

            if (s_prefabIcon)
                return s_prefabIcon as Texture2D;

            // Unfortunately we need to perform a file read, to ensure we don't pollute this field
            if (IsPrefabVariant(prefabPath))
            {
                return PrefabVariantIcon(prefabPath);
            }

            s_prefabIcon = AssetDatabase.GetCachedIcon(prefabPath);
            return s_prefabIcon as Texture2D;
        }

        /// <summary>
        /// Gets the prefab variant icon for a prefab, checking if it's a regular prefab and returning the correct icon
        /// </summary>
        /// <param name="prefabPath">Path to the prefab asset</param>
        /// <returns>The prefab variant icon texture, or null if the path is invalid</returns>
        public static Texture2D PrefabVariantIcon(string prefabPath)
        {
            if (!prefabPath.EndsWith(".prefab"))
            {
                Debug.LogError($"Attempting to get a prefab icon for an asset that isn't a prefab. Path provided is not a prefab: {prefabPath}.");
                return null;
            }

            if (s_prefabVariantIcon)
                return s_prefabVariantIcon as Texture2D;
            // Unfortunately we need to perform a file read, to ensure we don't pollute this field
            if (!IsPrefabVariant(prefabPath))
            {
                return PrefabIcon(prefabPath);
            }
            s_prefabVariantIcon = AssetDatabase.GetCachedIcon(prefabPath);
            return s_prefabVariantIcon as Texture2D;
        }
    }
    // Ensure we don't tank Editor performance when loading icons lazily
    private const double k_timeBudget = 0.008f;
    private const int k_maxFileReadsBeforePushingAsync = 64;

    private readonly Queue<IconRequest> m_needsIconRefresh = new();
    private readonly Queue<IconRequest> m_needsIconRefreshPriority = new();

    private readonly List<AssetEntryTreeViewItem> m_reloadWhenCacheInvalidated = new();
    private bool m_editorHooked;
    private int m_editorProgressId = -1;
    private int m_progressLength = -1;
    private int m_lastTempRequestFrame;
    private int m_currentFileReads;

    // Texture Caches
    private static Texture s_prefabIcon;
    private readonly Dictionary<string, Texture> m_scriptGuidToIconCache = new();

    /// <summary>
    /// Assigns a temporary interim texture for <paramref name="item"/>
    /// This will also register <paramref name="item"/> internally, such that it's true icon will be loaded later
    /// Does nothing if <paramref name="item"/> does not represent an asset.
    /// </summary>
    /// <param name="item">Tree item that needs to be registered</param>
    internal void LoadIconLazy(AssetEntryTreeViewItem item)
    {
        if (item.entry == null)
            return;
        if (Time.frameCount != m_lastTempRequestFrame)
        {
            m_lastTempRequestFrame = Time.frameCount;
            m_currentFileReads = 0;
        }

        if (m_currentFileReads < k_maxFileReadsBeforePushingAsync)
        {
            item.assetIcon = GetBestTempIcon(item.entry, true);
            m_currentFileReads++;
        }
        else
        {
            item.assetIcon = GetBestTempIcon(item.entry);
        }
        AddLazyIconLoadCallback();
        m_needsIconRefresh.Enqueue(new IconRequest(item, item.entry.AssetPath));
    }

    /// <summary>
    /// Registers an Image element to have its icon loaded lazily from the specified asset path
    /// This overload is used for build report UI where Image elements need icons loaded
    /// </summary>
    /// <param name="icon">The Image element to set the icon on</param>
    /// <param name="assetPath">Path to the asset whose icon should be loaded</param>
    internal void LoadIconLazy(Image icon, string assetPath)
    {
        if(icon == null)
            return;
        AddLazyIconLoadCallback();
        m_needsIconRefreshPriority.Enqueue(new IconRequest(icon, assetPath));
    }

    /// <summary>
    /// Ensure a registered item is loaded first. Intended for UX where the real icon must be shown
    /// (e.g. when a user clicks on a row, it should load it's real icon first)
    /// <seealso cref="LoadIconLazy"/>
    /// </summary>
    internal void LoadIconLazyPriority(AssetEntryTreeViewItem item)
    {
        if (item.entry == null)
            return;
        // No attempt is made to GetBestTempIcon, as this is only for prioritisation
        AddLazyIconLoadCallback();
        m_needsIconRefreshPriority.Enqueue(new IconRequest(item, item.entry.AssetPath));
    }

    /// <summary>
    /// Fast retrieval of a MonoBehaviour (e.g. ScriptableObject's) GUID from the beginning of it's asset
    /// Removes need to fully deserialize to deduce type information
    /// </summary>
    internal static string GetTypeGuidFromAsset(string assetPath)
    {
        if (assetPath == null)
            return null;
        if (!assetPath.EndsWith(".asset"))
            return null;

        // We need to do this to prevent the performance cost of deserializing the entire asset
        // Unfortunately this is the best way to do it
        using var reader = new StreamReader(assetPath);
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine()!.Trim();
            // Parse the line and attempt to extract a valid GUID
            if (!line.StartsWith("m_Script")) continue;
            var guidMatch = Regex.Match(line, "guid: .+,", RegexOptions.Compiled);
            // If we did not extract a valid GUID, we should get out now and let AssetDatabase do this properly...
            if (!guidMatch.Success) return null;
            var guidStr = guidMatch.Value.Replace("guid: ", "").Replace(",", "");
            return guidStr;
        }

        return null;
    }

    /// <summary>
    /// Get a useful icon for <paramref name="entry"/> without causing a full deserialization of the asset
    /// This is unlike <seealso cref="AssetDatabase.GetCachedIcon"/> which will deserialize an asset if the icon is not cached
    /// </summary>
    /// <param name="entry">Entry to get an icon for</param>
    /// <param name="performFileReads">
    /// Whether we should perform disk reads in order to determine the icon.
    /// Slightly slower, but is required to determine ScriptableObject icons and if a Prefab is a Variant
    /// </param>
    /// <returns></returns>
    private Texture2D FastIconFromPath(AddressableAssetEntry entry, bool performFileReads = false)
    {
        if (entry.AssetPath == null)
            return null;
        if (!File.Exists(entry.AssetPath))
            return null;
        var isPrefab = entry.AssetPath.EndsWith(".prefab");
        var isAsset = entry.AssetPath.EndsWith(".asset");

        if (!performFileReads)
        {
            if (isPrefab)
            {
                return PrefabIcons.PrefabIcon(entry.AssetPath);
            }

            return null;
        }

        if (isAsset)
        {
            var guid = GetTypeGuidFromAsset(entry.AssetPath);
            if (guid == null) return null;
            if (!m_scriptGuidToIconCache.ContainsKey(guid))
            {
                m_scriptGuidToIconCache[guid] = AssetDatabase.GetCachedIcon(entry.AssetPath);
            }
            return m_scriptGuidToIconCache[guid] as Texture2D;
        }
        if (isPrefab)
        {
            if (PrefabIcons.IsPrefabVariant(entry.AssetPath))
            {
                return PrefabIcons.PrefabVariantIcon(entry.AssetPath);
            }
            return PrefabIcons.PrefabIcon(entry.AssetPath);
        }
        return null;
    }

    /// <summary>
    /// Returns the best case icon that can be retrieved quickly
    /// </summary>
    private Texture2D GetBestTempIcon(AddressableAssetEntry entry, bool performFileRead = false)
    {
        var icon = FastIconFromPath(entry, performFileRead);
        return icon == null ? AssetPreview.GetMiniTypeThumbnail(entry.MainAssetType) : icon;
    }

    /// <summary>
    /// Hook into the Editor's update, if we are not already hooked in
    /// </summary>
    private void AddLazyIconLoadCallback()
    {
        if (!m_editorHooked)
        {
            EditorApplication.update += WorkOnIconQueue;
            m_editorHooked = true;
        }
    }

    /// <summary>
    /// Unhook from the Editor and clean up our resources
    /// </summary>
    internal void RemoveLazyIconLoadCallback()
    {
        EditorApplication.update -= WorkOnIconQueue;
        m_editorHooked = false;
        if (m_editorProgressId > 0)
            Progress.Finish(m_editorProgressId);
        m_editorProgressId = -1;
        m_progressLength = -1;
        m_reloadWhenCacheInvalidated.Clear();
        ClearWorkQueue();
    }

    /// <summary>
    /// Clear out the work queue, without unhooking.
    /// Use this when rebuilding the tree state, to remove any pending loads
    /// </summary>
    internal void ClearWorkQueue()
    {
        m_needsIconRefresh.Clear();
        m_needsIconRefreshPriority.Clear();
        m_reloadWhenCacheInvalidated.Clear();
        // this will cause the progress to be recomputed
        m_progressLength = 0;
    }

    /// <summary>
    /// Worker method that is hooked into EditorApplication
    /// </summary>
    private void WorkOnIconQueue()
    {
        double startTime = EditorApplication.timeSinceStartup;
        while (EditorApplication.timeSinceStartup - startTime < k_timeBudget)
        {
            // Always try and pull from priority queue first
            if (!m_needsIconRefreshPriority.TryDequeue(out IconRequest request))
            {
                if (!m_needsIconRefresh.TryDequeue(out request))
                {
                    RemoveLazyIconLoadCallback();
                    return;
                }
            }

            if (m_needsIconRefreshPriority.Count + m_needsIconRefresh.Count > m_progressLength)
            {
                m_progressLength = m_needsIconRefresh.Count;
            }

            if (m_editorProgressId == -1)
            {
                m_editorProgressId = Progress.Start("Loading icons for Addressable Groups...");
            }

            AssetEntryTreeViewItem item = request.TreeViewItem;
            if (item != null)
            {
                if (item.entry == null)
                {
                    item.assetIcon = null;
                    continue;
                }

                // no need to revalidate scene files, the icon won't change
                if (!item.entry.AssetPath.EndsWith(".unity"))
                    m_reloadWhenCacheInvalidated.Add(item);

                // Prefabs will already have the Blue Cube they always have *but* we should check if this is a variant...
                if (item.entry.AssetPath.EndsWith(".prefab") || item.entry.AssetPath.EndsWith(".asset"))
                {
                    // Always allow file reads in this path...
                    item.assetIcon = FastIconFromPath(item.entry, true);
                    continue;
                }
            }

            request.Execute();
        }
        Progress.Report(m_editorProgressId, m_progressLength - m_needsIconRefresh.Count, m_progressLength);
    }

    /// <summary>
    /// Clear the icon cache, and force any currently loaded rows to be reloaded
    /// </summary>
    internal void ClearIconCache()
    {
        // If we don't re-load icons, we have the potential for an icon to be representing an old state
        // As such, we should reload every icon to ensure it is valid
        foreach (var item in m_reloadWhenCacheInvalidated)
        {
            LoadIconLazy(item);
        }
        m_scriptGuidToIconCache.Clear();
        m_reloadWhenCacheInvalidated.Clear();
    }
}
