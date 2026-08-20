using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Serialization;
using static UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentDirectoryGroupSchema;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEditor.AddressableAssets.GUI;

namespace UnityEditor.AddressableAssets.Settings.GroupSchemas
{
    /// <summary>
    /// Schema for configuring groups to be built as Content Directories.
    /// Content Directories provide an alternative to AssetBundles for organizing and loading addressable content.
    /// </summary>
    [DisplayName("Content Directory")]
    [AddressablesHelpURL("GroupSchemas.html")]
    public class ContentDirectoryGroupSchema : AddressableAssetGroupSchema,
        ISerializationCallbackReceiver,
        IBuildableSchema,
        ICanIncludeFolderKeys,
        ICanIncludeLabels
    {
        // Retained (serialized) only so a project's previously-stored per-schema value can be migrated up to the
        // group on load. The IncludeInBuild property below no longer reads this field; it forwards to the group.
        [SerializeField]
        bool m_IncludeInBuild = true;

        internal override bool? GetDeprecatedIncludeInBuild() => m_IncludeInBuild;

        /// <summary>
        /// If true, the group's content will be included in the Addressables build.
        /// </summary>
        /// <remarks>
        /// Include in Build is stored on the owning <see cref="AddressableAssetGroup"/>. This property forwards to
        /// <see cref="AddressableAssetGroup.IncludeInBuild"/> so the group remains the single source of truth.
        /// </remarks>
        public bool IncludeInBuild
        {
            get => Group == null || Group.IncludeInBuild;
            set
            {
                if (Group != null)
                    Group.IncludeInBuild = value;
            }
        }

        [SerializeField]
        string m_CatalogName = ResourceManagerRuntimeData.kCatalogAddress;

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
        [Tooltip("The provider type to use for loading entries from group root assets.")]
        SerializedType m_GroupAssetEntryProviderType;

        /// <summary>
        /// The provider type to use for loading entries from group root assets.
        /// </summary>
        public SerializedType GroupAssetEntryProviderType
        {
            get => m_GroupAssetEntryProviderType;
            set
            {
                m_GroupAssetEntryProviderType = value;
                SetDirty(true);
            }
        }

        [SerializeField]
        bool m_IncludeLabelsInCatalog = true;

        [SerializeField]
        bool m_IncludeFolderKeysInCatalog = true;

        [SerializeField]
        bool m_IncludeAddressesForFolderChildren = true;

        /// <summary>
        /// Gets or sets whether labels are included in the content catalog for this content
        /// directory group. This is required if labels are used at runtime to load assets.
        /// </summary>
        public bool IncludeLabelsInCatalog
        {
            get => m_IncludeLabelsInCatalog;
            set
            {
                if (m_IncludeLabelsInCatalog != value)
                {
                    m_IncludeLabelsInCatalog = value;
                    SetDirty(true);
                }
            }
        }

        /// <summary>
        /// Gets or sets whether each addressable folder's own address is included as an extra
        /// shared key on every asset within that folder. This allows loading every asset in an
        /// addressable folder with a single call, for example
        /// Addressables.LoadAssetsAsync(folderAddress, ...), similar to Resources.LoadAll.
        /// </summary>
        public bool IncludeFolderKeysInCatalog
        {
            get => m_IncludeFolderKeysInCatalog;
            set
            {
                if (m_IncludeFolderKeysInCatalog != value)
                {
                    m_IncludeFolderKeysInCatalog = value;
                    SetDirty(true);
                }
            }
        }

        /// <summary>
        /// Gets or sets whether assets inside an addressable folder keep their own individual
        /// address in the catalog, in addition to the folder's shared key (see
        /// IncludeFolderKeysInCatalog). GUIDs are unaffected. Only takes effect when
        /// IncludeFolderKeysInCatalog is enabled. Disable to reduce catalog size when assets
        /// are always loaded via the folder key.
        /// </summary>
        public bool IncludeAddressesForFolderChildren
        {
            get => m_IncludeAddressesForFolderChildren;
            set
            {
                if (m_IncludeAddressesForFolderChildren != value)
                {
                    m_IncludeAddressesForFolderChildren = value;
                    SetDirty(true);
                }
            }
        }

        /// <summary>
        /// Determines whether a given schema will be included in a Schema Driven build. This is particularly useful
        /// if you want to alternate between building AssetBundles and ContentDirectories.
        /// Only one buildable schema can be enabled on a group at a time. If you attempt to enable multiple at once, an error will be thrown.
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
            if (m_GroupAssetEntryProviderType.Value == null)
                m_GroupAssetEntryProviderType.Value = typeof(NativeContentAssetEntryProvider);
#endif
        }

        static readonly GUIContent k_IncludeLabelsInCatalogContent = new GUIContent("Include Labels in Catalog",
            "If disabled, labels from this group will not be included in the catalog.  This is useful for reducing the size of the catalog if labels are not needed.");

