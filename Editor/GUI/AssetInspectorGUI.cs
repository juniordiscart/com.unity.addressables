using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.AddressableAssets.GUI
{
    using Object = UnityEngine.Object;

    [InitializeOnLoad, ExcludeFromCoverage]
    internal static class AddressableAssetInspectorGUI
    {
        static GUIStyle s_ToggleMixed;
        static GUIContent s_AddressableAssetToggleText;

        static GUIContent s_SystemSettingsGUIContent = new GUIContent("System Settings", "View Addressable Asset Settings");

        static GUIContent s_GroupsDropdownLabelContent = new GUIContent("Group", "The Addressable Group that this asset is assigned to.");

        static string s_GroupsDropdownControlName = nameof(AddressableAssetInspectorGUI) + ".GroupsPopupField";
        static Texture s_GroupsCaretTexture = null;
        static Texture s_FolderTexture = null;

        static int s_MaxLabelCharCount = 35;
        static GUIContent s_ConfigureLabelsGUIContent = new GUIContent("", "Configure Addressables Labels");
        static int s_RemoveButtonWidth = 8;
        static GUIContent s_RemoveButtonGUIContent = new GUIContent("", EditorGUIUtility.IconContent("toolbarsearchCancelButtonActive").image);

        static GUIStyle s_AssetLabelStyle = null;
        static GUIStyle s_AssetLabelIconStyle = null;
        static GUIStyle s_AssetLabelXButtonStyle = null;

        static AddressableAssetInspectorGUI()
        {
            s_ToggleMixed = null;
            s_AddressableAssetToggleText = new GUIContent("Addressable",
                "Check this to mark this asset as an Addressable Asset, which includes it in the bundled data and makes it loadable via script by its address.");
            Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
        }

        static void SetAaEntry(AddressableAssetSettings aaSettings, List<TargetInfo> targetInfos, bool create)
        {
            if (create && aaSettings.DefaultGroup.ReadOnly)
            {
                Debug.LogError("Current default group is ReadOnly.  Cannot add addressable assets to it");
                return;
            }

            Undo.RecordObject(aaSettings, "AddressableAssetSettings");

            if (!create)
            {
                List<AddressableAssetEntry> removedEntries = new List<AddressableAssetEntry>(targetInfos.Count);
                for (int i = 0; i < targetInfos.Count; ++i)
                {
                    AddressableAssetEntry e = aaSettings.FindAssetEntry(targetInfos[i].Guid);
                    AddressableAssetUtility.OpenAssetIfUsingVCIntegration(e.parentGroup);
                    removedEntries.Add(e);
                    aaSettings.RemoveAssetEntry(removedEntries[i], false);
                }

                if (removedEntries.Count > 0)
                    aaSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, removedEntries, true, false);
            }
            else
            {
                AddressableAssetGroup parentGroup = aaSettings.DefaultGroup;
                var resourceTargets = targetInfos.Where(ti => AddressableAssetUtility.IsInResources(ti.Path));
                if (resourceTargets.Any())
                {
                    var resourcePaths = resourceTargets.Select(t => t.Path).ToList();
                    var resourceGuids = resourceTargets.Select(t => t.Guid).ToList();
                    AddressableAssetUtility.SafeMoveResourcesToGroup(aaSettings, parentGroup, resourcePaths, resourceGuids);
                }

                var otherTargetInfos = targetInfos.Except(resourceTargets);
                List<string> otherTargetGuids = new List<string>(targetInfos.Count);
                foreach (var info in otherTargetInfos)
                    otherTargetGuids.Add(info.Guid);

                var entriesCreated = new List<AddressableAssetEntry>();
                var entriesMoved = new List<AddressableAssetEntry>();
                aaSettings.CreateOrMoveEntries(otherTargetGuids, parentGroup, entriesCreated, entriesMoved, false, false);

                bool openedInVC = false;
                if (entriesMoved.Count > 0)
                {
                    AddressableAssetUtility.OpenAssetIfUsingVCIntegration(parentGroup);
                    openedInVC = true;
                    aaSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesMoved, true, false);
                }

                if (entriesCreated.Count > 0)
                {
                    if (!openedInVC)
                        AddressableAssetUtility.OpenAssetIfUsingVCIntegration(parentGroup);
                    aaSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryAdded, entriesCreated, true, false);
                }
            }
        }

        [UnityEngine.TestTools.ExcludeFromCoverage]
        static void OnPostHeaderGUI(Editor editor)
        {
            var aaSettings = AddressableAssetSettingsDefaultObject.Settings;

            if (editor.targets.Length > 0)
            {
                // only display for the Prefab/Model importer not the displayed GameObjects
                if (editor.targets[0].GetType() == typeof(GameObject))
                    return;

                foreach (var t in editor.targets)
                {
                    if (t is AddressableAssetGroupSchema)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Profile: " + AddressableAssetSettingsDefaultObject.GetSettings(true).profileSettings
                            .GetProfileName(AddressableAssetSettingsDefaultObject.GetSettings(true).activeProfileId));

                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(s_SystemSettingsGUIContent, "MiniButton"))
                        {
                            EditorGUIUtility.PingObject(AddressableAssetSettingsDefaultObject.Settings);
                            Selection.activeObject = AddressableAssetSettingsDefaultObject.Settings;
                        }

                        GUILayout.EndHorizontal();
                        return;
                    }
                }

                List<TargetInfo> targetInfos = GatherTargetInfos(editor.targets, aaSettings);
                if (targetInfos.Count == 0)
                    return;

                bool targetHasAddressableSubObject = false;
                int mainAssetsAddressable = 0;
                int subAssetsAddressable = 0;
                foreach (TargetInfo info in targetInfos)
                {
                    if (info.MainAssetEntry == null)
                        continue;
                    if (info.MainAssetEntry.IsSubAsset)
                        subAssetsAddressable++;
                    else
                        mainAssetsAddressable++;
                    if (!info.IsMainAsset)
                        targetHasAddressableSubObject = true;
                }

                // Overrides a DisabledScope in the EditorElement.cs that disables GUI drawn in the header when the asset cannot be edited.
                bool prevEnabledState = UnityEngine.GUI.enabled;
                if (targetHasAddressableSubObject)
                    UnityEngine.GUI.enabled = false;
                else
                {
                    UnityEngine.GUI.enabled = true;
                    foreach (var info in targetInfos)
                    {
                        if (!info.IsMainAsset)
                        {
                            UnityEngine.GUI.enabled = false;
                            break;
                        }
                    }
                }

                int totalAddressableCount = mainAssetsAddressable + subAssetsAddressable;
                if (totalAddressableCount == 0) // nothing is addressable
                {
                    if (GUILayout.Toggle(false, s_AddressableAssetToggleText, GUILayout.ExpandWidth(false)))
                        SetAaEntry(AddressableAssetSettingsDefaultObject.GetSettings(true), targetInfos, true);
                }
                else if (totalAddressableCount == editor.targets.Length) // everything is addressable
                {
                    var entryInfo = targetInfos[targetInfos.Count - 1];
                    if (entryInfo == null || entryInfo.MainAssetEntry == null)
                        throw new NullReferenceException("EntryInfo incorrect for Addressables content.");

                    GUILayout.BeginHorizontal();

                    if (mainAssetsAddressable > 0 && subAssetsAddressable > 0)
                    {
                        if (s_ToggleMixed == null)
                            s_ToggleMixed = new GUIStyle("ToggleMixed");
                        if (GUILayout.Toggle(false, s_AddressableAssetToggleText, s_ToggleMixed, GUILayout.ExpandWidth(false)))
                            SetAaEntry(aaSettings, targetInfos, true);
                    }
                    else if (mainAssetsAddressable > 0)
                    {
                        if (!GUILayout.Toggle(true, s_AddressableAssetToggleText, GUILayout.ExpandWidth(false)))
                        {
                            SetAaEntry(aaSettings, targetInfos, false);
                            UnityEngine.GUI.enabled = prevEnabledState;
                            GUIUtility.ExitGUI();
                        }
                    }
                    else if (GUILayout.Toggle(false, s_AddressableAssetToggleText, GUILayout.ExpandWidth(false)))
                        SetAaEntry(aaSettings, targetInfos, true);

                    if (editor.targets.Length == 1)
                    {
                        if (!entryInfo.IsMainAsset || entryInfo.MainAssetEntry.IsSubAsset)
                        {
                            bool preAddressPrevEnabledState = UnityEngine.GUI.enabled;
                            UnityEngine.GUI.enabled = false;
                            string address = entryInfo.Address + (entryInfo.IsMainAsset ? "" : $"[{entryInfo.TargetObject.name}]");
                            EditorGUILayout.DelayedTextField(address, GUILayout.ExpandWidth(true));
                            UnityEngine.GUI.enabled = preAddressPrevEnabledState;
                        }
                        else
                        {
                            string newAddress = EditorGUILayout.DelayedTextField(entryInfo.Address, GUILayout.ExpandWidth(true));
                            if (newAddress != entryInfo.Address)
                            {
                                if (newAddress.Contains('[') && newAddress.Contains(']'))
                                    Debug.LogErrorFormat("Rename of address '{0}' cannot contain '[ ]'.", entryInfo.Address);
                                else
                                {
                                    entryInfo.MainAssetEntry.address = newAddress;
                                    AddressableAssetUtility.OpenAssetIfUsingVCIntegration(entryInfo.MainAssetEntry.parentGroup, true);
                                }
                            }
                        }
                    }
                    else
                    {
                        FindUniqueAssetGuids(targetInfos, out var uniqueAssetGuids, out var uniqueAddressableAssetGuids);
                        EditorGUILayout.LabelField(uniqueAddressableAssetGuids.Count + " out of " + uniqueAssetGuids.Count + " assets are addressable.");
                    }

                    DrawSelectEntriesButton(targetInfos);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    DrawGroupsDropdown(aaSettings, targetInfos);
                    GUILayout.EndHorizontal();

                    DrawLabels(targetInfos, aaSettings, editor);
                }
                else // mixed addressable selected
                {
                    GUILayout.BeginHorizontal();
                    if (s_ToggleMixed == null)
                        s_ToggleMixed = new GUIStyle("ToggleMixed");
                    if (GUILayout.Toggle(false, s_AddressableAssetToggleText, s_ToggleMixed, GUILayout.ExpandWidth(false)))
                        SetAaEntry(AddressableAssetSettingsDefaultObject.GetSettings(true), targetInfos, true);
                    FindUniqueAssetGuids(targetInfos, out var uniqueAssetGuids, out var uniqueAddressableAssetGuids);
                    EditorGUILayout.LabelField(uniqueAddressableAssetGuids.Count + " out of " + uniqueAssetGuids.Count + " assets are addressable.");
                    DrawSelectEntriesButton(targetInfos);
                    GUILayout.EndHorizontal();
                }

                UnityEngine.GUI.enabled = prevEnabledState;
            }
        }

        private static void DrawLabels(List<TargetInfo> entryInfos, AddressableAssetSettings aaSettings, Editor editor)
        {
            var entries = new List<AddressableAssetEntry>();
            var labelNameToFreqCount = new Dictionary<string, int>();
            var nonEditableLabels = new HashSet<string>();

            for (int i = 0; i < entryInfos.Count; ++i)
            {
                AddressableAssetEntry entry = aaSettings.FindAssetEntry(entryInfos[i].Guid);
                if (entry == null)
                    continue;

                entries.Add(entry);
                foreach (string label in entry.labels)
                {
                    labelNameToFreqCount.TryGetValue(label, out int labelCount);
                    labelCount++;
                    labelNameToFreqCount[label] = labelCount;

                    if (entry.ReadOnly || entry.IsSubAsset)
                        nonEditableLabels.Add(label);
                }
            }

            int totalNumLabels = labelNameToFreqCount.Count;
            Rect rowRect = EditorGUILayout.GetControlRect(true, 0f);
            float totalRowWidth = rowRect.width; // must be called outside of Begin/EndHoriziontal scope to get correct width

            GUILayout.BeginHorizontal();
            if (s_AssetLabelIconStyle == null)
                s_AssetLabelIconStyle = UnityEngine.GUI.skin.FindStyle("AssetLabel Icon") ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("AssetLabel Icon");
            var buttonRect = GUILayoutUtility.GetRect(s_ConfigureLabelsGUIContent, s_AssetLabelIconStyle);
            if (totalRowWidth > 1) // in some frames totalRowWidth is 1 only
                buttonRect.x = (totalRowWidth - buttonRect.width) + 3;

            GUIContent labelCountGUIContent = new GUIContent($"({totalNumLabels})");
            var labelCountRect = GUILayoutUtility.GetRect(labelCountGUIContent, EditorStyles.miniLabel);
            if (totalRowWidth > 1)
            {
                // in some frames totalRowWidth is 1 only
                labelCountRect.x = buttonRect.x - (labelCountRect.width + 2);
            }

            float xOffset = s_RemoveButtonWidth + labelCountRect.width + buttonRect.width;
            float xMax = labelCountRect.x;

            // Draw modifiable (shared) labels
            var disabledLabels = new List<string>();
            foreach (KeyValuePair<string, int> pair in labelNameToFreqCount)
            {
                string label = pair.Key;
                if (!nonEditableLabels.Contains(label) && entries.Count == labelNameToFreqCount[label])
                    DrawLabel(entries, aaSettings, label, xOffset, xMax);
                else
                    disabledLabels.Add(label);
            }

            // Draw disabled labels
            using (new EditorGUI.DisabledGroupScope(true))
            {
                foreach (string label in disabledLabels)
                {
                    DrawLabel(entries, aaSettings, label, xOffset, xMax);
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUI.LabelField(labelCountRect, labelCountGUIContent, EditorStyles.miniLabel);
            if (EditorGUI.DropdownButton(buttonRect, s_ConfigureLabelsGUIContent, FocusType.Passive, s_AssetLabelIconStyle))
            {
                PopupWindow.Show(buttonRect, new LabelMaskPopupContent(rowRect, aaSettings, entries, labelNameToFreqCount, editor));
            }

            GUILayout.EndHorizontal();
        }

        private static void DrawLabel(List<AddressableAssetEntry> entries, AddressableAssetSettings aaSettings, string label, float xOffset, float xMax)
        {
            GUIContent labelGUIContent = GetGUIContentForLabel(label, s_MaxLabelCharCount);
            if (s_AssetLabelStyle == null)
                s_AssetLabelStyle = UnityEngine.GUI.skin.FindStyle("AssetLabel") ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("AssetLabel");
            Rect labelRect = GUILayoutUtility.GetRect(labelGUIContent, s_AssetLabelStyle);

            labelRect.x -= xOffset;
            if (labelRect.xMax + s_RemoveButtonWidth < xMax)
            {
                EditorGUI.LabelField(labelRect, labelGUIContent, s_AssetLabelStyle);

                if (s_AssetLabelXButtonStyle == null)
                {
                    s_AssetLabelXButtonStyle = UnityEngine.GUI.skin.FindStyle("IconButton") ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("IconButton");
                }
                Rect removeButtonRect = GUILayoutUtility.GetRect(s_RemoveButtonWidth, s_RemoveButtonWidth, s_AssetLabelXButtonStyle);
                removeButtonRect.x = labelRect.xMax - 4; // overlap the button on the pill-shaped label
                if (EditorGUI.DropdownButton(removeButtonRect, s_RemoveButtonGUIContent, FocusType.Passive, s_AssetLabelXButtonStyle))
                    aaSettings.SetLabelValueForEntries(entries, label, false);
            }
        }

        private static GUIContent GetGUIContentForLabel(string labelName, int charCount)
        {
            string displayText;
            int maxLabelCharCount = charCount - 3; // account for length of "..."
            if (labelName.Length > maxLabelCharCount)
                displayText = labelName.Substring(0, maxLabelCharCount) + "...";
            else
                displayText = labelName;
            return new GUIContent(displayText, labelName);
        }

        // Caching due to Gathering TargetInfos is an expensive operation
        // The InspectorGUI needs to call this multiple times per layout and paint
        private static AddressableAssetSettings.Cache<int, List<TargetInfo>> s_Cache = null;

        internal static List<TargetInfo> GatherTargetInfos(Object[] targets, AddressableAssetSettings aaSettings)
        {
            int selectionHashCode = targets[0].GetHashCode();
            for (int i = 1; i < targets.Length; ++i)
                selectionHashCode = selectionHashCode * 31 ^ targets[i].GetHashCode();

            List<TargetInfo> targetInfos = null;
            if (s_Cache == null && aaSettings != null)
                s_Cache = new AddressableAssetSettings.Cache<int, List<TargetInfo>>(aaSettings);
            if (s_Cache != null && s_Cache.TryGetCached(selectionHashCode, out targetInfos))
                return targetInfos;

            targetInfos = new List<TargetInfo>(targets.Length);
            AddressableAssetEntry entry;
            foreach (var t in targets)
            {
                if (AddressableAssetUtility.TryGetPathAndGUIDFromTarget(t, out var path, out var guid))
                {
                    var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                    // Is asset
                    if (mainAssetType != null && !BuildUtility.IsEditorAssembly(mainAssetType.Assembly))
                    {
                        bool isMainAsset = t is AssetImporter || AssetDatabase.IsMainAsset(t);
                        var info = new TargetInfo() {TargetObject = t, Guid = guid, Path = path, IsMainAsset = isMainAsset};
                        if (aaSettings != null)
                        {
                            entry = aaSettings.FindAssetEntry(guid, true);
                            if (entry != null)
                                info.MainAssetEntry = entry;
                        }

                        targetInfos.Add(info);
                    }
                }
            }

            if (s_Cache != null && targetInfos != null && targetInfos.Count > 0)
                s_Cache.Add(selectionHashCode, targetInfos);
            return targetInfos;
        }

        internal static void FindUniqueAssetGuids(List<TargetInfo> targetInfos, out HashSet<string> uniqueAssetGuids, out HashSet<string> uniqueAddressableAssetGuids)
        {
            uniqueAssetGuids = new HashSet<string>();
            uniqueAddressableAssetGuids = new HashSet<string>();
            foreach (TargetInfo info in targetInfos)
            {
                uniqueAssetGuids.Add(info.Guid);
                if (info.MainAssetEntry != null)
                    uniqueAddressableAssetGuids.Add(info.Guid);
            }
        }

        static void DrawSelectEntriesButton(List<TargetInfo> targets)
        {
            var prevGuiEnabled = UnityEngine.GUI.enabled;
            UnityEngine.GUI.enabled = true;

            if (GUILayout.Button("Select"))
            {
                AddressableAssetsWindow.Init();
                var window = EditorWindow.GetWindow<AddressableAssetsWindow>();
                List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>(targets.Count);
                foreach (TargetInfo info in targets)
                {
                    if (info.MainAssetEntry != null)
                    {
                        if (info.IsMainAsset == false && ProjectConfigData.ShowSubObjectsInGroupView)
                        {
                            List<AddressableAssetEntry> subs = new List<AddressableAssetEntry>();
                            info.MainAssetEntry.GatherAllAssets(subs, false, true, true);
                            foreach (AddressableAssetEntry sub in subs)
                            {
                                if (sub.TargetAsset == info.TargetObject)
                                {
                                    entries.Add(sub);
                                    break;
                                }
                            }
                        }
                        else
                            entries.Add(info.MainAssetEntry);
                    }
                }

                if (entries.Count > 0)
                    window.SelectAssetsInGroupEditor(entries);
            }

            UnityEngine.GUI.enabled = prevGuiEnabled;
        }

        static void DrawGroupsDropdown(AddressableAssetSettings settings, List<TargetInfo> targets)
        {
            bool canEditGroup = true;
            bool mixedGroups = false;
            AddressableAssetGroup displayGroup = null;
            var entries = new List<AddressableAssetEntry>();
            foreach (TargetInfo info in targets)
            {
                AddressableAssetEntry entry = info.MainAssetEntry;
                if (entry == null)
                {
                    canEditGroup = false;
                }
                else
                {
                    entries.Add(entry);
                    if (entry.ReadOnly || entry.parentGroup.ReadOnly)
                    {
                        canEditGroup = false;
                    }

                    if (displayGroup == null)
                        displayGroup = entry.parentGroup;
                    else if (entry.parentGroup != displayGroup)
                    {
                        mixedGroups = true;
                    }
                }
            }

            using (new EditorGUI.DisabledScope(!canEditGroup))
            {
                GUILayout.Label(s_GroupsDropdownLabelContent);
                if (mixedGroups)
                    EditorGUI.showMixedValue = true;

                UnityEngine.GUI.SetNextControlName(s_GroupsDropdownControlName);

                float iconHeight = EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing * 3;
                Vector2 iconSize = EditorGUIUtility.GetIconSize();
                EditorGUIUtility.SetIconSize(new Vector2(iconHeight, iconHeight));
                if (s_FolderTexture == null)
                {
                    s_FolderTexture = EditorGUIUtility.IconContent("Folder Icon").image;
                }

                GUIContent groupGUIContent = new GUIContent(displayGroup.Name, s_FolderTexture);
                Rect groupFieldRect = GUILayoutUtility.GetRect(groupGUIContent, EditorStyles.objectField);
                EditorGUI.DropdownButton(groupFieldRect, groupGUIContent, FocusType.Keyboard, EditorStyles.objectField);
                EditorGUIUtility.SetIconSize(new Vector2(iconSize.x, iconSize.y));

                if (mixedGroups)
                    EditorGUI.showMixedValue = false;

                float pickerWidth = 12f;
                Rect groupFieldRectNoPicker = new Rect(groupFieldRect);
                groupFieldRectNoPicker.xMax = groupFieldRect.xMax - pickerWidth * 1.33f;

                Rect pickerRect = new Rect(groupFieldRectNoPicker.xMax, groupFieldRectNoPicker.y, pickerWidth, groupFieldRectNoPicker.height);
                bool isPickerPressed = Event.current.clickCount == 1 && pickerRect.Contains(Event.current.mousePosition);

                DrawCaret(pickerRect);

                if (canEditGroup)
                {
                    bool isEnterKeyPressed = Event.current.type == EventType.KeyDown && Event.current.isKey && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return);
                    bool enterKeyRequestsPopup = isEnterKeyPressed && (s_GroupsDropdownControlName == UnityEngine.GUI.GetNameOfFocusedControl());
                    if (isPickerPressed || enterKeyRequestsPopup)
                    {
                        var popupWindow = EditorWindow.GetWindow<GroupsPopupWindow>(true, "Select Addressable Group", true);
                        popupWindow.Initialize(settings, entries, !mixedGroups, true, AddressableAssetUtility.MoveEntriesToGroup);
                        popupWindow.SetPosition(new Vector2(pickerRect.xMax - popupWindow.position.width, pickerRect.position.y));
                    }

                    bool isDragging = Event.current.type == EventType.DragUpdated && groupFieldRectNoPicker.Contains(Event.current.mousePosition);
                    bool isDropping = Event.current.type == EventType.DragPerform && groupFieldRectNoPicker.Contains(Event.current.mousePosition);
                    HandleDragAndDrop(settings, entries, isDragging, isDropping);
                }

                if (!mixedGroups)
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

        static void HandleDragAndDrop(AddressableAssetSettings settings, List<AddressableAssetEntry> aaEntries, bool isDragging, bool isDropping)
        {
            var groupItems = DragAndDrop.GetGenericData("AssetEntryTreeViewItem") as List<AssetEntryTreeViewItem>;
            if (isDragging)
            {
                bool canDragGroup = groupItems != null && groupItems.Count == 1 && groupItems[0].IsGroup && !groupItems[0].group.ReadOnly;
                DragAndDrop.visualMode = canDragGroup ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            }
            else if (isDropping)
            {
                if (groupItems != null)
                {
                    var group = groupItems[0].group;
                    AddressableAssetUtility.MoveEntriesToGroup(settings, aaEntries, group);
                }
            }
        }

        internal class TargetInfo
        {
            public UnityEngine.Object TargetObject;
            public string Guid;
            public string Path;
            public bool IsMainAsset;
            public AddressableAssetEntry MainAssetEntry;

            public string Address
            {
                get
                {
                    if (MainAssetEntry == null)
                        throw new NullReferenceException("No Entry set for Target info with AssetPath " + Path);
                    return MainAssetEntry.address;
                }
            }
        }
    }
}
