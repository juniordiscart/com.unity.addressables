using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    internal abstract class ContentView : IAddressableView, IBuildReportConsumer
    {
        protected BuildLayout m_Report;

        internal ToolbarSearchField m_SearchField;
        internal string m_SearchValue;

        protected MultiColumnTreeView m_TreeView = null;
        public MultiColumnTreeView ContentTreeView
        {
            get { return m_TreeView; }
        }

        protected IList<IAddressablesBuildReportItem> m_TreeItems = null;
        public IList<IAddressablesBuildReportItem> TreeItems
        {
            get { return m_TreeItems; }
        }

        public struct TreeDataReportItem
        {
            public int Id;
            public IAddressablesBuildReportItem ReportItem;

            public TreeDataReportItem(int id, IAddressablesBuildReportItem reportItem)
            {
                Id = id;
                ReportItem = reportItem;
            }

        }
        protected Dictionary<Hash128, TreeDataReportItem> m_DataHashtoReportItem = null;
        public Dictionary<Hash128, TreeDataReportItem> DataHashtoReportItem
        {
            get { return m_DataHashtoReportItem; }
        }

        public Action<IEnumerable<object>> ItemsSelected;

        internal abstract ContentViewColumnData[] ColumnDataForView { get; }

        public abstract void Consume(BuildLayout buildReport);

        public abstract void CreateGUI(VisualElement rootVisualElement);

        public virtual void ClearGUI()
        {
            if (m_TreeView != null)
            {
                // Clear removes the column header
                // m_TreeView.Clear();
                m_TreeView.SetRootItems(default(IList<TreeViewItemData<IAddressablesBuildReportItem>>));
                m_TreeView.Rebuild();
            }
        }

        internal BuildReportHelperConsumer m_HelperConsumer;
        DetailsView m_DetailsView;
        VisualTreeAsset m_TreeViewItem;
        VisualTreeAsset m_TreeViewNavigableItem;

        internal ContentView(BuildReportHelperConsumer helperConsumer, DetailsView detailsView)
        {
            m_HelperConsumer = helperConsumer;
            m_DetailsView = detailsView;
            m_TreeViewItem = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildReportUtility.TreeViewItemFilePath);
            m_TreeViewNavigableItem = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildReportUtility.TreeViewNavigableItemFilePath);
        }

        public abstract IList<IAddressablesBuildReportItem> CreateTreeViewItems(BuildLayout report);

        // Expresses bundle data as a flat list of TreeViewItemData objects.
        protected IList<TreeViewItemData<IAddressablesBuildReportItem>> CreateTreeRootsFlatList(IList<IAddressablesBuildReportItem> items, Dictionary<Hash128, TreeDataReportItem> dataHashToReportItem)
        {
            int id = 0;
            var roots = new List<TreeViewItemData<IAddressablesBuildReportItem>>(items.Count);

            foreach (IAddressablesBuildReportItem item in items)
            {
                dataHashToReportItem.Add(BuildReportUtility.ComputeDataHash(item.Name, ""), new TreeDataReportItem(id, item));
                roots.Add(new TreeViewItemData<IAddressablesBuildReportItem>(id++, item));
            }
            return roots;
        }

        internal static void SetCallbacksForColumns(Columns columns, ContentViewColumnData[] columnNameToWidth)
        {
            foreach (ContentViewColumnData data in columnNameToWidth)
            {
                Column col = columns[data.Name];
                col.makeCell = () => new Label();
                col.bindCell = data.BindCellCallback;
                col.makeHeader = () => new Label();
                col.bindHeader = data.BindHeaderCallback;
            }
        }

        private IOrderedEnumerable<IAddressablesBuildReportItem> OrderByType(string columnName, Type t)
        {
            if (t == typeof(int))
                return m_TreeItems.OrderBy(item => int.Parse(item.GetSortContent(columnName)));
            if (t == typeof(ulong))
                return m_TreeItems.OrderBy(item => ulong.Parse(item.GetSortContent(columnName)));
            return null;
        }

        readonly Dictionary<string, Type> m_NumericColumnNames = new Dictionary<string, Type>()
        {
            {BuildReportUtility.AssetsContentViewColSizePlusRefs, typeof(ulong)},
            {BuildReportUtility.AssetsContentViewColSizeUncompressed, typeof(ulong)},
            {BuildReportUtility.AssetsContentViewColBundleSize, typeof(ulong)},
            {BuildReportUtility.AssetsContentViewColRefsBy, typeof(int)},
            {BuildReportUtility.AssetsContentViewColRefsTo, typeof(int)},
            {BuildReportUtility.BundlesContentViewColSizePlusRefs, typeof(ulong)},
            {BuildReportUtility.BundlesContentViewColSizeUncompressed, typeof(ulong)},
            {BuildReportUtility.BundlesContentViewBundleSize, typeof(ulong)},
            {BuildReportUtility.BundlesContentViewColRefsBy, typeof(int)},
            {BuildReportUtility.BundlesContentViewColRefsTo, typeof(int)},
            {BuildReportUtility.GroupsContentViewColSizePlusRefs, typeof(ulong)},
            {BuildReportUtility.GroupsContentViewColSizeUncompressed, typeof(ulong)},
            {BuildReportUtility.GroupsContentViewColBundleSize, typeof(ulong)},
            {BuildReportUtility.GroupsContentViewColRefsBy, typeof(int)},
            {BuildReportUtility.GroupsContentViewColRefsTo, typeof(int)},
            {BuildReportUtility.LabelsContentViewColSizePlusRefs, typeof(ulong)},
            {BuildReportUtility.LabelsContentViewColSizeUncompressed, typeof(ulong)},
            {BuildReportUtility.LabelsContentViewColSizeBundle, typeof(ulong)},
            {BuildReportUtility.LabelsContentViewColRefsBy, typeof(int)},
            {BuildReportUtility.LabelsContentViewColRefsTo, typeof(int)},
            {BuildReportUtility.DuplicatedAssetsContentViewSpaceSaved, typeof(ulong)},
            {BuildReportUtility.DuplicatedAssetsContentViewDuplicationCount, typeof(int)},
            {BuildReportUtility.DuplicatedAssetsContentViewColSize, typeof(ulong)}
        };

        public void CreateTreeViewHeader(VisualElement element, string colName, bool isAssetColumn)
        {
            (element as Label).text = ContentTreeView.columns[colName].title;
            if (isAssetColumn)
                element.AddToClassList(BuildReportUtility.TreeViewAssetHeader);
            else
                element.AddToClassList(BuildReportUtility.TreeViewHeader);
        }

        public void CreateTreeViewCell(VisualElement element, int index, string colName, bool isNameColumn, Type type)
        {
            IAddressablesBuildReportItem itemData = null;
            if (type == typeof(AssetsContentView))
               itemData = ContentTreeView.GetItemDataForIndex<AssetsViewBuildReportItem>(index);
            if (type == typeof(BundlesContentView))
                itemData = ContentTreeView.GetItemDataForIndex<BundlesViewBuildReportItem>(index);
            if (type == typeof(LabelsContentView))
                itemData = ContentTreeView.GetItemDataForIndex<LabelsViewBuildReportItem>(index);
            if (type == typeof(GroupsContentView))
                itemData = ContentTreeView.GetItemDataForIndex<GroupsViewBuildReportItem>(index);
            if (type == typeof(DuplicatedAssetsContentView))
                itemData = ContentTreeView.GetItemDataForIndex<DuplicatedAssetsViewBuildReportItem>(index);
            if (isNameColumn)
            {
                ShowEntryIcon(element, itemData, m_TreeViewItem, colName);
                element.AddToClassList(BuildReportUtility.TreeViewIconElement);
            }
            else
            {
                (element as Label).text = itemData.GetCellContent(colName);
                element.AddToClassList(BuildReportUtility.TreeViewElement);
            }
        }

        protected bool EntryAppearsInSearch(IAddressablesBuildReportItem item, string searchValue)
        {
            if (string.IsNullOrEmpty(searchValue))
                return true;
            if (item.Name.ToLowerInvariant().Contains(searchValue))
                return true;
            return false;
        }

        public void ShowEntryIcon(VisualElement element, IAddressablesBuildReportItem itemData, VisualTreeAsset baseItem, string colName)
        {
            (element as Label).text = string.Empty;
            element.Clear();

            VisualElement treeItem = baseItem.Clone(element);
            var icon = treeItem.Q<Image>(BuildReportUtility.TreeViewItemIcon);
            var name = treeItem.Q<TextElement>(BuildReportUtility.TreeViewItemName);
            name.text = itemData.GetCellContent(colName);

            if (itemData is IAddressablesBuildReportAsset asset)
            {
                string path = asset.ExplicitAsset == null ? asset.DataFromOtherAsset.AssetPath : asset.ExplicitAsset.AssetPath;
                Texture iconTexture = AssetDatabase.GetCachedIcon(path);
                if (iconTexture == null)
                    icon.AddToClassList(BuildReportUtility.TreeViewItemNoIcon);
                else
                    icon.image = iconTexture;

                if (asset.DataFromOtherAsset != null)
                    name.AddToClassList(BuildReportUtility.TreeViewImplicitAsset);
                if (asset is DuplicatedAssetsViewBuildReportDuplicatedAsset)
                    name.AddToClassList(BuildReportUtility.TreeViewDuplicatedAsset);
            }
            else if (itemData is LabelsViewBuildReportLabel label)
                icon.image = EditorGUIUtility.IconContent("FilterByLabel").image as Texture2D;
            else if (itemData is GroupsViewBuildReportGroup group ||
                     itemData is BundlesViewBuildReportIndirectlyReferencedBundles ||
                     itemData is GroupsViewBuildReportIndirectlyReferencedBundles)
                icon.image = EditorGUIUtility.IconContent("d_FolderOpened Icon").image as Texture2D;
            else if (itemData is IAddressablesBuildReportBundle)
                icon.image = EditorGUIUtility.IconContent("Package Manager").image as Texture2D;
            else
                icon.AddToClassList(BuildReportUtility.TreeViewItemNoIcon);

            name.AddManipulator(new ContextualMenuManipulator((ContextualMenuPopulateEvent evt) =>
            {
                evt.menu.AppendAction("Search in this window", (e) =>
                {
                    string newSearchValue = name.text;
                    m_SearchField.Q<TextField>().value = newSearchValue;
                });
            }));
        }

        public ContentView UseCachedView(VisualElement rootVisualElement)
        {
            VisualElement view = rootVisualElement.Q<VisualElement>(BuildReportUtility.ContentView);
            view.Add(m_TreeView);
            return this;
        }

        internal List<IAddressablesBuildReportItem> SortByColumnDescription(SortColumnDescription col)
        {
            IOrderedEnumerable<IAddressablesBuildReportItem> sortedTreeRootEnumerable;
            if (m_NumericColumnNames.ContainsKey(col.columnName))
            {
                Type t = m_NumericColumnNames[col.columnName];
                sortedTreeRootEnumerable = OrderByType(col.columnName, t);
            }
            else
            {
                sortedTreeRootEnumerable = m_TreeItems.OrderBy(item => item.GetSortContent(col.columnName));
            }

            List<IAddressablesBuildReportItem> finalTreeRoots = new List<IAddressablesBuildReportItem>(m_TreeItems.Count);
            foreach (var item in sortedTreeRootEnumerable)
                finalTreeRoots.Add(item);
            if (col.direction == SortDirection.Ascending)
                finalTreeRoots.Reverse();

            return finalTreeRoots;
        }
    }

    internal struct ContentViewColumnData
    {
        public string Name;
        public string Title;
        public Action<VisualElement, int> BindCellCallback;
        public Action<VisualElement> BindHeaderCallback;

        public ContentViewColumnData(string name, ContentView view, bool isNameColumn, string title = "N/a")
        {
            Name = name;
            Title = title;
            BindCellCallback = ((element, index) =>
            {
                view.CreateTreeViewCell(element, index, name, isNameColumn, view.GetType());
            });
            BindHeaderCallback = ((element) =>
            {
                view.CreateTreeViewHeader(element, name, isNameColumn);
            });
        }
    }

    /// <summary>
    /// Nested interface that can be either a bundle or asset.
    /// </summary>
    public interface IAddressablesBuildReportItem
    {
        /// <summary>
        /// The name of the build report item
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Create the UI element for a build report item
        /// </summary>
        /// <param name="rootVisualElement">The visual element container</param>
        void CreateGUI(VisualElement rootVisualElement);

        /// <summary>
        /// Get content for a cell given a column name
        /// </summary>
        /// <param name="colName">The name of the column</param>
        /// <returns>The display string for the cell</returns>
        string GetCellContent(string colName);

        /// <summary>
        /// Get the sortable content
        /// </summary>
        /// <param name="colName">The name of the column</param>
        /// <returns>The display string for the cell</returns>
        string GetSortContent(string colName);

    }

    /// <summary>
    /// Interface for an AssetBundle build report item
    /// </summary>
    public interface IAddressablesBuildReportBundle
    {
        /// <summary>
        /// The AssetBundle data
        /// </summary>
        public BuildLayout.Bundle Bundle { get; }
    }

    /// <summary>
    /// Interface for Asset build report item
    /// </summary>
    public interface IAddressablesBuildReportAsset
    {
        /// <summary>
        /// The data to set if the asset is an explicit asset (Addressable)
        /// </summary>
        public BuildLayout.ExplicitAsset ExplicitAsset { get; }

        /// <summary>
        /// The data to set if the asset is an implicit asset (non-Addressable, but pulled into an Asset Bundle)
        /// </summary>
        public BuildLayout.DataFromOtherAsset DataFromOtherAsset { get; }

        /// <summary>
        /// The data for the AssetBundle the asset belongs to
        /// </summary>
        public List<BuildLayout.Bundle> Bundles { get; }

        /// <summary>
        /// The total size of this asset plus its dependencies
        /// </summary>
        public ulong SizeWDependencies { get; }
    }
}
