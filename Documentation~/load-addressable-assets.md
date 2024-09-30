# Load Addressable assets introduction

The [`Addressables`](xref:UnityEngine.AddressableAssets.Addressables) class provides methods to load Addressable assets. You can load assets one at a time or in batches. To identify the assets to load, you pass either a single key or a list of keys to the loading method. A key can be one of the following objects:

* **Address**: A string containing the address you assigned to the asset
* **Label**: A string containing a label assigned to one or more assets
* **AssetReference object**: An instance of [`AssetReference`](xref:UnityEngine.AddressableAssets.AssetReference)
* [`IResourceLocation`](xref:UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation) instance: An intermediate object that contains information to load an asset and its dependencies.

## How Addressables loads assets

When you call one of the asset loading methods, the Addressables system begins an asynchronous operation that carries out the following tasks:

1. Looks up the resource locations for the specified keys, except `IResourceLocation` keys.
1. Gathers the list of dependencies
1. Downloads any remote AssetBundles that are required
1. Loads the AssetBundles into memory
1. Sets the [`Result`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Result) object of the operation to the loaded objects
1. Updates the [`Status`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Status) of the operation and calls any [`Completed`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed) event listeners

If the load operation succeeds, the `Status` is set to `Succeeded` and the loaded object or objects can be accessed from the [`Result`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Result) object.

If an error occurs, the exception is copied to the [`OperationException`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.OperationException) member of the operation object and the `Status` is set to `Failed`. By default, the exception isn't thrown as part of the operation. However, you can assign a handler function to the [`ResourceManager.ExceptionHandler`](xref:UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler) property to handle any exceptions. You can also enable the [Log Runtime Exceptions](xref:addressables-asset-settings) option in the Addressable system settings to record errors to the [Unity Console](xref:Console).

When you call loading methods that load multiple Addressable assets, you can specify whether to abort the entire operation if any single load operation fails or whether to load the operation any assets it can. In both cases, the operation status is set to failed. Set the `releaseDependenciesOnFailure` parameter to `true` in the call to the loading method to abort the entire operation on any failure.

Refer to [Operations](xref:addressables-async-operation-handling) for more information about asynchronous operations and writing asynchronous code in Unity scripts.

## Match loaded assets to their keys

The order that Unity loads individual assets isn't necessarily the same as the order of the keys in the list you pass to the loading method.

If you need to associate an asset in a combined operation with the key used to load it, you can perform the operation in the following steps:

1. Load the [`IResourceLocation`](xref:UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation) instances with the list of asset keys.
1. Load the individual assets using their `IResourceLocation` instances as keys.

The `IResourceLocation` object contains the key information so you can, for example, keep a dictionary to correlate the key to an asset. When you call a loading method, such as [`LoadAssetsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync*), the operation first looks up the [`IResourceLocation`](xref:UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation) instances that correspond to a key and then uses that to load the asset. When you load an asset using an `IResourceLocation`, the operation skips the first step, so performing the operation in two steps doesn't add significant additional work.

The following example loads the assets for a list of keys and inserts them into a dictionary by their address ([`PrimaryKey`](xref:UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation.PrimaryKey)). The example first loads the resource locations for the specified keys. When that operation is complete, it loads the asset for each location, using the `Completed` event to insert the individual operation handles into the dictionary. The operation handles can be used to instantiate the assets, and, when the assets are no longer needed, to release them.

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadLocation.cs#doc_Load)]

The loading method creates a group operation with [`ResourceManager.CreateGenericGroupOperation`](xref:UnityEngine.ResourceManagement.ResourceManager.CreateGenericGroupOperation*). This allows the method to continue after all the loading operations have finished. In this case, the method dispatches a `Ready` event to notify other scripts that the loaded data can be used.

