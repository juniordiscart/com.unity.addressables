using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace UnityEditor.AddressableAssets.Build.CatalogBuilders
{
    /// <summary>
    /// Catalog builder that generates content catalogs in JSON format.
    /// JSON catalogs are human-readable and useful for debugging, but larger than binary catalogs.
    /// </summary>
    public class JsonCatalogBuilder : BaseCatalogBuilder
    {
        /// <inheritdoc/>
        protected override string CatalogExtension { get => "json"; }

        /// <inheritdoc/>
        public override ContentCatalogData GenerateCatalog(
            IBuildLogger logger,
            CatalogPathConfig catalogPaths,
            string catalogLocatorId,
            IList<ContentCatalogDataEntry> catalogDataEntries,
            List<ResourceLocationData> catalogLocations,
            HashSet<Type> providerTypes,
            FileRegistry registry,
            string buildResultHash,
            bool buildRemoteCatalog,
            int catalogRequestsTimeout,
            CatalogBundleConfig catalogBundleConfig = null)
        {
            ContentCatalogData contentCatalog = null;
            using (logger.ScopedStep(LogLevel.Info, "Generate JSON Catalog"))
            {
                contentCatalog = new ContentCatalogData(catalogLocatorId);

                contentCatalog.SetData(catalogDataEntries.OrderBy(f => f.InternalId).ToList());
                contentCatalog.ProviderId = catalogLocatorId;

                IEnumerable<Type> sortedEnum = providerTypes.OrderBy(f => f.Name);
                foreach (var t in providerTypes)
                {
                    var serializedType = ObjectInitializationData.CreateSerializedInitializationData(t);
                    if (t.GetInterfaces().Contains(typeof(IInstanceProvider)))
                    {
                        contentCatalog.InstanceProviderData = serializedType;
                        continue;
                    }
                    if (t.GetInterfaces().Contains(typeof(ISceneProvider)))
                    {
                        contentCatalog.SceneProviderData = serializedType;
                        continue;
                    }
                    contentCatalog.ResourceProviderData.Add(serializedType);
                }

                //save catalog
                string contentHash = null;
                string jsonText = null;
                using (logger.ScopedStep(LogLevel.Info, "Generating Json"))
                    jsonText = JsonUtility.ToJson(contentCatalog);
                if (buildRemoteCatalog || ProjectConfigData.GenerateBuildLayout)
                {
                    using (logger.ScopedStep(LogLevel.Info, "Hashing Catalog"))
                        contentHash = HashingMethods.Calculate(jsonText).ToString();
                    contentCatalog.LocalHash = contentHash;
                }

                CreateCatalogFiles(
                    logger,
                    catalogPaths,
                    catalogLocatorId,
                    registry,
                    jsonText,
                    catalogLocations,
                    buildRemoteCatalog,
                    contentHash,
                    catalogRequestsTimeout,
                    catalogBundleConfig);
            }
            return contentCatalog;
        }

        private bool CreateCatalogFiles(
            IBuildLogger logger,
            CatalogPathConfig catalogPaths,
            string catalogLocatorId,
            FileRegistry registry,
            string jsonText,
            IList<ResourceLocationData> catalogLocations,
            bool buildRemoteCatalog,
            string catalogHash,
            int catalogRequestsTimeout,
            CatalogBundleConfig catalogBundleConfig)
        {
            if (string.IsNullOrEmpty(jsonText))
            {
                Addressables.LogError("Unable to create content catalog (Null arguments).");
                return false;
            }

            // Path needs to be resolved at runtime.
            string runtimeCatalogFilename = AddExtensionToCatalogFilename(catalogPaths.RuntimeCatalogFilename);
            string localLoadPath = "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/" + runtimeCatalogFilename;
            string catalogBuildPath = Path.Combine(catalogPaths.BuildPath, runtimeCatalogFilename);

            if (catalogBundleConfig != null)
            {
                localLoadPath = localLoadPath.Replace(".json", ".bundle");
                localLoadPath = localLoadPath.Replace(".json", ".bundle");
                var returnCode = CreateCatalogBundle(logger, catalogBundleConfig, registry, localLoadPath, jsonText);
                if (returnCode != ReturnCode.Success || !File.Exists(localLoadPath))
                {
                    Addressables.LogError($"An error occured during the creation of the content catalog bundle (return code {returnCode}).");
                    return false;
                }
            }
            else
            {
                BuildScriptBase.WriteStringToFile(catalogBuildPath, jsonText, registry);
                BuildScriptBase.WriteStringToFile(catalogBuildPath.Replace(".json", ".hash"), HashingMethods.Calculate(jsonText).ToString(), registry);
            }

            string[] dependencyHashes = null;
            if (buildRemoteCatalog)
            {
                dependencyHashes = CreateRemoteCatalog(
                    catalogPaths,
                    catalogLocatorId,
                    registry,
                    jsonText,
                    catalogLocations,
                    new ProviderLoadRequestOptions() {
                        IgnoreFailures = true,
                        WebRequestTimeout = catalogRequestsTimeout,
                    },
                    catalogHash);
            }

            ResourceLocationData localCatalog = new ResourceLocationData(
                new[] { catalogLocatorId },
                localLoadPath,
                typeof(ContentCatalogProvider),
                typeof(ContentCatalogData),
                dependencyHashes);
            //We need to set the data here because this location data gets used later if we decide to load the remote/cached catalog instead.  See DetermineIdToLoad(...)
            localCatalog.Data = new ProviderLoadRequestOptions() {
                IgnoreFailures = true,
                WebRequestTimeout = catalogRequestsTimeout,
            };

            catalogLocations.Add(localCatalog);

            return true;
        }

        // FIXME: why does CreateCatalogBundle need to be different for binary and JSON?
        private ReturnCode CreateCatalogBundle(IBuildLogger logger,
            CatalogBundleConfig catalogBundleConfig,
            FileRegistry registry,
            string filepath,
            string jsonText)
        {
            if (string.IsNullOrEmpty(filepath) || string.IsNullOrEmpty(jsonText))
            {
                throw new ArgumentException("Unable to create catalog bundle (null arguments).");
            }

            // A bundle requires an actual asset
            var tempFolderName = "TempCatalogFolder";

            var tempFolderPath = Path.Combine(catalogBundleConfig.ConfigFolder, tempFolderName);
            var tempFilePath = Path.Combine(tempFolderPath, Path.GetFileName(filepath).Replace(".bundle", ".json"));
            if (!BuildScriptBase.WriteStringToFile(tempFilePath, jsonText, registry))
            {
                throw new Exception("An error occured during the creation of temporary files needed to bundle the content catalog.");
            }

            AssetDatabase.Refresh();

            var bundleBuildContent = new BundleBuildContent(new[]
            {
                new AssetBundleBuild()
                {
                    assetBundleName = Path.GetFileName(filepath),
                    assetNames = new[] {tempFilePath},
                    addressableNames = new string[0]
                }
            });

            var buildTasks = new List<IBuildTask>
            {
                new CalculateAssetDependencyData(),
                new GenerateBundlePacking(),
                new GenerateBundleCommands(),
                new WriteSerializedFiles(),
                new ArchiveAndCompressBundles()
            };

            var buildParams = new BundleBuildParameters(catalogBundleConfig.Target, catalogBundleConfig.TargetGroup, Path.GetDirectoryName(filepath));
            if (catalogBundleConfig.Target == BuildTarget.WebGL)
                buildParams.BundleCompression = BuildCompression.LZ4Runtime;
            var retCode = ContentPipeline.BuildAssetBundles(buildParams, bundleBuildContent, out IBundleBuildResults result, buildTasks, logger);

            if (Directory.Exists(tempFolderPath))
            {
                Directory.Delete(tempFolderPath, true);
                registry.RemoveFile(tempFilePath);
            }

            var tempFolderMetaFile = tempFolderPath + ".meta";
            if (File.Exists(tempFolderMetaFile))
            {
                File.Delete(tempFolderMetaFile);
                registry.RemoveFile(tempFolderMetaFile);
            }

            if (File.Exists(filepath))
            {
                registry.AddFile(filepath);
            }

            return retCode;
        }

        private string[] CreateRemoteCatalog(
            CatalogPathConfig catalogPaths,
            string catalogLocatorId,
            FileRegistry registry,
            string jsonText,
            IList<ResourceLocationData> locations,
            ProviderLoadRequestOptions catalogLoadOptions,
            string contentHash)
        {
            string[] dependencyHashes = null;

            if (string.IsNullOrEmpty(contentHash))
                contentHash = HashingMethods.Calculate(jsonText).ToString();

            if (string.IsNullOrEmpty(catalogPaths.RemoteBuildPath) ||
                string.IsNullOrEmpty(catalogPaths.RemoteLoadPath) ||
                catalogPaths.RemoteBuildPath == AddressableAssetProfileSettings.undefinedEntryValue ||
                catalogPaths.RemoteLoadPath == AddressableAssetProfileSettings.undefinedEntryValue)
            {
                Addressables.LogWarning(
                    "Remote Build and/or Load paths are not set on the main AddressableAssetSettings asset, but 'Build Remote Catalog' is true.  Cannot create remote catalog.  In the inspector for any group, double click the 'Addressable Asset Settings' object to begin inspecting it. '" +
                    catalogPaths.RemoteBuildPath + "', '" + catalogPaths.RemoteLoadPath + "'");
            }
            else
            {
                var remoteJsonBuildPath = DirectoryUtility.EnsureTrailingSlash(catalogPaths.RemoteBuildPath) + catalogPaths.VersionedCatalogFileName + ".json";
                var remoteHashBuildPath = DirectoryUtility.EnsureTrailingSlash(catalogPaths.RemoteBuildPath) + catalogPaths.VersionedCatalogFileName + ".hash";

                BuildScriptBase.WriteStringToFile(remoteJsonBuildPath, jsonText, registry);
                BuildScriptBase.WriteStringToFile(remoteHashBuildPath, contentHash, registry);

                dependencyHashes = new string[((int)ContentCatalogProvider.DependencyHashIndex.Count)];
                dependencyHashes[(int)ContentCatalogProvider.DependencyHashIndex.Remote] = $"{catalogLocatorId}RemoteHash";
                dependencyHashes[(int)ContentCatalogProvider.DependencyHashIndex.Cache] = $"{catalogLocatorId}CacheHash";
                dependencyHashes[(int)ContentCatalogProvider.DependencyHashIndex.Local] = $"{catalogLocatorId}LocalHash";

                var remoteHashLoadPath = DirectoryUtility.EnsureTrailingSlash(catalogPaths.RemoteLoadPath) + catalogPaths.VersionedCatalogFileName + ".hash";
                var remoteHashLoadLocation = new ResourceLocationData(
                    new[] { dependencyHashes[(int)ContentCatalogProvider.DependencyHashIndex.Remote] },
                    remoteHashLoadPath,
                    typeof(TextDataProvider), typeof(string));
                remoteHashLoadLocation.Data = catalogLoadOptions.Copy();
                locations.Add(remoteHashLoadLocation);

#if UNITY_SWITCH || UNITY_SWITCH2
                var cacheLoadPath = remoteHashLoadPath; // ResourceLocationBase does not allow empty string id
#else
                var cacheLoadPath = "{UnityEngine.Application.persistentDataPath}/com.unity.addressables" + catalogPaths.VersionedCatalogFileName + ".hash";
#endif
                var cacheLoadLocation = new ResourceLocationData(
                    new[] { dependencyHashes[(int)ContentCatalogProvider.DependencyHashIndex.Cache] },
                    cacheLoadPath,
                    typeof(TextDataProvider), typeof(string));
                cacheLoadLocation.Data = catalogLoadOptions.Copy();
                locations.Add(cacheLoadLocation);

                var localCatalogLoadPath = DirectoryUtility.EnsureTrailingSlash("{UnityEngine.AddressableAssets.Addressables.RuntimePath}") + GetBaseCatalogFilename(catalogLocatorId) + ".hash";
                var localLoadLocation = new ResourceLocationData(
                    new[] { dependencyHashes[(int)ContentCatalogProvider.DependencyHashIndex.Local] },
                    localCatalogLoadPath,
                    typeof(TextDataProvider), typeof(string));
                locations.Add(localLoadLocation);
            }

            return dependencyHashes;
        }

    }
}
