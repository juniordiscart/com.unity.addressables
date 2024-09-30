# Check for content updates at runtime

You can add a custom script to periodically check whether there are new Addressables content updates. Use the following function call to start the update:

```c#
[public static AsyncOperationHandle\<List\<string\>\> CheckForCatalogUpdates(bool autoReleaseHandle = true)]
```

`List\<string\>` contains the list of modified locator IDs. You can filter this list to only update specific IDs, or pass it entirely into the UpdateCatalogs API.

If there is new content, you can either present the user with a button to perform the update, or do it automatically. It's up to you to make sure that stale Assets are released.

The list of catalogs can be null and if so, the following script updates all catalogs that need an update:

```c#
[public static AsyncOperationHandle\<List\<IResourceLocator\>\> UpdateCatalogs(IEnumerable\<string\> catalogs = null, bool autoReleaseHandle = true)]
```

The return value is the list of updated locators.

You might also want to remove any bundle cache entries that are no longer referenced as a result of updating the catalogs. If so, use this version of the `UpdateCatalogs` API instead where you can enable the additional parameter `autoCleanBundleCache` to remove any unneeded cache data:

```c#
[public static AsyncOperationHandle\<List\<IResourceLocator\>\> UpdateCatalogs(bool autoCleanBundleCache, IEnumerable\<string\> catalogs = null, bool autoReleaseHandle = true)]
```

Refer to [AssetBundle caching](xref:addressables-remote-content-distribution) for additional information about the bundle cache.

Refer to [Unique Bundle IDs setting](content-update-build-settings.md) for additional information about updating content at runtime.