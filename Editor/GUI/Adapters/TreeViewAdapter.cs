using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.AddressableAssets.GUI.Adapters
{
    /// <summary>
    /// An abstract adapter layer over Unity's <see cref="TreeView"/> that allows working with
    /// <see cref="TreeViewItemAdapter"/> while supporting both legacy and generic TreeView APIs.
    /// Implementors should provide root and row-building logic using the adapter types and override
    /// capability methods (rename, parenting, multi-select, search) as needed.
    /// </summary>
    public abstract class TreeViewAdapter :
#if UNITY_6000_2_OR_NEWER
        TreeView<int>
#else
        TreeView
#endif
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TreeViewAdapter"/> with the specified tree state.
        /// </summary>
        /// <param name="state">The adapter-wrapped <see cref="TreeViewState"/> used to maintain UI state.</param>
        public TreeViewAdapter(TreeViewStateAdapter state) : base(state)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TreeViewAdapter"/> with the specified tree state and header.
        /// </summary>
        /// <param name="state">The adapter-wrapped <see cref="TreeViewState"/> used to maintain UI state.</param>
        /// <param name="multiColumnHeader">A multi-column header to display columns in the tree view.</param>
        public TreeViewAdapter(TreeViewStateAdapter state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
        }

        /// <summary>
        /// Determines whether multiple items can be selected simultaneously.
        /// </summary>
        /// <param name="item">The item being considered for selection.</param>
        /// <returns>True if multi-selection is allowed; otherwise, false.</returns>
        protected virtual bool CanMultiSelect(TreeViewItemAdapter item)
        {
            return false;
        }

        /// <summary>
        /// Determines whether the given item should be included when a search filter is applied.
        /// </summary>
        /// <param name="item">The item to test against the search filter.</param>
        /// <param name="search">The current search string.</param>
        /// <returns>True if the item matches the search criteria; otherwise, false.</returns>
        protected virtual bool DoesItemMatchSearch(TreeViewItemAdapter item, string search)
        {
            return false;
        }

        /// <summary>
        /// Determines whether the specified item can be renamed.
        /// </summary>
        /// <param name="item">The item to evaluate for renaming.</param>
        /// <returns>True if the item can be renamed; otherwise, false.</returns>
        protected virtual bool CanRenameAdapter(TreeViewItemAdapter item)
        {
            return false;
        }

        /// <summary>
        /// Determines whether the specified item can act as a parent to other items.
        /// </summary>
        /// <param name="item">The item to evaluate as a potential parent.</param>
        /// <returns>True if the item can be a parent; otherwise, false.</returns>
        protected virtual bool CanBeParentAdapter(TreeViewItemAdapter item)
        {
            return false;
        }

        /// <summary>
        /// Handles the end of a rename operation.
        /// </summary>
        /// <param name="args">Arguments describing the rename operation and result.</param>
        protected override void RenameEnded(RenameEndedArgs args)
        {
            base.RenameEnded(args);
        }

        /// <summary>
        /// Constructs and returns the root item using the adapter type.
        /// Implementors should create the root <see cref="TreeViewItemAdapter"/> and set up its hierarchy.
        /// </summary>
        /// <returns>The root adapter item.</returns>
        protected abstract TreeViewItemAdapter BuildRootAdapter();

        /// <summary>
        /// Builds the visible row list from the provided root adapter item.
        /// This default implementation forwards to the base TreeView's row builder
        /// and casts items to <see cref="TreeViewItemAdapter"/>.
        /// </summary>
        /// <param name="root">The root adapter item.</param>
        /// <returns>A list of visible adapter rows.</returns>
        protected virtual IList<TreeViewItemAdapter> BuildRowsAdapter(TreeViewItemAdapter root)
        {
            var results = new List<TreeViewItemAdapter>();
            foreach (var row in base.BuildRows(root))
            {
                results.Add(row as TreeViewItemAdapter);
            }

            return results;
        }

#if UNITY_6000_2_OR_NEWER
        /// <inheritdoc/>
        protected override bool CanMultiSelect(TreeViewItem<int> item)
        {
            return CanMultiSelect(item as TreeViewItemAdapter);
        }

        /// <inheritdoc/>
        protected override IList<TreeViewItem<int>> BuildRows(TreeViewItem<int> root)
        {
            var rows = BuildRowsAdapter(root as TreeViewItemAdapter);
            var results = new List<TreeViewItem<int>>();
            foreach (var row in rows)
                results.Add(row);
            return results;
        }

        /// <inheritdoc/>
        protected override TreeViewItem<int> BuildRoot()
        {
            return BuildRootAdapter();
        }

        /// <inheritdoc/>
        protected override bool DoesItemMatchSearch(TreeViewItem<int> item, string search)
        {
            return DoesItemMatchSearch(item as TreeViewItemAdapter, search);
        }

        /// <inheritdoc/>
        protected override bool CanRename(TreeViewItem<int> item)
        {
            return CanRenameAdapter(item as TreeViewItemAdapter);
        }

        /// <inheritdoc/>
        protected override bool CanBeParent(TreeViewItem<int> item)
        {
            return CanBeParentAdapter(item as TreeViewItemAdapter);
        }
#else
        /// <inheritdoc/>
        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return CanMultiSelect(item as TreeViewItemAdapter);
        }

        /// <inheritdoc/>
        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = BuildRowsAdapter(root as TreeViewItemAdapter);
            var results = new List<TreeViewItem>();
            foreach (var row in rows)
                results.Add(row);
            return results;
        }

        /// <inheritdoc/>
        protected override TreeViewItem BuildRoot()
        {
            return BuildRootAdapter();
        }

        /// <inheritdoc/>
        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            return DoesItemMatchSearch(item as TreeViewItemAdapter, search);
        }

        /// <inheritdoc/>
        protected override bool CanRename(TreeViewItem item)
        {
            return CanRenameAdapter(item as TreeViewItemAdapter);
        }

        /// <inheritdoc/>
        protected override bool CanBeParent(TreeViewItem item)
        {
            return CanBeParentAdapter(item as TreeViewItemAdapter);
        }
#endif
    }
}
