using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.CatalogBuilders;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders
{
    public class BundledAssetMultiCatalogSchemaBuilder : BundledAssetSchemaBuilder
    {
        private List<ExternalCatalogSetup> m_ExternalCatalogSetups;
        private List<ExternalCatalogConfig> m_ExternalCatalogConfigs;

        public BundledAssetMultiCatalogSchemaBuilder(IEnumerable<ExternalCatalogConfig> externalCatalogConfigs)
        {
            m_ExternalCatalogConfigs = new List<ExternalCatalogConfig>(externalCatalogConfigs);
        }

        public override string Name => "Bundled Assets (Multi-Catalog)";

        public override List<ContentCatalogData> GenerateCatalogs(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext, AddressablesPlayerBuildResult addrResult)
        {
#if UNITY_6000_5_OR_NEWER
            // this variable is always reset when Init is called at the start of a build when we initialize the build context.
            m_BuiltTypeTreeDataPath = Path.Combine(Addressables.BuildPath, kTypeTreeDataFileName);
            if (aaContext.Settings.ExtractTypeTreeData)
            {
                aaContext.providerTypes.Add(typeof(CachedFileProvider));
                if (builderInput.PreviousContentState != null)
                {
                    var strippedPath = Path.GetTempFileName();
                    if (builderInput.PreviousContentState.typeTreeHashes != null)
                        ContentBuildInterface.StripTypeTreeDataFromFile(builderInput.PreviousContentState.typeTreeHashes, m_BuiltTypeTreeDataPath, strippedPath);
                    else
                        strippedPath = m_BuiltTypeTreeDataPath;

                    var hashStr = Hash128.Compute(File.ReadAllBytes(strippedPath)).ToString();
                    var newPath = $"{aaContext.Settings.RemoteCatalogBuildPath.GetValue(aaContext.Settings)}/{hashStr}{kTypeTreeDataExtension}";
                    if (!Directory.Exists(Path.GetDirectoryName(newPath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                    if(File.Exists(newPath))
                        File.Delete(newPath);
                    File.Move(strippedPath, newPath);
                    builderInput.Registry.AddFile(newPath);

                    string remoteURL = $"{aaContext.Settings.RemoteCatalogLoadPath.GetValue(aaContext.Settings)}/{hashStr}{kTypeTreeDataExtension}";
                    aaContext.locations.Add(new ContentCatalogDataEntry(typeof(string),
                        remoteURL,  //for remote content, the url
                        typeof(CachedFileProvider).FullName,
                        new string[] { ResourceManagerRuntimeData.kTypeTreeDataAddress },
                        null,
                        new ProviderLoadRequestOptions
                        {
                            IgnoreFailures = false,
                            LocalCachePath = $"{hashStr[0]}{hashStr[1]}/{hashStr}"
                        }));
                }
                //only add the local tt data location if this is NOT a content update OR if the baseline build has hashes (tt extraction was enabled)
                if (builderInput.PreviousContentState == null || (builderInput.PreviousContentState.typeTreeHashes != null && builderInput.PreviousContentState.typeTreeHashes.Length > 0))
                {
                    aaContext.locations.Add(new ContentCatalogDataEntry(typeof(string),
                    "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/" + kTypeTreeDataFileName,
                    typeof(CachedFileProvider).FullName,
                    new string[] { ResourceManagerRuntimeData.kTypeTreeDataAddress }));
                }
            }
            else
            {
                if (File.Exists(m_BuiltTypeTreeDataPath))
                    File.Delete(m_BuiltTypeTreeDataPath);
                m_BuiltTypeTreeDataPath = string.Empty;
            }
#endif

#if ENABLE_JSON_CATALOG
            CatalogBundleConfig catalogBundleConfig = null;
            if (aaContext.Settings.BundleLocalCatalog)
            {
                var configFolder = AddressableAssetSettingsDefaultObject.kDefaultConfigFolder;
                if (builderInput.AddressableSettings != null && builderInput.AddressableSettings.IsPersisted)
                    configFolder = builderInput.AddressableSettings.ConfigFolder;

                catalogBundleConfig = new CatalogBundleConfig
                {
                    ConfigFolder = configFolder
                };
            }

            BaseCatalogBuilder catalogBuilder = new JsonCatalogBuilder();
#else
            BaseCatalogBuilder catalogBuilder = new BinaryCatalogBuilder();
#endif

            List<ContentCatalogBuildInfo> catalogSetups = GenerateCatalogsBuildInfo(builderInput, aaContext, addrResult, catalogBuilder);

            m_CatalogBuildPath = catalogBuilder.AddExtensionToCatalogFilename(Path.Combine(Addressables.BuildPath, builderInput.RuntimeCatalogFilename));
            List<ContentCatalogData> catalogs = new List<ContentCatalogData>();
            foreach (var catalogSetup in catalogSetups)
            {
                var catalogContentData = catalogBuilder.GenerateCatalog(
                    builderInput.Logger,
                    catalogSetup.PathConfig,
                    catalogSetup.Identifier, // ResourceManagerRuntimeData.kCatalogAddress, //TODO: if we move AssetBundle builds to support multiple catalogs, we can change this to use the schema CatalogId
                    catalogSetup.Locations,
                    aaContext.runtimeData.CatalogLocations,
                    aaContext.providerTypes,
                    builderInput.Registry,
                    catalogSetup.BuildHashResult,
                    aaContext.Settings.BuildRemoteCatalog,
                    aaContext.Settings.CatalogRequestsTimeout);

                catalogs.Add(catalogContentData);
            }

            return catalogs;
        }

        private List<ContentCatalogBuildInfo> GenerateCatalogsBuildInfo(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext, AddressablesPlayerBuildResult addrResult, BaseCatalogBuilder catalogBuilder)
        {
            m_ExternalCatalogSetups = new List<ExternalCatalogSetup>();
            m_ExternalCatalogSetups.AddRange(m_ExternalCatalogConfigs.Where(ecc => ecc != null).Select(ecc => new ExternalCatalogSetup(ecc, builderInput, aaContext, catalogBuilder)));

            // Prepare catalogs -- default catalog is always first
            var aaSettings = aaContext.Settings;
			var defaultCatalog = new ContentCatalogBuildInfo(ResourceManagerRuntimeData.kCatalogAddress, builderInput.RuntimeCatalogFilename)
            {
                PathConfig = new CatalogPathConfig()
                {
                    BuildPath = Addressables.BuildPath,
                    LoadPath = "{UnityEngine.AddressableAssets.Addressables.RuntimePath}",
                    RemoteBuildPath = aaSettings.RemoteCatalogBuildPath.Id != string.Empty ? aaSettings.RemoteCatalogBuildPath.GetValue(aaSettings) : string.Empty,
                    RemoteLoadPath = aaSettings.RemoteCatalogLoadPath.Id != string.Empty ?  aaSettings.RemoteCatalogLoadPath.GetValue(aaSettings) : string.Empty,
                    RuntimeCatalogFilename = builderInput.RuntimeCatalogFilename,
                    VersionedCatalogFileName = aaSettings.profileSettings.EvaluateString(aaSettings.activeProfileId, "/catalog_" + builderInput.PlayerVersion),
                }
            };

			// Go over all locations and assign them to the appropriate external catalog if any was found.
			foreach (var loc in aaContext.locations)
            {
                var preferredCatalog = m_ExternalCatalogSetups.FirstOrDefault(cs => cs.CatalogConfig.IsPartOfCatalog(loc, aaContext));

                // If no preferred catalog is found, assign the asset to the default catalog and skip further processing.
                if (preferredCatalog == null)
                {
                    defaultCatalog.Locations.Add(loc);
                    continue;
                }

                if (loc.ResourceType != typeof(IAssetBundleResource))
                {
                    preferredCatalog.BuildInfo.Locations.Add(loc);
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
                var filename = GenerateLocationListsTask.GetFileName(loc.InternalId, builderInput.Target);
                var runtimeLoadPath = GenerateLocationListsTask.GetLoadPath(group, schema.LoadPath, filename, builderInput.Target);

                preferredCatalog.BuildInfo.Locations.Add(new ContentCatalogDataEntry(typeof(IAssetBundleResource), runtimeLoadPath, loc.Provider, loc.Keys, loc.Dependencies, loc.Data));
            }

            // Generate the dependencies for each external catalog.
			foreach (var externalCatalogSetup in m_ExternalCatalogSetups)
			{
				var locationQueue = new Queue<ContentCatalogDataEntry>(externalCatalogSetup.BuildInfo.Locations);
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
						var depLocation = defaultCatalog.Locations.Find(loc => loc.Keys[0] == entryDependency);

						if (depLocation != null)
						{
							locationQueue.Enqueue(depLocation);

							// If the dependency wasn't part of the catalog yet, add it.
							if (!externalCatalogSetup.BuildInfo.Locations.Contains(depLocation))
							{
								externalCatalogSetup.BuildInfo.Locations.Add(depLocation);
							}
						}
						else if (!externalCatalogSetup.BuildInfo.Locations.Exists(loc => loc.Keys[0] == entryDependency))
						{
							Debug.LogErrorFormat("Could not find location for dependency ID {0} in the default catalog.", entryDependency);
						}
					}
				}
			}

			// Gather catalogs
			var catalogs = new List<ContentCatalogBuildInfo>() { defaultCatalog };
            catalogs.AddRange(m_ExternalCatalogSetups.Where(ecs => !ecs.IsEmpty).Select(ecs => ecs.BuildInfo));

            // If there's no Addressables result, then we can skip the hashing.
            if (addrResult?.AssetBundleBuildResults == null)
            {
                return catalogs;
            }

            // For each catalog, go over the included bundle build results and compute their hash value.
            foreach (var contentCatalogBuildInfo in catalogs)
            {
                var hashingObjects = new List<object>(addrResult.AssetBundleBuildResults.Count);
                foreach (var hashingObject in addrResult.AssetBundleBuildResults)
                {
                    if (contentCatalogBuildInfo.Locations.Exists(l => (l.ResourceType == typeof(IAssetBundleResource)) && l.InternalId.Equals(hashingObject.FilePath)))
                    {
                        hashingObjects.Add(hashingObject.Hash);
                    }
                }

                contentCatalogBuildInfo.BuildHashResult = HashingMethods.Calculate(hashingObjects.ToArray()).ToString();
            }

            return catalogs;
        }
        private class ExternalCatalogSetup
        {
            public readonly ExternalCatalogConfig CatalogConfig;

            /// <summary>
            /// The catalog build info.
            /// </summary>
            public readonly ContentCatalogBuildInfo BuildInfo;

            /// <summary>
            /// Tells whether the catalog is empty.
            /// </summary>
            public bool IsEmpty => BuildInfo.Locations.Count == 0;

            public ExternalCatalogSetup(ExternalCatalogConfig catalogConfig, AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext, BaseCatalogBuilder catalogBuilder)
            {
                CatalogConfig = catalogConfig;

                var aaSettings = aaContext.Settings;
                var profileSettings = aaContext.Settings.profileSettings;
                var profileId = aaContext.Settings.activeProfileId;
                var catalogFileName = $"{catalogBuilder.AddExtensionToCatalogFilename(catalogConfig.CatalogName)}";
                var pathConfig = new CatalogPathConfig()
                {
                    BuildPath = catalogConfig.BuildPath.GetValue(profileSettings, profileId),
                    LoadPath = catalogConfig.RuntimeLoadPath.GetValue(profileSettings, profileId),
                    RemoteBuildPath = string.Empty, // Unsupported
                    RemoteLoadPath = string.Empty,  // Unsupported
                    RuntimeCatalogFilename = catalogConfig.CatalogName,
                    VersionedCatalogFileName = aaSettings.profileSettings.EvaluateString(aaSettings.activeProfileId, $"{CatalogConfig.CatalogName}_{builderInput.PlayerVersion}"),
                };

                // Set the build path.
                if (string.IsNullOrEmpty(pathConfig.BuildPath))
                {
                    pathConfig.BuildPath = profileSettings.EvaluateString(profileId, catalogConfig.BuildPath.Id);

                    if (string.IsNullOrWhiteSpace(pathConfig.BuildPath))
                    {
                        throw new Exception($"The catalog build path for external catalog '{catalogConfig.name}' is empty.");
                    }
                }

                // Set the load path.
                if (string.IsNullOrEmpty(pathConfig.LoadPath))
                {
                    pathConfig.LoadPath = profileSettings.EvaluateString(profileId, catalogConfig.RuntimeLoadPath.Id);
                }

                BuildInfo = new ContentCatalogBuildInfo(catalogConfig.CatalogName, catalogFileName)
                {
                    PathConfig = pathConfig
                };
            }
        }
    }
}

