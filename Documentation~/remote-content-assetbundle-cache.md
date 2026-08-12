# Remote content AssetBundle caching

By default, AssetBundles produced for an Addressables build are cached on the client device at the path defined by [`Application.dataPath`](xref:UnityEngine.Application.dataPath) after they're downloaded. Cached AssetBundles are only downloaded again if they're updated or if they're deleted from the cache. You can further control where the cache is stored with the [`Caching` API](xref:UnityEngine.Caching).

An updated catalog can exclude AssetBundle entries present in an older version of the catalog. When these entries are cached, their data is no longer needed on the device.

When you have unneeded cache data on the device, you can choose one of the following options:

* To delete the entire AssetBundle cache, use [`Caching.ClearCache`](xref:UnityEngine.Caching.ClearCache).
* To remove cache entries that are no longer referenced at any time, use [`Addressables.CleanBundleCache`](xref:UnityEngine.AddressableAssets.Addressables.CleanBundleCache*). You usually call this function after initializing Addressables, or after loading additional catalogs. For more information, refer to [Addressables initialization process](InitializeAsync.md)  and[Managing catalogs](LoadContentCatalogAsync.md).
* To automatically call [`Addressables.CleanBundleCache`](xref:UnityEngine.AddressableAssets.Addressables.CleanBundleCache*) after updating catalogs, use the parameter `autoCleanBundleCache` in [`Addressables.UpdateCatalogs`](xref:UnityEngine.AddressableAssets.Addressables.UpdateCatalogs(System.Boolean,System.Collections.Generic.IEnumerable{System.String},System.Boolean)). Refer to [Checking for content updates at runtime](ContentUpdateWorkflow.md) for an example script.

If you disable caching for a group, the remote AssetBundles produced for the group are stored in memory when they're downloaded until you unload them or the application exits. The next time the application loads the AssetBundle, Addressables downloads it again.

You can control how the AssetBundles produced by a group are cached with the __Use Asset Bundle Cache__ setting under [Advanced Options](ContentPackingAndLoadingSchema.md#advanced-options) in the Group Inspector settings.

For information about AssetBundle caching, refer to [AssetBundle compression formats](xref:um-asset-bundles-cache). The Addressables system sets the cache-related parameters of the [`UnityWebRequests`](xref:UnityEngine.Networking.UnityWebRequest) it uses to download Addressable AssetBundles based on the group settings.

Note that there are some limitations for WebGL AssetBundles. For more information, refer to [Technical limitations of WebGL](https://docs.unity3d.com/Manual/webgl-technical-overview.html).

## Additional resources

* [Enable remote content](remote-content-enable.md)
* [Define remote content profiles](remote-content-profiles.md)
* [Group Inspector settings reference](ContentPackingAndLoadingSchema.md)