using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;


namespace UnityEditor.AddressableAssets.Build.CatalogBuilders
{
    /// <summary>
    /// Base class for catalog builders that generate content catalog data for Addressables builds.
    /// Provides common functionality for creating catalog files in different formats.
    /// </summary>
    public abstract class BaseCatalogBuilder : ICatalogBuilder
    {
        /// <inheritdoc/>
        public abstract ContentCatalogData GenerateCatalog(IBuildLogger logger,
            CatalogPathConfig catalogPaths,
            string catalogLocatorId,
            IList<ContentCatalogDataEntry> catalogDataEntries,
            List<ResourceLocationData> catalogLocations,
            HashSet<Type> providerTypes,
            FileRegistry registry,
            string buildResultHash,
            bool buildRemoteCatalog,
            int catalogRequestsTimeout,
            CatalogBundleConfig catalogBundleConfig = null);


        /// <summary>
        /// Gets the file extension used for catalog files created by this builder (e.g., "json" or "bin").
        /// </summary>
        protected abstract string CatalogExtension { get; }

        /// <summary>
        /// Appends the catalog file extension to the given filename if not already present.
        /// </summary>
        /// <param name="catalogFilename">The catalog filename to modify.</param>
        /// <returns>The filename with the appropriate catalog extension.</returns>
        protected string AddExtensionToCatalogFilename(string catalogFilename)
        {
            if (Path.GetExtension(catalogFilename) != $".{CatalogExtension}")
            {
                return $"{catalogFilename}.{CatalogExtension}";
            }
            return catalogFilename;
        }

        /// <summary>
        /// Gets the base filename for the catalog based on the locator ID.
        /// Returns "catalog" for the default catalog address, otherwise returns the locator ID.
        /// </summary>
        /// <param name="catalogLocatorId">The catalog locator identifier.</param>
        /// <returns>The base filename to use for the catalog.</returns>
        protected string GetBaseCatalogFilename(string catalogLocatorId)
        {
            if (catalogLocatorId == ResourceManagerRuntimeData.kCatalogAddress)
                return "catalog";
            return catalogLocatorId;
        }
    }
}
