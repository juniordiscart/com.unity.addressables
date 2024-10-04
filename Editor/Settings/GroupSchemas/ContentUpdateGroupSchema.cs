using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings.GroupSchemas
{
    /// <summary>
    /// Schema for content updates.
    /// </summary>
    //  [CreateAssetMenu(fileName = "ContentUpdateGroupSchema.asset", menuName = "Addressables/Group Schemas/Content Update")]
    [DisplayName("Content Update Restriction")]
    public class ContentUpdateGroupSchema : AddressableAssetGroupSchema
    {
        [FormerlySerializedAs("m_staticContent")]
        [SerializeField]
        bool m_StaticContent;

        /// <summary>
        /// Is the group static.  This property is used in determining which assets need to be moved to a new remote group during the content update process.
        /// </summary>
        public bool StaticContent
        {
            get => m_StaticContent;
            set
            {
                if (m_StaticContent != value)
                {
                    m_StaticContent = value;
                    SetDirty(true);
                }
            }
        }

        private GUIContent m_UpdateRestrictionGUIContent = new GUIContent("Prevent Updates", "Assets in Prevent Update groups will be moved to a new remote group during the content update process");

        /// <inheritdoc/>
        public override void OnGUI()
        {
            var staticContent = EditorGUILayout.Toggle(m_UpdateRestrictionGUIContent, m_StaticContent);
            if (staticContent != m_StaticContent)
            {
                var prop = SchemaSerializedObject.FindProperty("m_StaticContent");
                prop.boolValue = staticContent;
                SchemaSerializedObject.ApplyModifiedProperties();
            }
        }

        /// <inheritdoc/>
        public override void OnGUIMultiple(List<AddressableAssetGroupSchema> otherSchemas)
        {
            string propertyName = "m_StaticContent";
            var prop = SchemaSerializedObject.FindProperty(propertyName);

            // Type/Static Content
            ShowMixedValue(prop, otherSchemas, typeof(bool), propertyName);
            EditorGUI.BeginChangeCheck();

            var staticContent = EditorGUILayout.Toggle(m_UpdateRestrictionGUIContent, m_StaticContent);

            if (EditorGUI.EndChangeCheck())
            {
                prop.boolValue = staticContent;
                SchemaSerializedObject.ApplyModifiedProperties();
                foreach (var s in otherSchemas)
                {
                    var otherProp = s.SchemaSerializedObject.FindProperty(propertyName);
                    otherProp.boolValue = staticContent;
                    s.SchemaSerializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUI.showMixedValue = false;
        }
    }
}
