using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Settings.GroupSchemas
{
    /// <summary>
    /// Helper utilities for drawing and managing build &amp; load path UI for schemas that expose
    /// ProfileValueReference build/load paths.
    /// </summary>
    internal static class BuildAndLoadPathUIHelper
    {
        static readonly GUIContent k_BuildAndLoadPathsGUIContent =
            new GUIContent("Build & Load Paths", "Paths to build or load content from");

        static readonly GUIContent k_PathsPreviewGUIContent =
            new GUIContent("Path Preview", "Preview of what the current paths will be evaluated to");

        static readonly string k_NoSettingsWarning =
            L10n.Tr("No Addressable Asset Settings found. Please create one via Window > Asset Management > Addressables > Groups.");

        internal static void ValidatePaths(AddressableAssetGroupSchema ownerSchema,
            AddressableAssetGroup group,
            ref ProfileValueReference buildPath,
            ref ProfileValueReference loadPath)
        {
            if (group == null || group.Settings == null)
                return;

            var settings = group.Settings;
            List<string> variableNames = settings.profileSettings.GetVariableNames();
            ownerSchema.SetPathVariable(settings, ref buildPath, AddressableAssetSettings.kLocalBuildPath,
                "LocalBuildPath", variableNames);
            ownerSchema.SetPathVariable(settings, ref loadPath, AddressableAssetSettings.kLocalLoadPath,
                "LocalLoadPath", variableNames);
        }

        // Formerly ShowSelectedPropertyPathPair
        internal static void DrawPathPair(AddressableAssetGroupSchema schema,
            SerializedObject so,
            ref ProfileValueReference buildPath,
            ref ProfileValueReference loadPath,
            ref bool useCustomPaths,
            ref bool showPaths,
            ref int selectedPathPairIndex)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                EditorGUILayout.HelpBox(k_NoSettingsWarning, MessageType.Warning);
                return;
            }

            var buildPathProperty = so.FindProperty("m_BuildPath");
            var loadPathProperty = so.FindProperty("m_LoadPath");

            List<ProfileGroupType> groupTypes = ProfileGroupType.CreateGroupTypes(settings.profileSettings.GetProfile(settings.activeProfileId), settings);
            List<string> options = groupTypes.Select(group => group.GroupTypePrefix).ToList();
            //Set selected to custom
            options.Add(AddressableAssetProfileSettings.customEntryString);

            //Determine selection and whether to show custom
            int? selected = DetermineSelectedIndex(buildPath, loadPath, useCustomPaths, groupTypes,
                options.Count - 1, settings);

            if (selected.HasValue && selected != options.Count - 1)
                useCustomPaths = false;
            else
                useCustomPaths = true;

            //Dropdown selector
            EditorGUI.BeginChangeCheck();
            var newIndex = EditorGUILayout.Popup(k_BuildAndLoadPathsGUIContent, selected.HasValue ? selected.Value : options.Count - 1, options.ToArray());
            if (EditorGUI.EndChangeCheck() && newIndex != selected)
            {
                selected = newIndex;
                selectedPathPairIndex = newIndex;
                SetPathPairOption(so, settings, ref buildPath, ref loadPath, ref useCustomPaths, options, groupTypes, newIndex);
                EditorUtility.SetDirty(schema);
            }

            if (useCustomPaths)
            {
                //ShowPaths
                DrawProfileValueReference(schema, so, "m_BuildPath", null, ref buildPath);
                DrawProfileValueReference(schema, so, "m_LoadPath", null, ref loadPath);
            }

            ShowPathsPreview(settings, buildPath, loadPath, ref showPaths, false);
            EditorGUI.showMixedValue = false;
        }

        // Formerly ShowSelectedPropertyPathPairMulti
        internal static bool DrawPathPairMulti(AddressableAssetGroupSchema schema,
            SerializedObject so,
            List<AddressableAssetGroupSchema> otherSchemas,
            ref ProfileValueReference buildPath,
            ref ProfileValueReference loadPath,
            ref bool useCustomPaths,
            ref bool showPaths,
            ref int selectedPathPairIndex)
        {
            bool modified = false;
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                EditorGUILayout.HelpBox(k_NoSettingsWarning, MessageType.Warning);
                return false;
            }

            var buildPathProperty = so.FindProperty("m_BuildPath");
            var loadPathProperty = so.FindProperty("m_LoadPath");

            schema.ShowMixedValue(buildPathProperty, otherSchemas, typeof(ProfileValueReference), "m_BuildPath");
            schema.ShowMixedValue(loadPathProperty, otherSchemas, typeof(ProfileValueReference), "m_LoadPath");

            List<ProfileGroupType> groupTypes = ProfileGroupType.CreateGroupTypes(settings.profileSettings.GetProfile(settings.activeProfileId), settings);
            List<string> options = groupTypes.Select(group => group.GroupTypePrefix).ToList();

            //set selected to custom
            options.Add(AddressableAssetProfileSettings.customEntryString);
            int? selected = null;

            //Determine selection and whether to show custom
            if (!EditorGUI.showMixedValue)
            {
                //disregard custom value, want to check if valid pair
                selected = DetermineSelectedIndex(buildPath, loadPath, useCustomPaths, groupTypes, options.Count - 1, settings);
                useCustomPaths = (selected == options.Count - 1);
            }

            //Dropdown selector
            EditorGUI.BeginChangeCheck();
            var newIndex = EditorGUILayout.Popup(k_BuildAndLoadPathsGUIContent, selected.HasValue ? selected.Value : -1, options.ToArray());
            if (EditorGUI.EndChangeCheck() && newIndex != selected)
            {
                modified = true;

                selected = newIndex;
                selectedPathPairIndex = newIndex;

                SetPathPairOption(so, settings, ref buildPath, ref loadPath, ref useCustomPaths, options, groupTypes, newIndex);

                // changes to other schemas is handled in OnGUIMultiple
                EditorGUI.showMixedValue = false;
            }

            if (useCustomPaths && selected.HasValue)
            {
                // ShowPathsMulti
                modified |= DrawProfileValueReferenceMulti(schema, otherSchemas, so, "m_BuildPath", null, ref buildPath);
                modified |= DrawProfileValueReferenceMulti(schema, otherSchemas,so, "m_LoadPath", null, ref loadPath);
            }

            ShowPathsPreview(settings, buildPath, loadPath, ref showPaths, !selected.HasValue);

            EditorGUI.showMixedValue = false;

            return modified;
        }

        // Formerly ShowSelectedPropertyPathMulti
        static void DrawProfileValueReference(AddressableAssetGroupSchema schema, SerializedObject so, string propertyName, GUIContent label, ref ProfileValueReference currentValue)
        {
            var prop = so.FindProperty(propertyName);
            string previousValue = currentValue.Id;
            EditorGUI.BeginChangeCheck();
            //Current implementation using ProfileValueReferenceDrawer
            EditorGUILayout.PropertyField(prop, label, true);
            if (EditorGUI.EndChangeCheck())
            {
                var newValue = currentValue.Id;
                currentValue.Id = previousValue;
                Undo.RecordObject(so.targetObject, so.targetObject.name + propertyName);
                currentValue.Id = newValue;
                EditorUtility.SetDirty(schema);
            }
        }

        // Formerly ShowSelectedPropertyPathMulti
        static bool DrawProfileValueReferenceMulti(AddressableAssetGroupSchema schema, List<AddressableAssetGroupSchema> otherSchemas, SerializedObject so, string propertyName, GUIContent label, ref ProfileValueReference currentValue)
        {
            bool modified = false;
            var prop = so.FindProperty(propertyName);
            schema.ShowMixedValue(prop, otherSchemas, typeof(ProfileValueReference), propertyName);

            string previousValue = currentValue.Id;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(prop, label, true);
            if (EditorGUI.EndChangeCheck())
            {
                var newValue = currentValue.Id;
                currentValue.Id = previousValue;
                Undo.RecordObject(so.targetObject, so.targetObject.name + propertyName);
                currentValue.Id = newValue;
                // changes to multi select is handled in OnGUIMultiple
                EditorUtility.SetDirty(schema);
                modified = true;
            }

            EditorGUI.showMixedValue = false;
            return modified;
        }
        internal static int DetermineSelectedIndex(ProfileValueReference buildPath,
            ProfileValueReference loadPath,
            bool useCustomPaths,
            List<ProfileGroupType> groupTypes,
            int defaultValue,
            AddressableAssetSettings settings,
            HashSet<string> vars)
        {
            int selected = defaultValue;

            if (settings == null)
                return defaultValue;

            if (vars.Contains(buildPath.Id) && vars.Contains(loadPath.Id) && !useCustomPaths)
            {
                for (int i = 0; i < groupTypes.Count; i++)
                {
                    var buildPathVar = groupTypes[i].GetVariableBySuffix("BuildPath");
                    var loadPathVar = groupTypes[i].GetVariableBySuffix("LoadPath");
                    if (buildPath.GetName(settings) == groupTypes[i].GetName(buildPathVar) &&
                        loadPath.GetName(settings) == groupTypes[i].GetName(loadPathVar))
                    {
                        selected = i;
                        break;
                    }
                }
            }
            return selected;
        }

        internal static int DetermineSelectedIndex(ProfileValueReference buildPath,
            ProfileValueReference loadPath,
            bool useCustomPaths,
            List<ProfileGroupType> groupTypes,
            int defaultValue,
            AddressableAssetSettings settings)
        {
            HashSet<string> vars = settings.profileSettings.GetAllVariableIds();
            return DetermineSelectedIndex(buildPath, loadPath, useCustomPaths, groupTypes, defaultValue, settings, vars);
        }

        static void SetPathPairOption(SerializedObject so,
            AddressableAssetSettings settings,
            ref ProfileValueReference buildPath,
            ref ProfileValueReference loadPath,
            ref bool useCustomPaths,
            List<string> options,
            List<ProfileGroupType> groupTypes,
            int newIndex)
        {
            if (options[newIndex] != AddressableAssetProfileSettings.customEntryString)
            {
                Undo.RecordObject(so.targetObject, so.targetObject.name + "Path Pair");
                buildPath.SetVariableByName(settings, groupTypes[newIndex].GroupTypePrefix + ProfileGroupType.k_PrefixSeparator + "BuildPath");
                loadPath.SetVariableByName(settings, groupTypes[newIndex].GroupTypePrefix + ProfileGroupType.k_PrefixSeparator + "LoadPath");
                useCustomPaths = false;
            }
            else
            {
                Undo.RecordObject(so.targetObject, so.targetObject.name + "Path Pair");
                useCustomPaths = true;
            }
        }

        static void ShowPathsPreview(AddressableAssetSettings settings,
            ProfileValueReference buildPath,
            ProfileValueReference loadPath,
            ref bool showPaths,
            bool showMixedValue)
        {
            EditorGUI.indentLevel++;
            showPaths = EditorGUILayout.Foldout(showPaths, k_PathsPreviewGUIContent, true);
            if (showPaths)
            {
                EditorStyles.helpBox.fontSize = 12;
                var buildPathValue = !string.IsNullOrEmpty(buildPath.Id) ? buildPath.GetValue(settings) : "";
                var loadPathValue = !string.IsNullOrEmpty(loadPath.Id) ? loadPath.GetValue(settings) : "";
                EditorGUILayout.HelpBox(String.Format("Build Path: {0}", showMixedValue ? "-" : buildPathValue), MessageType.None);
                EditorGUILayout.HelpBox(String.Format("Load Path: {0}", showMixedValue ? "-" : loadPathValue), MessageType.None);
            }

            EditorGUI.indentLevel--;
        }
    }
}
