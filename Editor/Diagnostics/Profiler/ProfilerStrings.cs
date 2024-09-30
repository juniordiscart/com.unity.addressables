#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal class ProfilerStrings
    {
        public const string NotLoadedAssetHelpText = "This is an asset that has not been loaded either directly or as a reference, and is not in memory";
        public const string ReferencedAssetText = "This is an asset that is loaded due to being referenced. The following assets are directly loaded through Addressables and have a dependency on this asset.";
        public const string LoadedAssetText = "This is an asset that has been loaded through the Addressables API and is still in use. If you are not expecting to see this asset, you may be missing a Release for this asset load.";

        public const string BundleReferencingWithNoLoadedDependencies = "Bundle is loaded due to another bundle having a dependency on an Asset contained in this bundle, without that Asset loaded.";
        public const string BundleReferencingWithDependencies = "Bundle contains Assets that have been loaded through Addressables from a different bundle.";
        public const string BundleWithLoadedAddressableContent = "Bundle contains Assets that have been loaded through Addressables.";

        public const string LocalBundleUsingCRC = "This Bundle is loaded from the local file system with CRC enabled. This can impact performance, due to requiring the Asset Bundle to be fully decompressed. If the check is not nessecary, consider disabling CRC checks for this group.";
        public const string CachedBundleUsingCRC = "This Bundle is loaded from download cache with CRC enabled. This can impact performance. If the check is not nessecary, consider setting CRC to \"Enabled, excluding cache\".";
        public const string DownloadWithoutCaching = "This Bundle is being downloaded and not cached, and will be downloaded each time it is needed. Consider enabling caching to save it to disk for next time it is needed.";
        public const string DownloadWithoutCRC = "This Bundle is being downloaded and cached without a CRC check. Consider setting CRC to \"Enabled, excluding cache\". When caching the CRC is calculated and will not impact performance.";
#if !ENABLE_CACHING
        public const string DownloadWithoutCachingEnabled = "This Bundle is being downloaded with caching enabled, but platform does not support caching.";
#endif

        public const string MissingBuildReportLabelText = "Build report <b>{0}</b> not found. Build report data is required to display correct frame data.";
    }
}

#endif
