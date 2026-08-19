using System.Collections.Generic;
using System.IO;
using UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    [CreateAssetMenu(fileName = "BuildScriptMultiCatalogPacked.asset", menuName = "Addressables/Content Builders/Multi-Catalog Build Script")]
    public class BuildScriptMultiCatalogPackedMode : BuildScriptSchemaDriven
    {
        [SerializeField]
        private List<ExternalCatalogConfig> externalCatalogs = new List<ExternalCatalogConfig>();

        public override string Name => "Multi-catalog Build Script";

        public override ISchemaBuilder[] CreateSchemaBuilders()
        {
            return new ISchemaBuilder[]
            {
                new BundledAssetMultiCatalogSchemaBuilder(externalCatalogs)
            };
        }

        public override void ClearCachedData()
        {
            // Clear the base cache.
            base.ClearCachedData();

            if ((externalCatalogs == null) || (externalCatalogs.Count == 0))
            {
                return;
            }

            // Cleanup the additional catalogs
            var profileSettings = AddressableAssetSettingsDefaultObject.Settings.profileSettings;
            var profileId = AddressableAssetSettingsDefaultObject.Settings.activeProfileId;

            var libraryDirectory = new DirectoryInfo("Library");
            var assetsDirectory = new DirectoryInfo("Assets");

            foreach (ExternalCatalogConfig externalCatalog in externalCatalogs)
            {
                string buildPath = externalCatalog.BuildPath.GetValue(profileSettings, profileId);
                if (string.IsNullOrEmpty(buildPath))
                {
                    buildPath = externalCatalog.BuildPath.Id;
                }

                if (!Directory.Exists(buildPath))
                {
                    continue;
                }

                // Stop if we're about to delete the whole library or assets directory.
                var buildDirectory = new DirectoryInfo(buildPath);
                if ((Path.GetRelativePath(buildDirectory.FullName, libraryDirectory.FullName) == ".") ||
                    (Path.GetRelativePath(buildDirectory.FullName, assetsDirectory.FullName) == "."))
                {
                    continue;
                }

                // Delete each file in the build directory.
                foreach (string catalogFile in Directory.GetFiles(buildPath))
                {
                    File.Delete(catalogFile);
                }

                Directory.Delete(buildPath, true);
            }
        }
    }
}