        static readonly GUIContent k_IncludeFolderKeysInCatalogContent = new GUIContent("Include Folder Keys in Catalog",
            "If enabled, each addressable folder's address is included as a shared key on every asset in that folder, so the folder's address can be used to load every asset inside it in one call.  If disabled, this is useful for reducing the size of the catalog if whole-folder loading is not needed.");

        static readonly GUIContent k_IncludeAddressesForFolderChildrenContent = new GUIContent("Include Individual Addresses for Folder Assets",
            "If disabled, assets inside an addressable folder will not have their own individual address included in the catalog -- only the folder's shared key will be included.  GUIDs are unaffected.  Disable this if you always load these assets via the folder to reduce the size of the catalog.");

        // Overriding the OnGUI here prevents the Include in Build setting from being shown twice
        // Currently the GUI for it is created in AssetInspectorGUI.DrawIncludeInBuildToggle
        /// <inheritdoc/>
        public override void OnGUI()
        {
            EditorGUI.BeginDisabledGroup(!IsEnabled);
            BuildAndLoadPathUIHelper.DrawPathPair(this, SchemaSerializedObject,
                ref m_BuildPath, ref m_LoadPath, ref m_UseCustomPaths, ref m_ShowPaths,
                ref m_SelectedPathPairIndex);
            EditorGUILayout.PropertyField(SchemaSerializedObject.FindProperty(nameof(m_IncludeLabelsInCatalog)), k_IncludeLabelsInCatalogContent, true);
            EditorGUILayout.PropertyField(SchemaSerializedObject.FindProperty(nameof(m_IncludeFolderKeysInCatalog)), k_IncludeFolderKeysInCatalogContent, true);
            if (m_IncludeFolderKeysInCatalog)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(SchemaSerializedObject.FindProperty(nameof(m_IncludeAddressesForFolderChildren)), k_IncludeAddressesForFolderChildrenContent, true);
                EditorGUI.indentLevel--;
            }
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

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(SchemaSerializedObject.FindProperty(nameof(m_IncludeLabelsInCatalog)), k_IncludeLabelsInCatalogContent, true);
                EditorGUILayout.PropertyField(SchemaSerializedObject.FindProperty(nameof(m_IncludeFolderKeysInCatalog)), k_IncludeFolderKeysInCatalogContent, true);
                if (m_IncludeFolderKeysInCatalog)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(SchemaSerializedObject.FindProperty(nameof(m_IncludeAddressesForFolderChildren)), k_IncludeAddressesForFolderChildrenContent, true);
                    EditorGUI.indentLevel--;
                }
                bool catalogTogglesModified = EditorGUI.EndChangeCheck();

                // Apply pending SerializedProperty edits to this schema's own fields before they
                // are read (by SetCatalogToggleOptions) to propagate to the other selected schemas.
                if (catalogTogglesModified)
                    SchemaSerializedObject.ApplyModifiedProperties();

                if (pathPairModified || catalogTogglesModified)
                {
                    Undo.SetCurrentGroupName("ContentDirectoryGroupSchemas BuildAndLoad Undos");
                    foreach (var schema in otherContentDirectorySchemas)
                    {
                        Undo.RecordObject(schema, "ContentDirectoryGroupSchema BuildAndLoad" + schema.name);
                        if (pathPairModified)
                            SetPathPairOption(this, schema);
                        if (catalogTogglesModified)
                            SetCatalogToggleOptions(this, schema);
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


        void SetCatalogToggleOptions(ContentDirectoryGroupSchema src, ContentDirectoryGroupSchema dst)
        {
            if (dst.m_IncludeLabelsInCatalog != src.m_IncludeLabelsInCatalog)
                dst.m_IncludeLabelsInCatalog = src.m_IncludeLabelsInCatalog;

            if (dst.m_IncludeFolderKeysInCatalog != src.m_IncludeFolderKeysInCatalog)
                dst.m_IncludeFolderKeysInCatalog = src.m_IncludeFolderKeysInCatalog;

            if (dst.m_IncludeAddressesForFolderChildren != src.m_IncludeAddressesForFolderChildren)
                dst.m_IncludeAddressesForFolderChildren = src.m_IncludeAddressesForFolderChildren;

            dst.SetDirty(true);
        }

        internal int DetermineSelectedIndex(List<ProfileGroupType> groupTypes, int defaultValue, AddressableAssetSettings addressableAssetSettings, HashSet<string> vars)
        {
            return BuildAndLoadPathUIHelper.DetermineSelectedIndex(BuildPath, LoadPath, m_UseCustomPaths, groupTypes, defaultValue, addressableAssetSettings, vars);
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
            if (m_GroupAssetEntryProviderType.Value == null)
                m_GroupAssetEntryProviderType.Value = typeof(NativeContentAssetEntryProvider);
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
