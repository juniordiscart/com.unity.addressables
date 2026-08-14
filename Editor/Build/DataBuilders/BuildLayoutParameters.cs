using System;
using System.Collections.Generic;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
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

        /// <summary>
        /// Gets the result of the Addressables player build process.
        /// </summary>
        AddressablesPlayerBuildResult BuildResult { get; }
    }

    /// <summary>
    /// Concreate implementation for objects used in the build layout
    /// </summary>
    public class BuildLayoutParameters : IBuildLayoutParameters
    {
        private Dictionary<string, string> m_BundleNameRemap;
        private ContentCatalogData[] m_contentCatalogData;
        AddressablesPlayerBuildResult m_BuildResult;

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
            m_contentCatalogData = new ContentCatalogData[] { contentCatalogData };
        }

        /// <summary>
        /// Create a build layout parameter
        /// </summary>
        /// <param name="bundleNameRemap">The map of internal bundle name to file name</param>
        /// <param name="contentCatalogData">Content Catalog used in the build</param>
        public BuildLayoutParameters(Dictionary<string, string> bundleNameRemap, ContentCatalogData[] contentCatalogData)
        {
            m_BundleNameRemap = bundleNameRemap;
            m_contentCatalogData = contentCatalogData;
        }

        /// <summary>
        /// Create a build layout parameter
        /// </summary>
        /// <param name="bundleNameRemap">The map of internal bundle name to file name</param>
        /// <param name="contentCatalogData">Content Catalog used in the build</param>
        /// <param name="playerBuildResult">The build result for the Addressables content build</param>
        public BuildLayoutParameters(Dictionary<string, string> bundleNameRemap, ContentCatalogData[] contentCatalogData, AddressablesPlayerBuildResult playerBuildResult)
        {
            m_BundleNameRemap = bundleNameRemap;
            m_contentCatalogData = contentCatalogData;
            m_BuildResult = playerBuildResult;
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
        /// Gets or sets the result of the Addressables player build process.
        /// </summary>
        public AddressablesPlayerBuildResult BuildResult
        {
            get => m_BuildResult;
            set => m_BuildResult = value;
        }

        /// <summary>
        /// Calculated hash of the build layout object
        /// </summary>
        public string BuildResultHash
        {
            get
            {
                if (m_contentCatalogData?.Length == 0)
                {
                    return null;
                }
                if (m_contentCatalogData?.Length == 1)
                {
                    return m_contentCatalogData[0].BuildResultHash;
                }
                // sort the catalogs by location PrimaryKey to ensure deterministic hash
                Array.Sort(m_contentCatalogData, (a, b) => string.Compare(a.ProviderId, b.ProviderId));

                List<string> hashes = new List<string>();
                foreach (var catalog in m_contentCatalogData)
                {
                    hashes.Add(catalog.BuildResultHash);
                }
                return HashingMethods.Calculate(hashes).ToString();
            }
        }

        /// <summary>
        /// The hash of the associated catalog
        /// </summary>
        public string CatalogHash
        {
            get
            {
                if (m_contentCatalogData?.Length == 0)
                {
                    return null;
                }
                if (m_contentCatalogData?.Length == 1)
                {
                    return m_contentCatalogData[0].LocalHash;
                }
                // sort the catalogs by location PrimaryKey to ensure deterministic hash
                Array.Sort(m_contentCatalogData, (a, b) => string.Compare(a.ProviderId, b.ProviderId));

                List<string> hashes = new List<string>();
                foreach (var catalog in m_contentCatalogData)
                {
                    hashes.Add(catalog.LocalHash);
                }
                return HashingMethods.Calculate(hashes).ToString();
            }
        }
    }
}
