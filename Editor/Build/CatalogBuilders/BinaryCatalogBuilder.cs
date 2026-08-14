using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace UnityEditor.AddressableAssets.Build.CatalogBuilders
{
    /// <summary>
    /// Catalog builder that generates content catalogs in a compact binary format.
    /// Binary catalogs are smaller and faster to load than JSON catalogs.
    /// </summary>
    public class BinaryCatalogBuilder : BaseCatalogBuilder
    {
        /// <inheritdoc/>
        protected override string CatalogExtension { get => "bin"; }

        /// <inheritdoc/>
        public override ContentCatalogData GenerateCatalog(
            IBuildLogger logger,
            CatalogPathConfig CatalogPathConfig,
            string catalogLocatorId,
            IList<ContentCatalogDataEntry> catalogDataEntries,
            List<ResourceLocationData> catalogLocations,
            HashSet<Type> providerTypes,
            FileRegistry registry,
            string buildResultHash,
            bool buildRemoteCatalog,
            int catalogRequestsTimeout,
            CatalogBundleConfig catalogBundleConfig = null
            )
        {
            ContentCatalogData contentCatalog = null;
            using (logger.ScopedStep(LogLevel.Info, "Generate Binary Catalog"))
            {
                contentCatalog = new ContentCatalogData(catalogLocatorId);
                contentCatalog.ProviderId = catalogLocatorId;
                contentCatalog.BuildResultHash = buildResultHash;

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

                contentCatalog.SetData(catalogDataEntries.OrderBy(f => f.InternalId).ToList());//, aaContext.Settings.OptimizeCatalogSize);
                var bytes = contentCatalog.SerializeToByteArray();
                var contentHash = HashingMethods.Calculate(bytes);

                if (buildRemoteCatalog || ProjectConfigData.GenerateBuildLayout)
                    contentCatalog.LocalHash = contentHash.ToString();

                CreateCatalogFiles(
                    CatalogPathConfig,
                    catalogLocatorId,
                    registry,
                    bytes,
                    catalogLocations,
                    buildRemoteCatalog,
                    catalogRequestsTimeout,
                    contentHash);
            }
            return contentCatalog;
        }

        private bool CreateCatalogFiles(
            CatalogPathConfig catalogPaths,
            string catalogLocatorId,
            FileRegistry registry,
            byte[] data,
            IList<ResourceLocationData> catalogLocations,
            bool buildRemoteCatalog,
            int catalogRequestsTimeout,
            RawHash catalogHash)
        {
            if (data == null || data.Length == 0)
            {
                Addressables.LogError("Unable to create content catalog (Null arguments).");
                return false;
            }
            // Path needs to be resolved at runtime.
            var runtimeCatalogFilename = AddExtensionToCatalogFilename(catalogPaths.RuntimeCatalogFilename);
            string localLoadPath = "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/" + runtimeCatalogFilename;
            string catalogBuildPath = Path.Combine(Addressables.BuildPath, runtimeCatalogFilename);

            BuildScriptBase.WriteFile(catalogBuildPath, data, registry);
            BuildScriptBase.WriteStringToFile(catalogBuildPath.Replace(".bin", ".hash"), HashingMethods.Calculate(data).ToString(), registry);

            string[] dependencyHashes = null;
            if (buildRemoteCatalog)
            {
                dependencyHashes = CreateRemoteCatalog(
                    catalogPaths,
                    catalogLocatorId,
                    registry,
                    data,
                    catalogLocations,
                    new ProviderLoadRequestOptions() {
                        IgnoreFailures = true,
                        WebRequestTimeout = catalogRequestsTimeout,
                    },
                    catalogHash);
            }

            catalogLocations.Add(new ResourceLocationData(
                new[] { catalogLocatorId },
                localLoadPath,
                typeof(ContentCatalogProvider),
                typeof(ContentCatalogData),
                dependencyHashes));

            return true;
        }
        private string[] CreateRemoteCatalog(
            CatalogPathConfig CatalogPathConfig,
            string catalogLocatorId,
            FileRegistry registry,
            byte[] data,
            IList<ResourceLocationData> locations,
            ProviderLoadRequestOptions catalogLoadOptions,
            RawHash contentHash)
        {
            string[] dependencyHashes = null;


            if (string.IsNullOrEmpty(CatalogPathConfig.RemoteBuildPath) ||
                string.IsNullOrEmpty(CatalogPathConfig.RemoteLoadPath) ||
                CatalogPathConfig.RemoteBuildPath == AddressableAssetProfileSettings.undefinedEntryValue ||
                CatalogPathConfig.RemoteLoadPath == AddressableAssetProfileSettings.undefinedEntryValue)
            {
                Addressables.LogWarning(
                    "Remote Build and/or Load paths are not set on the main AddressableAssetSettings asset, but 'Build Remote Catalog' is true.  Cannot create remote catalog.  In the inspector for any group, double click the 'Addressable Asset Settings' object to begin inspecting it. '" +
                    CatalogPathConfig.RemoteBuildPath + "', '" + CatalogPathConfig.RemoteLoadPath + "'");
            }
            else
            {
                var remoteCatalogBuildPath = DirectoryUtility.EnsureTrailingSlash(CatalogPathConfig.RemoteBuildPath) + CatalogPathConfig.VersionedCatalogFileName + ".bin";
                var remoteHashBuildPath = DirectoryUtility.EnsureTrailingSlash(CatalogPathConfig.RemoteBuildPath) + CatalogPathConfig.VersionedCatalogFileName + ".hash";

                BuildScriptBase.WriteFile(remoteCatalogBuildPath, data, registry);
                BuildScriptBase.WriteStringToFile(remoteHashBuildPath, contentHash.ToString(), registry);

                dependencyHashes = new string[((int)ContentCatalogProvider.DependencyHashIndex.Count)];
                dependencyHashes[(int)ContentCatalogProvider.DependencyHashIndex.Remote] = $"{catalogLocatorId}RemoteHash";
                dependencyHashes[(int)ContentCatalogProvider.DependencyHashIndex.Cache] = $"{catalogLocatorId}CacheHash";
                dependencyHashes[(int)ContentCatalogProvider.DependencyHashIndex.Local] = $"{catalogLocatorId}LocalHash";

                var remoteHashLoadPath = DirectoryUtility.EnsureTrailingSlash(CatalogPathConfig.RemoteLoadPath) + CatalogPathConfig.VersionedCatalogFileName + ".hash";
                var remoteHashLoadLocation = new ResourceLocationData(
                    new[] { dependencyHashes[(int)ContentCatalogProvider.DependencyHashIndex.Remote] },
                    remoteHashLoadPath,
                    typeof(TextDataProvider), typeof(string));
                remoteHashLoadLocation.Data = catalogLoadOptions.Copy();
                locations.Add(remoteHashLoadLocation);

#if UNITY_SWITCH || UNITY_SWITCH2
                var cacheLoadPath = remoteHashLoadPath; // ResourceLocationBase does not allow empty string id
#else
                var cacheLoadPath = DirectoryUtility.EnsureTrailingSlash("{UnityEngine.Application.persistentDataPath}/com.unity.addressables") + CatalogPathConfig.VersionedCatalogFileName + ".hash";
#endif
                var cacheLoadLocation = new ResourceLocationData(
                    new[] { dependencyHashes[(int)ContentCatalogProvider.DependencyHashIndex.Cache] },
                    cacheLoadPath,
                    typeof(TextDataProvider), typeof(string));
                cacheLoadLocation.Data = catalogLoadOptions.Copy();
                locations.Add(cacheLoadLocation);

                var localCatalogLoadPath = DirectoryUtility.EnsureTrailingSlash("{UnityEngine.AddressableAssets.Addressables.RuntimePath}/") + GetBaseCatalogFilename(catalogLocatorId) + ".hash";
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
