using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.GUI
{
    class ContentUpdatePreviewWindow : EditorWindow
    {
        private GUIContent m_ApplyChangesGUIContent = new GUIContent("Apply Changes", "Move assets to a new remote group in preparation for the content update build");

        internal static bool PrepareForContentUpdate(AddressableAssetSettings settings, string buildPath, Action applyChangesCallback = null)
        {
            var modifiedEntries = ContentUpdateScript.GatherModifiedEntriesWithDependencies(settings, buildPath);
            var previewWindow = GetWindow<ContentUpdatePreviewWindow>();
            previewWindow.Show(settings, modifiedEntries, applyChangesCallback);
            return true;
        }

        internal static void ShowUpdatePreviewWindow(AddressableAssetSettings settings, Dictionary<AddressableAssetEntry, List<AddressableAssetEntry>> modifiedEntries,
            Action applyChangesCallback = null)
        {
            var previewWindow = GetWindow<ContentUpdatePreviewWindow>();
            previewWindow.Show(settings, modifiedEntries, applyChangesCallback, true);
        }

        void OnEnable()
        {
            titleContent = new GUIContent("Assets with update issues");
        }

        class ContentUpdateTreeView : TreeView
        {
            class Item : TreeViewItem
            {
                internal AddressableAssetEntry entry;
                internal bool enabled;

                public Item(AddressableAssetEntry entry, int itemDepth = 1) : base(entry.guid.GetHashCode(), itemDepth)
                {
                    this.entry = entry;
                    enabled = true;
                }
            }

            ContentUpdatePreviewWindow m_Preview;

            public ContentUpdateTreeView(ContentUpdatePreviewWindow preview, TreeViewState state, MultiColumnHeaderState mchs) : base(state, new MultiColumnHeader(mchs))
            {
                m_Preview = preview;
            }

            internal List<AddressableAssetEntry> GetEnabledEntries()
            {
                var result = new HashSet<AddressableAssetEntry>();
                foreach (var i in GetRows())
                {
                    var item = i as Item;
                    if (item != null && item.enabled)
                    {
                        result.Add(item.entry);
                        if (item.hasChildren)
                        {
                            foreach (var child in i.children)
                            {
                                var childItem = child as Item;
                                if (childItem != null && !result.Contains(childItem.entry))
                                    result.Add(childItem.entry);
                            }
                        }
                    }
                }

                return result.ToList();
            }

            protected override TreeViewItem BuildRoot()
            {
                columnIndexForTreeFoldouts = 1;

                var root = new TreeViewItem(-1, -1);
                root.children = new List<TreeViewItem>();
                foreach (var k in m_Preview.m_DepEntriesMap.Keys)
                {
                    var mainItem = new Item(k, 0);
                    root.AddChild(mainItem);

                    foreach (var dep in m_Preview.m_DepEntriesMap[k])
                        mainItem.AddChild(new Item(dep, mainItem.depth + 1));
                }

                return root;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                var item = args.item as Item;
                if (item == null)
                {
                    base.RowGUI(args);
                    return;
                }

                for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    CellGUI(args.GetCellRect(i), item, args.GetColumn(i));
                }
            }

            private const int kToggleOffset = 5;
            private const int kMainAssetXOffset = 20;
            private const int kDependencyAssetXOffset = 40;

            void CellGUI(Rect cellRect, Item item, int column)
            {
                if (column == 0)
                    cellRect.xMin = (cellRect.xMax / 2) - kToggleOffset;
                else //Only want this indent on every column that isn't 0
                {
                    if ((item.parent as Item) != null)
                        cellRect.xMin += kDependencyAssetXOffset;
                    else
                        cellRect.xMin += kMainAssetXOffset;
                }

                if (column == 0)
                {
                    if (item.entry != null)
                    {
                        if ((item.parent as Item) != null)
                            item.enabled = (item.parent as Item).enabled;
                        else
                            item.enabled = EditorGUI.Toggle(cellRect, item.enabled);
                    }
                }
                else if (column == 1)
                {
                    EditorGUI.LabelField(cellRect, item.entry.address);
                }
                else if (column == 2)
                {
                    EditorGUI.LabelField(cellRect, item.entry.AssetPath);
                }
                else if (column == 3)
                {
                    EditorGUI.LabelField(cellRect, item.entry.parentGroup.Name);
                }
            }

            internal static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
            {
                var retVal = new MultiColumnHeaderState.Column[]
                {
                    new MultiColumnHeaderState.Column()
                    {
                        headerContent = new GUIContent("Include", "Include change in Update"),
                        minWidth = 50,
                        width = 50,
                        maxWidth = 50,
                        headerTextAlignment = TextAlignment.Left,
                        canSort = true,
                        autoResize = true
                    },
                    new MultiColumnHeaderState.Column()
                    {
                        headerContent = new GUIContent("Address", "Data Value"),
                        minWidth = 300,
                        width = 300,
                        maxWidth = 1000,
                        headerTextAlignment = TextAlignment.Left,
                        canSort = true,
                        autoResize = true
                    },
                    new MultiColumnHeaderState.Column()
                    {
                        headerContent = new GUIContent("Path", "Asset Path"),
                        minWidth = 300,
                        width = 300,
                        maxWidth = 1000,
                        headerTextAlignment = TextAlignment.Left,
                        canSort = true,
                        autoResize = true
                    },
                    new MultiColumnHeaderState.Column()
                    {
                        headerContent = new GUIContent("Modified Group", "The modified Addressable group"),
                        minWidth = 300,
                        width = 300,
                        maxWidth = 1000,
                        headerTextAlignment = TextAlignment.Left,
                        canSort = true,
                        autoResize = true
                    }
                };

                return new MultiColumnHeaderState(retVal);
            }
        }

        string m_GroupName = "Content Update";
        AddressableAssetSettings m_Settings;
        Dictionary<AddressableAssetEntry, List<AddressableAssetEntry>> m_DepEntriesMap;
        Action m_ApplyChangesCallback;
        Vector2 m_ScrollPosition;
        ContentUpdateTreeView m_Tree;

        [FormerlySerializedAs("treeState")]
        [SerializeField]
        TreeViewState m_TreeState;

        [FormerlySerializedAs("mchs")]
        [SerializeField]
        MultiColumnHeaderState m_Mchs;

        bool m_LogOutcomeAnalytics = false;

        public void Show(AddressableAssetSettings settings, Dictionary<AddressableAssetEntry, List<AddressableAssetEntry>> entryDependencies, Action applyChangesCallback = null,
            bool logAnalytics = false)
        {
            m_Settings = settings;
            m_DepEntriesMap = entryDependencies;
            m_ApplyChangesCallback = applyChangesCallback;
            m_LogOutcomeAnalytics = logAnalytics;
            Show();
        }

        public void OnGUI()
        {
            if (m_DepEntriesMap == null)
                return;

            Rect toolbarRect = new Rect(16, 5, position.width - 32, 70);
            float buttonAreaRectY = position.height - 50;
            Rect contentRect = new Rect(16, 75, position.width - 32, buttonAreaRectY - 75);

            if (m_Tree == null)
            {
                if (m_TreeState == null)
                    m_TreeState = new TreeViewState();

                var headerState = ContentUpdateTreeView.CreateDefaultMultiColumnHeaderState();
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_Mchs, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_Mchs, headerState);
                m_Mchs = headerState;

                m_Tree = new ContentUpdateTreeView(this, m_TreeState, m_Mchs);
                m_Tree.Reload();
            }

            if (m_DepEntriesMap.Count == 0)
            {
                Rect emptyContentRect = new Rect(0, 0, position.width, position.height - 50);
                GUILayout.BeginArea(emptyContentRect);
                GUILayout.BeginVertical();

                GUILayout.Label("No Addressable groups with a BundledAssetGroupSchema and ContentUpdateGroupSchema (with Prevent Updates enabled) appear to have been modified.");

                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
            else
            {
                GUILayout.BeginArea(toolbarRect);
                GUILayout.BeginVertical();
                EditorGUILayout.HelpBox("Modified assets that are part of a group with Prevent Update enabled have been detected during this content update build. " +
                                        "Applying the changes moves all selected items into a new group that has Prevent Updates disabled.", MessageType.Info);

                GUILayout.Space(12f);
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("New Group Name: ", "This value is used to set the name of the new group that is created as part of applying the changes from " +
                                                                   "this tool. If the group already exists, a number is appended to the group name."));

                m_GroupName = GUILayout.TextArea(m_GroupName, GUILayout.MinWidth(400f));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.EndArea();

                m_Tree.OnGUI(contentRect);
            }

            GUILayout.BeginArea(new Rect(0, buttonAreaRectY, position.width, 50));
            GUILayout.BeginHorizontal();
            bool hasPostApplyCallback = m_ApplyChangesCallback != null;

            string cancelButtonName = hasPostApplyCallback ? "Cancel build" : "Cancel";
            if (GUILayout.Button(cancelButtonName))
            {
                if (m_LogOutcomeAnalytics)
                    AddressableAnalytics.ReportUsageEvent(AddressableAnalytics.UsageEventType.ContentUpdateCancelled);
                Close();
            }

            bool showApplyChanges = m_Tree.GetEnabledEntries().Count != 0;
            if (showApplyChanges)
            {
                string buttonName = hasPostApplyCallback ? "Apply and Continue" : "Apply Changes";
                m_ApplyChangesGUIContent.text = buttonName;

                if (GUILayout.Button(m_ApplyChangesGUIContent))
                {
                    if (m_LogOutcomeAnalytics)
                        AddressableAnalytics.ReportUsageEvent(AddressableAnalytics.UsageEventType.ContentUpdateHasChangesInUpdateRestrictionWindow);
                    string groupName = string.IsNullOrEmpty(m_GroupName) ? "Content Update" : m_GroupName;
                    var enabledEntries = m_Tree.GetEnabledEntries();
                    HashSet<AddressableAssetGroup> clearedGroups = new HashSet<AddressableAssetGroup>();
                    foreach (var entry in enabledEntries)
                    {
                        if (clearedGroups.Contains(entry.parentGroup))
                            continue;
                        entry.parentGroup.FlaggedDuringContentUpdateRestriction = false;
                        clearedGroups.Add(entry.parentGroup);
                    }
                    ContentUpdateScript.CreateContentUpdateGroup(m_Settings, enabledEntries, groupName);
                    m_ApplyChangesCallback?.Invoke();

                    Close();
                }
            }
            else
            {
                using (new EditorGUI.DisabledScope(m_ApplyChangesCallback == null))
                {
                    string buttonName = m_ApplyChangesCallback == null ? "Apply Changes" : "Continue without changes";
                    if (GUILayout.Button(buttonName))
                    {
                        if (m_LogOutcomeAnalytics)
                            AddressableAnalytics.ReportUsageEvent(AddressableAnalytics.UsageEventType.ContentUpdateContinuesWithoutChanges);
                        m_ApplyChangesCallback?.Invoke();
                        Close();
                    }
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
