---
uid: addressables-async-operation-handling
---

# Wait for asynchronous loads to complete

Addressables uses asynchronous operations for tasks that require loading or downloading data, which prevents these operations from blocking your application's execution while they complete.

In contrast to a [synchronous operation](SynchronousAddressables.md), which doesn't return control until the result is available, an asynchronous operation returns control to the calling method almost immediately. However, the results might not be available until some time in the future.

When you call a method, such as [`LoadAssetAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetAsync*), it doesn't return the loaded assets directly. Instead, it returns an [`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle) object, which you can use to access the loaded assets when they become available.

You can use the following techniques to wait for the results of an asynchronous operation while allowing other scripts to continue processing:

* [Coroutines and `IEnumerator` loops](load-wait-asynchronous-coroutines.md)
* [Event based operation handling](load-wait-asynchronous-events.md)
* [Task based operation handling](load-wait-asynchronous-async-await.md)

> [!NOTE]
> You can block the current thread to wait for the completion of an asynchronous operation. Doing so can introduce performance problems and frame rate hitches. Refer to [Using operations synchronously](SynchronousAddressables.md) for more information.

## Release AsyncOperationHandle instances

Methods like [`LoadAssetsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync*) return [`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle) instances that give the results of the operation and a way to release both the results and the operation object itself.

You must keep the handle object for as long as you want to use the results. Depending on the situation, that might be one frame, until the end of a level, or even the lifetime of the application. Use the [`Addressables.Release`](xref:UnityEngine.AddressableAssets.Addressables.Release*) method to release operation handles and any associated Addressable assets.

Releasing an operation handle decrements the reference count of any assets loaded by the operation and invalidates the operation handle object itself. Refer to [Memory management](MemoryManagement.md) for more information about reference counting in the Addressables system.

If you don't need to use the results of an operation beyond a limited scope, you can release the handles immediately. Some Addressables methods, such as [`UnloadSceneAsync`](xref:UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync*) allow you to automatically release the operation handle when it's complete.

If an operation is unsuccessful, it's best practice to still release the operation handle. Unity releases any assets that it loaded during a failed operation, but releasing the handle still clears the handle's instance data. Some methods that load multiple assets, like [`LoadAssetsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync*), give you the option to either keep any successfully loaded assets, or to fail and release everything if any part of the load operation fails.

## Typed and typeless operation handles

Most `Addressables` methods that start an operation return a generic [`AsyncOperationHandle<T>`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle`1) struct, allowing type safety for the [`AsyncOperationHandle.Completed`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed) event and for the [`AsyncOperationHandle.Result`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Result) object. You can also use a non-generic [`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle) struct and convert between the two handle types as desired.

A runtime exception happens if you try to cast a non-generic handle to a generic handle of a wrong type. For example:

[!code-cs[sample](../Tests/Editor/DocExampleCode/OperationHandleTypes.cs#doc_ConvertTypes)]

## Additional resources

* [Wait for asynchronous loads with coroutines](load-wait-asynchronous-coroutines.md)
* [Wait for asynchronous loads with events](load-wait-asynchronous-events.md)
* [Wait for asynchronous loads with async and await](load-wait-asynchronous-async-await.md)