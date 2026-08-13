using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace UnityEditor.AddressableAssets.GUI.Adapters
{
    /// <summary>
    /// Adapter wrapper around Unity's <see cref="TreeViewItem"/> (or <see cref="TreeViewItem{T}"/> in newer versions),
    /// providing consistent constructors and utilities for Addressables GUI tree structures.
    /// </summary>
    public class TreeViewItemAdapter :
#if UNITY_6000_2_OR_NEWER
        TreeViewItem<int>
#else
        TreeViewItem
#endif
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TreeViewItemAdapter"/> with default values.
        /// </summary>
        public TreeViewItemAdapter() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TreeViewItemAdapter"/> with the specified ID and depth.
        /// </summary>
        /// <param name="id">The unique identifier of the item.</param>
        /// <param name="depth">The depth of the item within the tree hierarchy.</param>
        public TreeViewItemAdapter(int id, int depth) : base(id, depth)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TreeViewItemAdapter"/> with the specified ID, depth, and display name.
        /// </summary>
        /// <param name="id">The unique identifier of the item.</param>
        /// <param name="depth">The depth of the item within the tree hierarchy.</param>
        /// <param name="displayName">The text displayed for the item.</param>
        public TreeViewItemAdapter(int id, int depth, string displayName) : base(id, depth, displayName)
        {
        }

#if UNITY_6000_2_OR_NEWER
        /// <summary>
        /// Creates and returns an empty list of generic <see cref="TreeViewItem{T}"/> items compatible with newer Unity versions.
        /// </summary>
        /// <returns>An empty list of <see cref="TreeViewItem{T}"/> items.</returns>
        internal static List<TreeViewItem<int>> EmptyList()
        {
            return new List<TreeViewItem<int>>();
        }
#else
        /// <summary>
        /// Creates and returns an empty list of non-generic <see cref="TreeViewItem"/> items compatible with legacy Unity versions.
        /// </summary>
        /// <returns>An empty list of <see cref="TreeViewItem"/> items.</returns>
        internal static List<TreeViewItem> EmptyList()
        {
            return new List<TreeViewItem>();
        }
#endif
    }
}
