namespace UnityEditor.AddressableAssets.Build.CatalogBuilders
{
    /// <summary>
    /// Configuration for bundling the content catalog into an AssetBundle.
    /// Used when the BundleLocalCatalog option is enabled in Addressables settings.
    /// </summary>
    public class CatalogBundleConfig
    {
        /// <summary>
        /// The folder path where temporary catalog assets are created during bundling.
        /// </summary>
        public string ConfigFolder { get; set; }

        /// <summary>
        /// The build target group for the catalog bundle.
        /// </summary>
        public BuildTargetGroup TargetGroup { get; set; }

        /// <summary>
        /// The build target platform for the catalog bundle.
        /// </summary>
        public BuildTarget Target { get; set; }
    }
}
