using System.ComponentModel;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace UnityEngine.AddressableAssets.ResourceProviders
{
    /// <summary>
    /// Content catalog provider for JSON-format catalogs.
    /// </summary>
    [DisplayName("Json Catalog Provider")]
    public class JsonCatalogProvider : ContentCatalogProvider
    {
        /// <summary>
        /// Constructor for this provider.
        /// </summary>
        /// <param name="resourceManagerInstance">The resource manager to use.</param>
        public JsonCatalogProvider(ResourceManager resourceManagerInstance) : base(resourceManagerInstance) { }

        /// <inheritdoc />
        public override string CatalogExtension => ".json";

        /// <inheritdoc />
        protected override IResourceLocation CreateInnerCatalogLocation(string idToLoad, ProviderLoadRequestOptions opts)
        {
            var loc = new ResourceLocationBase(idToLoad, idToLoad,
                typeof(JsonAssetProvider).FullName,
                typeof(JsonContentCatalogData));
            loc.Data = opts;
            return loc;
        }

        /// <inheritdoc />
        protected override ContentCatalogData ParseBundledCatalog(TextAsset textAsset)
        {
            return JsonUtility.FromJson<JsonContentCatalogData>(textAsset.text);
        }
    }
}
