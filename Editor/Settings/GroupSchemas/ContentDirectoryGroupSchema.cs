using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Serialization;
using static UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentDirectoryGroupSchema;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEditor.AddressableAssets.GUI;

namespace UnityEditor.AddressableAssets.Settings.GroupSchemas
{
    /// <summary>
    /// Schema for configuring groups to be built as Content Directories.
    /// Content Directories provide an alternative to AssetBundles for organizing and loading addressable content.
    /// </summary>
    [DisplayName("Content Directory")]
    public class ContentDirectoryGroupSchema : AddressableAssetGroupSchema, ISerializationCallbackReceiver, ICanIncludeInBuild
    {
        [SerializeField]
        bool m_IncludeInBuild = true;

        [SerializeField]
        string m_CatalogName = "ContentDirectoryCatalog";

        /// <summary>
        /// Gets or sets the catalog identifier for this content directory group.
        /// Groups with the same CatalogId will be built into the same catalog.
        /// </summary>
        public override string CatalogId { get => m_CatalogName; set => m_CatalogName = value; }

        private bool m_ShowPaths = true;

        /// <summary>
        /// Used to determine if dropdown should be custom
        /// </summary>
        internal bool m_UseCustomPaths = false;

        /// <summary>
        /// Internal settings
        /// </summary>
        internal AddressableAssetSettings settings
        {
            get { return AddressableAssetSettingsDefaultObject.Settings; }
        }

        [SerializeField]
        int m_SelectedPathPairIndex;
        /// <summary>
        /// The selected path pair in use.
        /// Use this with care, as it could change when path pairs are added ore removed. It is generally more
        /// valid to lookup the path pair by Id for the current profile.
        /// </summary>
        public int SelectedPathPairIndex
        {
            get => m_SelectedPathPairIndex;
            set
            {
                if (m_SelectedPathPairIndex != value)
                {
                    m_SelectedPathPairIndex = value;
                    if (m_SelectedPathPairIndex < 0)
                        m_SelectedPathPairIndex = 0;
                    SetDirty(true);
                }
            }
        }

        [SerializeField]
        [Tooltip("The path to copy asset bundles to.")]
        internal ProfileValueReference m_BuildPath = new ProfileValueReference();

        /// <summary>
        /// The path to copy the built content directory to.
        /// </summary>
        public ProfileValueReference BuildPath
        {
            get { return m_BuildPath; }
        }

        [SerializeField]
        [Tooltip("The path to load bundles from.")]
        internal ProfileValueReference m_LoadPath = new ProfileValueReference();

        /// <summary>
        /// The path to load the content directory from at runtime.
        /// </summary>
        public ProfileValueReference LoadPath
        {
            get { return m_LoadPath; }
        }

        [SerializeField]
        [SerializedTypeRestriction(type = typeof(IResourceProvider))]
        [Tooltip("The provider type to use for loading content directories.")]
        SerializedType m_ContentDirectoryProviderType;

        /// <summary>
        /// The provider type to use for loading content directories.
        /// </summary>
        public SerializedType ContentDirectoryProviderType
        {
            get => m_ContentDirectoryProviderType;
            set
            {
                m_ContentDirectoryProviderType = value;
                SetDirty(true);
            }
        }

        [SerializeField]
        [SerializedTypeRestriction(type = typeof(IResourceProvider))]
        [Tooltip("The provider type to use for loading group root assets from content directories.")]
        SerializedType m_GroupRootAssetProviderType;

        /// <summary>
        /// The provider type to use for loading group root assets from content directories.
        /// </summary>
        public SerializedType GroupRootAssetProviderType
        {
            get => m_GroupRootAssetProviderType;
            set
            {
                m_GroupRootAssetProviderType = value;
                SetDirty(true);
            }
        }

        [SerializeField]
        [SerializedTypeRestriction(type = typeof(IResourceProvider))]
        [Tooltip("The provider type to use for loading entries from group root assets.")]
        SerializedType m_GroupRootAssetEntryProviderType;

