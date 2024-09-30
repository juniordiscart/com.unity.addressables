using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.AddressableAssets.GUI
{
    [InitializeOnLoad, ExcludeFromCoverage]
    class GroupsPopupWindow : EditorWindow
    {
        private AddressableAssetSettings m_Settings;
        private List<AddressableAssetEntry> m_Entries;
        private bool m_SetInitialSelection;
        private bool m_StayOpenAfterSelection;
        private Action<AddressableAssetSettings, List<AddressableAssetEntry>, AddressableAssetGroup> m_Action;

        private GroupsPopupTreeView m_Tree;
        private TreeViewState m_TreeState;
        private SearchField m_SearchField;
        private string m_CurrentName = string.Empty;
        private Texture2D m_FolderTexture;

        private bool m_ShouldClose;
        private void ForceClose()
        {
            m_ShouldClose = true;
        }

        public void Initialize(AddressableAssetSettings settings, List<AddressableAssetEntry> entries, bool setInitialSelection, bool stayOpenAfterSelection,
            Action<AddressableAssetSettings, List<AddressableAssetEntry>, AddressableAssetGroup> action)
        {
            m_Settings = settings;
            m_Entries = entries;
            m_SetInitialSelection = setInitialSelection;
            m_StayOpenAfterSelection = stayOpenAfterSelection;

            m_Action = action;

            m_SearchField = new SearchField();
            m_SearchField.SetFocus();
            m_SearchField.downOrUpArrowKeyPressed += () => { m_Tree.SetFocus(); };

            if (m_Tree != null && m_SetInitialSelection)
                m_Tree.SetInitialSelection(m_Entries[0].parentGroup.Name);

            m_FolderTexture = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;

            m_ShouldClose = false;
        }

        public void SetPosition(Vector2 location)
        {
            Rect currentRect = position;
            Vector2 newPos = GUIUtility.GUIToScreenPoint(location);
            currentRect.position = new Vector2(Math.Max(newPos.x, 0), newPos.y);
            position = currentRect;
        }

        private void OnLostFocus()
        {
            ForceClose();
        }

        private void OnGUI()
        {
            Rect rect = position;

            int border = 4;
            int topPadding = 2;
            int searchHeight = 20;
            var searchRect = new Rect(border, topPadding, rect.width - border * 2, searchHeight);
            var remainTop = topPadding + searchHeight + border;
            var remainingRect = new Rect(border, topPadding + searchHeight + border, rect.width - border * 2, rect.height - remainTop - border);

            if (m_Tree == null || m_Entries == null)
            {
                if (m_TreeState == null)
                    m_TreeState = new TreeViewState();
                m_Tree = new GroupsPopupTreeView(m_TreeState, this);
                m_Tree.Reload();

                if (m_Entries != null && m_SetInitialSelection)
                    m_Tree.SetInitialSelection(m_Entries[0].parentGroup.Name);
            }

            bool isKeyPressed = Event.current.type == EventType.KeyDown && Event.current.isKey;
            bool isEnterKeyPressed = isKeyPressed && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return);
            bool isUpOrDownArrowPressed = isKeyPressed && (Event.current.keyCode == KeyCode.UpArrow || Event.current.keyCode == KeyCode.DownArrow);

            if (isUpOrDownArrowPressed)
                m_Tree.SetFocus();

            m_CurrentName = m_SearchField.OnGUI(searchRect, m_CurrentName);
            m_Tree.searchString = m_CurrentName;
            m_Tree.IsEnterKeyPressed = isEnterKeyPressed;
            m_Tree.OnGUI(remainingRect);

            if (m_ShouldClose || isEnterKeyPressed)
            {
                GUIUtility.hotControl = 0;
                Close();
            }
        }

        internal class GroupsPopupTreeItem : TreeViewItem
        {
            internal AddressableAssetGroup m_Group;

            public GroupsPopupTreeItem(int id, int depth, string displayName, AddressableAssetGroup group, Texture2D folderTexture)
                : base(id, depth, displayName)
            {
                m_Group = group;
                icon = folderTexture;
            }
        }

        internal class GroupsPopupTreeView : TreeView
        {
            internal GroupsPopupWindow m_Popup;

            internal bool IsEnterKeyPressed { get; set; }

            public GroupsPopupTreeView(TreeViewState state, GroupsPopupWindow popup)
                : base(state)
            {
                m_Popup = popup;
#if UNITY_2022_1_OR_NEWER
                enableItemHovering = true;
#endif
            }

            public override void OnGUI(Rect rect)
            {
                base.OnGUI(rect);
                if (IsEnterKeyPressed && HasFocus())
                {
                    m_Popup.ForceClose();
                }
            }

            protected override void DoubleClickedItem(int id)
            {
                var groupTreeItem = FindItem(id, rootItem) as GroupsPopupTreeItem;
                if (groupTreeItem != null)
                {
                    m_Popup.m_Action(m_Popup.m_Settings, m_Popup.m_Entries, groupTreeItem.m_Group);
                }
                m_Popup.ForceClose();
            }

            internal void SetInitialSelection(string assetString)
            {
                foreach (var child in rootItem.children)
                {
                    if (child.displayName.IndexOf(assetString, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        SetSelection(new List<int> { child.id });
                        return;
                    }
                }
            }

            protected override void SelectionChanged(IList<int> selectedIds)
            {
                if (selectedIds != null && selectedIds.Count == 1)
                {
                    var groupTreeItem = FindItem(selectedIds[0], rootItem) as GroupsPopupTreeItem;
                    if (groupTreeItem != null)
                    {
                        m_Popup.m_Action(m_Popup.m_Settings, m_Popup.m_Entries, groupTreeItem.m_Group);
                    }
                    if (m_Popup.m_StayOpenAfterSelection)
                        SetFocus();
                    else
                        m_Popup.ForceClose();
                }
            }

            protected override bool CanMultiSelect(TreeViewItem item)
            {
                return false;
            }

            protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
            {
                if (string.IsNullOrEmpty(searchString))
                {
                    return base.BuildRows(root);
                }

                List<TreeViewItem> rows = new List<TreeViewItem>();
                foreach (var child in rootItem.children)
                {
                    if (child.displayName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                        rows.Add(child);
                }
                return rows;
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem(-1, -1);

                var aaSettings = AddressableAssetSettingsDefaultObject.Settings;
                if (aaSettings == null)
                {
                    var message = "Use 'Window->Addressables' to initialize.";
                    root.AddChild(new GroupsPopupTreeItem(message.GetHashCode(), 0, message, null, m_Popup.m_FolderTexture));
                }
                else
                {
                    foreach (AddressableAssetGroup group in aaSettings.groups)
                    {
                        if (!group.ReadOnly)
                        {
                            var child = new GroupsPopupTreeItem(group.Guid.GetHashCode(), 0, group.name, group, m_Popup.m_FolderTexture);
                            root.AddChild(child);
                        }
                    }
                }
                return root;
            }
        }
    }
}
