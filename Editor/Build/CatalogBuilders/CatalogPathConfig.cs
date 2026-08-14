namespace UnityEditor.AddressableAssets.Build.CatalogBuilders
{
    /// <summary>
    /// Configuration containing all paths used for building and loading content catalogs.
    /// </summary>
    public class CatalogPathConfig
    {
        /// <summary>
        /// The runtime load path for the local catalog.
        /// </summary>
        public string LoadPath { get; set; }

        /// <summary>
        /// The local build path where the catalog is written during build.
        /// </summary>
        public string BuildPath { get; set; }

        /// <summary>
        /// The build path for the remote catalog when remote catalog building is enabled.
        /// </summary>
        public string RemoteBuildPath { get; set; }

        /// <summary>
        /// The runtime load path for the remote catalog.
        /// </summary>
        public string RemoteLoadPath { get; set; }

        /// <summary>
        /// The filename of the catalog file at runtime.
        /// </summary>
        public string RuntimeCatalogFilename { get; set; }

        /// <summary>
        /// The versioned catalog filename, typically including the player version for cache busting.
        /// </summary>
        public string VersionedCatalogFileName { get; set; }
    }
}
