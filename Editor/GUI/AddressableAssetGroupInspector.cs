using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;

// ReSharper disable DelegateSubtraction

namespace UnityEditor.AddressableAssets.GUI
{
    [CustomEditor(typeof(AddressableAssetGroup)), CanEditMultipleObjects]
    class AddressableAssetGroupInspector : Editor
    {
        AddressableAssetGroup m_GroupTarget;
        List<Type> m_SchemaTypes;

        // Used for Multi-group editing
        AddressableAssetGroup[] m_GroupTargets;

        // Stores a 2D list of schemas found on the other selected asset groups.
        // Each schema list contains only schemas of the same type (e.g. BundledAssetGroupSchema).
        List<List<AddressableAssetGroupSchema>> m_GroupSchemas;

        private GUIContent m_InspectAASettings = new GUIContent("Inspect Top Level Settings", "View Addressable Asset Settings");

        void OnEnable()
        {
            m_GroupTargets = new AddressableAssetGroup[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                m_GroupTargets[i] = targets[i] as AddressableAssetGroup;
            }

            // use item with largest index as base
            m_GroupTarget = m_GroupTargets[m_GroupTargets.Length - 1];

            if (m_GroupTarget != null)
            {
                m_GroupTarget.Settings.OnModification += OnSettingsModification;
                m_SchemaTypes = AddressableAssetUtility.GetTypes<AddressableAssetGroupSchema>();
            }
        }

        void OnDisable()
        {
            if (m_GroupTarget != null)
                m_GroupTarget.Settings.OnModification -= OnSettingsModification;
        }

        void OnSettingsModification(AddressableAssetSettings settings, AddressableAssetSettings.ModificationEvent evnt, object o)
        {
            switch (evnt)
            {
                case AddressableAssetSettings.ModificationEvent.GroupAdded:
                case AddressableAssetSettings.ModificationEvent.GroupRemoved:
                case AddressableAssetSettings.ModificationEvent.GroupRenamed:
                case AddressableAssetSettings.ModificationEvent.BatchModification:
                case AddressableAssetSettings.ModificationEvent.ActiveProfileSet:
                case AddressableAssetSettings.ModificationEvent.GroupSchemaAdded:
                case AddressableAssetSettings.ModificationEvent.GroupSchemaModified:
                case AddressableAssetSettings.ModificationEvent.GroupSchemaRemoved:
                    Repaint();
                    break;
            }
        }

        void DrawDivider()
        {
            GUILayout.Space(1.5f);
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(2.5f));
            r.x = 0;
            r.width = EditorGUIUtility.currentViewWidth;
            r.height = 1;

            Color color = new Color(0.6f, 0.6f, 0.6f, 1.333f);
            if (EditorGUIUtility.isProSkin)
            {
                color.r = 0.12f;
                color.g = 0.12f;
                color.b = 0.12f;
            }

