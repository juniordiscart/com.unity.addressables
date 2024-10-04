# Load assets by location

When you load an Addressable asset by address, label, or AssetReference, the Addressables system first looks up the resource locations for the assets and uses the [`IResourceLocation`](xref:UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation) instances to download the required AssetBundles and any dependencies. To perform the asset load operation, get the `IResourceLocation` objects with [`LoadResourceLocationsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync*) and then use those objects as keys to load or instantiate the assets.

`IResourceLocation` objects contain the information needed to load one or more assets.

The `LoadResourceLocationsAsync` method never fails. If it can't resolve the specified keys to the locations of any assets, it returns an empty list. You can restrict the types of asset locations returned by the method by specifying a specific type in the `type` parameter.

The following example loads locations for all assets labeled with `knight` or `villager`:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadLocation.cs#doc_LoadLocations)]

## Load locations of sub-objects

Unity generates locations for `SubObjects` at runtime to reduce the size of the content catalogs and improve runtime performance. When you call [`LoadResourceLocationsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync*) with the key of an asset with sub-objects and don't specify a type, then the method generates `IResourceLocation` instances for all the sub-objects and the main object. Likewise, if you don't specify which sub-object to use for an AssetReference that points to an asset with sub-objects, then the system generates `IResourceLocation` instances for every sub-object.

For example, if you load the locations for an FBX asset, with the address, `myFBXObject`, you might get locations for three assets: a GameObject, a mesh, and a material. If, instead, you specify the type in the address, `myFBXObject[Mesh]`, you only get the mesh object. You can also specify the type using the `type` parameter of the `LoadResourceLocationsAsync` method.
