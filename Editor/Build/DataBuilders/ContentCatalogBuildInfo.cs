using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build.CatalogBuilders;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    /// <summary>
    /// Contains information about a catalog to be built.
    /// </summary>
    public class ContentCatalogBuildInfo
    {
        /// <summary>
        /// The catalog identifier.
        ///
        /// Note that "AddressablesMainContentCatalog" is used for the default main catalog.
        /// </summary>
        public readonly string Identifier;

        /// <summary>
        /// The filename of the JSON file to contain the catalog data.
        ///
        /// Note that the default main catalog is written to "catalog.json"
        /// </summary>
        public readonly string CatalogFilename;

        /// <summary>
        /// The locations, i.e., the addressable assets, contained in this catalog.
        /// </summary>
        public readonly List<ContentCatalogDataEntry> Locations = new List<ContentCatalogDataEntry>();

        /// <summary>
        /// Configuration for defining and managing catalog paths.
        ///
        /// This variable holds an instance of <c>CatalogPathConfig</c>, which is utilized in catalog generation processes
        /// to specify and manage path-related configurations for content catalogs.
        /// </summary>
        public CatalogPathConfig PathConfig;

		/// <summary>
		/// Represents the result of the hash calculation for a content catalog during the build process.
		/// </summary>
		public string BuildHashResult;

        /// <summary>
        /// Construct an empty catalog build info.
        /// </summary>
        /// <param name="identifier">the identifier</param>
        /// <param name="catalogFileName">the json filename</param>
        public ContentCatalogBuildInfo(string identifier, string catalogFileName)
        {
            Identifier = identifier;
            CatalogFilename = catalogFileName;
        }
    }
}
