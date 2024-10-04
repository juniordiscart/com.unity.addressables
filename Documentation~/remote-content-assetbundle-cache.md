# Remote content AssetBundle caching

By default, AssetBundles produced for an Addressables build are cached on the client device after they are downloaded. Cached bundles are only downloaded again if they are updated or if they are deleted from the cache. 

An updated catalog can exclude bundle entries present in an older version of the catalog. When these entries are cached, their data is no longer needed on the device.

When you have unneeded cache data on the device, you can choose one of the following options:

* To delete the entire bundle cache, use [`Caching.ClearCache`](xref:UnityEngine.Caching.ClearCache).
* To remove cache entries that are no longer referenced at any time, use [`Addressables.CleanBundleCache`](xref:UnityEngine.AddressableAssets.Addressables.CleanBundleCache*). You usually call this function after initializing Addressables (see [Customizing Addressables initialization](xref:addressables-api-initialize-async)) or after loading additional catalogs (see [Managing catalogs at runtime](xref:addressables-api-load-content-catalog-async)).
* To automatically call [`Addressables.CleanBundleCache`](xref:UnityEngine.AddressableAssets.Addressables.CleanBundleCache*) after updating catalogs, use the parameter `autoCleanBundleCache` in [`Addressables.UpdateCatalogs`](xref:UnityEngine.AddressableAssets.Addressables.UpdateCatalogs(System.Boolean,System.Collections.Generic.IEnumerable{System.String},System.Boolean)). Refer to [Checking for content updates at runtime](xref:addressables-content-update-builds) for an example script.

If you disable caching for a group, the remote bundles produced for the group are stored in memory when they are downloaded until you unload them or the application exits. The next time the application loads the bundle, Addressables downloads it again.

You can control whether the bundles produced by a group are cached or not with the __Use Asset Bundle Cache__ setting under [Advanced Options](xref:addressables-content-packing-and-loading-schema) in the Group Inspector.

See [AssetBundle compression](xref:AssetBundles-Cache) for additional information about AssetBundle caching. The Addressables system sets the cache-related parameters of the [`UnityWebRequests`](xref:UnityEngine.Networking.UnityWebRequest) it uses to download Addressable bundles based on the group settings.

Note that there are some limitations for WebGL AssetBundles. For more information, see [Building and running a WebGL project](xref:webgl-building).