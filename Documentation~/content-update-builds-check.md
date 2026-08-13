# Create a script to check for content updates

You can use [`CheckForCatalogUpdates`](xref:UnityEngine.AddressableAssets.Addressables.CheckForCatalogUpdates*) to check whether there are new Addressables content updates. Use the following call to start the update:

```c#
public static AsyncOperationHandle<List<string>> CheckForCatalogUpdates(bool autoReleaseHandle=true)
{
    // Implementation for checking catalog updates
}
```

`List<string>` contains the list of modified locator IDs. You can filter this list to only update specific IDs, or pass it entirely into the [`UpdateCatalogs`](xref:UnityEngine.AddressableAssets.Addressables.UpdateCatalogs*) API.

If there's new content, you can either ask the user to perform the update through your application's UI, or perform it automatically.

The list of catalogs can be null and if so, the following script updates all catalogs that need an update:

```c#
public static AsyncOperationHandle<List<IResourceLocator>> UpdateCatalogs(IEnumerable<string> catalogs = null, bool autoReleaseHandle = true)
{
    // Implementation for updating catalogs
}
```

The return value is the list of updated locators.

You might also want to remove any AssetBundle cache entries that are no longer referenced. If so, use the following version of the `UpdateCatalogs` API where you can enable the additional parameter `autoCleanBundleCache` to remove any unneeded cache data:

```c#
public static AsyncOperationHandle<List<IResourceLocator>> UpdateCatalogs(bool autoCleanBundleCache, IEnumerable<string> catalogs = null, bool autoReleaseHandle = true)
{
    // Implementation for updating catalogs
}

```

For more information about AssetBundle caching, refer to [AssetBundle caching](RemoteContentDistribution.md).


## Additional resources

* [Content update build settings](content-update-build-settings.md)
* [Create an update build](builds-update-build.md)