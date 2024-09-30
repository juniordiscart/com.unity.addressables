#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER

using System.Collections.Generic;
using System.Text;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.AddressableAssets.GUIElements;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.ResourceManagement.Profiling;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal class AddressablesProfilerDetailsDataInspector
    {
        private ContentDataTreeView m_ReferencesTree = new ContentDataTreeView();
        private ContentData m_SelectionSource;

        private HelpDisplayManager m_HelpManager = new HelpDisplayManager();

        private InspectorPane m_Elements;
        private Stack<LabeledLabel> m_InformationLabelsCache = new Stack<LabeledLabel>();

        public AddressablesProfilerDetailsDataInspector(InspectorPane rootView)
        {
            m_Elements = rootView;
        }

        public void Initialise(MultiColumnTreeView mainProfilerAssetTree)
        {
            m_HelpManager.Initialise(m_Elements, mainProfilerAssetTree);

            m_Elements.ReferencesTypeSelection.Clicked += ReferencesRibbonOnClicked;
            m_ReferencesTree.Initialise(m_Elements.ReferencesTree, mainProfilerAssetTree);

            m_Elements.SelectInEditor.clicked += SelectInEditorOnClicked;
            m_Elements.SelectInGroups.clicked += SelectInGroupsOnClicked;
            m_Elements.SelectedAsset.Wrap = true;

            SetSourceContent(null);
        }

        public void SetSourceContent(ContentData source)
        {
            m_Elements.SelectInEditor.SetEnabled(false);
            m_Elements.SelectInGroups.SetEnabled(false);
            m_SelectionSource = source;

            m_Elements.PreviewImage.image = null;
            m_HelpManager.Clear();

            if (source == null)
            {
                m_Elements.MainContainer.visible = false;
                return;
            }

            RemoveLabels(m_Elements.SelectionDetailsFoldout);
            m_Elements.MainContainer.visible = true;
            if (source is BundleData bundleData)
                SetSourceBundle(bundleData);
            else if (source is GroupData groupData)
                SetSourceGroup(groupData);
            else if (source is AssetData assetData)
                SetSourceAsset(assetData);
            else if (source is ObjectData objectData)
                SetSourceObject(objectData);
            else
                m_Elements.MainContainer.visible = false;

            m_HelpManager.HideIfEmpty();
            SetInfoFoldoutsVisibility();
        }

        private LabeledLabel AddLabel(string label, string text, VisualElement parent)
        {
            LabeledLabel lbl = null;
            if (m_InformationLabelsCache.Count > 0)
            {
                lbl = m_InformationLabelsCache.Pop();
                lbl.Label = label;
                lbl.Text = text;
            }
            else
                lbl = new LabeledLabel(label, text, true);

            parent.Add(lbl);
            return lbl;
        }

        private void RemoveLabels(VisualElement parent)
        {
            List<VisualElement> toRemove = new List<VisualElement>();
            foreach (VisualElement child in parent.Children())
            {
                if (child is LabeledLabel)
                    toRemove.Add(child);
            }

            foreach (VisualElement element in toRemove)
            {
                parent.Remove(element);
                if (element is LabeledLabel l)
                    m_InformationLabelsCache.Push(l);
            }
        }

        private void SetInfoFoldoutsVisibility()
        {
            if (m_Elements.SelectionDetailsFoldout.childCount == 0)
                ProfilerGUIUtilities.Hide(m_Elements.SelectionDetailsFoldout);
            else
                ProfilerGUIUtilities.Show(m_Elements.SelectionDetailsFoldout);
        }

        private void SetSourceGroup(GroupData groupData)
        {
            m_Elements.SelectedAsset.SetContent(groupData);

            AddLabel("Name", groupData.m_ReportGroup.Name, m_Elements.SelectionDetailsFoldout);
            AddLabel("GUID", groupData.m_ReportGroup.Guid, m_Elements.SelectionDetailsFoldout);
            AddLabel("Packing Mode", groupData.m_ReportGroup.PackingMode, m_Elements.SelectionDetailsFoldout);
            AddLabel("Bundle Count", groupData.m_ReportGroup.Bundles.Count.ToString(), m_Elements.SelectionDetailsFoldout);

            ProfilerGUIUtilities.Hide(m_Elements.ReferencesFoldout);
            ProfilerGUIUtilities.Hide(m_Elements.PreviewFoldout);
        }

        private void SetSourceBundle(BundleData bundleData)
        {
            m_Elements.SelectedAsset.SetContent(bundleData);

            // basic
            AddLabel("Source", bundleData.Source.ToString(), m_Elements.SelectionDetailsFoldout);
            AddLabel("Load Path", bundleData.ReportBundle.LoadPath, m_Elements.SelectionDetailsFoldout);
            AddLabel("Compression", bundleData.ReportBundle.Compression + " (at build)", m_Elements.SelectionDetailsFoldout);

            // addressables group
            AddLabel("Group", bundleData.ReportBundle.Group.Name, m_Elements.SelectionDetailsFoldout);
            AddLabel("Provider", bundleData.ReportBundle.Provider, m_Elements.SelectionDetailsFoldout);

            AddLabel("Hash", bundleData.ReportBundle.Hash.ToString(), m_Elements.SelectionDetailsFoldout);
            AddLabel("CRC", bundleData.ReportBundle.CRC.ToString(), m_Elements.SelectionDetailsFoldout);

            ProfilerGUIUtilities.Show(m_Elements.ReferencesFoldout);
            ReferencesRibbonOnClicked(m_Elements.ReferencesTypeSelection.m_CurrentOption);
            ProfilerGUIUtilities.Hide(m_Elements.PreviewFoldout);

            if (bundleData.Children.Count > 0)
            {
                List<ContentData> addressedAssetsInBundle = new List<ContentData>(bundleData.Children.Count);
                foreach (ContentData childAsset in bundleData.Children)
                {
                    if (childAsset.AddressableHandles > 0)
                        addressedAssetsInBundle.Add(childAsset);
                }

                m_HelpManager.MakeHelp(ProfilerStrings.BundleWithLoadedAddressableContent, addressedAssetsInBundle);
            }

            if (bundleData.ReferencesToThis.Count > 0)
            {
                HashSet<ContentData> referencingAssets = new HashSet<ContentData>();
                HashSet<ContentData> bundlesReferencingWithoutLoadingReferencingAsset = new HashSet<ContentData>();
                foreach (ContentData bundleReferencingThis in bundleData.ReferencesToThis)
                {
                    bool hasLoadedAssetReferencing = false;
                    // find children referencing this
                    foreach (ContentData assetInReferencingBundle in bundleReferencingThis.Children)
                    {
                        foreach (ContentData other in assetInReferencingBundle.ThisReferencesOther)
                        {
                            if (other.Parent == bundleData)
                            {
                                referencingAssets.Add(assetInReferencingBundle);
                                hasLoadedAssetReferencing = true;
                                break;
                            }
                        }
                    }

                    if (!hasLoadedAssetReferencing)
                        bundlesReferencingWithoutLoadingReferencingAsset.Add(bundleReferencingThis);
                }

                if (referencingAssets.Count > 0)
                {
                    m_HelpManager.MakeHelp(ProfilerStrings.BundleReferencingWithDependencies, new List<ContentData>(referencingAssets), "AssetDependencies.html", "Learn more: About Dependencies");
                }

                if (bundlesReferencingWithoutLoadingReferencingAsset.Count > 0)
                {
                    m_HelpManager.MakeHelp(ProfilerStrings.BundleReferencingWithNoLoadedDependencies, new List<ContentData>(bundlesReferencingWithoutLoadingReferencingAsset), "AssetDependencies.html", "Learn more: About Dependencies");
                }
            }

            if (bundleData.Source == BundleSource.Local)
            {
                if (bundleData.CheckSumEnabled)
                    m_HelpManager.MakeHelp(ProfilerStrings.LocalBundleUsingCRC, new List<ContentData>(), "ContentPackingAndLoadingSchema.html#assetbundle-crc", "Learn more: About CRC");
            }
            else if (bundleData.Source == BundleSource.Cache)
            {
                if (bundleData.CheckSumEnabled)
                    m_HelpManager.MakeHelp(ProfilerStrings.CachedBundleUsingCRC, new List<ContentData>(), "ContentPackingAndLoadingSchema.html#assetbundle-crc", "Learn more: About CRC");
            }
            else if (bundleData.Source == BundleSource.Download)
            {
#if ENABLE_CACHING
                if (!bundleData.CachingEnabled)
                    m_HelpManager.MakeHelp(ProfilerStrings.DownloadWithoutCaching);
#else
                if (bundleData.CachingEnabled)
                    m_HelpManager.MakeHelp(ProfilerStrings.DownloadWithoutCachingEnabled);
#endif
                if (bundleData.CachingEnabled && !bundleData.CheckSumEnabled)
                    m_HelpManager.MakeHelp(ProfilerStrings.DownloadWithoutCRC, new List<ContentData>(), "ContentPackingAndLoadingSchema.html#assetbundle-crc", "Learn more: About CRC");
            }
        }

        private void SetSourceAsset(AssetData assetData)
        {
            m_Elements.SelectedAsset.SetContent(assetData);
            AddLabel("AssetPath", assetData.AssetPath, m_Elements.SelectionDetailsFoldout);
            AddLabel("GUID", assetData.AssetGuid, m_Elements.SelectionDetailsFoldout);
            AddLabel("Asset Type", assetData.MainAssetType.ToString(), m_Elements.SelectionDetailsFoldout);

            if (assetData.ReportExplicitData != null)
            {
                AddLabel("Address", assetData.ReportExplicitData.AddressableName, m_Elements.SelectionDetailsFoldout);

                ContentData parent = assetData.Parent;
                while (parent != null)
                {
                    if (parent is GroupData g)
                    {
                        AddLabel("Group Name", g.m_ReportGroup.Name, m_Elements.SelectionDetailsFoldout);
                        break;
                    }

                    parent = parent.Parent;
                }

                if (assetData.ReportExplicitData.Labels.Length > 0)
                {
                    StringBuilder labels = new StringBuilder(assetData.ReportExplicitData.Labels[0]);
                    for (int i = 1; i < assetData.ReportExplicitData.Labels.Length; ++i)
                        labels.Append($", {assetData.ReportExplicitData.Labels[i]}");
                    AddLabel("Labels", labels.ToString(), m_Elements.SelectionDetailsFoldout);
                }

                AddLabel("Asset Hash", assetData.ReportExplicitData.AssetHash.ToString(), m_Elements.SelectionDetailsFoldout);
                AddLabel("Internal Id", assetData.ReportExplicitData.InternalId, m_Elements.SelectionDetailsFoldout);
            }
            else if (assetData.ReportImplicitData != null)
            {
                AddLabel("Inclusion", "Implicit", m_Elements.SelectionDetailsFoldout);
            }

            if (assetData.Status == ContentStatus.None)
                ProfilerGUIUtilities.Hide(m_Elements.ReferencesFoldout);
            else
            {
                ProfilerGUIUtilities.Show(m_Elements.ReferencesFoldout);
                ReferencesRibbonOnClicked(m_Elements.ReferencesTypeSelection.m_CurrentOption);
            }

            if (assetData.AddressableHandles > 0)
                m_HelpManager.MakeHelp(ProfilerStrings.LoadedAssetText);
            if (assetData.Status == ContentStatus.None)
                m_HelpManager.MakeHelp(ProfilerStrings.NotLoadedAssetHelpText);

            if (assetData.ReferencesToThis.Count > 0)
            {
                // get root addressable assets
                Queue<ContentData> refQueue = new Queue<ContentData>(assetData.ReferencesToThis);
                HashSet<ContentData> addressedContentReferencingSelection = new HashSet<ContentData>();
                while (refQueue.Count > 0)
                {
                    ContentData referencingContent = refQueue.Dequeue();
                    if (referencingContent.AddressableHandles > 0)
                        addressedContentReferencingSelection.Add(referencingContent);
                    foreach (ContentData innerRef in referencingContent.ReferencesToThis)
                        refQueue.Enqueue(innerRef);
                }

                Stack<ContentData> stack = new Stack<ContentData>();
                List<ContentData[]> addressableStacks = new List<ContentData[]>();
                foreach (ContentData refTo in assetData.ReferencesToThis)
                    ProcessReference(refTo, stack, addressableStacks);
                m_HelpManager.MakeHelp(ProfilerStrings.ReferencedAssetText, addressableStacks);

                // m_HelpManager.MakeHelp(ProfilerStrings.ReferencedAssetText, new List<ContentData>(addressedContentReferencingSelection));
            }

            DisplayAssetPreview(assetData);

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null && settings.FindAssetEntry(assetData.AssetGuid) != null)
                m_Elements.SelectInGroups.SetEnabled(true);
        }

        private void ProcessReference(ContentData processing, in Stack<ContentData> stack, in List<ContentData[]> addressableStacks)
        {
            stack.Push(processing);

            if (processing.AddressableHandles > 0)
                addressableStacks.Add(stack.ToArray());

            foreach (ContentData refTo in processing.ReferencesToThis)
                ProcessReference(refTo, stack, addressableStacks);

            stack.Pop();
        }

        private void DisplayAssetPreview(AssetData assetData)
        {
            string path = AssetDatabase.GUIDToAssetPath(assetData.AssetGuid);
            if (!string.IsNullOrEmpty(path))
            {
                m_Elements.SelectInEditor.SetEnabled(true);
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (obj != null)
                {
                    var editor = UnityEditor.Editor.CreateEditor(obj);
                    Texture2D preview = editor.RenderStaticPreview(path, new Object[] { obj }, 256, 256);
                    Object.DestroyImmediate(editor);
                    if (preview != null)
                        m_Elements.PreviewImage.image = preview;
                }
            }

            m_Elements.PreviewFoldout.style.display =
                new StyleEnum<DisplayStyle>(m_Elements.PreviewImage.image == null ? DisplayStyle.None : DisplayStyle.Flex);
        }

        private void SetSourceObject(ObjectData objectData)
        {
            m_Elements.SelectedAsset.SetContent(objectData);

            bool sceneObject = false;
            if (objectData.Parent is AssetData assetData)
            {
                if (assetData.MainAssetType == AssetType.Scene)
                    sceneObject = true;
            }

            if (sceneObject && objectData.AssetType != AssetType.SceneObject)
            {
                m_HelpManager.MakeHelp("This indicates that the scene contains this object type, but no other data is available.");
                ProfilerGUIUtilities.Hide(m_Elements.ReferencesFoldout);
            }
            else
            {
                ProfilerGUIUtilities.Show(m_Elements.ReferencesFoldout);
                ReferencesRibbonOnClicked(m_Elements.ReferencesTypeSelection.m_CurrentOption);
            }
            ProfilerGUIUtilities.Hide(m_Elements.PreviewFoldout);

            AddLabel("Object Type", objectData.AssetType.ToString(), m_Elements.SelectionDetailsFoldout);
            if (objectData.AssetType == AssetType.Component)
                AddLabel("Component", objectData.ComponentName, m_Elements.SelectionDetailsFoldout);
        }

        private void ReferencesRibbonOnClicked(int optionValue)
        {
            if (m_SelectionSource == null)
            {
                m_Elements.ReferencedByButton.text = "Referenced By (0)";
                m_Elements.ReferencesToButton.text = "References To (0)";
                m_ReferencesTree.SetSource(new List<ContentData>());
                return;
            }

            m_ReferencesTree.SetSource(optionValue == 0 ? m_SelectionSource.ThisReferencesOther : m_SelectionSource.ReferencesToThis);
            m_Elements.ReferencedByButton.text = $"Referenced By ({m_SelectionSource.ReferencesToThis.Count})";
            m_Elements.ReferencesToButton.text = $"References To ({m_SelectionSource.ThisReferencesOther.Count})";
        }

        private void SelectInEditorOnClicked()
        {
            var assetData = m_SelectionSource as AssetData;
            if (assetData == null)
                return;
            string path = assetData.ReportExplicitData != null ? assetData.ReportExplicitData.AssetPath :
                assetData.ReportImplicitData != null ? assetData.ReportImplicitData.AssetPath : null;
            if (string.IsNullOrEmpty(path))
                return;
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj == null)
                return;

            EditorGUIUtility.PingObject(obj);
            Selection.activeObject = obj;
        }

        private void SelectInGroupsOnClicked()
        {
            var assetData = m_SelectionSource as AssetData;
            if (assetData == null)
                return;
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return;
            AddressableAssetEntry entry = settings.FindAssetEntry(assetData.AssetGuid);
            if (entry == null)
                return;

            AddressableAssetsWindow.Init();
            var window = EditorWindow.GetWindow<AddressableAssetsWindow>();
            List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>(){entry};
            window.SelectAssetsInGroupEditor(entries);
        }
    }
}

#endif