        /// <summary>
        /// The provider type to use for loading entries from group root assets.
        /// </summary>
        public SerializedType GroupRootAssetEntryProviderType
        {
            get => m_GroupRootAssetEntryProviderType;
            set
            {
                m_GroupRootAssetEntryProviderType = value;
                SetDirty(true);
            }
        }

        /// <summary>
        /// Gets or sets whether this group should be included in the Addressables build.
        /// When false, the group's content will not be built or included in the catalog.
        /// </summary>
        public bool IncludeInBuild
        {
            get => m_IncludeInBuild;
            set
            {
                if (m_IncludeInBuild != value)
                {
                    m_IncludeInBuild = value;
                    SetDirty(true);
                }
            }
        }

        /// <summary>
        /// Determines whether a given schema will be included in a Schema Driven build. This is particularly useful
        /// if you want to alternate between building AssetBundles and ContentDirectories.
        /// Only one ICanIncludeInBuildSchema can be enabled on a group at a time. If you attempt to enable multiple at once, an error will be thrown.
        /// </summary>
        public override bool IsEnabled
        {
            get => m_SchemaIsEnabled;
            set
            {
                if (m_SchemaIsEnabled != value)
                {
                    if (value)
                    {
                        string warningString = CanEnableSchema();
                        if (!string.IsNullOrEmpty(warningString))
                            Debug.LogError(warningString);
                        // Allow the set even when another buildable schema is enabled so the user can enable both;
                        // the group inspector shows an error and logs when both are enabled.
                    }

                    m_SchemaIsEnabled = value;
                    SetDirty(true);
                }
            }
        }

        /// <summary>
        /// Determines whether the ContentDirectorySchema can be enabled or not.
        /// A ContentDirectorySchema can be enabled if there are no other buildable schemas (such as a Content Packing &amp; Loading Schema) enabled.
        /// Used e.g. when adding a schema via Add Schema so the new schema is defaulted to disabled when the other is already enabled.
        /// The user can still manually enable both in the inspector; the group inspector then shows an error.
        /// </summary>
        /// <returns>Returns an empty string if enabling is valid, or an error/warning string if another buildable schema is already enabled.</returns>
        public override string CanEnableSchema()
        {
            foreach (var schema in Group.Schemas)
            {
                if (schema != this && schema is BundledAssetGroupSchema bags && bags.IsEnabled)
                    return AddressablesGUIUtility.CanEnableSchemaError(Group.Name, this.GetType(), schema.GetType());
            }
            return "";
        }

        internal override void Validate()
        {
            if (Group != null && Group.Settings != null)
            {
                List<string> variableNames = Group.Settings.profileSettings.GetVariableNames();
                SetPathVariable(Group.Settings, ref m_BuildPath, AddressableAssetSettings.kLocalBuildPath, "LocalBuildPath", variableNames);
                SetPathVariable(Group.Settings, ref m_LoadPath, AddressableAssetSettings.kLocalLoadPath, "LocalLoadPath", variableNames);
            }

#if ENABLE_CONTENT_DIRECTORIES
            if (m_ContentDirectoryProviderType.Value == null)
                m_ContentDirectoryProviderType.Value = typeof(ContentDirectoryProvider);
            if (m_GroupRootAssetProviderType.Value == null)
                m_GroupRootAssetProviderType.Value = typeof(GroupRootAssetProvider);
            if (m_GroupRootAssetEntryProviderType.Value == null)
                m_GroupRootAssetEntryProviderType.Value = typeof(GroupRootAssetEntryProvider);
#endif
        }

