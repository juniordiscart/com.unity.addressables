using System.Collections.Generic;
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
        /// The directory path this catalog is expected to be build.
        /// </summary>
		public string BuildPath;

        /// <summary>
        /// The directory path this catalog is expected to be loaded from.
        /// </summary>
		public string LoadPath;

		/// <summary>
		/// Determines whether the catalog is going to be registered in settings.json.
		///
		/// Registered catalogs are automatically loaded on application startup.
		/// Use "false" for catalogs that are to be loaded dynamicaly.
		/// </summary>
		public bool Register = true;

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
