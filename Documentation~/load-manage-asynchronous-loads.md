# Manage asynchronous asset loading

By default, Addressables uses asynchronous loading to prevent tasks from blocking operations while they load or download data. You can control how Unity performs asynchronous loading through coroutines, events, and async and await tasks, or alternatively synchronously load assets.

|**Topic**|**Description**|
|---|---|
|**[Wait for asynchronous loads to complete](AddressableAssetsAsyncOperationHandle.md)**|Use `AsyncOperationHandle `to access asynchronous loaded objects.|
|**[Wait for asynchronous loads with coroutines](load-wait-asynchronous-coroutines.md)**|Use coroutines and `IEnumerator` to yield `AsyncOperationHandle` objects until operations complete.|
|**[Wait for asynchronous loads with events](load-wait-asynchronous-events.md)**|Use event delegates to handle asynchronous operations.|
|**[Wait for asynchronous loads with async and await](load-wait-asynchronous-async-await.md)**|Use C# async/await patterns to handle asynchronous operations.|
|**[Create a custom wait operation](load-custom-wait-operation.md)**|Create custom operations with proper execution, completion handling, and termination lifecycle management through `ResourceManager`.|
|**[Load assets synchronously](SynchronousAddressables.md)**|Load assets synchronously with `WaitForCompletion`, which blocks execution until operations finish.|
|**[Monitor wait operations](load-monitor-wait-operations.md)**|Track operation progress using `GetDownloadStatus` and `PercentComplete`.|

## Additional resources

* [Load Addressable assets](LoadingAddressableAssets.md)
* [Memory management](MemoryManagement.md)
* [Unity coroutines documentation](xref:um-coroutines)
* [`ResourceManager` API reference](xref:UnityEngine.ResourceManagement.ResourceManager)
* [`AsyncOperationHandle` API reference](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle)