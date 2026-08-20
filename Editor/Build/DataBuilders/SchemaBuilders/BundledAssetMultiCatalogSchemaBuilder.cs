using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders
{
    public class BundledAssetMultiCatalogSchemaBuilder : BundledAssetSchemaBuilder
    {
        private List<ExternalCatalogConfig> m_ExternalCatalogConfigs;

        public BundledAssetMultiCatalogSchemaBuilder(IEnumerable<ExternalCatalogConfig> externalCatalogConfigs)
        {
            m_ExternalCatalogConfigs = new List<ExternalCatalogConfig>(externalCatalogConfigs);
        }

        public override string Name => "Bundled Assets (Multi-Catalog)";

        public override Dictionary<string, List<ContentCatalogDataEntry>> GenerateCatalogLocations(AddressableAssetsBuildContext aaContext, AddressablesPlayerBuildResult addrResult)
        {
            var catalogEntryMapping = new Dictionary<string, List<ContentCatalogDataEntry>>()
            {
                { ResourceManagerRuntimeData.kCatalogAddress, new List<ContentCatalogDataEntry>() }
            };

            foreach (var externalCatalogConfig in m_ExternalCatalogConfigs)
            {
                catalogEntryMapping.Add(externalCatalogConfig.CatalogId, new List<ContentCatalogDataEntry>());
            }

            var defaultCatalogLocations = catalogEntryMapping[ResourceManagerRuntimeData.kCatalogAddress];

            // Go over all locations and assign them to the appropriate external catalog if any was found.
            foreach (var loc in aaContext.locations)
            {
                var preferredCatalog = m_ExternalCatalogConfigs.FirstOrDefault(cs => cs.IsPartOfCatalog(loc, aaContext));

                // If no preferred catalog is found, assign the asset to the default catalog and skip further processing.
                if (preferredCatalog == null)
                {
                    defaultCatalogLocations.Add(loc);
                    continue;
                }

                var externalCatalogEntries = catalogEntryMapping[preferredCatalog.CatalogId];

                if (loc.ResourceType != typeof(IAssetBundleResource))
                {
                    externalCatalogEntries.Add(loc);
                    continue;
                }

                var bundleId = ((AssetBundleRequestOptions)loc.Data).BundleName + ".bundle";
                var group = aaContext.Settings.FindGroup(g => (g != null) && (g.Guid == aaContext.bundleToAssetGroup[bundleId]));

                if (group == null)
                {
                    Debug.LogErrorFormat($"Could not find the group that belongs to location {loc.InternalId}.");
                    continue;
                }

                // Generate a new load path based on the settings of the external catalog or the schema's custom defined values.
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                var filename = GenerateLocationListsTask.GetFileName(loc.InternalId, BuildTarget);
                var runtimeLoadPath = GenerateLocationListsTask.GetLoadPath(group, schema.LoadPath, filename, BuildTarget);

                externalCatalogEntries.Add(new ContentCatalogDataEntry(typeof(IAssetBundleResource), runtimeLoadPath, loc.Provider, loc.Keys, loc.Dependencies, loc.Data));
            }

            // Generate the dependencies for each external catalog.
            foreach (var externalCatalogSetup in m_ExternalCatalogConfigs)
            {
                var externalCatalogLocations = catalogEntryMapping[externalCatalogSetup.CatalogId];
                var locationQueue = new Queue<ContentCatalogDataEntry>(externalCatalogLocations);
                var processedLocations = new HashSet<ContentCatalogDataEntry>();

                while (locationQueue.Count > 0)
                {
                    ContentCatalogDataEntry location = locationQueue.Dequeue();

                    // If the location has already been processed or doesn't have any dependencies, then skip it.
                    if (!processedLocations.Add(location) || (location.Dependencies == null) || (location.Dependencies.Count == 0))
                    {
                        continue;
                    }

                    foreach (var entryDependency in location.Dependencies)
                    {
                        // Search for the dependencies in the default catalog only.
                        var depLocation = defaultCatalogLocations.Find(loc => loc.Keys[0] == entryDependency);

                        if (depLocation != null)
                        {
                            locationQueue.Enqueue(depLocation);

                            // If the dependency wasn't part of the catalog yet, add it.
                            if (!externalCatalogLocations.Contains(depLocation))
                            {
                                externalCatalogLocations.Add(depLocation);
                            }
                        }
                        else if (!externalCatalogLocations.Exists(loc => loc.Keys[0] == entryDependency))
                        {
                            Debug.LogErrorFormat($"Could not find location for dependency ID {entryDependency} in the default catalog.");
                        }
                    }
                }
            }

            return catalogEntryMapping;
        }
    }
}