#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.ResourceManagement.Profiling;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal class AddressablesProfilerDetailsView : IDisposable
    {
        private const string k_ViewGroupsActionName = "Groups";
        private const string k_ViewAssetBundlesActionName = "Asset Bundles";
        private const string k_ViewAssetsActionName = "Assets";
        private const string k_ViewObjectsActionName = "Objects";
        private const string k_ViewNonLoadedActionName = "Assets Not Loaded";

        private const string k_PreferencesKeyPrefix = "com.unity.addressables.profiler-";
        private const string k_ViewGroupsPreferencesKey = k_PreferencesKeyPrefix + k_ViewGroupsActionName;
        private const string k_ViewAssetBundlesPreferencesKey = k_PreferencesKeyPrefix + k_ViewAssetBundlesActionName;
        private const string k_ViewAssetsPreferencesKey = k_PreferencesKeyPrefix + k_ViewAssetsActionName;
        private const string k_ViewNonLoadedPreferencesKey = k_PreferencesKeyPrefix + k_ViewNonLoadedActionName;
        private const string k_ViewObjectsPreferencesKey = k_PreferencesKeyPrefix + k_ViewObjectsActionName;

        private readonly struct FrameData
        {
            public readonly NativeArray<CatalogFrameData> CatalogValues;
            public readonly NativeArray<BundleFrameData> BundleValues;
            public readonly NativeArray<AssetFrameData> AssetValues;
            public readonly NativeArray<AssetFrameData> SceneValues;

            public bool HasValues => CatalogValues.IsCreated && CatalogValues.Length > 0;

            public FrameData(int frame)
            {
                using (var rawFrameData = UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(frame, 0))
                {
                    if (rawFrameData != null && rawFrameData.valid)
                    {
                        CatalogValues = rawFrameData.GetFrameMetaData<CatalogFrameData>(ProfilerRuntime.kResourceManagerProfilerGuid, ProfilerRuntime.kCatalogTag);
                        BundleValues = rawFrameData.GetFrameMetaData<BundleFrameData>(ProfilerRuntime.kResourceManagerProfilerGuid, ProfilerRuntime.kBundleDataTag);
                        AssetValues = rawFrameData.GetFrameMetaData<AssetFrameData>(ProfilerRuntime.kResourceManagerProfilerGuid, ProfilerRuntime.kAssetDataTag);
                        SceneValues = rawFrameData.GetFrameMetaData<AssetFrameData>(ProfilerRuntime.kResourceManagerProfilerGuid, ProfilerRuntime.kSceneDataTag);
                    }
                    else
                    {
                        CatalogValues = default;
                        BundleValues = default;
                        AssetValues = default;
                        SceneValues = default;
                    }
                }
            }
        }

        private AddressablesProfilerDetailsTreeView m_Tree;
        private ToolbarSearchField m_SearchField;
        private ContentSearch m_SearchController = new ContentSearch();



        private AddressablesProfilerDetailsDataInspector m_DetailsInspector;

        private bool m_ViewGroups = false;
        private bool m_ViewAssetBundles = true;
        private bool m_ViewAssets = true;
        private bool m_ViewObjects = true;
        private bool m_ViewNonLoadedAssets = false;

        private readonly ProfilerWindow m_ProfilerWindow;
        private long m_ProfilerFrameSelected = -1;
        private TreeViewPane m_TreePane;
        private InspectorPane m_InspectorPane;
        private TwoPaneSplitView m_RootSplitView;
        public VisualElement RootVisualElement => m_RootSplitView;
        private List<MissingReport> m_MissingReportElements = new List<MissingReport>();

        private List<GroupData> m_RootGroupsContentData = null;

        public AddressablesProfilerDetailsView(ProfilerWindow profilerWindow)
        {
            m_ProfilerWindow = profilerWindow;
        }

        public VisualElement CreateView()
        {
            m_ViewGroups = EditorPrefs.GetBool(k_ViewGroupsPreferencesKey, false);
            m_ViewAssetBundles = EditorPrefs.GetBool(k_ViewAssetBundlesPreferencesKey, true);
            m_ViewAssets = EditorPrefs.GetBool(k_ViewAssetsPreferencesKey, true);
            m_ViewNonLoadedAssets = EditorPrefs.GetBool(k_ViewNonLoadedPreferencesKey, false);
            m_ViewObjects = EditorPrefs.GetBool(k_ViewObjectsPreferencesKey, false);

            CreateViewsWithToolbarInLeft();
            OnReinitialise();
            return m_RootSplitView;
        }

        private VisualElement CreateViewsWithToolbarInLeft()
        {
            m_TreePane = new TreeViewPane(ProfilerTemplates.TreeViewPane.Instantiate());
            m_TreePane.Root.style.minWidth = new StyleLength(550);

            m_InspectorPane = new InspectorPane(ProfilerTemplates.DetailsPane.Instantiate());
            m_InspectorPane.Root.style.minWidth = new StyleLength(275);

            m_RootSplitView = new TwoPaneSplitView(0, 550, TwoPaneSplitViewOrientation.Horizontal);
            m_RootSplitView.Add(m_TreePane.Root);
            m_RootSplitView.Add(m_InspectorPane.Root);

            m_TreePane.ViewMenu.menu.AppendAction(k_ViewGroupsActionName, ViewMenuActionSelectedCallback, ViewMenuActionStatusCallback);
            m_TreePane.ViewMenu.menu.AppendAction(k_ViewAssetBundlesActionName, ViewMenuActionSelectedCallback, ViewMenuActionStatusCallback);
            m_TreePane.ViewMenu.menu.AppendAction(k_ViewAssetsActionName, ViewMenuActionSelectedCallback, ViewMenuActionStatusCallback);
            m_TreePane.ViewMenu.menu.AppendAction(k_ViewObjectsActionName, ViewMenuActionSelectedCallback, ViewMenuActionStatusCallback);
            m_TreePane.ViewMenu.menu.AppendSeparator();
            m_TreePane.ViewMenu.menu.AppendAction(k_ViewNonLoadedActionName, ViewMenuActionSelectedCallback, ViewMenuActionStatusCallback);

            m_DetailsInspector = new AddressablesProfilerDetailsDataInspector(m_InspectorPane);
            m_Tree = new AddressablesProfilerDetailsTreeView(m_TreePane);

            m_DetailsInspector.Initialise(m_TreePane.TreeView);
            m_Tree.Initialise(m_DetailsInspector);
            m_Tree.SetSortingChangeCallback(TreeSortingChanged);

            return m_RootSplitView;
        }

        public void OnReinitialise()
        {
            m_SearchField = m_TreePane.SearchField;
            m_SearchField.RegisterValueChangedCallback(SearchChanged);
            m_SearchController.InitialiseFilterMenu(m_TreePane.SearchMenu.menu, m_SearchField);

            m_ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;
            ReloadData(m_ProfilerWindow.selectedFrameIndex);
        }

        private void TreeSortingChanged()
        {
            BuildTree();
        }

        private DropdownMenuAction.Status ViewMenuActionStatusCallback(DropdownMenuAction action)
        {
            return action.name switch
            {
                k_ViewGroupsActionName => m_ViewGroups ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal,
                k_ViewAssetBundlesActionName => m_ViewAssetBundles ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal,
                k_ViewAssetsActionName => m_ViewAssets ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal,
                k_ViewNonLoadedActionName => m_ViewNonLoadedAssets ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal,
                k_ViewObjectsActionName => m_ViewObjects ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal,
                _ => DropdownMenuAction.Status.Normal
            };
        }

        private void ViewMenuActionSelectedCallback(DropdownMenuAction action)
        {
            switch (action.name)
            {
                case k_ViewGroupsActionName:
                    m_ViewGroups = !m_ViewGroups;
                    EditorPrefs.SetBool(k_ViewGroupsPreferencesKey, m_ViewGroups);
                    break;
                case k_ViewAssetBundlesActionName:
                    m_ViewAssetBundles = !m_ViewAssetBundles;
                    EditorPrefs.SetBool(k_ViewAssetBundlesPreferencesKey, m_ViewAssetBundles);
                    break;
                case k_ViewAssetsActionName:
                    m_ViewAssets = !m_ViewAssets;
                    EditorPrefs.SetBool(k_ViewAssetsPreferencesKey, m_ViewAssets);
                    break;
                case k_ViewNonLoadedActionName:
                    m_ViewNonLoadedAssets = !m_ViewNonLoadedAssets;
                    EditorPrefs.SetBool(k_ViewNonLoadedPreferencesKey, m_ViewNonLoadedAssets);
                    break;
                case k_ViewObjectsActionName:
                    m_ViewObjects = !m_ViewObjects;
                    EditorPrefs.SetBool(k_ViewObjectsPreferencesKey, m_ViewObjects);
                    break;
            }

            BuildTree();
        }

        private void SearchChanged(ChangeEvent<string> evt)
        {
            string newSearch = string.IsNullOrEmpty(evt.newValue) ? null : evt.newValue;
            m_SearchController.ProcessSearchValue(newSearch);
            BuildTree();
        }

        void OnSelectedFrameIndexChanged(long selectedFrameIndex)
        {
            m_ProfilerFrameSelected = selectedFrameIndex;
            ReloadData(selectedFrameIndex);
        }

        void ReloadData(long selectedFrameIndex)
        {
            for (int i = 0; i < m_MissingReportElements.Count; ++i)
                m_TreePane.Root.RemoveAt(0);
            m_MissingReportElements.Clear();

            FrameData frameData = new FrameData(Convert.ToInt32(selectedFrameIndex));
            if (frameData.HasValues)
            {
                HashSet<Hash128> missingBuildHashes = AddressablesProfilerViewController.LayoutsManager.SetActiveReportsAndGetMissingBuildHashes(frameData.CatalogValues);
                if (missingBuildHashes != null)
                {
                    foreach (Hash128 missingBuildHash in missingBuildHashes)
                    {
                        MissingBuildReportDisplay(missingBuildHash);
                    }
                }
                GenerateContentDataForFrame(frameData);
                BuildTree();
            }
            else
            {
                List<TreeViewItemData<ContentData>> roots = new List<TreeViewItemData<ContentData>>();
                m_Tree.SetRootItemsAndRebuild(roots);
            }
        }

        private void MissingBuildReportDisplay(Hash128 hash)
        {
            MissingReport missingDisplay = MissingReport.Create();
            m_MissingReportElements.Add(missingDisplay);
            missingDisplay.Icon.image = EditorGUIUtility.IconContent(AddressableIconNames.ErrorIcon).image;
            missingDisplay.MissingBuildHashLabel.text = string.Format(ProfilerStrings.MissingBuildReportLabelText, hash.ToString());
            missingDisplay.SearchForBuildReportButton.clickable.clicked += MissingBuildReportClicked;

            m_TreePane.Root.Insert(0, missingDisplay.Root);
        }

        private void MissingBuildReportClicked()
        {
            string assetPath = Application.dataPath;

            assetPath = EditorUtility.OpenFilePanel("Build Report File", System.IO.Path.GetDirectoryName(assetPath), "json");
            if (string.IsNullOrEmpty(assetPath))
                return;

            if (AddressablesProfilerViewController.LayoutsManager.LoadManualReport(assetPath))
                ReloadData(m_ProfilerFrameSelected);
        }

        private void BuildTree()
        {
            if (m_RootGroupsContentData == null)
            {
                m_Tree.SetRootItemsAndRebuild(new List<TreeViewItemData<ContentData>>());
                return;
            }

            List<TreeViewItemData<ContentData>> rootItems = new List<TreeViewItemData<ContentData>>();
            foreach (GroupData groupData in m_RootGroupsContentData)
            {
                if (!m_ViewGroups && !m_ViewAssetBundles && !m_ViewAssets && !m_ViewObjects)
                    break;

                List<TreeViewItemData<ContentData>> groupChildren = new List<TreeViewItemData<ContentData>>();
                CreateBundleChildrenForGroup(groupData, groupChildren);

                if (groupChildren.Count == 0)
                {
                    if (!m_SearchController.IsValidSearch(groupData))
                        continue;
                }

                CreateAndAddContentItem(m_ViewGroups, groupData, groupChildren, rootItems);
            }

            SortContent(rootItems);
            m_Tree.SetRootItemsAndRebuild(rootItems);
        }

        private void CreateBundleChildrenForGroup(GroupData groupData, List<TreeViewItemData<ContentData>> groupChildren)
        {
            foreach (BundleData bundleData in groupData.Children)
            {
                if (!m_ViewAssetBundles && !m_ViewAssets && !m_ViewObjects)
                    break;

                List<TreeViewItemData<ContentData>> bundleChildren = new List<TreeViewItemData<ContentData>>();
                CreateAssetChildrenForBundle(bundleData, bundleChildren);
                CreateNonLoadedAssetChildren(bundleData, bundleChildren);

                if (bundleChildren.Count == 0)
                {
                    if (!m_SearchController.IsValidSearch(bundleData))
                        continue;
                }

                CreateAndAddContentItem(m_ViewAssetBundles, bundleData, bundleChildren, groupChildren);
            }
        }

        private void CreateNonLoadedAssetChildren(BundleData bundleData, List<TreeViewItemData<ContentData>> bundleChildren)
        {
            if (m_ViewAssets && m_ViewNonLoadedAssets && bundleData.NotLoadedChildren.Count > 0)
            {
                ContainerData nonLoadedFolderData = new ContainerData();
                nonLoadedFolderData.Parent = bundleData;
                nonLoadedFolderData.Name = "Non-loaded Assets";

                List<TreeViewItemData<ContentData>> folderChildren = new List<TreeViewItemData<ContentData>>();
                foreach (ContentData notLoadedAssetData in bundleData.NotLoadedChildren)
                {
                    TreeViewItemData<ContentData> assetItem =
                        new TreeViewItemData<ContentData>(notLoadedAssetData.TreeViewID, notLoadedAssetData);
                    folderChildren.Add(assetItem);
                }

                SortContent(folderChildren);
                TreeViewItemData<ContentData> folderItem =
                    new TreeViewItemData<ContentData>(nonLoadedFolderData.TreeViewID, nonLoadedFolderData, folderChildren);
                bundleChildren.Add(folderItem);
            }
        }

        private void CreateAssetChildrenForBundle(BundleData bundleData, List<TreeViewItemData<ContentData>> bundleChildren)
        {
            foreach (AssetData assetData in bundleData.Children)
            {
                if (!m_ViewAssets && !m_ViewObjects)
                    break;
                if (!m_SearchController.IsValidSearch(assetData))
                    continue;

                List<TreeViewItemData<ContentData>> assetChildren = new List<TreeViewItemData<ContentData>>();
                if (m_ViewObjects)
                {
                    CreateObjectChildrenForAsset(assetData, assetChildren);
                }

                CreateAndAddContentItem(m_ViewAssets, assetData, assetChildren, bundleChildren);
            }
        }

        private void CreateAndAddContentItem(bool typeVisible, ContentData content, List<TreeViewItemData<ContentData>> childrenList, List<TreeViewItemData<ContentData>> parentList)
        {
            if (typeVisible)
            {
                SortContent(childrenList);
                parentList.Add(new TreeViewItemData<ContentData>(content.TreeViewID, content, childrenList));
            }
            else
                parentList.AddRange(childrenList);
        }

        private void CreateObjectChildrenForAsset(AssetData assetData, List<TreeViewItemData<ContentData>> assetChildren)
        {
            foreach (ObjectData objectData in assetData.Children)
            {
                if (!m_SearchController.IsValidSearch(objectData))
                    continue;
                assetChildren.Add(new TreeViewItemData<ContentData>(objectData.TreeViewID, objectData));
            }
        }

        private void SortContent(List<TreeViewItemData<ContentData>> content)
        {
            if (m_Tree.SortDescriptions.Count == 0)
                return;

            content.Sort(CompareTreeColumnData);
        }

        private int CompareTreeColumnData(TreeViewItemData<ContentData> x, TreeViewItemData<ContentData> y)
        {
            string columnName = m_Tree.SortDescriptions[0].columnName;
            ContentData left = m_Tree.SortDescriptions[0].direction == SortDirection.Ascending ? x.data : y.data;
            ContentData right = m_Tree.SortDescriptions[0].direction == SortDirection.Ascending ? y.data : x.data;
            if (columnName == TreeColumnNames.TreeColumnName)
                return string.Compare(left.Name, right.Name, StringComparison.Ordinal);
            if (columnName == TreeColumnNames.TreeColumnStatus)
                return left.Status.CompareTo(right.Status);
            if (columnName == TreeColumnNames.TreeColumnAddressedCount)
                return left.AddressableHandles.CompareTo(right.AddressableHandles);
            if (columnName == TreeColumnNames.TreeColumnBundleSource)
            {
                BundleData leftBundle = left as BundleData;
                if (leftBundle == null)
                    return 0;
                BundleData rightBundle = right as BundleData;
                if (rightBundle == null)
                    return 0;
                return leftBundle.Source.CompareTo(rightBundle.Source);
            }
            if (columnName == TreeColumnNames.TreeColumnReferencedBy)
                return left.ReferencesToThis.Count.CompareTo(right.ReferencesToThis.Count);
            if (columnName == TreeColumnNames.TreeColumnReferencesTo)
                return left.ThisReferencesOther.Count.CompareTo(right.ThisReferencesOther.Count);
            if (columnName == TreeColumnNames.TreeColumnType)
            {
                if (left is AssetData leftAsset)
                {
                    AssetData rightAsset = right as AssetData;
                    return leftAsset.MainAssetType.CompareTo(rightAsset.MainAssetType);
                }
                else if (left is ObjectData leftObject)
                {
                    ObjectData rightObject = right as ObjectData;
                    return leftObject.AssetType.CompareTo(rightObject.AssetType);
                }
            }
            if (columnName == TreeColumnNames.TreeColumnPercentage)
                return left.PercentComplete.CompareTo(right.PercentComplete);
            return 0;
        }

        private void GenerateContentDataForFrame(FrameData frameData)
        {
            Dictionary<BuildLayout.Bundle, BundleData> reportBundleToBundleData = new Dictionary<BuildLayout.Bundle, BundleData>();
            List<BundleData> bundleContent = GenerateBundleRoots(frameData.BundleValues, reportBundleToBundleData);
            GenerateAssetData(bundleContent, frameData.AssetValues, frameData.SceneValues);
            List<GroupData> groupContent = CollectGroups(bundleContent);
            m_RootGroupsContentData = groupContent;
        }

        List<BundleData> GenerateBundleRoots(in NativeArray<BundleFrameData> bundleValues, in Dictionary<BuildLayout.Bundle, BundleData> reportBundleToBundleData)
        {
            List<BundleData> bundleDatas = new List<BundleData>();
            foreach (BundleFrameData frameData in bundleValues)
            {
                BuildLayout.Bundle layoutBundle = AddressablesProfilerViewController.LayoutsManager.GetBundle(frameData.BundleCode);
                if (layoutBundle == null)
                    return new List<BundleData>();
                BundleData bundleData = new BundleData(layoutBundle, frameData);
                bundleDatas.Add(bundleData);
                reportBundleToBundleData.Add(layoutBundle, bundleData);
            }

            GenerateBundleDependencies(bundleDatas, reportBundleToBundleData);
            return bundleDatas;
        }

        void GenerateAssetData(List<BundleData> bundleDatas, in NativeArray<AssetFrameData> assetValues, in NativeArray<AssetFrameData> sceneValues)
        {
            Dictionary<int, BundleData> bundleCodeToData = new Dictionary<int, BundleData>();
            foreach (BundleData data in bundleDatas)
                bundleCodeToData.Add(data.BundleCode, data);

            List<AssetData> addressableLoadedAssets = new List<AssetData>();
            // add all addressable loaded assets so later know they are fully loaded
            foreach (AssetFrameData frameData in assetValues)
            {
                var assetData = GetAssetDataForFrameData(frameData, bundleCodeToData);
                if (assetData != null)
                {
                    addressableLoadedAssets.Add(assetData);
                }
            }
            foreach (AssetFrameData frameData in sceneValues)
            {
                var assetData = GetAssetDataForFrameData(frameData, bundleCodeToData);
                if (assetData != null)
                {
                    addressableLoadedAssets.Add(assetData);
                }
            }

            ProcessAssetReferences(addressableLoadedAssets, bundleCodeToData);
            GatherNonLoadedAssets(bundleDatas);
        }

        private static void GatherNonLoadedAssets(List<BundleData> bundleDatas)
        {
            foreach (BundleData bundleData in bundleDatas)
            {
                HashSet<string> activeGuids = new HashSet<string>();
                foreach (ContentData dataChild in bundleData.Children)
                {
                    activeGuids.Add((dataChild as AssetData)?.AssetGuid);
                }

                foreach (BuildLayout.File bundleFile in bundleData.ReportBundle.Files)
                {
                    foreach (BuildLayout.ExplicitAsset explicitAsset in bundleFile.Assets)
                    {
                        if (activeGuids.Contains(explicitAsset.Guid))
                            continue;
                        var assetData = new AssetData(explicitAsset);
                        bundleData.NotLoadedChildren.Add(assetData);
                        assetData.Parent = bundleData;
                    }

                    foreach (BuildLayout.DataFromOtherAsset implicitAsset in bundleFile.OtherAssets)
                    {
                        if (activeGuids.Contains(implicitAsset.AssetGuid))
                            continue;
                        var assetData = new AssetData(implicitAsset);
                        bundleData.NotLoadedChildren.Add(assetData);
                        assetData.Parent = bundleData;
                    }
                }
            }
        }

        private AssetData GetAssetDataForFrameData(AssetFrameData frameData, Dictionary<int, BundleData> bundleCodeToData)
        {
            BuildLayout.ExplicitAsset reportAsset = AddressablesProfilerViewController.LayoutsManager.GetAsset(frameData.BundleCode, frameData.AssetCode);
            if (reportAsset == null)
                return null;
            return GetAssetDataForFrameData(reportAsset, frameData, bundleCodeToData);
        }

        internal AssetData GetAssetDataForFrameData(BuildLayout.ExplicitAsset reportAsset, AssetFrameData frameData, Dictionary<int, BundleData> bundleCodeToData) {
            if (!bundleCodeToData.TryGetValue(frameData.BundleCode, out BundleData parentBundle))
            {
                return null;
            }

            AssetData assetData = parentBundle.GetOrCreateAssetData(reportAsset);
            assetData.Update(frameData);
            return assetData;
        }

        private void ProcessAssetReferences(List<AssetData> activeAddressableAssets, Dictionary<int, BundleData> bundleCodeToData)
        {
            // instead get a stack
            Stack<AssetData> assetsToBeProcessed = new Stack<AssetData>(activeAddressableAssets);
            HashSet<AssetData> processedAssets = new HashSet<AssetData>();
            HashSet<BuildLayout.ObjectReference> processedObjectReferences = new HashSet<BuildLayout.ObjectReference>();

            while (assetsToBeProcessed.Count > 0)
            {
                AssetData assetData = assetsToBeProcessed.Pop();
                if(!processedAssets.Add(assetData))
                    continue; // not needed

                BundleData parentBundle = bundleCodeToData[((BundleData)assetData.Parent).BundleCode];
                foreach (BuildLayout.ObjectData objectData in assetData.ReportObjects)
                {
                    // get or create object for self, this may have already been made from a reference
                    ObjectData objData = assetData.GetOrCreateObjectData(objectData);

                    if (assetData.Status < ContentStatus.Active || activeAddressableAssets.Contains(assetData))
                        objData.Status = assetData.Status;
                    else if (objData.Status < ContentStatus.Released)
                        objData.Status = ContentStatus.Released;

                    foreach (BuildLayout.ObjectReference objectReference in objectData.References)
                    {
                        if (processedObjectReferences.Add(objectReference))
                        {
                            AssetData appendAsset = ProcessReference(assetData, objData, objectReference, parentBundle, bundleCodeToData);
                            if (appendAsset != null && !processedAssets.Contains(appendAsset) && !assetsToBeProcessed.Contains(appendAsset))
                            {
                                assetsToBeProcessed.Push(appendAsset);
                            }
                        }
                    }
                }
            }
        }

        private AssetData ProcessReference(AssetData referencingAssetData, ObjectData referencingObjectData,
            BuildLayout.ObjectReference objectReference, BundleData parentBundle, Dictionary<int, BundleData> bundleCodeToData)
        {
            BuildLayout.File file = referencingAssetData.IsImplicit ? referencingAssetData.ReportImplicitData.File : referencingAssetData.ReportExplicitData.File;
            BundleData bundleContainingReferencedObj = parentBundle;
            AssetData referencedAssetData = null;
            int assetId = objectReference.AssetId;
            if (assetId < file.OtherAssets.Count)
            {
                bundleContainingReferencedObj.GetOrCreateAssetData(file.OtherAssets[assetId], out referencedAssetData);
            }
            else
            {
                assetId -= file.OtherAssets.Count;
                BuildLayout.ExplicitAsset referencedReportAsset = null;
                if (assetId >= file.Assets.Count)
                {
                    assetId -= file.Assets.Count;
                    referencedReportAsset = file.ExternalReferences[assetId];
                }
                else
                    referencedReportAsset = file.Assets[assetId];

                // if its external need to go to its bundle
                if (referencedReportAsset.Bundle != bundleContainingReferencedObj.ReportBundle)
                {
                    int bundleCode = referencedReportAsset.Bundle.InternalName.GetHashCode();
                    bundleContainingReferencedObj = bundleCodeToData[bundleCode];
                }

                bundleContainingReferencedObj.GetOrCreateAssetData(referencedReportAsset, out referencedAssetData);
            }

            if (referencedAssetData == null)
                return null;

            if (referencedAssetData.Status < referencingObjectData.Status)
                referencedAssetData.Status = referencingObjectData.Status;

            referencedAssetData.AddLoadedObjects(objectReference.ObjectIds);
            List<BuildLayout.ObjectData> layoutObjects = referencedAssetData.IsImplicit ?
                referencedAssetData.ReportImplicitData.Objects : referencedAssetData.ReportExplicitData.Objects;

            foreach (int objectId in objectReference.ObjectIds)
            {
                BuildLayout.ObjectData referencedLayoutObject = layoutObjects[objectId];
                ObjectData referencedObjectData = referencedAssetData.GetOrCreateObjectData(referencedLayoutObject);
                if (referencedObjectData.Status < referencingObjectData.Status)
                    referencedObjectData.Status = referencingObjectData.Status;

                if (referencedObjectData != referencingObjectData)
                {
                    referencingObjectData.AddReferenceTo(referencedObjectData);
                    referencedObjectData.AddReferenceBy(referencingObjectData);
                }
            }

            if (referencedAssetData != referencingAssetData)
            {
                referencingAssetData.AddReferenceTo(referencedAssetData);
                referencedAssetData.AddReferenceBy(referencingAssetData);
                return referencedAssetData;
            }

            return null;
        }

        private static void GenerateBundleDependencies(List<BundleData> bundleDatas, in Dictionary<BuildLayout.Bundle, BundleData> reportBundleToBundleData)
        {
            foreach (BundleData data in bundleDatas)
            {
                foreach (BuildLayout.Bundle dependency in data.ReportBundle.Dependencies)
                {
                    if (!reportBundleToBundleData.TryGetValue(dependency, out var bundleDataIsDependantOn))
                        continue;
                    if (!bundleDataIsDependantOn.ReferencesToThis.Contains(data))
                        bundleDataIsDependantOn.ReferencesToThis.Add(data);
                    if (!data.ThisReferencesOther.Contains(bundleDataIsDependantOn))
                        data.ThisReferencesOther.Add(bundleDataIsDependantOn);
                }
            }
        }

        List<GroupData> CollectGroups(List<BundleData> bundleDatas)
        {
            GroupData missingGroup = new GroupData(null);

            Dictionary<string, GroupData> nameToGroups = new Dictionary<string, GroupData>();

            foreach (BundleData bundleData in bundleDatas)
            {
                if (bundleData.ReportBundle == null)
                {
                    missingGroup.AddChild(bundleData);
                    continue;
                }

                if (!nameToGroups.TryGetValue(bundleData.ReportBundle.Group.Guid, out GroupData group))
                {
                    group = new GroupData(bundleData.ReportBundle.Group);
                    nameToGroups[group.m_ReportGroup.Guid] = group;
                }
                group.AddChild(bundleData);
            }

            List<GroupData> groups = new List<GroupData>(nameToGroups.Values);
            if (missingGroup.Children.Count > 0)
                groups.Insert(0, missingGroup);
            return groups;
        }

        public void Dispose()
        {
            m_Tree?.SaveColumnVisibility();
            if (m_ProfilerWindow != null)
            {
                m_ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;
            }
        }
    }
}

#endif
