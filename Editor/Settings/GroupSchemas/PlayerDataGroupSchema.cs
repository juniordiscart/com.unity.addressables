using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings.GroupSchemas
{
    /// <summary>
    /// Schema for the player data asset group
    /// </summary>
    //[CreateAssetMenu(fileName = "PlayerDataGroupSchema.asset", menuName = "Addressables/Group Schemas/Player Data")]
    [DisplayName("Resources and Built In Scenes")]
    public class PlayerDataGroupSchema : AddressableAssetGroupSchema
    {
        [Tooltip("Assets in resources folders will have addresses generated during the build")]
        [FormerlySerializedAs("m_includeResourcesFolders")]
        [SerializeField]
        bool m_IncludeResourcesFolders = true;

        /// <summary>
        /// If enabled, all assets in resources folders will have addresses generated during the build.
        /// </summary>
        public bool IncludeResourcesFolders
        {
            get => m_IncludeResourcesFolders;
            set
            {
                if (m_IncludeResourcesFolders != value)
                {
                    m_IncludeResourcesFolders = value;
                    SetDirty(true);
                }
            }
        }

        [Tooltip("All scenes in the editor build settings will have addresses generated during the build")]
        [FormerlySerializedAs("m_includeBuildSettingsScenes")]
        [SerializeField]
        bool m_IncludeBuildSettingsScenes = true;

        /// <summary>
        /// If enabled, all scenes in the editor build settings will have addresses generated during the build.
        /// </summary>
        public bool IncludeBuildSettingsScenes
        {
            get => m_IncludeBuildSettingsScenes;
            set
            {
                if (m_IncludeBuildSettingsScenes != value)
                {
                    m_IncludeBuildSettingsScenes = value;
                    SetDirty(true);
                }
            }
        }

        /// <inheritdoc/>
        public override void OnGUIMultiple(List<AddressableAssetGroupSchema> otherSchemas)
        {
            SerializedProperty prop;
            string propertyName = "m_IncludeResourcesFolders";
            HashSet<SerializedObject> applyModifications = new HashSet<SerializedObject>();

            // IncludeResourcesFolders
            prop = SchemaSerializedObject.FindProperty(propertyName);
            ShowMixedValue(prop, otherSchemas, typeof(bool), propertyName);
            EditorGUI.BeginChangeCheck();
            bool newIncludeResourcesFolders = (bool)EditorGUILayout.Toggle(new GUIContent(prop.displayName, "Assets in resources folders will have addresses generated during the build"),
                IncludeResourcesFolders);
            if (EditorGUI.EndChangeCheck())
            {
                prop.boolValue = newIncludeResourcesFolders;
                applyModifications.Add(SchemaSerializedObject);
                foreach (var s in otherSchemas)
                {
                    var otherProp = s.SchemaSerializedObject.FindProperty(propertyName);
                    otherProp.boolValue = newIncludeResourcesFolders;
                    applyModifications.Add(s.SchemaSerializedObject);
                }
            }

            EditorGUI.showMixedValue = false;

            // IncludeBuildSettingsScenes
            propertyName = "m_IncludeBuildSettingsScenes";
            prop = SchemaSerializedObject.FindProperty(propertyName);
            ShowMixedValue(prop, otherSchemas, typeof(bool), propertyName);
            EditorGUI.BeginChangeCheck();
            bool newIncludeBuildSettingsScenes =
                (bool)EditorGUILayout.Toggle(new GUIContent(prop.displayName, "All scenes in the editor build settings will have addresses generated during the build"), IncludeBuildSettingsScenes);
            if (EditorGUI.EndChangeCheck())
            {
                prop.boolValue = newIncludeBuildSettingsScenes;
                applyModifications.Add(SchemaSerializedObject);
                foreach (var s in otherSchemas)
                {
                    var otherProp = s.SchemaSerializedObject.FindProperty(propertyName);
                    otherProp.boolValue = newIncludeBuildSettingsScenes;
                    applyModifications.Add(s.SchemaSerializedObject);
                }
            }

            EditorGUI.showMixedValue = false;

            foreach (SerializedObject serializedObject in applyModifications)
                serializedObject.ApplyModifiedProperties();
        }
    }
}
