using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings
{
    /// <summary>
    /// Contains data for AddressableAssetGroups.
    /// </summary>
    public class AddressableAssetGroupSchema : ScriptableObject, ICanBeEnabled
    {
        [FormerlySerializedAs("m_group")]
        [AddressableReadOnly]
        [SerializeField]
        AddressableAssetGroup m_Group;

        SerializedObject m_SchemaSerializedObject = null;

        /// <summary>
        /// The identifier used to associate this schema's group with a specific content catalog.
        /// Groups with the same CatalogId will be built into the same catalog.
        /// </summary>
        public virtual string CatalogId { get; set; } = ResourceManagerRuntimeData.kCatalogAddress;

        internal SerializedObject SchemaSerializedObject
        {
            get
            {
                if (m_SchemaSerializedObject == null)
                    m_SchemaSerializedObject = new SerializedObject(this);
                return m_SchemaSerializedObject;
            }
            set { m_SchemaSerializedObject = value; }
        }

        /// <summary>
        /// Backing field for <see cref="IsEnabled"/>. Indicates whether this schema is enabled for builds.
        /// </summary>
        [SerializeField]
        protected bool m_SchemaIsEnabled = true;

        /// <summary>
        /// Determines whether this schema is enabled and will participate in builds.
        /// </summary>
        public virtual bool IsEnabled
        {
            get => m_SchemaIsEnabled;
            set
            {
                if (m_SchemaIsEnabled != value)
                {
                    m_SchemaIsEnabled = value;
                    SetDirty(true);
                }
            }
        }

        /// <summary>
        /// Get the group that the schema belongs to.
        /// </summary>
        public AddressableAssetGroup Group
        {
            get { return m_Group; }
            internal set
            {
                m_Group = value;
                if (m_Group != null)
                {
                    OnSetGroup(m_Group);
                    Validate();
                }
            }
        }

        /// <summary>
        /// Override this method to perform post creation initialization.
        /// </summary>
        /// <param name="group">The group that the schema is added to.</param>
        protected virtual void OnSetGroup(AddressableAssetGroup group)
        {
        }

        internal virtual void Validate()
        {
        }

        /// <summary>
        /// Determines whether a given schema can be enabled or not.
        /// </summary>
        /// <returns>Returns an empty string if enabling the schema is valid.
        /// If enabling the schema is not valid, it will instead return an error/warning string.</returns>
        public virtual string CanEnableSchema()
        {
            return "";
        }

        /// <summary>
        /// Used to display the GUI of the schema.
        /// </summary>
        public virtual void OnGUI()
        {
            var type = GetType();
            var fieldMap = new Dictionary<string, FieldInfo>();
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            for (var t = type; t != null; t = t.BaseType)
            {
                foreach (var field in t.GetFields(flags))
                {
                    if (!fieldMap.ContainsKey(field.Name))
                        fieldMap.Add(field.Name, field);
                }
            }

            var p = SchemaSerializedObject.GetIterator();
            p.Next(true);
            while (p.Next(false))
            {
                if (fieldMap.ContainsKey(p.name))
                    EditorGUILayout.PropertyField(p, true);
            }

            SchemaSerializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Used to display the GUI of multiple selected groups.
        /// </summary>
        /// <param name="otherSchemas">Schema instances in the other selected groups</param>
        public virtual void OnGUIMultiple(List<AddressableAssetGroupSchema> otherSchemas)
        {
        }

        /// <summary>
        /// Used to notify the addressables settings that data has been modified.  This must be called by subclasses to ensure proper cache invalidation.
        /// </summary>
        /// <param name="postEvent">Determines if this method call will post an event to the internal addressables event system</param>
        protected internal void SetDirty(bool postEvent)
        {
            m_SchemaSerializedObject = null;
            if (m_Group != null)
            {
                if (m_Group.Settings != null && m_Group.Settings.IsPersisted)
                {
                    EditorUtility.SetDirty(this);
                    AddressableAssetUtility.OpenAssetIfUsingVCIntegration(this);
                }

                if (m_Group != null)
                    m_Group.SetDirty(AddressableAssetSettings.ModificationEvent.GroupSchemaModified, this, postEvent, false);
            }
        }

        /// <summary>
        /// Used for drawing properties in the inspector.
        /// </summary>
        public virtual void ShowAllProperties()
        {
        }

        /// <summary>
        /// Display mixed values for the specified property found in a list of schemas.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="otherSchemas">The list of schemas that may contain the property.</param>
        /// <param name="type">The property type.</param>
        /// <param name="propertyName">The property name.</param>
        protected internal void ShowMixedValue(SerializedProperty property, List<AddressableAssetGroupSchema> otherSchemas, Type type, string propertyName)
        {
            ShowMixedValue<AddressableAssetGroupSchema>(property, otherSchemas, type, propertyName);
        }

        internal void ShowMixedValue<TSchema>(SerializedProperty property, List<TSchema> otherSchemas, Type type, string propertyName)
            where TSchema : AddressableAssetGroupSchema
        {
            foreach (var schema in otherSchemas)
            {
                var s_prop = schema.SchemaSerializedObject.FindProperty(propertyName);
                if ((property.propertyType == SerializedPropertyType.Enum && (property.enumValueIndex != s_prop.enumValueIndex)) ||
                    (property.propertyType == SerializedPropertyType.String && (property.stringValue != s_prop.stringValue)) ||
                    (property.propertyType == SerializedPropertyType.Integer && (property.intValue != s_prop.intValue)) ||
                    (property.propertyType == SerializedPropertyType.Boolean && (property.boolValue != s_prop.boolValue)))
                {
                    EditorGUI.showMixedValue = true;
                    return;
                }

                if (type == typeof(ProfileValueReference))
                {
                    var targetObj = property.serializedObject.targetObject;
                    var otherObj = s_prop.serializedObject.targetObject;
                    FieldInfo field = GetField(targetObj, property.name);
                    FieldInfo otherField = GetField(otherObj, property.name);

                    string lhsId = (field?.GetValue(targetObj) as ProfileValueReference)?.Id;
                    string rhsId = (otherField?.GetValue(otherObj) as ProfileValueReference)?.Id;

                    if (lhsId != null && rhsId != null && lhsId != rhsId)
                    {
                        EditorGUI.showMixedValue = true;
                        return;
                    }
                }

                if (type == typeof(SerializedType))
                {
                    var field = property.serializedObject.targetObject.GetType().GetField(property.name,
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance |
                        BindingFlags.DeclaredOnly);

                    Type lhs = ((SerializedType)field?.GetValue(property.serializedObject.targetObject)).Value;
                    Type rhs = ((SerializedType)field?.GetValue(s_prop.serializedObject.targetObject)).Value;

                    if (lhs != null && rhs != null && lhs != rhs)
                    {
                        EditorGUI.showMixedValue = true;
                        return;
                    }
                }
            }
        }
        internal FieldInfo GetField(UnityEngine.Object obj, string propertyName)
        {
            return obj.GetType().GetField(propertyName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance |
                BindingFlags.DeclaredOnly);
        }

        /// <summary>
        /// Compare two AddressableAssetGroupSchemas to see if they're the same.
        /// </summary>
        /// <param name="x">Left hand side</param>
        /// <param name="y">Right hand side</param>
        /// <returns>0 if typre equal, 1 or -1 otherwise.</returns>
        public static int Compare(AddressableAssetGroupSchema x, AddressableAssetGroupSchema y)
        {
            if (x == null && y == null)
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;
            // you can only have one schema of a given type in a set so using the name should be ok.
            // Use direct name property instead of SchemaSerializedObject.targetObject.name to avoid
            // creating GC handles that can become invalid during domain reload
            return string.CompareOrdinal(x.name, y.name);
        }

        internal void SetPathVariable(AddressableAssetSettings addressableAssetSettings, ref ProfileValueReference path, string newPathName, string oldPathName, List<string> variableNames)
        {
            if (path == null || !path.HasValue(addressableAssetSettings))
            {
                bool hasNewPath = variableNames.Contains(newPathName);
                bool hasOldPath = variableNames.Contains(oldPathName);

                if (hasNewPath && string.IsNullOrEmpty(path?.Id))
                {
                    path = new ProfileValueReference();
                    path.SetVariableByName(addressableAssetSettings, newPathName);
                    SetDirty(true);
                }
                else if (hasOldPath && string.IsNullOrEmpty(path?.Id))
                {
                    path = new ProfileValueReference();
                    path.SetVariableByName(addressableAssetSettings, oldPathName);
                    SetDirty(true);
                }
                else if (!hasOldPath && !hasNewPath)
                    Debug.LogWarning("Default path variable " + newPathName + " not found when initializing BundledAssetGroupSchema. Please manually set the path via the groups window.");
            }
        }
    }
}
