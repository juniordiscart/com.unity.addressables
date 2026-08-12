# Introduction to loading Addressable assets

The [`Addressables`](xref:UnityEngine.AddressableAssets.Addressables) class provides methods to load Addressable assets. You can load assets one at a time or in batches. To identify the assets to load, you pass either a single key or a list of keys to the loading method. A key can be one of the following objects:

* **Address**: A string containing the address you assigned to the asset
* **Label**: A string containing a label assigned to one or more assets
* **AssetReference object**: An instance of [`AssetReference`](xref:UnityEngine.AddressableAssets.AssetReference)
* [`IResourceLocation`](xref:UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation) instance: An intermediate object that contains information to load an asset and its dependencies.

## How Addressables loads assets

When you call one of the asset loading methods, the Addressables system begins an asynchronous operation that carries out the following tasks:

1. Looks up the resource locations for the specified keys, except `IResourceLocation` keys.
1. Gathers the list of dependencies.
1. Downloads any remote AssetBundles that are required.
1. Loads the AssetBundles into memory.
1. Sets the [`Result`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Result) object of the operation to the loaded objects.
1. Updates the [`Status`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Status) of the operation and calls any [`Completed`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed) event listeners.

If the load operation succeeds, the `Status` is set to `Succeeded` and the loaded object or objects can be accessed from the [`Result`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Result) object.

If an error occurs, the exception is copied to the [`OperationException`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.OperationException) member of the operation object and the `Status` is set to `Failed`. By default, the exception isn't thrown as part of the operation. However, you can assign a handler function to the [`ResourceManager.ExceptionHandler`](xref:UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler) property to handle any exceptions. You can also enable the [Log Runtime Exceptions](xref:addressables-asset-settings) option in the Addressable system settings to record errors to the [Unity Console](xref:um-console).

When you call loading methods that load multiple Addressable assets, you can specify whether to abort the operation if any single load operation fails, or to load any assets it can. In both cases, the operation status is set to failed. Set the `releaseDependenciesOnFailure` parameter to `true` in the call to the loading method to abort the entire operation on any failure.

## Asynchronous loading

The Addressables API is asynchronous and returns an [`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle) to manage operation progress and completion.

Addressables is designed to be content location agnostic. The content might need to be downloaded first or use other methods that can take a long time. To force synchronous execution, refer to [Synchronous loading](SynchronousAddressables.md) for more information.

When loading an asset for the first time, the handle is complete after a minimum of one frame. If the content has already loaded, execution times might differ between the various asynchronous loading options. You can wait until the load has completed as follows:

* [Coroutine](xref:UnityEngine.Coroutine): Always delayed at a minimum of one frame before execution continues.
* [`Completed` callback](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed): A minimum of one frame if the content hasn't already loaded, otherwise the callback is invoked in the same frame.
* Awaiting [`AsyncOperationHandle.Task`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Task): A minimum of one frame if the content hasn't already loaded, otherwise the execution continues in the same frame.

[!code-cs[sample](../Tests/Editor/DocExampleCode/AsynchronousLoading.cs#doc_asyncload)]

## Additional resources

* [Asynchronous operation handles](AddressableAssetsAsyncOperationHandle.md)
* [Load assets](load-assets.md)