            EditorGUI.DrawRect(r, color);
        }

        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        public override void OnInspectorGUI()
        {
            try
            {
                serializedObject.Update();
                DrawSchemas(GetSchemasToDraw());
                serializedObject.ApplyModifiedProperties();
            }
            catch (UnityEngine.ExitGUIException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        List<AddressableAssetGroupSchema> GetSchemasToDraw()
        {
            List<AddressableAssetGroupSchema> values = new List<AddressableAssetGroupSchema>();

            if (m_GroupTargets == null || m_GroupTargets.Length == 0)
                return values;

            // For single selection, use display order
            if (m_GroupTargets.Length == 1)
            {
                var displayOrder = m_GroupTarget.SchemaDisplayOrder;
                for (int i = 0; i < displayOrder.Count; i++)
                {
                    var schema = m_GroupTarget.GetSchemaByDisplayIndex(i);
                    if (schema != null)
                        values.Add(schema);
                }
                return values;
            }

            // For multi-selection, use intersection (alphabetical order)
            values.AddRange(m_GroupTarget.Schemas);

            foreach (var group in m_GroupTargets)
            {
                if (group != m_GroupTarget)
                    values = values.Intersect(group.Schemas, new GroupSchemasCompare()).ToList();
            }

            return values;
        }

        List<AddressableAssetGroupSchema> GetSchemasForOtherTargets(AddressableAssetGroupSchema schema)
        {
            List<AddressableAssetGroupSchema> values = m_GroupTargets
                .Where(t => t.HasSchema(schema.GetType()) && t != m_GroupTarget)
                .Select(t => t.GetSchema(schema.GetType())).ToList();
            return values;
        }

        void DrawSchemas(List<AddressableAssetGroupSchema> schemas)
        {
            GUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();
            var activeProfileName = m_GroupTarget.Settings.profileSettings.GetProfileName(m_GroupTarget.Settings.activeProfileId);
            if (string.IsNullOrEmpty(activeProfileName))
            {
                m_GroupTarget.Settings.activeProfileId = null; //this will reset it to default.
                activeProfileName = m_GroupTarget.Settings.profileSettings.GetProfileName(m_GroupTarget.Settings.activeProfileId);
            }

            EditorGUILayout.PrefixLabel("Active Profile: " + activeProfileName);
            if (GUILayout.Button(m_InspectAASettings))
            {
                EditorGUIUtility.PingObject(AddressableAssetSettingsDefaultObject.Settings);
                Selection.activeObject = AddressableAssetSettingsDefaultObject.Settings;
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(6);

            var bundledSchema = m_GroupTarget.GetSchema(typeof(BundledAssetGroupSchema));
            var contentDirSchema = m_GroupTarget.GetSchema<ContentDirectoryGroupSchema>();
            bool contentDirEnabled = contentDirSchema != null && contentDirSchema.IsEnabled;
            bool bothEnabled = bundledSchema != null && bundledSchema.IsEnabled && contentDirEnabled;

            bool issueDividerDrawn = false;
            if (contentDirEnabled)
            {
                string contentDirLoadPath = contentDirSchema.LoadPath.GetValue(m_GroupTarget.Settings);

                if (ResourceManagerConfig.IsPathRemote(contentDirLoadPath))
                {
                    DrawDivider();
                    GUILayout.Space(6);
                    issueDividerDrawn = true;

                    AddressablesGUIUtility.DrawErrorBoxWithLink(
                        $"Currently, \"{AddressableAssetUtility.GetCachedTypeDisplayName(contentDirSchema.GetType())}\" only supports local content. Change the Load Path to resolve.",
                        "Read more...",
                        AddressableAssetUtility.GenerateDocsURL("group-inspector-settings-reference.html"));
                    GUILayout.Space(6);
                }
            }

            if (bothEnabled)
            {
                if (!issueDividerDrawn)
                {
                    DrawDivider();
                    GUILayout.Space(6);
                }

                AddressablesGUIUtility.DrawErrorBoxWithLink(
                    $"Cannot enable \"{AddressableAssetUtility.GetCachedTypeDisplayName(bundledSchema.GetType())}\" and \"{AddressableAssetUtility.GetCachedTypeDisplayName(contentDirSchema.GetType())}\" schemas at the same time. Disable one to resolve.",
                    "Read more...",
                    AddressableAssetUtility.GenerateDocsURL("group-inspector-settings-reference.html"));
                GUILayout.Space(6);
            }

            bool doDrawDivider = false;

            EditorGUILayout.BeginVertical();
            for (int i = 0; i < schemas.Count; i++)
            {
                var schema = schemas[i];
                var schemaType = schema.GetType();
                int currentIndex = i;

                string foldoutKey = "Addressables.GroupSchema." + schemaType.Name;
                bool foldoutActive = AddressablesGUIUtility.GetFoldoutValue(foldoutKey);

                string helpUrl = null;
                if (schemaType == typeof(BundledAssetGroupSchema))
                    helpUrl = AddressableAssetUtility.GenerateDocsURL("group-inspector-settings-reference.html");
                if (schemaType == typeof(ContentUpdateGroupSchema))
                    helpUrl = AddressableAssetUtility.GenerateDocsURL("group-inspector-settings-reference.html#content-update-group-schema");
                Action helpAction = null;

                if (!string.IsNullOrEmpty(helpUrl))
                    helpAction = () => { Application.OpenURL(helpUrl); };

                Action<Rect> menuAction = null;
                if (!m_GroupTarget.ReadOnly)
                    menuAction = rect =>
                {
                    var menu = new GenericMenu();
                    menu.AddItem(AddressableAssetGroup.RemoveSchemaContent, false, () =>
                    {
                        string dialogMessage = "Are you sure you want to remove " + AddressableAssetUtility.GetCachedTypeDisplayName(schemaType) + " schema?";
                        bool removingBundledAssetGroupSchemaFromSharedBundleGroup = false;
                        if (schema is BundledAssetGroupSchema)
                        {
                            var sharedGroup = schema.Group.Settings.GetSharedBundleGroup();
                            foreach (var t in targets)
                            {
                                if (t is AddressableAssetGroup group)
                                {
                                    if (sharedGroup == group)
                                    {
                                        dialogMessage += $"\n\nNote: Your current Addressable build settings are using data from this schema on group {sharedGroup.Name}. If you remove this schema, it may cause issues with building and loading Addressable assets. " +
                                            "You can change which Group is used for these settings in AddressableAssetSettings -> Shared Group Settings.";
                                        removingBundledAssetGroupSchemaFromSharedBundleGroup = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (targets.Length > 1)
                        {
                            dialogMessage += $"\n\nThis will apply to {targets.Length} selected groups.";
                        }

                        dialogMessage += "\n\nYou cannot undo this action.";

                        if (EditorUtility.DisplayDialog("Remove selected schema?", dialogMessage, "Yes", "No"))
                        {
                            OnRemoveSchema(schemaType);
                            if (removingBundledAssetGroupSchemaFromSharedBundleGroup)
                            {
                                Debug.LogWarning("You have removed the Bundled Asset Group Schema from the Shared Bundle Settings group. " +
                                    "Please ensure that another group has this schema added and you change the AddressableAssetSettings -> Shared Bundle Settings " +
                                    "before building Addressables to avoid build issues.");
                            }
                        }
                    });
                    menu.AddItem(AddressableAssetGroup.MoveSchemaUpContent, false, () =>
                    {
                        if (currentIndex > 0)
                        {
                            Undo.RecordObject(m_GroupTarget, "Move Schema Up");
                            var displayOrder = m_GroupTarget.SchemaDisplayOrder;
                            var temp = displayOrder[currentIndex];
                            displayOrder[currentIndex] = displayOrder[currentIndex - 1];
                            displayOrder[currentIndex - 1] = temp;
                            EditorUtility.SetDirty(m_GroupTarget);
                        }
                    });
                    menu.AddItem(AddressableAssetGroup.MoveSchemaDownContent, false, () =>
                    {
                        if (currentIndex < m_GroupTarget.SchemaDisplayOrder.Count - 1)
                        {
                            Undo.RecordObject(m_GroupTarget, "Move Schema Down");
                            var displayOrder = m_GroupTarget.SchemaDisplayOrder;
                            var temp = displayOrder[currentIndex];
                            displayOrder[currentIndex] = displayOrder[currentIndex + 1];
                            displayOrder[currentIndex + 1] = temp;
                            EditorUtility.SetDirty(m_GroupTarget);
                        }
                    });
                    menu.AddSeparator("");
                    menu.AddItem(AddressableAssetGroup.ExpandSchemaContent, false, () =>
                    {
                        if (foldoutActive == false)
                        {
                            foldoutActive = true;
                            AddressablesGUIUtility.SetFoldoutValue(foldoutKey, foldoutActive);
                        }

                        foreach (var targetSchema in m_GroupTarget.Schemas)
                            targetSchema.ShowAllProperties();
                    });
                    menu.ShowAsContext();
                };
                string displayName = AddressableAssetUtility.GetCachedTypeDisplayName(schemaType);
                GUIContent foldoutContent = new GUIContent(displayName);
                EditorGUI.BeginChangeCheck();

                foldoutActive = AddressablesGUIUtility.BeginFoldoutHeaderGroupWithHelp(
                    foldoutActive, foldoutContent, helpAction, 0,
                    menuAction, schema, m_GroupTarget, m_GroupTargets);

                if (EditorGUI.EndChangeCheck())
                    AddressablesGUIUtility.SetFoldoutValue(foldoutKey, foldoutActive);
                EditorGUI.EndFoldoutHeaderGroup();

                if (foldoutActive)
                {
                    try
                    {
                        EditorGUI.indentLevel++;
                        if (m_GroupTargets.Length == 1)
                            schema.OnGUI();
                        else
                            schema.OnGUIMultiple(GetSchemasForOtherTargets(schema));
                        EditorGUI.indentLevel--;
                    }
                    catch (Exception se)
                    {
                        Debug.LogException(se);
                    }

                    GUILayout.Space(10);
                }

                if (foldoutActive && i == schemas.Count - 1)
                    doDrawDivider = true;
            }

            if (doDrawDivider)
                DrawDivider();
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            GUIStyle addSchemaButton = new GUIStyle(UnityEngine.GUI.skin.button);
            addSchemaButton.fontSize = 12;
            addSchemaButton.fixedWidth = 225;
            addSchemaButton.fixedHeight = 22;

            if (!m_GroupTarget.ReadOnly)
            {
                if (EditorGUILayout.DropdownButton(new GUIContent("Add Schema", "Add new schema to this group."), FocusType.Keyboard, addSchemaButton))
                {
                    var menu = new GenericMenu();
                    for (int i = 0; i < m_SchemaTypes.Count; i++)
                    {
                        var type = m_SchemaTypes[i];

                        if (CanMultiSelectForAddSchema(type))
                        {
                            menu.AddItem(new GUIContent(AddressableAssetUtility.GetCachedTypeDisplayName(type), ""), false, () => OnAddSchema(type));
                        }
                        else
                        {
                            menu.AddDisabledItem(new GUIContent(AddressableAssetUtility.GetCachedTypeDisplayName(type), ""), true);
                        }
                    }

                    menu.ShowAsContext();
                }
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        internal static List<AddressableAssetGroup> GetGroupsWithoutSchema(IEnumerable<AddressableAssetGroup> groups, Type schemaType)
        {
            var result = new List<AddressableAssetGroup>();
            foreach (var group in groups)
            {
                if (group != null && !group.ReadOnly && !group.HasSchema(schemaType))
                    result.Add(group);
            }
            return result;
        }

        internal static List<AddressableAssetGroup> GetGroupsWithSchema(IEnumerable<AddressableAssetGroup> groups, Type schemaType)
        {
            var result = new List<AddressableAssetGroup>();
            foreach (var group in groups)
            {
                if (group != null && !group.ReadOnly && group.HasSchema(schemaType))
                    result.Add(group);
            }
            return result;
        }

        void OnAddSchema(Type schemaType)
        {
            if (targets.Length > 1)
            {
                var groupsToAdd = GetGroupsWithoutSchema(m_GroupTargets, schemaType);
                if (groupsToAdd.Count == 0)
                    return;

                // Batch asset operations for better performance
                AssetDatabase.StartAssetEditing();
                try
                {
                    for (int i = 0; i < groupsToAdd.Count; i++)
                    {
                        // Don't save assets in the loop - we'll save once at the end
                        groupsToAdd[i].AddSchema(schemaType, postEvent: true, saveAssets: false);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                // Save all assets once at the end instead of once per group
                AssetDatabase.SaveAssets();
            }
            else
                m_GroupTarget.AddSchema(schemaType);
        }

        private bool CanMultiSelectForAddSchema(Type schemaType)
        {
            // Single selection: check only the primary target
            if (targets.Length == 1)
                return m_GroupTarget.GetSchema(schemaType) == null;

            // Multi-selection: return true if ANY group is missing the schema
            var groupsWithoutSchema = GetGroupsWithoutSchema(m_GroupTargets, schemaType);
            return groupsWithoutSchema.Count > 0;
        }

        void OnRemoveSchema(Type schemaType)
        {
            if (targets.Length > 1)
            {
                var groupsToRemove = GetGroupsWithSchema(m_GroupTargets, schemaType);
                if (groupsToRemove.Count == 0)
                    return;

                // Batch asset operations for better performance
                AssetDatabase.StartAssetEditing();
                try
                {
                    for (int i = 0; i < groupsToRemove.Count; i++)
                    {
                        // Don't save assets in the loop - we'll save once at the end
                        groupsToRemove[i].RemoveSchema(schemaType, postEvent: true, saveAssets: false);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                // Save all assets once at the end instead of once per group
                AssetDatabase.SaveAssets();
            }
            else
                m_GroupTarget.RemoveSchema(schemaType);
        }

        class GroupSchemasCompare : IEqualityComparer<AddressableAssetGroupSchema>
        {
            public bool Equals(AddressableAssetGroupSchema x, AddressableAssetGroupSchema y)
            {
                if (x.GetType() == y.GetType())
                    return true;

                return false;
            }

            public int GetHashCode(AddressableAssetGroupSchema obj)
            {
                return obj.GetType().GetHashCode();
            }
        }
    }
}
