using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.GUI.Adapters;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.AddressableAssets.GUI
{
    [ExcludeFromCoverage]
    class GroupsPopupContent : PopupWindowContent
    {
        // Minimum popup width when the activator rect is narrower (e.g. opened from a context menu).
        private const float k_MinWidth = 240f;
        private const float k_DefaultHeight = 300f;

        private AddressableAssetGroup m_InitialSelection;
        private bool m_AllowReadOnlyGroups;
        private bool m_StayOpenAfterSelection;
        private Rect m_ActivatorRect;

        private Action<AddressableAssetSettings, List<AddressableAssetEntry>, AddressableAssetGroup> m_Action;
        private AddressableAssetSettings m_SettingsContext;
        private List<AddressableAssetEntry> m_EntriesContext;

        private GroupsPopupTreeView m_Tree;
        private AddressableAssetEntryTreeViewState m_TreeState;
        private SearchField m_SearchField;
        private string m_CurrentName = string.Empty;
        private Texture2D m_FolderTexture;

        private bool m_ShouldClose;

        public GroupsPopupContent(Rect activatorRect, AddressableAssetGroup initialSelection, bool allowReadOnlyGroups, bool stayOpenAfterSelection,
            Action<AddressableAssetSettings, List<AddressableAssetEntry>, AddressableAssetGroup> action, AddressableAssetSettings settingsContext, List<AddressableAssetEntry> entriesContext)
        {
            m_ActivatorRect = activatorRect;
            m_InitialSelection = initialSelection;
            m_AllowReadOnlyGroups = allowReadOnlyGroups;
            m_StayOpenAfterSelection = stayOpenAfterSelection;

            m_Action = action;
            m_SettingsContext = settingsContext;
            m_EntriesContext = entriesContext;

            m_SearchField = new SearchField();
            m_SearchField.downOrUpArrowKeyPressed += () => { m_Tree.SetFocus(); };

            m_FolderTexture = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;

            m_ShouldClose = false;
        }

        private void ForceClose()
        {
            m_ShouldClose = true;
        }

        public override Vector2 GetWindowSize()
        {
            float width = Mathf.Max(m_ActivatorRect.width, k_MinWidth);
            return new Vector2(width, k_DefaultHeight);
        }

        public override void OnOpen()
        {
            m_SearchField.SetFocus();
        }

        public override void OnGUI(Rect rect)
        {
            int border = 4;
            int topPadding = 2;
            int searchHeight = 20;
            var searchRect = new Rect(border, topPadding, rect.width - border * 2, searchHeight);
            var remainTop = topPadding + searchHeight + border;
            var remainingRect = new Rect(border, topPadding + searchHeight + border, rect.width - border * 2, rect.height - remainTop - border);

            if (m_Tree == null)
            {
                if (m_TreeState == null)
                    m_TreeState = new AddressableAssetEntryTreeViewState();
                m_Tree = new GroupsPopupTreeView(m_TreeState, this, m_AllowReadOnlyGroups);
                m_Tree.Reload();

                if (m_InitialSelection != null)
                    m_Tree.SetInitialSelection(m_InitialSelection.Name);
            }

            bool isKeyPressed = Event.current.type == EventType.KeyDown && Event.current.isKey;
            bool isEnterKeyPressed = isKeyPressed && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return);
            bool isUpOrDownArrowPressed = isKeyPressed && (Event.current.keyCode == KeyCode.UpArrow || Event.current.keyCode == KeyCode.DownArrow);

            if (isUpOrDownArrowPressed)
                m_Tree.SetFocus();

            m_CurrentName = m_SearchField.OnGUI(searchRect, m_CurrentName);
            m_Tree.searchString = m_CurrentName;

            if (m_Tree.GetRows().Count == 0)
            {
                string helpMessage = "The Shared Bundle Settings Group must have an enabled Content Packing & Loading schema attached to it. It appears none of the Groups that match your search criteria " +
                    "have this schema attached and enabled. A quick fix is to create an empty Group with the Content Packing & Loading schema, then set the shared Group to that empty Group.";
                // Reserve room for the info icon so the message height accounts for the narrower text area and is not clipped.
                const float helpBoxIconWidth = 55f;
                float helpBoxHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(helpMessage), remainingRect.width - helpBoxIconWidth);
                Rect helpBoxRect = new Rect(remainingRect.x, remainingRect.y, remainingRect.width, Mathf.Min(helpBoxHeight, remainingRect.height));
                EditorGUI.HelpBox(helpBoxRect, helpMessage, MessageType.Info);
            }
            else
            {
                m_Tree.IsEnterKeyPressed = isEnterKeyPressed;
                m_Tree.OnGUI(remainingRect);
            }

            if (m_ShouldClose || isEnterKeyPressed)
            {
                GUIUtility.hotControl = 0;
                editorWindow.Close();
            }
        }

        internal class GroupsPopupTreeItem : TreeViewItemAdapter
        {
            internal AddressableAssetGroup m_Group;

            public GroupsPopupTreeItem(int id, int depth, string displayName, AddressableAssetGroup group, Texture2D folderTexture)
                : base(id, depth, displayName)
            {
                m_Group = group;
                icon = folderTexture;
            }
        }

        internal class GroupsPopupTreeView : TreeViewAdapter
        {
            internal GroupsPopupContent m_Popup;

            internal bool IsEnterKeyPressed { get; set; }

            internal bool m_ShowReadOnlyGroups;

            public GroupsPopupTreeView(TreeViewStateAdapter state, GroupsPopupContent popup, bool showReadOnlyGroups)
                : base(state)
            {
                m_Popup = popup;
                m_ShowReadOnlyGroups = showReadOnlyGroups;
                enableItemHovering = true;
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
                    m_Popup.m_Action(m_Popup.m_SettingsContext, m_Popup.m_EntriesContext, groupTreeItem.m_Group);
                }
                m_Popup.ForceClose();
            }

            internal void SetInitialSelection(string assetString)
            {
                if(rootItem == null || rootItem.children == null)
                    return;

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
                        m_Popup.m_Action(m_Popup.m_SettingsContext, m_Popup.m_EntriesContext, groupTreeItem.m_Group);
                    }
                    if (m_Popup.m_StayOpenAfterSelection)
                        SetFocus();
                    else
                        m_Popup.ForceClose();
                }
            }

            protected override bool CanMultiSelect(TreeViewItemAdapter item)
            {
                return false;
            }

            protected override IList<TreeViewItemAdapter> BuildRowsAdapter(TreeViewItemAdapter root)
            {
                if (string.IsNullOrEmpty(searchString))
                {
                    return base.BuildRowsAdapter(root);
                }

                List<TreeViewItemAdapter> rows = new List<TreeViewItemAdapter>();
                if(root.children == null)
                    return rows;

                foreach (var child in rootItem.children)
                {
                    if (child.displayName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                        rows.Add(child as TreeViewItemAdapter);
                }
                return rows;
            }

            protected override TreeViewItemAdapter BuildRootAdapter()
            {
                var root = new TreeViewItemAdapter(-1, -1);

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
                        if (group == null)
                            continue;

                        var bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
                        if (bundledSchema == null || !bundledSchema.IsEnabled)
                            continue;

                        if (!m_ShowReadOnlyGroups && group.ReadOnly)
                            continue;
                        var child = new GroupsPopupTreeItem(group.Guid.GetHashCode(), 0, group.name, group, m_Popup.m_FolderTexture);
                        root.AddChild(child);
                    }
                }
                return root;
            }
        }
    }

    class GroupsPopupUtility
    {
        static string s_GroupsDropdownControlName = nameof(AddressableAssetInspectorGUI) + ".GroupsPopupField";
        static Texture s_GroupsCaretTexture = null;
        static Texture s_FolderTexture = null;

        internal static void ShowGroupsPopup(Rect activatorRect, AddressableAssetGroup initialSelection, bool allowReadOnlyGroups, bool stayOpenAfterSelection,
            Action<AddressableAssetSettings, List<AddressableAssetEntry>, AddressableAssetGroup> action, AddressableAssetSettings settingsContext, List<AddressableAssetEntry> entriesContext)
        {
            var content = new GroupsPopupContent(activatorRect, initialSelection, allowReadOnlyGroups, stayOpenAfterSelection, action, settingsContext, entriesContext);
            PopupWindow.Show(activatorRect, content);
        }

        internal static void DrawGroupsDropdown(GUIContent dropdownlabelContent, AddressableAssetGroup displayGroup, bool enabledDropdown, bool mixedValueDropdown, bool allowReadOnlyGroups,
        Action<AddressableAssetSettings, List<AddressableAssetEntry>, AddressableAssetGroup> action, List<AddressableAssetEntry> entriesContext)
        {
            using (new EditorGUI.DisabledScope(!enabledDropdown))
            {
                GUILayout.Label(dropdownlabelContent);
                if (mixedValueDropdown)
                    EditorGUI.showMixedValue = true;

                float iconHeight = EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing * 3;
                Vector2 iconSize = EditorGUIUtility.GetIconSize();
                EditorGUIUtility.SetIconSize(new Vector2(iconHeight, iconHeight));
                if (s_FolderTexture == null)
                {
                    s_FolderTexture = EditorGUIUtility.IconContent("Folder Icon").image;
                }

                string displayName = displayGroup != null ? displayGroup.Name : "No Valid Group";
                GUIContent groupGUIContent = new GUIContent(displayName, s_FolderTexture);

                Rect groupFieldRect = GUILayoutUtility.GetRect(new GUIContent(), EditorStyles.objectField);
                float newXPos = EditorGUIUtility.labelWidth + 20;
                float newWidth = groupFieldRect.width + (groupFieldRect.x - newXPos);
                groupFieldRect.x = newXPos;
                groupFieldRect.width = newWidth;

                float pickerWidth = 12f;
                Rect groupFieldRectNoPicker = new Rect(groupFieldRect);
                groupFieldRectNoPicker.xMax = groupFieldRect.xMax - pickerWidth * 1.33f;
                Rect pickerRect = new Rect(groupFieldRectNoPicker.xMax, groupFieldRectNoPicker.y, pickerWidth, groupFieldRectNoPicker.height);

                // Take hotControl on mouse-down so the mouse-up is delivered here and the popup opens on release; opening
                // on the press leaves a pending mouse-up that returns focus to the host window and closes the popup.
                // Runs before DropdownButton so the button cannot consume the picker click first.
                int pickerControlId = GUIUtility.GetControlID(FocusType.Passive);
                bool isPickerPressed = false;
                if (enabledDropdown)
                {
                    switch (Event.current.GetTypeForControl(pickerControlId))
                    {
                        case EventType.MouseDown:
                            if (Event.current.button == 0 && pickerRect.Contains(Event.current.mousePosition))
                            {
                                GUIUtility.hotControl = pickerControlId;
                                Event.current.Use();
                            }
                            break;
                        case EventType.MouseUp:
                            if (GUIUtility.hotControl == pickerControlId)
                            {
                                GUIUtility.hotControl = 0;
                                isPickerPressed = pickerRect.Contains(Event.current.mousePosition);
                                Event.current.Use();
                            }
                            break;
                    }
                }

                UnityEngine.GUI.SetNextControlName(s_GroupsDropdownControlName);
                EditorGUI.DropdownButton(groupFieldRect, groupGUIContent, FocusType.Keyboard, EditorStyles.objectField);
                EditorGUIUtility.SetIconSize(new Vector2(iconSize.x, iconSize.y));

                if (mixedValueDropdown)
                    EditorGUI.showMixedValue = false;

                DrawCaret(pickerRect);

                if (enabledDropdown)
                {
                    // Get settings from displayGroup if available, otherwise from entriesContext
                    AddressableAssetSettings settings = displayGroup?.Settings;
                    if (settings == null && entriesContext != null && entriesContext.Count > 0)
                        settings = entriesContext[0]?.parentGroup?.Settings;
                    if (settings == null)
                        settings = AddressableAssetSettingsDefaultObject.Settings;

                    if (settings != null)
                    {
                        bool isEnterKeyPressed = Event.current.type == EventType.KeyDown && Event.current.isKey && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return);
                        bool enterKeyRequestsPopup = isEnterKeyPressed && (s_GroupsDropdownControlName == UnityEngine.GUI.GetNameOfFocusedControl());
                        if (isPickerPressed || enterKeyRequestsPopup)
                        {
                            AddressableAssetGroup initialSelection = !mixedValueDropdown ? displayGroup : null;
                            ShowGroupsPopup(groupFieldRect, initialSelection, allowReadOnlyGroups, true, action, settings, entriesContext);
                            Event.current.Use();
                        }

                        bool isDragging = Event.current.type == EventType.DragUpdated && groupFieldRectNoPicker.Contains(Event.current.mousePosition);
                        bool isDropping = Event.current.type == EventType.DragPerform && groupFieldRectNoPicker.Contains(Event.current.mousePosition);
                        HandleDragAndDrop(isDragging, isDropping, allowReadOnlyGroups, action, settings, entriesContext);
                    }
                }

                if (!mixedValueDropdown && displayGroup != null)
                {
                    if (Event.current.clickCount == 1 && groupFieldRectNoPicker.Contains(Event.current.mousePosition))
                    {
                        UnityEngine.GUI.FocusControl(s_GroupsDropdownControlName);
                        AddressableAssetsWindow.Init();
                        var window = EditorWindow.GetWindow<AddressableAssetsWindow>();
                        window.SelectGroupInGroupEditor(displayGroup, false);
                    }

                    if (Event.current.clickCount == 2 && groupFieldRectNoPicker.Contains(Event.current.mousePosition))
                    {
                        AddressableAssetsWindow.Init();
                        var window = EditorWindow.GetWindow<AddressableAssetsWindow>();
                        window.SelectGroupInGroupEditor(displayGroup, true);
                    }
                }
            }
        }

        static void DrawCaret(Rect pickerRect)
        {
            if (s_GroupsCaretTexture == null)
            {
                s_GroupsCaretTexture = EditorGUIUtility.IconContent("d_pick").image;
            }
            UnityEngine.GUI.DrawTexture(pickerRect, s_GroupsCaretTexture, ScaleMode.ScaleToFit);
        }

        static void HandleDragAndDrop( bool isDragging, bool isDropping, bool allowReadOnlyGroups,
            Action<AddressableAssetSettings, List<AddressableAssetEntry>, AddressableAssetGroup> action, AddressableAssetSettings settingsContext, List<AddressableAssetEntry> entriesContext)
        {
            var groupItems = DragAndDrop.GetGenericData("AssetEntryTreeViewItem") as List<AssetEntryTreeViewItem>;
            if (isDragging)
            {
                bool singleItem = groupItems != null && groupItems.Count == 1;
                AssetEntryTreeViewItem item = groupItems[0];
                bool validGroup = item.IsGroup && allowReadOnlyGroups ? true : !item.group.ReadOnly;
                DragAndDrop.visualMode = (singleItem && validGroup) ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            }
            else if (isDropping)
            {
                if (groupItems != null)
                {
                    var group = groupItems[0].group;
                    action(settingsContext, entriesContext, group);
                }
            }
        }
    }
}