        // Overriding the OnGUI here prevents the Include in Build setting from being shown twice
        // Currently the GUI for it is created in AssetInspectorGUI.DrawIncludeInBuildToggle
        /// <inheritdoc/>
        public override void OnGUI()
        {
            EditorGUI.BeginDisabledGroup(!IsEnabled);
            BuildAndLoadPathUIHelper.DrawPathPair(this, SchemaSerializedObject,
                ref m_BuildPath, ref m_LoadPath, ref m_UseCustomPaths, ref m_ShowPaths,
                ref m_SelectedPathPairIndex);
            SchemaSerializedObject.ApplyModifiedProperties();
            EditorGUI.EndDisabledGroup();
        }

        /// <inheritdoc/>
        public override void OnGUIMultiple(List<AddressableAssetGroupSchema> otherSchemas)
        {
            using (new EditorGUI.DisabledScope(!IsEnabled))
            {
                List<ContentDirectoryGroupSchema> otherContentDirectorySchemas = new List<ContentDirectoryGroupSchema>();
                foreach (var otherSchema in otherSchemas)
                {
                    if (otherSchema is ContentDirectoryGroupSchema otherContentDirectorySchema)
                        otherContentDirectorySchemas.Add(otherContentDirectorySchema);
                }

                bool pathPairModified = BuildAndLoadPathUIHelper.DrawPathPairMulti(this, SchemaSerializedObject, otherSchemas,
                    ref m_BuildPath, ref m_LoadPath, ref m_UseCustomPaths, ref m_ShowPaths,
                    ref m_SelectedPathPairIndex);

                if (pathPairModified)
                {
                    Undo.SetCurrentGroupName("ContentDirectoryGroupSchemas BuildAndLoad Undos");
                    foreach (var schema in otherContentDirectorySchemas)
                    {
                        Undo.RecordObject(schema, "ContentDirectoryGroupSchema BuildAndLoad" + schema.name);
                        SetPathPairOption(this, schema);
                    }
                    SchemaSerializedObject.ApplyModifiedProperties();
                    Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                }
            }
        }

        void SetPathPairOption(ContentDirectoryGroupSchema src, ContentDirectoryGroupSchema dst)
        {
            if (dst.m_BuildPath.Id != src.BuildPath.Id)
                dst.m_BuildPath.Id = src.BuildPath.Id;

            if (dst.m_LoadPath.Id != src.m_LoadPath.Id)
                dst.m_LoadPath.Id = src.m_LoadPath.Id;

            if (dst.m_UseCustomPaths != src.m_UseCustomPaths)
                dst.m_UseCustomPaths = src.m_UseCustomPaths;

            if (dst.SelectedPathPairIndex != src.SelectedPathPairIndex)
                dst.SelectedPathPairIndex = src.SelectedPathPairIndex;

            dst.SetDirty(true);
        }

        /// <summary>
        /// Implementation of ISerializationCallbackReceiver. Used to set callbacks for ProfileValueReference changes and default provider types.
        /// </summary>
        public void OnAfterDeserialize()
        {
            BuildPath.OnValueChanged -= OnPathValueChanged;
            BuildPath.OnValueChanged += OnPathValueChanged;
            LoadPath.OnValueChanged -= OnPathValueChanged;
            LoadPath.OnValueChanged += OnPathValueChanged;
#if ENABLE_CONTENT_DIRECTORIES
            if (m_ContentDirectoryProviderType.Value == null)
                m_ContentDirectoryProviderType.Value = typeof(ContentDirectoryProvider);
            if (m_GroupRootAssetProviderType.Value == null)
                m_GroupRootAssetProviderType.Value = typeof(GroupRootAssetProvider);
            if (m_GroupRootAssetEntryProviderType.Value == null)
                m_GroupRootAssetEntryProviderType.Value = typeof(GroupRootAssetEntryProvider);
#endif
        }

        void OnPathValueChanged(ProfileValueReference _)
        {
            SetDirty(true);
        }


        /// <summary>
        /// Implementation of ISerializationCallbackReceiver. Does nothing.
        /// </summary>
        public void OnBeforeSerialize()
        {
        }
    }
}
