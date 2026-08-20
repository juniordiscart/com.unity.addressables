using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets.Build.CatalogBuilders;
using UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    [CreateAssetMenu(fileName = "BuildScriptMultiCatalogPacked.asset", menuName = "Addressables/Content Builders/Multi-Catalog Build Script")]
    public class BuildScriptMultiCatalogPackedMode : BuildScriptSchemaDriven
    {
        [SerializeField]
        private List<ExternalCatalogConfig> externalCatalogs = new List<ExternalCatalogConfig>();

        public override string Name => "Multi-Catalog Build Script";

        public override ISchemaBuilder[] CreateSchemaBuilders()
        {
            return new ISchemaBuilder[]
            {
                new BundledAssetMultiCatalogSchemaBuilder(externalCatalogs)
            };
        }

        internal override CatalogPathConfig CreateCatalogPathConfig(AddressableAssetSettings aaSettings, string catalogId, string playerVersion, string runtimeCatalogFilename)
        {
            // If it's the default catalog, then return the default catalog path config.
            if (catalogId == ResourceManagerRuntimeData.kCatalogAddress)
            {
                return base.CreateCatalogPathConfig(aaSettings, catalogId, playerVersion, runtimeCatalogFilename);
            }

            // Find the external catalog belonging to the catalogId
            var catalogConfig = externalCatalogs.FirstOrDefault(c => c.CatalogId == catalogId);

            if (catalogConfig == null)
            {
                throw new ArgumentException($"External catalog with id {catalogId} not found");
            }

            var profileSettings = aaSettings.profileSettings;
            var profileId = aaSettings.activeProfileId;
            var catalogName = catalogConfig.CatalogName;
            return new CatalogPathConfig()
            {
                BuildPath = catalogConfig.BuildPath.GetValue(profileSettings, profileId),
                LoadPath = DirectoryUtility.EnsureTrailingSlash(catalogConfig.RuntimeLoadPath.GetValue(profileSettings, profileId)) + catalogName,
                RemoteBuildPath = string.Empty, // Unsupported
                RemoteLoadPath = string.Empty, // Unsupported
                RuntimeCatalogFilename = catalogName,
                VersionedCatalogFileName = aaSettings.profileSettings.EvaluateString(aaSettings.activeProfileId, $"{catalogName}_{playerVersion}"),
            };
        }

        internal override string ComputeCatalogBuildHash(string catalogId, AddressablesPlayerBuildResult addrResult, List<ContentCatalogDataEntry> catalogEntries)
        {
            if (addrResult == null)
                return null;

            var allHashes = new List<object>();

            foreach (var hashingObject in addrResult.AssetBundleBuildResults)
            {
                if (catalogEntries.Exists(l => (l.ResourceType == typeof(IAssetBundleResource)) && l.InternalId.Equals(hashingObject.FilePath)))
                {
                    allHashes.Add(hashingObject.Hash);
                }
            }

#if ENABLE_CONTENT_DIRECTORIES
            foreach (var r in addrResult.ContentDirectoryBuildResults)
                if (r.CatalogName == catalogId)
                    allHashes.Add(r.Hash);
#endif

            return HashingMethods.Calculate(allHashes.ToArray()).ToString();
        }

        internal override string GenerateRuntimeSettingsFile(AddressableAssetsBuildContext aaContext, AddressablesDataBuilderInput builderInput)
        {
            // Go over the external catalogs and check whether they want to be registered in the runtime settings.
            aaContext.runtimeData.CatalogLocations.RemoveAll(rld =>
            {
                return externalCatalogs.Exists(ec => !ec.RegisterForStartup && Array.Exists(rld.Keys, k => k == ec.CatalogId));
            });

            return base.GenerateRuntimeSettingsFile(aaContext, builderInput);
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