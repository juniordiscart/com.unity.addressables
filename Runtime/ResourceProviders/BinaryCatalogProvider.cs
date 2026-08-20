using System;
using System.ComponentModel;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace UnityEngine.AddressableAssets.ResourceProviders
{
    /// <summary>
    /// Content catalog provider for binary-format catalogs.
    /// </summary>
    [DisplayName("Binary Catalog Provider")]
    public class BinaryCatalogProvider : ContentCatalogProvider
    {

        /// <summary>
        /// Constructor for this provider.
        /// </summary>
        /// <param name="resourceManagerInstance">The resource manager to use.</param>
        public BinaryCatalogProvider(ResourceManager resourceManagerInstance) : base(resourceManagerInstance)  { }

        /// <inheritdoc />
        public override string CatalogExtension => ".bin";

        /// <inheritdoc />
        protected override IResourceLocation CreateInnerCatalogLocation(string idToLoad, ProviderLoadRequestOptions opts)
        {
            var loc = new ResourceLocationBase(idToLoad, idToLoad,
                BinaryContentCatalogData.kBinaryAssetProviderId,
                typeof(BinaryContentCatalogData));
            loc.Data = opts;
            return loc;
        }

        /// <inheritdoc />
        protected override ContentCatalogData ParseBundledCatalog(TextAsset textAsset)
        {
            throw new NotSupportedException("Loading a binary catalog from a local AssetBundle is not supported.");
        }
    }
}
