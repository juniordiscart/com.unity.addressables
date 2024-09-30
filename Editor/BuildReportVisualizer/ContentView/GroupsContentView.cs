#if UNITY_2022_2_OR_NEWER
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    internal class GroupsViewBuildReportItem : IAddressablesBuildReportItem
    {
        public BuildLayout.Group Group { get; set; }
        public string Name { get; protected set; }
        public ulong FileSizePlusRefs { get; set; }

        public ulong FileSizeUncompressed { get; set; }

        public ulong FileSizeBundle { get; set; }

        public int RefsTo { get; set; }

        public int RefsBy { get; set; }

        public virtual void CreateGUI(VisualElement rootVisualElement)
        {
        }

        public virtual string GetCellContent(string colName)
        {
            if (colName == BuildReportUtility.GroupsContentViewColGroupName)
                return Name;
            else if (colName == BuildReportUtility.GroupsContentViewColSizePlusRefs)
            {
                if (FileSizePlusRefs == 0)
                    return "--";
                return BuildReportUtility.GetDenominatedBytesString(FileSizePlusRefs);
            }
            else if (colName == BuildReportUtility.GroupsContentViewColSizeUncompressed)
            {
                if (FileSizeUncompressed == 0)
                    return "--";
                return BuildReportUtility.GetDenominatedBytesString(FileSizeUncompressed);
            }
            else if (colName == BuildReportUtility.GroupsContentViewColBundleSize)
            {
                if (FileSizeBundle == 0)
                    return "--";
                return BuildReportUtility.GetDenominatedBytesString(FileSizeBundle);
            }
            else if (colName == BuildReportUtility.GroupsContentViewColRefsTo)
            {
                if (RefsTo == -1)
                    return "--";
                return RefsTo.ToString();
            }
            else if (colName == BuildReportUtility.GroupsContentViewColRefsBy)
            {
                if (RefsBy == -1)
                    return "--";
                return RefsBy.ToString();
            }

            return "";
        }

        public string GetSortContent(string colName)
        {
            if (colName == BuildReportUtility.GroupsContentViewColSizePlusRefs)
                return FileSizePlusRefs.ToString();
            if (colName == BuildReportUtility.GroupsContentViewColSizeUncompressed)
                return FileSizeUncompressed.ToString();
            if (colName == BuildReportUtility.GroupsContentViewColBundleSize)
                return FileSizeBundle.ToString();
            if (colName == BuildReportUtility.GroupsContentViewColRefsBy)
                return RefsBy.ToString();
            if (colName == BuildReportUtility.GroupsContentViewColRefsTo)
                return RefsTo.ToString();
            return GetCellContent(colName);
        }
    }

    internal class GroupsViewBuildReportGroup : GroupsViewBuildReportItem
    {
        public List<BuildLayout.ExplicitAsset> BuildReportAssets;

        public GroupsViewBuildReportGroup(BuildLayout.Group group, List<BuildLayout.ExplicitAsset> reportAssets)
        {
            Name = group.Name;
            Group = group;
            BuildReportAssets = reportAssets;
            FileSizePlusRefs = 0;
            FileSizeBundle = 0;
            FileSizeUncompressed = 0;
            RefsBy = -1;
            RefsTo = group.Bundles.Count;
        }
    }

    internal class GroupsViewBuildReportAsset : GroupsViewBuildReportItem, IAddressablesBuildReportAsset
    {
        public List<BuildLayout.ExplicitAsset> InternallyReferencedAssets { get; }

        public List<BuildLayout.ExplicitAsset> ExternallyReferencedAssets { get; }

        public List<BuildLayout.DataFromOtherAsset> ImplicitDependencies { get; }


        public BuildLayout.ExplicitAsset ExplicitAsset { get; }
        public BuildLayout.DataFromOtherAsset DataFromOtherAsset { get; }
        public List<BuildLayout.Bundle> Bundles { get; }
        public ulong SizeWDependencies { get; }

        public List<BuildLayout.ExplicitAsset> ReferencingAssets { get; }
        public GroupsViewBuildReportAsset(BuildLayout.ExplicitAsset asset)
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
                FileSizePlusRefs += r.SerializedSize + r.StreamedSize;
            foreach (var r in asset.InternalReferencedExplicitAssets)
                FileSizePlusRefs += r.SerializedSize + r.StreamedSize;
            RefsTo = asset.ExternallyReferencedAssets.Count + asset.InternalReferencedExplicitAssets.Count;
            RefsBy = asset.ReferencingAssets != null ? asset.ReferencingAssets.Count : -1;
        }

        public GroupsViewBuildReportAsset(BuildLayout.DataFromOtherAsset asset)
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

    internal class GroupsViewBuildReportBundle : GroupsViewBuildReportItem, IAddressablesBuildReportBundle
    {
        public GroupsViewBuildReportBundle(BuildLayout.Bundle bundle)
        {
            Name = bundle.Name;
            Bundle = bundle;
            FileSizeBundle = bundle.FileSize;
            FileSizeUncompressed = bundle.UncompressedFileSize;
            FileSizePlusRefs = bundle.FileSize + bundle.ExpandedDependencyFileSize + bundle.DependencyFileSize;
            foreach (var file in bundle.Files)
                RefsTo += file.Assets.Count + file.OtherAssets.Count;
            RefsBy = Bundle.DependentBundles.Count;
        }

        public BuildLayout.Bundle Bundle { get; }
    }

    internal class GroupsViewBuildReportUnrelatedAssets : GroupsViewBuildReportItem
    {
        public GroupsViewBuildReportUnrelatedAssets(ulong assetSize, int assetCount)
        {
            Name = $"({assetCount} unrelated assets)";
            FileSizeUncompressed = assetSize;
        }
    }

    internal class GroupsViewBuildReportIndirectlyReferencedBundles : GroupsViewBuildReportItem
    {
        public GroupsViewBuildReportIndirectlyReferencedBundles(List<BuildLayout.Bundle> bundles)
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


    class GroupsContentView : ContentView
    {
        public GroupsContentView(BuildReportHelperConsumer helperConsumer,  DetailsView detailsView)
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
                    new ContentViewColumnData(BuildReportUtility.GroupsContentViewColGroupName, this, true, "Group Name"),
                    new ContentViewColumnData(BuildReportUtility.GroupsContentViewColSizePlusRefs, this, false, "Total Size (+ refs)"),
                    new ContentViewColumnData(BuildReportUtility.GroupsContentViewColSizeUncompressed, this, false, "Uncompressed Size"),
                    new ContentViewColumnData(BuildReportUtility.GroupsContentViewColBundleSize, this, false, "Bundle File Size"),
                    new ContentViewColumnData(BuildReportUtility.GroupsContentViewColRefsTo, this, false, "Refs To"),
                    new ContentViewColumnData(BuildReportUtility.GroupsContentViewColRefsBy, this, false, "Refs By")
                };
            }
        }

        public override IList<IAddressablesBuildReportItem> CreateTreeViewItems(BuildLayout report)
        {
            List<IAddressablesBuildReportItem> buildReportGroups = new List<IAddressablesBuildReportItem>();
            if (report == null)
                return buildReportGroups;

            var groupToAssets = new Dictionary<BuildLayout.Group, List<BuildLayout.ExplicitAsset>>();
            foreach (var asset in BuildLayoutHelpers.EnumerateAssets(report))
            {
                if (groupToAssets.ContainsKey(asset.Bundle.Group))
                    groupToAssets[asset.Bundle.Group].Add(asset);
                else
                    groupToAssets.Add(asset.Bundle.Group, new List<BuildLayout.ExplicitAsset> { asset });
            }

            foreach (var pair in groupToAssets)
            {
                buildReportGroups.Add(new GroupsViewBuildReportGroup(pair.Key, pair.Value));
            }

            return buildReportGroups;
        }

        internal IList<TreeViewItemData<GroupsViewBuildReportItem>> CreateTreeRootsNestedList(IList<IAddressablesBuildReportItem> items)
        {
            int id = 0;
            var roots = new List<TreeViewItemData<GroupsViewBuildReportItem>>();
            foreach (var item in items)
            {
                var group = item as GroupsViewBuildReportGroup;
                if (group == null)
                    continue;

                bool includeAllDependencies = EntryAppearsInSearch(group, m_SearchValue);

                List<TreeViewItemData<GroupsViewBuildReportItem>> bundlesUnderGroup = CreateGroupBundles(group, ref id, includeAllDependencies);

                if (bundlesUnderGroup.Count > 0 || includeAllDependencies)
                {
                    var groupItem = new TreeViewItemData<GroupsViewBuildReportItem>(++id, group, bundlesUnderGroup);
                    m_DataHashtoReportItem.TryAdd(BuildReportUtility.ComputeDataHash(group.Name), new TreeDataReportItem(id, groupItem.data));
                    roots.Add(groupItem);
                }
            }

            return roots;
        }

        private List<TreeViewItemData<GroupsViewBuildReportItem>> CreateGroupBundles(GroupsViewBuildReportGroup group, ref int id, bool includeAllDependencies)
        {
            var bundlesUnderGroup = new List<TreeViewItemData<GroupsViewBuildReportItem>>();
            foreach (var bundle in group.Group.Bundles)
            {
                var bundleReportItem = new GroupsViewBuildReportBundle(bundle);
                var children = new List<TreeViewItemData<GroupsViewBuildReportItem>>();
                var directlyReferencedBundles = new HashSet<BuildLayout.Bundle>();

                PopulateAssets(children, directlyReferencedBundles, bundle, ref id, includeAllDependencies);
                PopulateIndirectlyReferencedBundles(children, directlyReferencedBundles, bundle, ref id, includeAllDependencies);

                if (children.Count > 0 || EntryAppearsInSearch(bundleReportItem, m_SearchValue))
                {
                    var bundleItem = new TreeViewItemData<GroupsViewBuildReportItem>(++id, bundleReportItem, children);
                    m_DataHashtoReportItem.TryAdd(BuildReportUtility.ComputeDataHash(group.Name, bundle.Name), new TreeDataReportItem(id, bundleItem.data));
                    bundlesUnderGroup.Add(bundleItem);
                }
            }

            return bundlesUnderGroup;
        }

        private void PopulateAssets(List<TreeViewItemData<GroupsViewBuildReportItem>> children,
                                                        HashSet<BuildLayout.Bundle> directlyReferencedBundles, BuildLayout.Bundle bundle, ref int id, bool includeAllDependencies)
        {
            foreach (var asset in BuildLayoutHelpers.EnumerateAssets(bundle))
            {
                var reportAssetItem = new GroupsViewBuildReportAsset(asset);
                bool assetAppearsInSearch = EntryAppearsInSearch(reportAssetItem, m_SearchValue) || includeAllDependencies;
                var childrenOfAsset = GenerateChildrenOfAsset(asset, ref id, directlyReferencedBundles, assetAppearsInSearch);
                if (assetAppearsInSearch || childrenOfAsset.Count > 0)
                {
                    var assetItem = new TreeViewItemData<GroupsViewBuildReportItem>(++id, reportAssetItem, childrenOfAsset);
                    m_DataHashtoReportItem.TryAdd(BuildReportUtility.ComputeDataHash(asset.AddressableName), new TreeDataReportItem(id, assetItem.data));
                    children.Add(assetItem);
                }
            }

            foreach (var asset in BuildLayoutHelpers.EnumerateImplicitAssets(bundle))
            {
                var reportImplicitAssetItem = new GroupsViewBuildReportAsset(asset);
                if (includeAllDependencies || EntryAppearsInSearch(reportImplicitAssetItem, m_SearchValue))
                    children.Add(new TreeViewItemData<GroupsViewBuildReportItem>(++id, reportImplicitAssetItem));
            }
        }

        private void PopulateIndirectlyReferencedBundles(List<TreeViewItemData<GroupsViewBuildReportItem>> children,
                                                        HashSet<BuildLayout.Bundle> directlyReferencedBundles, BuildLayout.Bundle bundle, ref int id, bool includeAllDependencies)
        {
            var indirectlyReferencedBundleReportItems = new List<TreeViewItemData<GroupsViewBuildReportItem>>();
            var indirectlyReferencedBundles = new List<BuildLayout.Bundle>();
            foreach (var depBundle in bundle.ExpandedDependencies)
            {
                if (!directlyReferencedBundles.Contains(depBundle))
                {
                    var reportBundle = new GroupsViewBuildReportBundle(depBundle);
                    if (includeAllDependencies || EntryAppearsInSearch(reportBundle, m_SearchValue))
                    {
                        indirectlyReferencedBundleReportItems.Add(new TreeViewItemData<GroupsViewBuildReportItem>(++id, reportBundle));
                        indirectlyReferencedBundles.Add(depBundle);
                    }
                }
            }

            if (indirectlyReferencedBundles.Count > 0)
            {
                children.Add(new TreeViewItemData<GroupsViewBuildReportItem>(++id, new GroupsViewBuildReportIndirectlyReferencedBundles(indirectlyReferencedBundles), indirectlyReferencedBundleReportItems));
            }
        }

        private List<TreeViewItemData<GroupsViewBuildReportItem>> GenerateChildrenOfAsset(BuildLayout.ExplicitAsset asset, ref int id, HashSet<BuildLayout.Bundle> referencedBundles, bool includeAllDependencies)
        {
            var childrenOfAsset = new List<TreeViewItemData<GroupsViewBuildReportItem>>();
            foreach (var dep in asset.InternalReferencedExplicitAssets)
            {
                var reportDepAsset = new GroupsViewBuildReportAsset(dep);
                if (includeAllDependencies || EntryAppearsInSearch(reportDepAsset, m_SearchValue))
                {
                    var tvid = new TreeViewItemData<GroupsViewBuildReportItem>(++id, reportDepAsset);
                    childrenOfAsset.Add(tvid);
                }
            }

            Dictionary<BuildLayout.Bundle, List<BuildLayout.ExplicitAsset>> bundleToAssetList = new Dictionary<BuildLayout.Bundle, List<BuildLayout.ExplicitAsset>>();
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
                var bundleReportAsset = new GroupsViewBuildReportBundle(bundle);
                bool includeAllDependenciesUnderBundle = EntryAppearsInSearch(bundleReportAsset, m_SearchValue) || includeAllDependencies;
                var assetTreeViewItems = new List<TreeViewItemData<GroupsViewBuildReportItem>>();
                var assetList = bundleToAssetList[bundle];
                ulong unrelatedAssetSize = bundle.FileSize;
                foreach (var bundleAsset in assetList)
                {
                    var reportAsset = new GroupsViewBuildReportAsset(bundleAsset);
                    if (includeAllDependenciesUnderBundle || EntryAppearsInSearch(reportAsset, m_SearchValue))
                    {
                        var tvid = new TreeViewItemData<GroupsViewBuildReportItem>(++id, reportAsset);
                        assetTreeViewItems.Add(tvid);
                        unrelatedAssetSize -= bundleAsset.SerializedSize + bundleAsset.StreamedSize;
                    }
                }

                int unrelatedAssetCount = bundle.AssetCount - assetList.Count;
                if (unrelatedAssetCount > 0 && includeAllDependenciesUnderBundle)
                {
                    var tvid = new TreeViewItemData<GroupsViewBuildReportItem>(++id, new GroupsViewBuildReportUnrelatedAssets(unrelatedAssetSize, unrelatedAssetCount));
                    assetTreeViewItems.Add(tvid);
                }

                if (includeAllDependenciesUnderBundle || assetTreeViewItems.Count > 0)
                {
                    var rootItem = new TreeViewItemData<GroupsViewBuildReportItem>(++id, bundleReportAsset, assetTreeViewItems);
                    childrenOfAsset.Add(rootItem);
                }
            }

            foreach (var dep in asset.InternalReferencedOtherAssets)
            {
                var reportDepAsset = new GroupsViewBuildReportAsset(dep);
                if (includeAllDependencies || EntryAppearsInSearch(reportDepAsset, m_SearchValue))
                {
                    var tvid = new TreeViewItemData<GroupsViewBuildReportItem>(++id, reportDepAsset);
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
            IList<TreeViewItemData<GroupsViewBuildReportItem>> treeRoots = CreateTreeRootsNestedList(m_TreeItems);
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
    }

}
#endif
