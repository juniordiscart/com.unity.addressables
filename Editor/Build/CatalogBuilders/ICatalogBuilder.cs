using System;
using System.Collections.Generic;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace UnityEditor.AddressableAssets.Build.CatalogBuilders
{
    /// <summary>
    /// Interface for catalog builders that generate content catalog data during Addressables builds.
    /// Implementations create catalogs in specific formats (e.g., JSON, binary).
    /// </summary>
    public interface ICatalogBuilder
    {
        /// <summary>
        /// Generates a content catalog from the provided build data.
        /// </summary>
        /// <param name="logger">The build logger for recording build steps.</param>
        /// <param name="catalogPaths">Configuration containing catalog build and load paths.</param>
        /// <param name="catalogLocatorId">The unique identifier for the catalog locator.</param>
        /// <param name="catalogDataEntries">The catalog data entries to include in the catalog.</param>
        /// <param name="catalogLocations">List to populate with resource location data for catalog loading.</param>
        /// <param name="providerTypes">Set of provider types used by the catalog.</param>
        /// <param name="registry">The file registry to track generated files.</param>
        /// <param name="buildResultHash">The hash of the build result for versioning.</param>
        /// <param name="buildRemoteCatalog">Whether to build a remote catalog in addition to the local one.</param>
        /// <param name="catalogRequestsTimeout">The timeout in seconds for catalog download requests.</param>
        /// <param name="catalogBundleConfig">Optional configuration for bundling the catalog into an AssetBundle.</param>
        /// <returns>The generated content catalog data.</returns>
        public ContentCatalogData GenerateCatalog(
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
            CatalogBundleConfig catalogBundleConfig = null
        );
    }
}
