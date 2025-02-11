using System.Collections.Generic;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    /// <summary>
    /// Object used in the build layout
    /// </summary>
    public interface IBuildLayoutParameters : IContextObject
    {
        /// <summary>
        /// A mapping of internal AssetBundle names to the file name
        /// </summary>
        Dictionary<string, string> BundleNameRemap { get; set; }

        /// <summary>
        /// Calculated hash of the build layout object
        /// </summary>
        string BuildResultHash { get; }

        /// <summary>
        /// The hash of the associated catalog
        /// </summary>
        string CatalogHash { get; }
    }

    /// <summary>
    /// Concreate implementation for objects used in the build layout
    /// </summary>
    public class BuildLayoutParameters : IBuildLayoutParameters
    {
        private Dictionary<string, string> m_BundleNameRemap;
        private ContentCatalogData m_contentCatalogData;

        /// <summary>
        /// Create a build layout parameter
        /// </summary>
        /// <param name="bundleNameRemap">The map of internal bundle name to file name</param>
        public BuildLayoutParameters(Dictionary<string, string> bundleNameRemap)
        {
            m_BundleNameRemap = bundleNameRemap;
        }

        /// <summary>
        /// Create a build layout parameter
        /// </summary>
        /// <param name="bundleNameRemap">The map of internal bundle name to file name</param>
        /// <param name="contentCatalogData">Content Catalog used in the build</param>
        public BuildLayoutParameters(Dictionary<string, string> bundleNameRemap, ContentCatalogData contentCatalogData)
        {
            m_BundleNameRemap = bundleNameRemap;
            m_contentCatalogData = contentCatalogData;
        }

        /// <summary>
        /// A map of the internal AssetBundle name to the file name
        /// </summary>
        public Dictionary<string, string> BundleNameRemap
        {
            get => m_BundleNameRemap;
            set => m_BundleNameRemap = value;
        }

        /// <summary>
        /// Calculated hash of the build layout object
        /// </summary>
        public string BuildResultHash
        {
            get => m_contentCatalogData?.BuildResultHash;
        }

        /// <summary>
        /// The hash of the associated catalog
        /// </summary>
        public string CatalogHash
        {
            get => m_contentCatalogData?.LocalHash;
        }
    }
}
