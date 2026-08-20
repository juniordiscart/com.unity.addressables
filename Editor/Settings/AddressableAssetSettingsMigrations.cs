using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace UnityEditor.AddressableAssets.Settings
{
    // Contains one-time migration steps for AddressableAssetSettings
    // and its group schemas, run once per settings asset as the format evolves.
    // Note: no XML doc comment here; the class <summary> lives on the partial
    // declaration in AddressableAssetSettings.cs, and duplicating it fails doc validation.
    public partial class AddressableAssetSettings
    {
        [InitializeOnLoadMethod]
        static void CheckForUpgrades()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return;

            RunMigrationSteps(settings);

#if ENABLE_CONTENT_DIRECTORIES
            {
                if (!settings.ContentDirectoryGroupTemplateCreated)
                {
                    CreateContentDirectoryGroupTemplate(settings);
                    settings.EnsureBuildScriptAdded<BuildScriptSchemaDriven>();
                }

                // Runs before Validate() repairs the builder list, so guard against a null/missing active builder.
                var activePlayerDataBuilder = settings.ActivePlayerDataBuilder;
                bool defaultBuildScriptIsPackedMode = activePlayerDataBuilder != null && activePlayerDataBuilder.GetType() == typeof(BuildScriptPackedMode);
                if (defaultBuildScriptIsPackedMode)
                {
                    int schemaDrivenBuildScriptIndex = settings.DataBuilders.FindIndex(x => x != null && x.GetType() == typeof(BuildScriptSchemaDriven));
                    if(schemaDrivenBuildScriptIndex != -1)
                        settings.ActivePlayerDataBuilderIndex = schemaDrivenBuildScriptIndex;
                }

                CombineContentDirectoryCatalog(settings);
            }
#endif
        }

        internal static void RunMigrationSteps(AddressableAssetSettings settings)
        {
            if (!settings.CatalogFormatMigrated)
            {
#if ENABLE_JSON_CATALOG
                settings.m_EnableJsonCatalog = true;
#endif
                // Migrate the legacy bool to the new provider-type field so the
                // dropdown reflects the previously configured format.
                settings.CatalogProviderType = settings.m_EnableJsonCatalog
                    ? typeof(JsonCatalogProvider) : typeof(BinaryCatalogProvider);
                settings.CatalogFormatMigrated = true;
            }

            MigrateStaleCrcCachedFlags(settings);
        }

        internal static void MigrateStaleCrcCachedFlags(AddressableAssetSettings settings)
        {
            // One-time repair for UUM-140558: older versions could leave the
            // cached-bundle CRC flag stuck on after disabling CRC entirely.
            if (settings.CrcCachedBundleFlagMigrated)
                return;

            foreach (var group in settings.groups)
            {
                if (group == null)
                    continue;
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema != null && MigrateStaleCrcCachedFlag(schema))
                    EditorUtility.SetDirty(schema);
            }

            foreach (var template in settings.GroupTemplateObjects)
            {
                if (!(template is AddressableAssetGroupTemplate groupTemplate))
                    continue;
                foreach (var schema in groupTemplate.SchemaObjects)
                {
                    if (schema is BundledAssetGroupSchema bundledSchema && MigrateStaleCrcCachedFlag(bundledSchema))
                        EditorUtility.SetDirty(schema);
                }
            }

            settings.CrcCachedBundleFlagMigrated = true;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// One-time repair for UUM-140558: older versions left the cached-CRC flag
        /// set when CRC was disabled. Clears the stale flag if found.
        /// </summary>
        /// <returns>True if the flag was stale and got cleared.</returns>
        internal static bool MigrateStaleCrcCachedFlag(BundledAssetGroupSchema schema)
        {
            if (!schema.m_UseAssetBundleCrc && schema.m_UseAssetBundleCrcForCachedBundles)
            {
                schema.m_UseAssetBundleCrcForCachedBundles = false;
                return true;
            }
            return false;
        }

        internal static void CombineContentDirectoryCatalog(AddressableAssetSettings settings)
        {
            // Migrate any ContentDirectoryGroupSchema whose CatalogId is still the
            // old default "ContentDirectoryCatalog" to the shared main catalog id.
            if (!settings.ContentDirectoryCatalogNameMigrated)
            {
                foreach (var group in settings.groups)
                {
                    if (group == null) continue;
                    var schema = group.GetSchema<ContentDirectoryGroupSchema>();
                    if (schema != null && schema.CatalogId == "ContentDirectoryCatalog")
                    {
                        schema.CatalogId = ResourceManagerRuntimeData.kCatalogAddress;
                        EditorUtility.SetDirty(schema);
                    }
                }

                foreach (var template in settings.GroupTemplateObjects)
                {
                    if (template == null)
                        continue;
                    if (!(template is AddressableAssetGroupTemplate groupTemplate))
                        continue;
                    foreach (var schema in groupTemplate.SchemaObjects)
                    {
                        if (schema != null && schema.CatalogId == "ContentDirectoryCatalog")
                        {
                            schema.CatalogId = ResourceManagerRuntimeData.kCatalogAddress;
                            EditorUtility.SetDirty(schema);
                        }
                    }

                }
                settings.ContentDirectoryCatalogNameMigrated = true;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }

        [SerializeField]
        bool m_ContentDirectoryCatalogNameMigrated = false;
        internal bool ContentDirectoryCatalogNameMigrated
        {
            get { return m_ContentDirectoryCatalogNameMigrated; }
            set
            {
                m_ContentDirectoryCatalogNameMigrated = value;
                EditorUtility.SetDirty(this);
            }
        }

        [SerializeField]
        internal bool m_CatalogFormatMigrated = false;
        internal bool CatalogFormatMigrated
        {
            get { return m_CatalogFormatMigrated; }
            set
            {
                m_CatalogFormatMigrated = value;
                EditorUtility.SetDirty(this);
            }
        }

        [SerializeField]
        bool m_CrcCachedBundleFlagMigrated = false;
        /// <summary>
        /// Tracks whether <see cref="AddressableAssetSettings.MigrateStaleCrcCachedFlags"/>
        /// has already run for UUM-140558's cached-CRC-flag repair.
        /// </summary>
        internal bool CrcCachedBundleFlagMigrated
        {
            get { return m_CrcCachedBundleFlagMigrated; }
            set
            {
                m_CrcCachedBundleFlagMigrated = value;
                EditorUtility.SetDirty(this);
            }
        }
    }
}
