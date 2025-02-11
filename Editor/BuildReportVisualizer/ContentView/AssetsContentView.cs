using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    /// <summary>
    /// UI item used to display assets in the Addressables Build Report
    /// </summary>
    public class AssetsViewBuildReportItem : IAddressablesBuildReportItem
    {
        /// <summary>
        /// The name of the asset
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The name of the AssetBundle where the asset is stored
        /// </summary>
        public string BundleName { get; set; }

        /// <summary>
        /// The Addressable Group the asset belonged to
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// The total size of the asset and its dependencies
        /// </summary>
        public ulong FileSizePlusRefs { get; set; }

        /// <summary>
        /// The size of the uncompressed asset
        /// </summary>
        public ulong FileSizeUncompressed { get; set; }

        /// <summary>
        /// The file size of the AssetBundle where the asset is stored
        /// </summary>
        public ulong BundleSize { get; set; }

        /// <summary>
        /// The ids of assets this asset references
        /// </summary>
        public int RefsTo { get; set; }

        /// <summary>
        /// The ids that reference this asset
        /// </summary>
        public int RefsBy { get; set; }

        /// <summary>
        /// Data store for Assets explicitly defined in an AssetBundle
        /// </summary>
        public BuildLayout.ExplicitAsset ExplicitAsset { get; set; }

        /// <summary>
        /// Data store for implicit Asset references
        /// </summary>
        public BuildLayout.DataFromOtherAsset DataFromOtherAsset { get; set; }

        /// <summary>
        /// Data store for the AssetBundle the asset is stored in
        /// </summary>
        public BuildLayout.Bundle Bundle { get; set; }

        /// <summary>
        /// If this asset is an implicit dependecy, this is the list of all the AssetBundles where the asset was duplicated
        /// </summary>
        public List<BuildLayout.Bundle> Bundles { get; set; }

        /// <summary>
        /// The total size with dependencies
        /// </summary>
        public ulong SizeWDependencies => 9999;

        /// <summary>
        /// Used to build the GUI for the asset data
        /// </summary>
        /// <param name="rootVisualElement">The visual element container</param>
        public virtual void CreateGUI(VisualElement rootVisualElement) { }

        /// <summary>
        /// Get specific data for a given column
        /// </summary>
        /// <param name="colName">The name of the column to get data for</param>
        /// <returns>The display string for a GUI element</returns>
        public virtual string GetCellContent(string colName)
        {
            if (colName == BuildReportUtility.AssetsContentViewColAssetName)
                return Name;
            else if (colName == BuildReportUtility.AssetsContentViewColSizePlusRefs)
            {
                if (FileSizePlusRefs == 0)
                    return "--";
                return BuildReportUtility.GetDenominatedBytesString(FileSizePlusRefs);
            }
            else if (colName == BuildReportUtility.AssetsContentViewColSizeUncompressed)
            {
                if (FileSizeUncompressed == 0)
                    return "--";
                return BuildReportUtility.GetDenominatedBytesString(FileSizeUncompressed);
            }
            else if (colName == BuildReportUtility.AssetsContentViewColBundleSize)
            {
                if (BundleSize == 0)
                    return "--";
                return BuildReportUtility.GetDenominatedBytesString(BundleSize);
            }
            else if (colName == BuildReportUtility.AssetsContentViewColRefsTo)
            {
                if (RefsTo == -1)
                    return "--";
                return RefsTo.ToString();
            }
            else if (colName == BuildReportUtility.AssetsContentViewColRefsBy)
            {
                if (RefsBy == -1)
                    return "--";
                return RefsBy.ToString();
            }

            return "";
        }

        /// <summary>
        /// Get sortable content data for a column
        /// </summary>
        /// <param name="colName">The name of the column to get data for</param>
        /// <returns>The display string for a GUI element</returns>
        public string GetSortContent(string colName)
        {
            if (colName == BuildReportUtility.AssetsContentViewColSizePlusRefs)
                return FileSizePlusRefs.ToString();
            if (colName == BuildReportUtility.AssetsContentViewColSizeUncompressed)
                return FileSizeUncompressed.ToString();
            if (colName == BuildReportUtility.AssetsContentViewColBundleSize)
                return BundleSize.ToString();
            return GetCellContent(colName);
        }
    }

    /// <summary>
    /// The type header for the asset view
    /// </summary>
    public class AssetsViewTypeHeader : AssetsViewBuildReportItem
    {
        /// <summary>
        /// Create a new type header object
        /// </summary>
        /// <param name="name">The title name to use</param>
        public AssetsViewTypeHeader(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Get the content for a given cell
        /// </summary>
        /// <param name="colName">The name of the column to get data for</param>
        /// <returns>The display data for the cell</returns>
        public override string GetCellContent(string colName)
        {
            if (colName == BuildReportUtility.AssetsContentViewColAssetName)
                return Name;
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Build the GUI for a given asset
        /// </summary>
        /// <param name="rootVisualElement">The visual element container</param>
        public override void CreateGUI(VisualElement rootVisualElement)
        {

        }
    }

    /// <summary>
    /// Container for the UI data for an AssetBundle
    /// </summary>
    public class AssetsViewBuildReportBundle : AssetsViewBuildReportItem, IAddressablesBuildReportBundle
    {
        /// <summary>
        /// Create a UI element data container for a Bundle
        /// </summary>
        /// <param name="bundle">The AssetBundle you're creating data for</param>
        public AssetsViewBuildReportBundle(BuildLayout.Bundle bundle)
        {
            Name = bundle.Name;
            Bundle = bundle;
            BundleSize = bundle.FileSize;
            GroupName = bundle.Group.Name;
            BundleName = "";
            FileSizePlusRefs = bundle.FileSize;
            foreach (var b in Bundle.ExpandedDependencies)
                FileSizePlusRefs += b.FileSize;
            RefsTo = Bundle.ExpandedDependencies.Count;
            RefsBy = Bundle.DependentBundles.Count;
        }
    }

    /// <summary>
    /// Container for UI element (Unrelated Assets)
    /// </summary>
    public class AssetsViewBuildReportUnrelatedAssets : AssetsViewBuildReportItem
    {
        /// <summary>
        /// Create a UI container for Unrelated Assets
        /// </summary>
        /// <param name="assetSize">The file size of the assets</param>
        /// <param name="assetCount">The number of assets</param>
        public AssetsViewBuildReportUnrelatedAssets(ulong assetSize, int assetCount)
        {
            Name = $"({assetCount} unrelated assets)";
            FileSizeUncompressed = assetSize;
            GroupName = "";
            BundleName = "";
        }
    }

    /// <summary>
    /// Container for UI element (Assets)
    /// </summary>
    public class AssetsViewBuildReportAsset : AssetsViewBuildReportItem, IAddressablesBuildReportAsset
    {
        /// <summary>
        /// For a given asset, the assets in the same AssetBundle it references
        /// </summary>
        public List<BuildLayout.ExplicitAsset> InternallyReferencedAssets { get; }

        /// <summary>
        /// For a given asset, the assets in different AssetBundles it references
        /// </summary>
        public List<BuildLayout.ExplicitAsset> ExternallyReferencedAssets { get; }

        /// <summary>
        /// Asset data from implicit (non-Addressable) dependencies
        /// </summary>
        public List<BuildLayout.DataFromOtherAsset> ImplicitDependencies { get; }

        /// <summary>
        /// Asset data for explicit (Addressable) dependencies
        /// </summary>
        List<BuildLayout.ExplicitAsset> ReferencingAssets { get; }

        /// <summary>
        /// Create the UI container for an Addressable asset
        /// </summary>
        /// <param name="asset">The Addressable asset</param>
        public AssetsViewBuildReportAsset(BuildLayout.ExplicitAsset asset)
        {
            ExplicitAsset = asset;
            Name = asset.AddressableName;
            Bundle = asset.Bundle;
            Bundles = new List<BuildLayout.Bundle>(){ Bundle };
            BundleName = asset.Bundle?.Name;
            ExternallyReferencedAssets = asset.ExternallyReferencedAssets;
            InternallyReferencedAssets = asset.InternalReferencedExplicitAssets;
            ImplicitDependencies = asset.InternalReferencedOtherAssets;
            ReferencingAssets = asset.ReferencingAssets;
            FileSizeUncompressed = asset.SerializedSize + asset.StreamedSize;
            FileSizePlusRefs = FileSizeUncompressed;
            foreach (var r in asset.ExternallyReferencedAssets)
                if (r != null)
                    FileSizePlusRefs += r.SerializedSize + asset.StreamedSize;
            foreach (var r in asset.InternalReferencedExplicitAssets)
                if (r != null)
                    FileSizePlusRefs += r.SerializedSize + asset.StreamedSize;
            foreach (var r in asset.InternalReferencedOtherAssets)
                if (r != null)
                    FileSizePlusRefs += r.SerializedSize + asset.StreamedSize;
            RefsTo = asset.ExternallyReferencedAssets.Count + asset.InternalReferencedExplicitAssets.Count + asset.InternalReferencedOtherAssets.Count;
            RefsBy = asset.ReferencingAssets != null ? asset.ReferencingAssets.Count : -1;
        }

        /// <summary>
        /// Create a UI container for a non-Addressable asset
        /// </summary>
        /// <param name="asset">The non-Addressable asset</param>
        public AssetsViewBuildReportAsset(BuildLayout.DataFromOtherAsset asset)
        {
            DataFromOtherAsset = asset;
            Bundles = new List<BuildLayout.Bundle>() { asset.File.Bundle };
            Name = asset.AssetPath;
            RefsBy = asset.ReferencingAssets.Count;
            RefsTo = -1;
            FileSizeUncompressed = asset.SerializedSize + asset.StreamedSize;
            FileSizePlusRefs = asset.SerializedSize + asset.StreamedSize;
        }

    }


    class AssetsContentView : ContentView
    {
        public AssetsContentView(BuildReportHelperConsumer helperConsumer, DetailsView detailsView)
            : base(helperConsumer, detailsView)
        {
            m_DataHashtoReportItem = new Dictionary<Hash128, TreeDataReportItem>();
        }

        internal override ContentViewColumnData[] ColumnDataForView
        {
            get
            {
                return new ContentViewColumnData[]
                {
                new ContentViewColumnData(BuildReportUtility.AssetsContentViewColAssetName, this, true, "Asset Name"),
                new ContentViewColumnData(BuildReportUtility.AssetsContentViewColSizePlusRefs, this, false, "Total Size (+ refs)"),
                new ContentViewColumnData(BuildReportUtility.AssetsContentViewColSizeUncompressed, this, false, "Uncompressed Size"),
                new ContentViewColumnData(BuildReportUtility.AssetsContentViewColBundleSize, this, false, "Bundle File Size"),
                new ContentViewColumnData(BuildReportUtility.AssetsContentViewColRefsTo, this, false, "Refs To"),
                new ContentViewColumnData(BuildReportUtility.AssetsContentViewColRefsBy, this, false, "Refs By")
                };
            }
        }

        // Data about assets from our currently selected build report.
        public override IList<IAddressablesBuildReportItem> CreateTreeViewItems(BuildLayout report)
        {
            List<IAddressablesBuildReportItem> buildReportAssets = new List<IAddressablesBuildReportItem>();
            if (report == null)
                return buildReportAssets;

            foreach (BuildLayout.ExplicitAsset asset in BuildLayoutHelpers.EnumerateAssets(report))
                buildReportAssets.Add(new AssetsViewBuildReportAsset(asset));

            foreach (BuildLayout.DataFromOtherAsset implicitAsset in BuildLayoutHelpers.EnumerateImplicitAssets(report))
                buildReportAssets.Add(new AssetsViewBuildReportAsset(implicitAsset));

            return buildReportAssets;
        }


        public override void Consume(BuildLayout buildReport)
        {
            if (buildReport == null)
                return;

            m_Report = buildReport;
            m_TreeItems = CreateTreeViewItems(m_Report);
            m_TreeView.SetRootItems(CreateTreeRootsNestedList(m_TreeItems));
            m_TreeView.Rebuild();
            m_TreeView.columnSortingChanged += ColumnSortingChanged;
        }

        private void ColumnSortingChanged()
        {
            var columnList = m_TreeView.sortedColumns;

            IList<IAddressablesBuildReportItem> sortedRootList = new List<IAddressablesBuildReportItem>();
            foreach (var col in columnList)
            {
                sortedRootList = SortByColumnDescription(col);
            }

            m_DataHashtoReportItem = new Dictionary<Hash128, TreeDataReportItem>();
            m_TreeView.SetRootItems(CreateTreeRootsNestedList(sortedRootList));
            m_TreeView.Rebuild();
        }

        public override void CreateGUI(VisualElement rootVisualElement)
        {
            VisualElement view = rootVisualElement.Q<VisualElement>(BuildReportUtility.ContentView);
            TreeBuilder tb = new TreeBuilder()
                .With(ColumnDataForView)
                .With((items) => ItemsSelected.Invoke(items));

            m_TreeView = tb.Build();
            view.Add(m_TreeView);
            SetCallbacksForColumns(m_TreeView.columns, ColumnDataForView);
            m_SearchField = rootVisualElement.Q<ToolbarSearchField>(BuildReportUtility.SearchField);
            m_SearchField.RegisterValueChangedCallback(OnSearchValueChanged);
            m_SearchValue = m_SearchField.value;
        }

        private void OnSearchValueChanged(ChangeEvent<string> evt)
        {
            if (m_TreeItems == null)
                return;
            m_SearchValue = evt.newValue.ToLower();
            m_TreeView.SetRootItems(CreateTreeRootsNestedList(m_TreeItems));
            m_TreeView.Rebuild();
        }

        public IList<TreeViewItemData<AssetsViewBuildReportItem>> CreateTreeRootsNestedList(IList<IAddressablesBuildReportItem> items)
        {
            int id = 0;
            var roots = new List<TreeViewItemData<AssetsViewBuildReportItem>>(items.Count);
            foreach (AssetsViewBuildReportItem item in items)
            {
                AssetsViewBuildReportAsset asset = item as AssetsViewBuildReportAsset;

                if (asset == null)
                    continue;

                bool includeAllDependencies = EntryAppearsInSearch(asset, m_SearchValue);

                bool assetIsExplicitAsset = asset.ExplicitAsset != null;
                if (assetIsExplicitAsset)
                {
                    var children = CreateChildrenOfAsset(asset, ref id, includeAllDependencies);

                    if (children.Count > 0 || EntryAppearsInSearch(asset, m_SearchValue))
                        roots.Add(new TreeViewItemData<AssetsViewBuildReportItem>(++id, item, children));
                }
                else if (includeAllDependencies)
                {
                    roots.Add(new TreeViewItemData<AssetsViewBuildReportItem>(++id, item));
                }
            }

            return roots;
        }

        List<TreeViewItemData<AssetsViewBuildReportItem>> CreateChildrenOfAsset(AssetsViewBuildReportAsset asset, ref int id, bool includeAllDependencies)
        {
            var children = new List<TreeViewItemData<AssetsViewBuildReportItem>>();

            PopulateInternallyReferencedEntries(children, asset, ref id, includeAllDependencies);
            PopulateExternallyReferencedEntries(children, asset, ref id, includeAllDependencies);
            PopulateImplicitEntries(children, asset, ref id, includeAllDependencies);

            return children;
        }

        void PopulateInternallyReferencedEntries(List<TreeViewItemData<AssetsViewBuildReportItem>> children, AssetsViewBuildReportAsset asset, ref int id, bool includeAllDependencies)
        {
            foreach (var dep in asset.InternallyReferencedAssets)
            {
                var buildReportAsset = new AssetsViewBuildReportAsset(dep);
                if (EntryAppearsInSearch(buildReportAsset, m_SearchValue) || includeAllDependencies)
                    children.Add(new TreeViewItemData<AssetsViewBuildReportItem>(++id, buildReportAsset));
            }
        }

        void PopulateExternallyReferencedEntries(List<TreeViewItemData<AssetsViewBuildReportItem>> children, AssetsViewBuildReportAsset asset, ref int id, bool includeAllDependencies)
        {
            Dictionary<BuildLayout.Bundle, List<BuildLayout.ExplicitAsset>> bundleToAssetList = new Dictionary<BuildLayout.Bundle, List<BuildLayout.ExplicitAsset>>();
            foreach (var dep in asset.ExternallyReferencedAssets)
            {
                if (!bundleToAssetList.ContainsKey(dep.Bundle))
                    bundleToAssetList.Add(dep.Bundle, new List<BuildLayout.ExplicitAsset>());
                bundleToAssetList[dep.Bundle].Add(dep);
            }

            foreach (var bundle in bundleToAssetList.Keys)
            {
                var assetTreeViewItems = new List<TreeViewItemData<AssetsViewBuildReportItem>>();
                var assetList = bundleToAssetList[bundle];
                ulong unrelatedAssetSize = bundle.FileSize;
                foreach (var bundleAsset in assetList)
                {
                    var bundleReportAsset = new AssetsViewBuildReportAsset(bundleAsset);
                    if (includeAllDependencies || EntryAppearsInSearch(bundleReportAsset, m_SearchValue))
                        assetTreeViewItems.Add(new TreeViewItemData<AssetsViewBuildReportItem>(++id, bundleReportAsset));

                    unrelatedAssetSize -= (bundleAsset.SerializedSize + bundleAsset.StreamedSize);
                }

                int unrelatedAssetCount = bundle.AssetCount - assetList.Count;
                if (unrelatedAssetCount > 0 && includeAllDependencies)
                    assetTreeViewItems.Add(new TreeViewItemData<AssetsViewBuildReportItem>(++id, new AssetsViewBuildReportUnrelatedAssets(unrelatedAssetSize, unrelatedAssetCount)));

                if (includeAllDependencies || assetTreeViewItems.Count > 0)
                    children.Add(new TreeViewItemData<AssetsViewBuildReportItem>(++id, new AssetsViewBuildReportBundle(bundle), assetTreeViewItems));
            }
        }

        void PopulateImplicitEntries(List<TreeViewItemData<AssetsViewBuildReportItem>> children, AssetsViewBuildReportAsset asset, ref int id, bool includeAllDependencies)
        {
            foreach (var dep in asset.ImplicitDependencies)
            {
                var reportImplicitAsset = new AssetsViewBuildReportAsset(dep);
                if (EntryAppearsInSearch(reportImplicitAsset, m_SearchValue) || includeAllDependencies)
                    children.Add(new TreeViewItemData<AssetsViewBuildReportItem>(++id, reportImplicitAsset));
            }
        }

    }
}
