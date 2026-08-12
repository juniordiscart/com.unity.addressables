# Load assets by location

When you load an Addressable asset by address, label, or AssetReference, the Addressables system first looks up the resource locations for the assets and uses the [`IResourceLocation`](xref:UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation) instances to download the required AssetBundles and any dependencies. To perform the asset load operation, get the `IResourceLocation` objects with [`LoadResourceLocationsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync*) and then use those objects as keys to load or instantiate the assets.

`IResourceLocation` objects contain the information needed to load one or more assets.

The `LoadResourceLocationsAsync` method never fails. If it can't resolve the specified keys to the locations of any assets, it returns an empty list. You can restrict the types of asset locations returned by the method by specifying a specific type in the `type` parameter.

The following example loads locations for all assets labeled with `knight` or `villager`:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadLocation.cs#doc_LoadLocations)]

## Load locations of sub objects

Unity generates locations for `SubObjects` at runtime to reduce the size of the content catalogs and improve runtime performance. When you call [`LoadResourceLocationsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync*) with the key of an asset with sub objects and don't specify a type, then the method generates `IResourceLocation` instances for all the sub objects and the main object. If you don't specify which sub object to use for an AssetReference that points to an asset with sub objects, then the system generates `IResourceLocation` instances for every sub object.

For example, if you load the locations for an FBX asset, with the address, `myFBXObject`, you might get locations for three assets: a GameObject, a mesh, and a material. If you specify the type in the address, `myFBXObject[Mesh]`, you only get the mesh object. You can also specify the type using the `type` parameter of the `LoadResourceLocationsAsync` method.

## Match loaded assets to their keys

The order that Unity loads individual assets isn't necessarily the same as the order of the keys in the list you pass to the loading method.

If you need to associate an asset in a combined operation with the key used to load it, you can perform the operation in the following steps:

1. Load the [`IResourceLocation`](xref:UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation) instances with the list of asset keys.
1. Load the individual assets using their `IResourceLocation` instances as keys.

The `IResourceLocation` object contains the key information so you can, for example, keep a dictionary to correlate the key to an asset. When you call a loading method, such as [`LoadAssetsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync*), the operation first looks up the [`IResourceLocation`](xref:UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation) instances that correspond to a key and then uses that to load the asset. When you load an asset using an `IResourceLocation`, the operation skips the first step, so performing the operation in two steps doesn't add significant additional work.

The following example loads the assets for a list of keys and inserts them into a dictionary by their address ([`PrimaryKey`](xref:UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation.PrimaryKey)). The example first loads the resource locations for the specified keys. When that operation is complete, it loads the asset for each location, using the `Completed` event to insert the individual operation handles into the dictionary. You can use the operation handles to instantiate the assets, and release the assets when they're no longer needed.

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadLocation.cs#doc_Load)]

The loading method creates a group operation with [`ResourceManager.CreateGenericGroupOperation`](xref:UnityEngine.ResourceManagement.ResourceManager.CreateGenericGroupOperation*). This allows the method to continue after all the loading operations have finished. In this case, the method dispatches a `Ready` event to notify other scripts that the loaded data can be used.

## Additional resources

* [`IResourceLocation` API reference](xref:UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation)
* [Load assets](load-assets.md)
* [Load scenes](LoadingScenes.md)
* [Load AssetBundles](LoadingAssetBundles.md)
* [Load assets from multiple projects](MultiProject.md)