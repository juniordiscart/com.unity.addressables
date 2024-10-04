using System.Collections.Generic;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    public interface IBuildLayoutParameters : IContextObject
    {
        Dictionary<string, string> BundleNameRemap { get; set; }

        string BuildResultHash { get; }

        string CatalogHash { get; }
    }

    public class BuildLayoutParameters : IBuildLayoutParameters
    {
        private Dictionary<string, string> m_BundleNameRemap;
        private ContentCatalogData m_contentCatalogData;

        public BuildLayoutParameters(Dictionary<string, string> bundleNameRemap)
        {
            m_BundleNameRemap = bundleNameRemap;
        }

        public BuildLayoutParameters(Dictionary<string, string> bundleNameRemap, ContentCatalogData contentCatalogData)
        {
            m_BundleNameRemap = bundleNameRemap;
            m_contentCatalogData = contentCatalogData;
        }
        public Dictionary<string, string> BundleNameRemap
        {
            get => m_BundleNameRemap;
            set => m_BundleNameRemap = value;
        }

        public string BuildResultHash
        {
            get => m_contentCatalogData?.BuildResultHash;
        }

        public string CatalogHash
        {
            get => m_contentCatalogData?.LocalHash;
        }
    }
}
