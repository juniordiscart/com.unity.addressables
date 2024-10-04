---
uid: addressables-api-load-content-catalog-async
---

# Manage catalogs at runtime

By default, the Addressables system manages the [catalog](build-content-catalogs.md) automatically at runtime. If you built your application with a remote catalog, the Addressables system automatically checks for a new catalog, and downloads the new version and loads it into memory. 

You can load additional catalogs at runtime. For example, you can load a catalog produced by a separate, compatible project to load Addressable assets built by that project. Refer to [Loading content from multiple projects](xref:addressables-multiple-projects) for more information.

If you want to change the default catalog update behavior of the Addressables system, you can disable the automatic check and check for updates manually. Refer to [Updating catalogs](#update-catalogs) for more information.

## Load additional catalogs

Use [`Addressables.LoadContentCatalogAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadContentCatalogAsync*) to load additional content catalogs, either from a hosting service or from the local file system. You need to supply the location of the catalog you want to load. After the operation to load the catalog is complete, you can call any Addressables loading functions using the keys in the new catalog.

If you give the catalog hash file at the same URL as the catalog, Addressables caches the secondary catalog. When the client application loads the catalog, it only downloads a new version of the catalog if the hash changes.

The hash file needs to be in the same location and have the same name as the catalog. The only difference to the path should be the extension.

`LoadContentCatalogAsync` comes with a parameter `autoReleaseHandle`. In order for the system to download a new remote catalog, any prior calls to `LoadContentCatalogAsync` that point to the catalog you want to load need to be released. Otherwise, the system picks up the content catalog load operation from the operation cache. If the cached operation is picked up, the new remote catalog isn't downloaded. If set to true, the parameter `autoReleaseHandle` makes sure that the operation doesn't stay in the operation cache after completing.

Once you load a catalog, you can't unload it. However, you can update a loaded catalog. You must release the operation handle for the operation that loaded the catalog before updating a catalog. Refer to [Updating catalogs](#update-catalogs) for more information.

In general, there is no reason to hold on to the operation handle after loading a catalog. You can release it automatically by setting the `autoReleaseHandle` parameter to true when loading a catalog, as shown in the following example: 

[!code-cs[sample](../Tests/Editor/DocExampleCode/MiscellaneousTopics.cs#doc_LoadAdditionalCatalog)]

> [!TIP]
> Use the [Catalog Download Timeout](xref:addressables-asset-settings) property in Addressables settings to specify a timeout for downloading catalogs.

## Update catalogs

If the catalog hash file is available, Addressables checks the hash when loading a catalog to check if the version at the provided URL is more recent than the cached version of the catalog. You can disable the default catalog check, and call the [`Addressables.UpdateCatalogs`](xref:UnityEngine.AddressableAssets.Addressables.UpdateCatalogs*) method when you want to update the catalog. If you loaded a catalog manually with [`LoadContentCatalogAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadContentCatalogAsync*), you must release the operation handle before you can update the catalog.

When you call the `UpdateCatalog` method, Unity blocks all other Addressable requests until the operation is complete. You can release the operation handle that `UpdateCatalog` returns immediately after the operation finishes, or set the `autoRelease` parameter to `true`.

If you call `UpdateCatalog` without providing a list of catalogs, Addressables checks all the loaded catalogs for updates. 

[!code-cs[sample](../Tests/Editor/DocExampleCode/MiscellaneousTopics.cs#doc_UpdateCatalog)]

You can also call [`Addressables.CheckForCatalogUpdates`](xref:UnityEngine.AddressableAssets.Addressables.CheckForCatalogUpdates*) directly to get the list of catalogs that have updates and then perform the update:

[!code-cs[sample](../Tests/Editor/DocExampleCode/MiscellaneousTopics.cs#doc_CheckCatalog)]

> [!IMPORTANT]
> If you update a catalog when you have already loaded content from the related AssetBundles, there might be conflicts between the loaded AssetBundles and the updated versions. Enable the [Unique Bundle Ids](xref:addressables-content-update-builds) option in Addressable settings to stop the possibility of bundle ID collisions at runtime. Enabling this option also means that more AssetBundles must typically be rebuilt when you perform a content update. Refer to [Content update builds](content-update-builds-overview.md) for more information. You can also unload any content and AssetBundles that must be updated, which can be a slow operation.
