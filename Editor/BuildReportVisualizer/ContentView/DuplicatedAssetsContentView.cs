#if UNITY_2022_2_OR_NEWER
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    internal class DuplicatedAssetsViewBuildReportItem : IAddressablesBuildReportItem
    {
        public string Name { get; set; }

        public BuildLayout.ExplicitAsset ExplicitAsset { get; set; }

        public BuildLayout.DataFromOtherAsset DataFromOtherAsset { get; set; }

        public List<BuildLayout.Bundle> Bundles { get; set; }

        public List<BuildLayout.ExplicitAsset> AssetDepsOf { get; set; }

        public ulong SizeWDependencies { get; protected set; }
        public ulong Size { get; protected set; }
        public int DepsCount { get; set; }
        public int DepsOfCount { get; set; }

        public ulong SpaceSavedIfDeduplicated { get; set; }

        public int DuplicationCount { get; set; }

        public void CreateGUI(VisualElement rootVisualElement)
        {

        }

        public string GetCellContent(string colName)
        {
            if (colName == BuildReportUtility.DuplicatedAssetsContentViewColAssetName)
                return Name;
            if (colName == BuildReportUtility.DuplicatedAssetsContentViewColSize)
                return BuildReportUtility.GetDenominatedBytesString(Size);
            if (colName == BuildReportUtility.DuplicatedAssetsContentViewSpaceSaved)
            {
                if (this is DuplicatedAssetsViewBuildReportAsset asset)
                    return asset.UsedByString;
                if (DuplicationCount == -1)
                    return "";
                return BuildReportUtility.GetDenominatedBytesString(SpaceSavedIfDeduplicated);
            }
            if (colName == BuildReportUtility.DuplicatedAssetsContentViewDuplicationCount)
            {
                if (DuplicationCount == -1)
                    return "";
                return DuplicationCount.ToString();
            }
            return "";
        }

        public string GetSortContent(string colName)
        {
            if (colName == BuildReportUtility.DuplicatedAssetsContentViewColSize)
                return Size.ToString();
            if (colName == BuildReportUtility.DuplicatedAssetsContentViewSpaceSaved)
                return SpaceSavedIfDeduplicated.ToString();
            if (colName == BuildReportUtility.DuplicatedAssetsContentViewSpaceSaved)
                return DuplicationCount.ToString();
            return GetCellContent(colName);
        }
    }

    internal class DuplicatedAssetsViewBuildReportDuplicatedAsset : DuplicatedAssetsViewBuildReportItem, IAddressablesBuildReportAsset
    {
        public new string GetCellContent(string colName)
        {
            if (colName == BuildReportUtility.DuplicatedAssetsContentViewColAssetName)
                return Name;
            if (colName == BuildReportUtility.DuplicatedAssetsContentViewColSize)
                return BuildReportUtility.GetDenominatedBytesString(Size);
            if (colName == BuildReportUtility.DuplicatedAssetsContentViewSpaceSaved)
                return BuildReportUtility.GetDenominatedBytesString(SpaceSavedIfDeduplicated);
            if (colName == BuildReportUtility.DuplicatedAssetsContentViewDuplicationCount)
                return DuplicationCount.ToString();
            return "";
        }

        public DuplicatedAssetsViewBuildReportDuplicatedAsset(BuildReportHelperDuplicateImplicitAsset helperAsset)
        {
            DataFromOtherAsset = helperAsset.Asset;
            Name = DataFromOtherAsset.AssetPath;
            SizeWDependencies = DataFromOtherAsset.SerializedSize + DataFromOtherAsset.StreamedSize;
            Size = DataFromOtherAsset.SerializedSize + DataFromOtherAsset.StreamedSize;
            DepsCount = 0;
            Bundles = helperAsset.Bundles;
            AssetDepsOf = helperAsset.GUIDToReferencingAssets.Values.ToList();
            DepsOfCount = AssetDepsOf.Count;
            DuplicationCount = Bundles.Count;
            if (DuplicationCount > 1)
            {
                SpaceSavedIfDeduplicated = (ulong) (DuplicationCount - 1) * Size;
            }
        }

        public DuplicatedAssetsViewBuildReportDuplicatedAsset(BuildLayout.DataFromOtherAsset implicitAsset)
        {
            DataFromOtherAsset = implicitAsset;
            Bundles = new List<BuildLayout.Bundle>() { implicitAsset.File.Bundle };
            Name = DataFromOtherAsset.AssetPath;
            SizeWDependencies = DataFromOtherAsset.SerializedSize + DataFromOtherAsset.StreamedSize;
            Size = DataFromOtherAsset.SerializedSize + DataFromOtherAsset.StreamedSize;
            DuplicationCount = -1;
        }
    }

    internal class DuplicatedAssetsViewBuildReportAsset : DuplicatedAssetsViewBuildReportItem, IAddressablesBuildReportAsset
    {
        public string UsedByString { get; set; }
        public DuplicatedAssetsViewBuildReportAsset(BuildLayout.ExplicitAsset asset, BuildLayout.DataFromOtherAsset implicitAsset)
        {
            ExplicitAsset = asset;
            Name = asset.AddressableName;
            Size = asset.SerializedSize + asset.StreamedSize;
            UsedByString = $"Uses {implicitAsset.AssetPath}";
            DuplicationCount = -1;
            SpaceSavedIfDeduplicated = 0;
        }
    }

    internal class DuplicatedAssetsViewBuildReportUnrelatedAssets : DuplicatedAssetsViewBuildReportItem
    {
        public DuplicatedAssetsViewBuildReportUnrelatedAssets(ulong assetSize, int assetCount)
        {
            Name = $"({assetCount} unrelated assets)";
            Size = assetSize;
            DuplicationCount = -1;
            SpaceSavedIfDeduplicated = 0;
        }
    }

    internal class DuplicatedAssetsViewBuildReportBundle : DuplicatedAssetsViewBuildReportItem, IAddressablesBuildReportBundle
    {
        public DuplicatedAssetsViewBuildReportBundle(BuildLayout.Bundle bundle)
        {
            Bundle = bundle;
            Name = bundle.Name;
            Size = bundle.FileSize;
            DuplicationCount = -1;
            SpaceSavedIfDeduplicated = 0;
        }

        public BuildLayout.Bundle Bundle { get; set; }
    }

    class DuplicatedAssetsContentView : ContentView
    {
        public DuplicatedAssetsContentView(BuildReportHelperConsumer helperConsumer, DetailsView detailsView)
            : base(helperConsumer, detailsView) { }

    internal override ContentViewColumnData[] ColumnDataForView
    {
        get
        {
            return new ContentViewColumnData[]
            {
                new ContentViewColumnData(BuildReportUtility.DuplicatedAssetsContentViewColAssetName,this, true, "Asset Name"),
                new ContentViewColumnData(BuildReportUtility.DuplicatedAssetsContentViewColSize, this, false, "Size"),
                new ContentViewColumnData(BuildReportUtility.DuplicatedAssetsContentViewSpaceSaved, this, false, "Space saved if De-duplicated"),
                new ContentViewColumnData(BuildReportUtility.DuplicatedAssetsContentViewDuplicationCount, this, false, "Number of times duplicated")
            };
        }
     }

        public override IList<IAddressablesBuildReportItem> CreateTreeViewItems(BuildLayout report)
        {
            List<IAddressablesBuildReportItem> buildReportAssets = new List<IAddressablesBuildReportItem>();
            if (report == null)
                return buildReportAssets;

            foreach (BuildReportHelperDuplicateImplicitAsset helperAsset in m_HelperConsumer.GUIDToDuplicateAssets.Values)
            {
                buildReportAssets.Add(new DuplicatedAssetsViewBuildReportDuplicatedAsset(helperAsset));
            }

            return buildReportAssets;
        }

        internal IList<TreeViewItemData<DuplicatedAssetsViewBuildReportItem>> CreateTreeRootsNestedList(IList<IAddressablesBuildReportItem> items)
        {
            int id = 0;
            var roots = new List<TreeViewItemData<DuplicatedAssetsViewBuildReportItem>>();

            foreach (DuplicatedAssetsViewBuildReportItem item in items)
            {
                DuplicatedAssetsViewBuildReportDuplicatedAsset duplicatedAsset = item as DuplicatedAssetsViewBuildReportDuplicatedAsset;
                if (duplicatedAsset == null)
                    continue;

                bool includeAllDependencies = EntryAppearsInSearch(duplicatedAsset, m_SearchValue);

                var bundles = CreateBundleEntries(duplicatedAsset, ref id, includeAllDependencies);

                if (includeAllDependencies || bundles != null)
                    roots.Add(new TreeViewItemData<DuplicatedAssetsViewBuildReportItem>(id++, item, bundles));
            }

            return roots;
        }

        private List<TreeViewItemData<DuplicatedAssetsViewBuildReportItem>> CreateBundleEntries(DuplicatedAssetsViewBuildReportDuplicatedAsset duplicatedAsset, ref int id, bool includeAllDependencies)
        {
            var bundles = new List<TreeViewItemData<DuplicatedAssetsViewBuildReportItem>>();
            var bundleToAssetList = new Dictionary<BuildLayout.Bundle, List<BuildLayout.ExplicitAsset>>();

            foreach (var dependentAsset in duplicatedAsset.AssetDepsOf)
            {
                if (!bundleToAssetList.ContainsKey(dependentAsset.Bundle))
                    bundleToAssetList.Add(dependentAsset.Bundle, new List<BuildLayout.ExplicitAsset>());
                bundleToAssetList[dependentAsset.Bundle].Add(dependentAsset);
            }

            bool includeDuplicationInSearch = includeAllDependencies;
            foreach (var bundle in bundleToAssetList.Keys)
            {
                var bundleReportItem = new DuplicatedAssetsViewBuildReportBundle(bundle);
                var depAssets = new List<TreeViewItemData<DuplicatedAssetsViewBuildReportItem>>();
                var unrelatedAssetsSize = bundle.FileSize;

                depAssets.Add(new TreeViewItemData<DuplicatedAssetsViewBuildReportItem>(id++, new DuplicatedAssetsViewBuildReportDuplicatedAsset(duplicatedAsset.DataFromOtherAsset)));
                foreach (var dependentAsset in bundleToAssetList[bundle])
                {
                    var assetReportItem = new DuplicatedAssetsViewBuildReportAsset(dependentAsset, duplicatedAsset.DataFromOtherAsset);
                    includeDuplicationInSearch = includeDuplicationInSearch || EntryAppearsInSearch(assetReportItem, m_SearchValue);
                    unrelatedAssetsSize -= dependentAsset.SerializedSize + dependentAsset.StreamedSize;
                    depAssets.Add(new TreeViewItemData<DuplicatedAssetsViewBuildReportItem>(id++, assetReportItem));
                }

                int unrelatedAssetCount = bundle.AssetCount - bundleToAssetList[bundle].Count;
                if (unrelatedAssetCount > 0)
                {
                    depAssets.Add(new TreeViewItemData<DuplicatedAssetsViewBuildReportItem>(id++, new DuplicatedAssetsViewBuildReportUnrelatedAssets(unrelatedAssetsSize, unrelatedAssetCount)));
                }

                bundles.Add(new TreeViewItemData<DuplicatedAssetsViewBuildReportItem>(id++, bundleReportItem, depAssets));
            }

            if (includeDuplicationInSearch)
                return bundles;
            return null;
        }

        public override void Consume(BuildLayout buildReport)
        {
            if (buildReport == null)
                return;

            m_Report = buildReport;
            m_TreeItems = CreateTreeViewItems(m_Report);
            IList<TreeViewItemData<DuplicatedAssetsViewBuildReportItem>> treeRoots = CreateTreeRootsNestedList(m_TreeItems);
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
