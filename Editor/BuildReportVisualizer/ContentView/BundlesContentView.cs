#if UNITY_2022_2_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.Build.Reporting;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using BuildLayout = UnityEditor.AddressableAssets.Build.Layout.BuildLayout;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    // Nested class that represents a generic item in the bundle view (can be an asset, header, or bundle).
    class BundlesViewBuildReportItem : IAddressablesBuildReportItem
    {
        public string Name { get; protected set; }

        public ulong FileSize { get; protected set; }

        public int RefsTo { get; protected set; }
        public int RefsBy { get; protected set; }

        public ulong FileSizeUncompressed { get; set; }

        public ulong FileSizePlusRefs { get; set; }

        public ulong FileSizeBundle { get; set; }

        public int AssetsCount { get; set; }

        public string GroupName { get; protected set; }

        public virtual void CreateGUI(VisualElement rootVisualElement) { }

        public virtual string GetCellContent(string colName)
        {
            if (colName == BuildReportUtility.BundlesContentViewColBundleName)
                return Name;
            else if (colName == BuildReportUtility.BundlesContentViewColSizePlusRefs)
            {
                if (FileSizePlusRefs == 0)
                    return "--";
                return BuildReportUtility.GetDenominatedBytesString(FileSizePlusRefs);
            }

            if (colName == BuildReportUtility.BundlesContentViewColSizeUncompressed)
            {
                if (FileSizeUncompressed == 0)
                    return "--";
                return BuildReportUtility.GetDenominatedBytesString(FileSizeUncompressed);
            }
            else if (colName == BuildReportUtility.BundlesContentViewBundleSize)
            {
                if (FileSizeBundle == 0)
                    return "--";
                return BuildReportUtility.GetDenominatedBytesString(FileSizeBundle);
            }
            else if (colName == BuildReportUtility.BundlesContentViewColRefsTo)
            {
                if (RefsTo == -1)
                    return "--";
                return RefsTo.ToString();
            }
            else if (colName == BuildReportUtility.BundlesContentViewColRefsBy)
            {
                if (RefsBy == -1)
                    return "--";
                return RefsBy.ToString();
            }
            return "";
        }

        public string GetSortContent(string colName)
        {
            if (colName == BuildReportUtility.BundlesContentViewColSizePlusRefs)
                return FileSizePlusRefs.ToString();
            if (colName == BuildReportUtility.BundlesContentViewColSizeUncompressed)
                return FileSizeUncompressed.ToString();
            if (colName == BuildReportUtility.BundlesContentViewBundleSize)
                return FileSizeBundle.ToString();
            return GetCellContent(colName);
        }
    }

    // Nested class that represents an bundle.
    class BundlesViewBuildReportBundle : BundlesViewBuildReportItem, IAddressablesBuildReportBundle
    {
        public BuildLayout.Bundle Bundle { get; set; }
        public List<BuildLayout.Bundle> Dependencies { get; set; }
        public List<BuildLayout.Bundle> DependentBundles { get; set; }

        public BundlesViewBuildReportBundle(BuildLayout.Bundle bundle)
        {
            Bundle = bundle;
            Name = bundle.Name;
            foreach (var file in bundle.Files)
                RefsTo += file.Assets.Count + file.OtherAssets.Count;
            RefsBy = bundle.DependentBundles.Count;
            GroupName = bundle.Group?.Name;

            Dependencies = bundle.Dependencies;
            DependentBundles = bundle.DependentBundles;
            foreach (var f in bundle.Files)
            {
                foreach (var ex in f.Assets)
                    ExplicitAssetsFromLayout.Add(ex);
            }

            FileSizeBundle = bundle.FileSize;
            FileSizeUncompressed = bundle.UncompressedFileSize;
            FileSizePlusRefs = bundle.FileSize + bundle.ExpandedDependencyFileSize + bundle.DependencyFileSize;
        }

        public List<BuildLayout.ExplicitAsset> ExplicitAssetsFromLayout = new List<BuildLayout.ExplicitAsset>();

        public List<BuildLayout.ExplicitAsset> ExplicitAssets { get; set; }

        public List<BuildLayout.DataFromOtherAsset> ImplicitAssets { get; set; }
    }

    class BundlesViewBuildReportAsset : BundlesViewBuildReportItem, IAddressablesBuildReportAsset
    {
        public BuildLayout.ExplicitAsset ExplicitAsset { get; }
        public BuildLayout.DataFromOtherAsset DataFromOtherAsset { get; }
        public List<BuildLayout.Bundle> Bundles { get;}

        public string AssetPath { get; private set; }

        public ulong SizeWDependencies { get; set; }

        public bool IsAddressable = false;

        public BundlesViewBuildReportAsset(BuildLayout.ExplicitAsset asset)
        {
            ExplicitAsset = asset;
            Bundles = new List<BuildLayout.Bundle>(){ asset.Bundle };
            Name = asset.AddressableName;
            RefsTo = asset.InternalReferencedOtherAssets.Count + asset.ExternallyReferencedAssets.Count + asset.InternalReferencedExplicitAssets.Count;
            RefsBy = asset.ReferencingAssets != null ? asset.ReferencingAssets.Count : -1;
            FileSize = asset.SerializedSize + asset.StreamedSize;
            AssetPath = asset.AssetPath;
            IsAddressable = true;
            FileSizeUncompressed = asset.SerializedSize + asset.StreamedSize;
            FileSizePlusRefs = FileSizeUncompressed;
            foreach (var r in asset.ExternallyReferencedAssets)
                if (r != null)
                    FileSizePlusRefs += r.SerializedSize + r.StreamedSize;
            foreach (var r in asset.InternalReferencedExplicitAssets)
                if (r != null)
                    FileSizePlusRefs += r.SerializedSize + r.StreamedSize;
            SizeWDependencies = FileSizePlusRefs;
        }

        public BundlesViewBuildReportAsset(BuildLayout.DataFromOtherAsset asset)
        {
            DataFromOtherAsset = asset;
            Bundles = new List<BuildLayout.Bundle>() { asset.File.Bundle };
            Name = asset.AssetPath;
            AssetPath = asset.AssetPath;
            RefsBy = asset.ReferencingAssets.Count;
            RefsTo = -1;
            IsAddressable = false;
            FileSizeUncompressed = asset.SerializedSize + asset.StreamedSize;
            FileSizePlusRefs = asset.SerializedSize + asset.StreamedSize;
            SizeWDependencies = FileSizePlusRefs;
            FileSizeBundle = 0;
        }
    }

    class BundlesViewBuildReportUnrelatedAssets : BundlesViewBuildReportItem
    {
        public BundlesViewBuildReportUnrelatedAssets(ulong assetSize, int assetCount)
        {
            Name = $"({assetCount} unrelated assets)";
            FileSizeUncompressed = assetSize;
        }
    }

    class BundlesViewBuildReportIndirectlyReferencedBundles : BundlesViewBuildReportItem
    {
        public BundlesViewBuildReportIndirectlyReferencedBundles(List<BuildLayout.Bundle> bundles)
        {
            Name = bundles.Count > 1 ? $"{bundles.Count} indirectly referenced bundles" : $"{bundles.Count} indirectly referenced bundle";
            FileSizeBundle = 0;
            FileSizeUncompressed = 0;

            HashSet<BuildLayout.Bundle> countedBundles = new HashSet<BuildLayout.Bundle>();
            foreach (var b in bundles)
            {
                FileSizeBundle += b.FileSize;
                FileSizeUncompressed += b.UncompressedFileSize;
                if (!countedBundles.Contains(b))
                {
                    FileSizePlusRefs += b.FileSize;
                    countedBundles.Add(b);
                }

                foreach (var depB in b.ExpandedDependencies)
                {
                    if (!countedBundles.Contains(depB))
                    {
                        FileSizePlusRefs += depB.FileSize;
                        countedBundles.Add(depB);
                    }
                }

                foreach (var depB in b.Dependencies)
                {
                    if (!countedBundles.Contains(depB))
                    {
                        FileSizePlusRefs += depB.FileSize;
                        countedBundles.Add(depB);
                    }
                }
            }
        }
    }


    class BundlesContentView : ContentView
    {
        IList<TreeViewItemData<BundlesViewBuildReportItem>> m_TreeRoots;

        public BundlesContentView(BuildReportHelperConsumer helperConsumer, DetailsView detailsView)
            : base(helperConsumer, detailsView)
        {

        }

        internal override ContentViewColumnData[] ColumnDataForView
        {
            get
            {
                return new ContentViewColumnData[]
                {
                new ContentViewColumnData(BuildReportUtility.BundlesContentViewColBundleName, this, true, "Bundle Name"),
                new ContentViewColumnData(BuildReportUtility.BundlesContentViewColSizePlusRefs, this, false, "Total Size (+ refs)"),
                new ContentViewColumnData(BuildReportUtility.BundlesContentViewColSizeUncompressed, this, false, "Uncompressed Size"),
                new ContentViewColumnData(BuildReportUtility.BundlesContentViewBundleSize, this, false, "Bundle File Size"),
                new ContentViewColumnData(BuildReportUtility.BundlesContentViewColRefsTo, this, false, "Refs To"),
                new ContentViewColumnData(BuildReportUtility.BundlesContentViewColRefsBy, this, false, "Refs By"),
               };
            }
        }

        // Data about bundles from our currently selected build report.
        public override IList<IAddressablesBuildReportItem> CreateTreeViewItems(BuildLayout report)
        {
            List<IAddressablesBuildReportItem> buildReportBundles = new List<IAddressablesBuildReportItem>();
            if (report == null)
                return buildReportBundles;

            foreach (BuildLayout.Bundle bundle in BuildLayoutHelpers.EnumerateBundles(report))
            {
                var buildReportBundle = new BundlesViewBuildReportBundle(bundle);

                var explicitAssets = new List<BuildLayout.ExplicitAsset>();
                var implicitAssets = new List<BuildLayout.DataFromOtherAsset>();
                foreach (BuildLayout.File file in bundle.Files)
                {
                    foreach (BuildLayout.ExplicitAsset asset in file.Assets)
                    {
                        explicitAssets.Add(asset);
                    }
                    foreach (BuildLayout.DataFromOtherAsset asset in file.OtherAssets)
                    {
                        implicitAssets.Add(asset);
                    }
                }
                buildReportBundle.AssetsCount = explicitAssets.Count + implicitAssets.Count;
                buildReportBundle.ExplicitAssets = explicitAssets;
                buildReportBundle.ImplicitAssets = implicitAssets;
                buildReportBundles.Add(buildReportBundle);
            }

            return buildReportBundles;
        }

        public override void Consume(BuildLayout buildReport)
        {
            if (buildReport == null)
                return;

            m_DataHashtoReportItem = new Dictionary<Hash128, TreeDataReportItem>();
            m_Report = buildReport;
            m_TreeItems = CreateTreeViewItems(m_Report);
            IList<TreeViewItemData<BundlesViewBuildReportItem>> treeRoots = CreateTreeRootsNestedList(m_TreeItems, m_DataHashtoReportItem);
            m_TreeView.SetRootItems(treeRoots);
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

            m_TreeView.SetRootItems(CreateTreeRootsNestedList(sortedRootList, m_DataHashtoReportItem));
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
            m_SearchValue = evt.newValue.ToLowerInvariant();
            m_TreeRoots = CreateTreeRootsNestedList(m_TreeItems, m_DataHashtoReportItem);
            m_TreeView.SetRootItems(m_TreeRoots);
            m_TreeView.Rebuild();
        }

        // Expresses bundle data as a hierarchal list of BuildReportBundleViewItem objects.
        public IList<TreeViewItemData<BundlesViewBuildReportItem>> CreateTreeRootsNestedList(IList<IAddressablesBuildReportItem> items, Dictionary<Hash128, TreeDataReportItem> dataHashToReportItem)
        {
            int id = 0;
            var roots = new List<TreeViewItemData<BundlesViewBuildReportItem>>();

            foreach (BundlesViewBuildReportItem item in items)
            {
                BundlesViewBuildReportBundle bundle = item as BundlesViewBuildReportBundle;
                if (bundle == null)
                    continue;

                bool includeAllDependencies = EntryAppearsInSearch(item, m_SearchValue);

                var children = CreateChildrenOfBundle(bundle, ref id, includeAllDependencies);

                if (children.Count > 0 || includeAllDependencies)
                {
                    var rootItem = new TreeViewItemData<BundlesViewBuildReportItem>(++id, item, children);
                    dataHashToReportItem.TryAdd(BuildReportUtility.ComputeDataHash(item.Name), new TreeDataReportItem(id, rootItem.data));
                    roots.Add(rootItem);
                }
            }
            return roots;
        }
        private List<TreeViewItemData<BundlesViewBuildReportItem>> CreateChildrenOfBundle(BundlesViewBuildReportBundle bundle, ref int id, bool includeAllDependencies)
        {
           var children = new List<TreeViewItemData<BundlesViewBuildReportItem>>();
           var indirectlyReferencedBundleReportItems = new List<TreeViewItemData<BundlesViewBuildReportItem>>();
           var indirectlyReferencedBundles = new List<BuildLayout.Bundle>();

           CreateAssetEntries(children, out var bundlesReferencedByAssetEntries, bundle, ref id, includeAllDependencies);

           foreach (var depBundle in bundle.Bundle.ExpandedDependencies)
           {
               if (!bundlesReferencedByAssetEntries.Contains(depBundle))
               {
                   var reportBundle = new BundlesViewBuildReportBundle(depBundle);
                   if (includeAllDependencies || EntryAppearsInSearch(reportBundle, m_SearchValue))
                   {
                       indirectlyReferencedBundleReportItems.Add(new TreeViewItemData<BundlesViewBuildReportItem>(++id, reportBundle));
                       indirectlyReferencedBundles.Add(depBundle);
                   }
               }
           }

           if (indirectlyReferencedBundles.Count > 0)
           {
               children.Add(new TreeViewItemData<BundlesViewBuildReportItem>(++id, new BundlesViewBuildReportIndirectlyReferencedBundles(indirectlyReferencedBundles), indirectlyReferencedBundleReportItems));
           }

           return children;
        }

        private void CreateAssetEntries(List<TreeViewItemData<BundlesViewBuildReportItem>> children, out HashSet<BuildLayout.Bundle> directlyReferencedBundles, BundlesViewBuildReportBundle bundle, ref int id, bool includeAllDependencies)
        {
            directlyReferencedBundles = new HashSet<BuildLayout.Bundle>();
            foreach (var asset in bundle.ExplicitAssets)
            {
                if (asset == null)
                    continue;
                var reportAsset = new BundlesViewBuildReportAsset(asset);
                bool includeAsset = EntryAppearsInSearch(reportAsset, m_SearchValue);
                var childrenOfAsset = GenerateChildrenOfAsset(asset, ref id, directlyReferencedBundles, includeAllDependencies || includeAsset);
                if (includeAsset || includeAllDependencies || childrenOfAsset.Count > 0)
                {
                    var dataItem = new TreeViewItemData<BundlesViewBuildReportItem>(++id, reportAsset, childrenOfAsset);
                    m_DataHashtoReportItem.TryAdd(BuildReportUtility.ComputeDataHash(bundle.Name, asset.AddressableName), new TreeDataReportItem(id, dataItem.data));
                    children.Add(dataItem);
                }
            }
            foreach (var asset in bundle.ImplicitAssets)
            {
                if (asset == null)
                    continue;
                var reportAsset = new BundlesViewBuildReportAsset(asset);
                if (EntryAppearsInSearch(reportAsset, m_SearchValue) || includeAllDependencies)
                {
                    children.Add(new TreeViewItemData<BundlesViewBuildReportItem>(++id, reportAsset));
                }
            }
        }

        private List<TreeViewItemData<BundlesViewBuildReportItem>> GenerateChildrenOfAsset(BuildLayout.ExplicitAsset asset, ref int id, HashSet<BuildLayout.Bundle> referencedBundles, bool includeAllDependencies)
        {
            var childrenOfAsset = new List<TreeViewItemData<BundlesViewBuildReportItem>>();
            Dictionary<BuildLayout.Bundle, List<BuildLayout.ExplicitAsset>> bundleToAssetList = new Dictionary<BuildLayout.Bundle, List<BuildLayout.ExplicitAsset>>();

            foreach (var dep in asset.InternalReferencedExplicitAssets)
            {
                var reportAsset = new BundlesViewBuildReportAsset(dep);
                if (EntryAppearsInSearch(reportAsset, m_SearchValue) || includeAllDependencies)
                {
                    childrenOfAsset.Add(new TreeViewItemData<BundlesViewBuildReportItem>(++id, reportAsset));
                }
            }

            foreach (var dep in asset.ExternallyReferencedAssets)
            {
                if (!bundleToAssetList.ContainsKey(dep.Bundle))
                {
                    bundleToAssetList.Add(dep.Bundle, new List<BuildLayout.ExplicitAsset>());
                    referencedBundles.Add(dep.Bundle);
                }
                bundleToAssetList[dep.Bundle].Add(dep);
            }

            foreach (var bundle in bundleToAssetList.Keys)
            {
                var reportBundle = new BundlesViewBuildReportBundle(bundle);
                bool bundleIncludedInSearch = EntryAppearsInSearch(reportBundle, m_SearchValue) || includeAllDependencies;

                var assetTreeViewItems = new List<TreeViewItemData<BundlesViewBuildReportItem>>();
                var assetList = bundleToAssetList[bundle];
                ulong unrelatedAssetSize = bundle.FileSize;
                foreach (var bundleAsset in assetList)
                {
                    var reportBundleAsset = new BundlesViewBuildReportAsset(bundleAsset);
                    if (bundleIncludedInSearch || EntryAppearsInSearch(reportBundleAsset, m_SearchValue))
                    {
                        assetTreeViewItems.Add(new TreeViewItemData<BundlesViewBuildReportItem>(++id, reportBundleAsset));
                        unrelatedAssetSize -= bundleAsset.SerializedSize + bundleAsset.StreamedSize;
                    }
                }

                int unrelatedAssetCount = bundle.AssetCount - assetList.Count;
                if (unrelatedAssetCount > 0 && bundleIncludedInSearch)
                    assetTreeViewItems.Add(new TreeViewItemData<BundlesViewBuildReportItem>(++id, new BundlesViewBuildReportUnrelatedAssets(unrelatedAssetSize, unrelatedAssetCount)));

                if (bundleIncludedInSearch || assetTreeViewItems.Count > 0)
                {
                    childrenOfAsset.Add(new TreeViewItemData<BundlesViewBuildReportItem>(++id, reportBundle, assetTreeViewItems));
                }
            }

            foreach (var dep in asset.InternalReferencedOtherAssets)
            {
                var reportAsset = new BundlesViewBuildReportAsset(dep);
                if (EntryAppearsInSearch(reportAsset, m_SearchValue) || includeAllDependencies)
                {
                    childrenOfAsset.Add(new TreeViewItemData<BundlesViewBuildReportItem>(++id, reportAsset));
                }
            }

            return childrenOfAsset;
        }
    }
}
#endif
