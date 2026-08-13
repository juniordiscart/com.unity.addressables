using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.GUI.Adapters;
using UnityEngine;

namespace UnityEditor.AddressableAssets.GUI
{
    /// <summary>
    /// Serializable state container for the Addressable Asset Entry TreeView.
    /// Extends <see cref="TreeViewStateAdapter"/> to include column widths and sort order configuration.
    /// </summary>
    [Serializable]
    public class AddressableAssetEntryTreeViewState : TreeViewStateAdapter
    {
        /// <summary>
        /// The widths of each column in the tree view. Indices correspond to column order in the UI.
        /// </summary>
        [SerializeField]
        public float[] columnWidths;

        /// <summary>
        /// The legacy sort order configuration. Use <see cref="sortOrderList"/> instead.
        /// </summary>
        /// <remarks>
        /// This field is retained for backward compatibility and migration purposes.
        /// </remarks>
        [SerializeField]
        [Obsolete("Use sortOrderList instead.")]
        public String[] sortOrder;

        /// <summary>
        /// The current sort order configuration as a list of column identifiers or keys.
        /// The first entry represents the primary sort, followed by secondary sorts.
        /// </summary>
        [SerializeField]
        public List<string> sortOrderList = new List<string>();
    }
}
