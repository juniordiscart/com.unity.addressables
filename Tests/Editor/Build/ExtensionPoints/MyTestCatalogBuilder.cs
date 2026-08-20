using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.CatalogBuilders;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace ThirdParty.AddressablesExtensions.Tests
{
    /// <summary>
    /// Third-party ICatalogBuilder that delegates to JsonCatalogBuilder and records each invocation.
    /// Subclasses JsonCatalogBuilder so the build produces a valid catalog file.
    /// </summary>
    internal class MyTestCatalogBuilder : JsonCatalogBuilder
    {
        internal static int GenerateCatalogCallCount;
        internal static string LastCatalogLocatorId;

        internal static void ClearCallRecord()
        {
            GenerateCatalogCallCount = 0;
            LastCatalogLocatorId = null;
        }

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
            GenerateCatalogCallCount++;
            LastCatalogLocatorId = catalogLocatorId;
            return base.GenerateCatalog(
                logger, catalogPaths, catalogLocatorId, catalogDataEntries,
                catalogLocations, providerTypes, registry, buildResultHash,
                buildRemoteCatalog, catalogRequestsTimeout, catalogBundleConfig);
        }
    }
}
