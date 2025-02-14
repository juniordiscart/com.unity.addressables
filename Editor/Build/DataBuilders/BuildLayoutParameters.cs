using System.Collections.Generic;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    /// <summary>
    /// Build Layout Parameters
    /// </summary>
    public interface IBuildLayoutParameters : IContextObject
    {
        /// <summary>
        /// Map of the internal bundle name to the file name on disk
        /// </summary>
        Dictionary<string, string> BundleNameRemap { get; set; }

        /// <summary>
        /// The resulting hash of content in the build
        /// </summary>
        string BuildResultHash { get; }

        /// <summary>
        /// The hash of the content catalog
        /// </summary>
        string CatalogHash { get; }
    }

    /// <summary>
    /// Concrete implementation of build layout parameters
    /// </summary>
    public class BuildLayoutParameters : IBuildLayoutParameters
    {
        private Dictionary<string, string> m_BundleNameRemap;
        private ContentCatalogData m_contentCatalogData;

        /// <summary>
        /// Create a new BuildLayoutParameter.
        /// </summary>
        /// <param name="bundleNameRemap">Map of internal name to file name</param>
        public BuildLayoutParameters(Dictionary<string, string> bundleNameRemap)
        {
            m_BundleNameRemap = bundleNameRemap;
        }

        /// <summary>
        /// Create a new BuildLayoutParameter.
        /// </summary>
        /// <param name="bundleNameRemap">Map of internal name to file name</param>
        /// <param name="contentCatalogData">The data for the content catalog</param>
        public BuildLayoutParameters(Dictionary<string, string> bundleNameRemap, ContentCatalogData contentCatalogData)
        {
            m_BundleNameRemap = bundleNameRemap;
            m_contentCatalogData = contentCatalogData;
        }

        /// <summary>
        /// Mapping of internal bundle name to file name
        /// </summary>
        public Dictionary<string, string> BundleNameRemap
        {
            get => m_BundleNameRemap;
            set => m_BundleNameRemap = value;
        }

        /// <summary>
        /// Hash of the build contents
        /// </summary>
        public string BuildResultHash
        {
            get => m_contentCatalogData?.BuildResultHash;
        }

        /// <summary>
        /// Hash of the content catalog
        /// </summary>
        public string CatalogHash
        {
            get => m_contentCatalogData?.LocalHash;
        }
    }
}
