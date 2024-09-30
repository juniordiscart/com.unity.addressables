#if UNITY_2022_2_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.AddressableAssets.BuildReportVisualizer.BuildReportWindow;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    internal class LabelsViewBuildReportItem : IAddressablesBuildReportItem
    {
        public string Name { get; protected set; }

        public ulong FileSizePlusRefs { get; set; }

        public ulong FileSizeUncompressed { get; set; }

        public ulong FileSizeBundle { get; set; }

        public int RefsBy { get; set; }

        public int RefsTo { get; set; }


        public virtual void CreateGUI(VisualElement rootVisualElement) { }

        public virtual string GetCellContent(string colName)
        {
            if (colName == BuildReportUtility.LabelsContentViewColLabelName)
                return Name;
            else if (colName == BuildReportUtility.LabelsContentViewColSizePlusRefs)
            {
                if (FileSizePlusRefs == 0)
                    return "--";
                return BuildReportUtility.GetDenominatedBytesString(FileSizePlusRefs);
            }
            else if (colName == BuildReportUtility.LabelsContentViewColSizeUncompressed)
            {
                if (FileSizeUncompressed == 0)
                    return "--";
                return BuildReportUtility.GetDenominatedBytesString(FileSizeUncompressed);
            }
            else if (colName == BuildReportUtility.LabelsContentViewColSizeBundle)
            {
                if (FileSizeBundle == 0)
                    return "--";
                return BuildReportUtility.GetDenominatedBytesString(FileSizeBundle);
            }
            else if (colName == BuildReportUtility.LabelsContentViewColRefsTo)
            {
                if (RefsTo == -1)
                    return "--";
                return RefsTo.ToString();
            }
            else if (colName == BuildReportUtility.LabelsContentViewColRefsBy)
            {
                if (RefsBy == -1)
                    return "--";
                return RefsBy.ToString();
            }

            return "";
        }

        public virtual string GetSortContent(string colName)
        {
            if (colName == BuildReportUtility.LabelsContentViewColSizePlusRefs)
                return FileSizePlusRefs.ToString();
            if (colName == BuildReportUtility.LabelsContentViewColSizeUncompressed)
                return FileSizeUncompressed.ToString();
            if (colName == BuildReportUtility.LabelsContentViewColSizeBundle)
                return FileSizeBundle.ToString();
            if (colName == BuildReportUtility.LabelsContentViewColRefsBy)
                return RefsBy.ToString();
            if (colName == BuildReportUtility.LabelsContentViewColRefsTo)
                return RefsTo.ToString();
            return GetCellContent(colName);
        }
    }

    internal class LabelsViewBuildReportLabel : LabelsViewBuildReportItem
    {
        public ulong SizeWDependencies { get; set; }
        public List<BuildLayout.ExplicitAsset> BuildReportAssets { get; set; }

        public LabelsViewBuildReportLabel(string name, List<BuildLayout.ExplicitAsset> reportAssets)
        {
            Name = name;
            BuildReportAssets = reportAssets;
            FileSizePlusRefs = 0;
            FileSizeBundle = 0;
            RefsBy = -1;
            RefsTo = reportAssets.Count;
            var seenBundles = new HashSet<BuildLayout.Bundle>();
            foreach (var reportItem in reportAssets)
            {
                FileSizePlusRefs += reportItem.SerializedSize + reportItem.StreamedSize;
                if (!seenBundles.Contains(reportItem.Bundle))
                {
                    seenBundles.Add(reportItem.Bundle);
                    FileSizeBundle += reportItem.Bundle.FileSize;
                }

                foreach (var dep in reportItem.ExternallyReferencedAssets)
                {
                    FileSizePlusRefs += dep.SerializedSize + dep.StreamedSize;
                    if (!seenBundles.Contains(dep.Bundle))
                    {
                        seenBundles.Add(dep.Bundle);
                        FileSizeBundle += dep.Bundle.FileSize;
                    }
                }
            }
            FileSizeUncompressed = FileSizePlusRefs;
        }
    }

    internal class LabelsViewBuildReportAsset : LabelsViewBuildReportItem, IAddressablesBuildReportAsset
    {
        public BuildLayout.ExplicitAsset ExplicitAsset { get; }

        public BuildLayout.DataFromOtherAsset DataFromOtherAsset { get; }

        public List<BuildLayout.ExplicitAsset> InternallyReferencedAssets { get; }

        public List<BuildLayout.ExplicitAsset> ExternallyReferencedAssets { get; }

        List<BuildLayout.ExplicitAsset> ReferencingAssets { get; }
        public List<BuildLayout.DataFromOtherAsset> ImplicitDependencies { get; }
        public List<BuildLayout.Bundle> Bundles { get; }
        public ulong SizeWDependencies { get; }

        public bool IsAddressable => ExplicitAsset != null;

        public LabelsViewBuildReportAsset(BuildLayout.ExplicitAsset asset)
        {
            ExplicitAsset = asset;
            Name = asset.AddressableName;
            ExternallyReferencedAssets = asset.ExternallyReferencedAssets;
            InternallyReferencedAssets = asset.InternalReferencedExplicitAssets;
            ImplicitDependencies = asset.InternalReferencedOtherAssets;
            ReferencingAssets = asset.ReferencingAssets;
            FileSizeUncompressed = asset.SerializedSize + asset.StreamedSize;
            FileSizePlusRefs = FileSizeUncompressed;
            foreach (var r in asset.ExternallyReferencedAssets)
                if (r!= null)
                    FileSizePlusRefs += r.SerializedSize + r.StreamedSize;
            foreach (var r in asset.InternalReferencedExplicitAssets)
                if (r!= null)
                    FileSizePlusRefs += r.SerializedSize + r.StreamedSize;
            RefsTo = asset.ExternallyReferencedAssets.Count + asset.InternalReferencedExplicitAssets.Count;
            RefsBy = asset.ReferencingAssets != null ? asset.ReferencingAssets.Count : -1;
        }

        public LabelsViewBuildReportAsset(BuildLayout.DataFromOtherAsset asset)
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

    internal class LabelsViewBuildReportBundle : LabelsViewBuildReportItem, IAddressablesBuildReportBundle
    {
        public LabelsViewBuildReportBundle(BuildLayout.Bundle bundle)
        {
            Name = bundle.Name;
            Bundle = bundle;
            FileSizePlusRefs = bundle.FileSize + bundle.ExpandedDependencyFileSize + bundle.DependencyFileSize;
            FileSizeBundle = bundle.FileSize;
            FileSizeUncompressed = bundle.UncompressedFileSize;
            RefsTo = Bundle.ExpandedDependencies.Count;
            RefsBy = Bundle.DependentBundles.Count;
        }

        public BuildLayout.Bundle Bundle { get; }
    }

    internal class LabelsViewBuildReportUnrelatedAssets : LabelsViewBuildReportItem
    {
        public LabelsViewBuildReportUnrelatedAssets(ulong assetSize, int assetCount)
        {
            Name = $"({assetCount} unrelated assets)";
            FileSizeUncompressed = assetSize;
        }
    }

    class LabelsContentView : ContentView
    {
        public LabelsContentView(BuildReportHelperConsumer helperConsumer, DetailsView detailsView)
            : base(helperConsumer, detailsView) { }

        internal override ContentViewColumnData[] ColumnDataForView
        {
            get
            {
                return new ContentViewColumnData[]
                {
                    new ContentViewColumnData(BuildReportUtility.LabelsContentViewColLabelName, this, true, "Label Name"),
                    new ContentViewColumnData(BuildReportUtility.LabelsContentViewColSizePlusRefs, this, false, "Total Size (+ refs)"),
                    new ContentViewColumnData(BuildReportUtility.LabelsContentViewColSizeUncompressed, this, false, "Uncompressed Size"),
                    new ContentViewColumnData(BuildReportUtility.LabelsContentViewColSizeBundle, this, false, "Bundle File Size"),
                    new ContentViewColumnData(BuildReportUtility.LabelsContentViewColRefsTo, this, false, "Refs To"),
                    new ContentViewColumnData(BuildReportUtility.LabelsContentViewColRefsBy, this, false, "Refs By")
                };
            }
        }

        public override IList<IAddressablesBuildReportItem> CreateTreeViewItems(BuildLayout report)
        {
            List<IAddressablesBuildReportItem> buildReportLabels = new List<IAddressablesBuildReportItem>();
            if (report == null)
                return buildReportLabels;

            var labelToAssets = new Dictionary<string, List<BuildLayout.ExplicitAsset>>();
            foreach (var asset in BuildLayoutHelpers.EnumerateAssets(report))
            {
                foreach (string label in asset.Labels)
                {
                    if (labelToAssets.ContainsKey(label))
                        labelToAssets[label].Add(asset);
                    else
                        labelToAssets.Add(label, new List<BuildLayout.ExplicitAsset> { asset });
                }
            }

            foreach (var pair in labelToAssets)
            {
                buildReportLabels.Add(new LabelsViewBuildReportLabel(pair.Key, pair.Value));
            }

            return buildReportLabels;
        }

        IList<TreeViewItemData<LabelsViewBuildReportItem>> CreateTreeRootsNestedList(IList<IAddressablesBuildReportItem> items)
        {
            int id = 0;
            var roots = new List<TreeViewItemData<LabelsViewBuildReportItem>>();
            foreach (var item in items)
            {
                var label = item as LabelsViewBuildReportLabel;
                if (label == null)
                    continue;

                bool includeAllDependencies = EntryAppearsInSearch(label, m_SearchValue);

                var assetsUnderLabel = new List<TreeViewItemData<LabelsViewBuildReportItem>>();
                foreach (var asset in label.BuildReportAssets)
                {
                    var assetReportItem = new LabelsViewBuildReportAsset(asset);
                    bool assetAppearsInSearch = EntryAppearsInSearch(assetReportItem, m_SearchValue) || includeAllDependencies;
                    var childrenOfAsset = GenerateChildrenOfAsset(asset, ref id, assetAppearsInSearch);
                    if (assetAppearsInSearch || childrenOfAsset.Count > 0)
                    {
                        var tvid = new TreeViewItemData<LabelsViewBuildReportItem>(++id, assetReportItem, childrenOfAsset);
                        m_DataHashtoReportItem.TryAdd(BuildReportUtility.ComputeDataHash(label.Name, asset.AddressableName), new TreeDataReportItem(id, tvid.data));
                        assetsUnderLabel.Add(tvid);
                    }
                }

                if (includeAllDependencies || assetsUnderLabel.Count > 0)
                {
                    var rootItem = new TreeViewItemData<LabelsViewBuildReportItem>(++id, label, assetsUnderLabel);
                    m_DataHashtoReportItem.TryAdd(BuildReportUtility.ComputeDataHash(label.Name), new TreeDataReportItem(id, rootItem.data));
                    roots.Add(rootItem);
                }
            }

            return roots;
        }

        private List<TreeViewItemData<LabelsViewBuildReportItem>> GenerateChildrenOfAsset(BuildLayout.ExplicitAsset asset, ref int id, bool includeAllDependencies)
        {
            var childrenOfAsset = new List<TreeViewItemData<LabelsViewBuildReportItem>>();
            foreach (var dep in asset.InternalReferencedExplicitAssets)
            {
                var reportDepAsset = new LabelsViewBuildReportAsset(dep);
                if (includeAllDependencies || EntryAppearsInSearch(reportDepAsset, m_SearchValue))
                {
                    var tvid = new TreeViewItemData<LabelsViewBuildReportItem>(++id, reportDepAsset);
                    childrenOfAsset.Add(tvid);
                }
            }

            Dictionary<BuildLayout.Bundle, List<BuildLayout.ExplicitAsset>> bundleToAssetList = new Dictionary<BuildLayout.Bundle, List<BuildLayout.ExplicitAsset>>();
            foreach (var dep in asset.ExternallyReferencedAssets)
            {
                if (!bundleToAssetList.ContainsKey(dep.Bundle))
                    bundleToAssetList.Add(dep.Bundle, new List<BuildLayout.ExplicitAsset>());
                bundleToAssetList[dep.Bundle].Add(dep);
            }

            foreach (var bundle in bundleToAssetList.Keys)
            {
                var bundleReportAsset = new LabelsViewBuildReportBundle(bundle);
                bool includeAllDependenciesUnderBundle = EntryAppearsInSearch(bundleReportAsset, m_SearchValue) || includeAllDependencies;
                var assetTreeViewItems = new List<TreeViewItemData<LabelsViewBuildReportItem>>();
                var assetList = bundleToAssetList[bundle];
                ulong unrelatedAssetSize = bundle.FileSize;
                foreach (var bundleAsset in assetList)
                {
                    var reportAsset = new LabelsViewBuildReportAsset(bundleAsset);
                    if (includeAllDependenciesUnderBundle || EntryAppearsInSearch(reportAsset, m_SearchValue))
                    {
                        var tvid = new TreeViewItemData<LabelsViewBuildReportItem>(++id, reportAsset);
                        assetTreeViewItems.Add(tvid);
                        unrelatedAssetSize -= bundleAsset.SerializedSize + bundleAsset.StreamedSize;
                    }
                }

                int unrelatedAssetCount = bundle.AssetCount - assetList.Count;
                if (unrelatedAssetCount > 0 && includeAllDependenciesUnderBundle)
                {
                    var tvid = new TreeViewItemData<LabelsViewBuildReportItem>(++id, new LabelsViewBuildReportUnrelatedAssets(unrelatedAssetSize, unrelatedAssetCount));
                    assetTreeViewItems.Add(tvid);
                }

                if (includeAllDependenciesUnderBundle || assetTreeViewItems.Count > 0)
                {
                    var rootItem = new TreeViewItemData<LabelsViewBuildReportItem>(++id, bundleReportAsset, assetTreeViewItems);
                    childrenOfAsset.Add(rootItem);
                }
            }

            foreach (var dep in asset.InternalReferencedOtherAssets)
            {
                var reportDepAsset = new LabelsViewBuildReportAsset(dep);
                if (includeAllDependencies || EntryAppearsInSearch(reportDepAsset, m_SearchValue))
                {
                    var tvid = new TreeViewItemData<LabelsViewBuildReportItem>(++id, reportDepAsset);
                    childrenOfAsset.Add(tvid);
                }
            }

            return childrenOfAsset;
        }

        public override void Consume(BuildLayout buildReport)
        {
            if (buildReport == null)
                return;

            m_Report = buildReport;
            m_TreeItems = CreateTreeViewItems(m_Report);
            m_DataHashtoReportItem = new Dictionary<Hash128, TreeDataReportItem>();
            IList<TreeViewItemData<LabelsViewBuildReportItem>> treeRoots = CreateTreeRootsNestedList(m_TreeItems);
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

    }
}
#endif
