using UnityEditor.AddressableAssets.Settings;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.AddressableAssets.GUI
{
    public class AddressableAssetSettingsGroupHeader : MultiColumnHeader
    {
        private static string kTreeViewPrefPrefixHeaders = nameof(AddressableAssetEntryTreeView) + ".Headers";

        private AddressableAssetSettings m_Settings;

        public AddressableAssetSettingsGroupHeader(MultiColumnHeaderState mchs, AddressableAssetSettings settings) : base(mchs)
        {
            m_Settings = settings;
        }

        protected override void AddColumnHeaderContextMenuItems(GenericMenu menu)
        {
            base.AddColumnHeaderContextMenuItems(menu);
            menu.AddSeparator("");
            menu.AddItem(EditorGUIUtility.TrTextContent("Reset Header Defaults"), false, ResetHeaderDefaults);
        }

        private void ResetHeaderDefaults()
        {
            state = new MultiColumnHeaderState(GetColumns());
            SaveEditorPrefs();
        }

        internal static MultiColumnHeaderState.Column[] GetColumns()
        {
            var retVal = new[]
            {
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
            };

            int counter = 0;

            retVal[counter].headerContent = new GUIContent(string.Empty, "Notifications");
            retVal[counter].minWidth = 25;
            retVal[counter].width = 25;
            retVal[counter].maxWidth = 25;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = false;
            retVal[counter].autoResize = true;
            counter++;

            retVal[counter].headerContent = new GUIContent("Group Name \\ Addressable Name", "Address used to load asset at runtime");
            retVal[counter].minWidth = 100;
            retVal[counter].width = 260;
            retVal[counter].maxWidth = 10000;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = true;
            retVal[counter].autoResize = true;
            counter++;

            retVal[counter].headerContent = new GUIContent("Type", "Asset type");
            retVal[counter].minWidth = 40;
            retVal[counter].width = 40;
            retVal[counter].maxWidth = 10000;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = false;
            retVal[counter].autoResize = true;
            counter++;

            retVal[counter].headerContent = new GUIContent("Path", "Current Path of asset");
            retVal[counter].minWidth = 100;
            retVal[counter].width = 150;
            retVal[counter].maxWidth = 10000;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = true;
            retVal[counter].autoResize = true;
            counter++;

            retVal[counter].headerContent = new GUIContent("Labels", "Assets can have multiple labels");
            retVal[counter].minWidth = 20;
            retVal[counter].width = 160;
            retVal[counter].maxWidth = 1000;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = true;
            retVal[counter].autoResize = true;

            return retVal;
        }


        internal void LoadEditorPrefs()
        {
            var guid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(m_Settings));
            string columnHeaderJson = EditorPrefs.GetString(GetEditorPreferenceKey(kTreeViewPrefPrefixHeaders, guid), "");
            if (!string.IsNullOrEmpty(columnHeaderJson))
            {
                var savedState = (MultiColumnHeaderState) JsonUtility.FromJson(columnHeaderJson, typeof(MultiColumnHeaderState));
                // check if serialized state is compatible with current header
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(savedState, state))
                {
                    MultiColumnHeaderState.OverwriteSerializedFields(savedState, state);
                    foreach (var column in state.columns)
                        column.width = Mathf.Clamp(column.width, column.minWidth, column.maxWidth);
                    return;
                }
            }
        }

        private string GetEditorPreferenceKey(string key, GUID guid)
        {
            return $"{key}.{guid.ToString()}";
        }

        internal void SaveEditorPrefs()
        {
            var guid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(m_Settings));
            SaveEditorPrefs(guid, state);
        }
        private void SaveEditorPrefs(GUID guid, MultiColumnHeaderState s)
        {
            EditorPrefs.SetString(GetEditorPreferenceKey(kTreeViewPrefPrefixHeaders, guid), JsonUtility.ToJson(s));
        }
    }
}
