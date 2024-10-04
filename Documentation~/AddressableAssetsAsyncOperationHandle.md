---
uid: addressables-async-operation-handling
---
# Asynchronous operation handles

Many tasks in the Addressables need to load or download information before they can return a result. To avoid blocking program execution, Addressables implements such tasks as asynchronous operations.

In contrast to a [synchronous operation](SynchronousAddressables.md), which doesn’t return control until the result is available, an asynchronous operation returns control to the calling method almost immediately. However, the results might not be available until some time in the future. 

When you call a method, such as [`LoadAssetAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetAsync*), it doesn't return the loaded assets directly. Instead, it returns an [`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle) object, which you can use to access the loaded assets when they become available.

You can use the following techniques to wait for the results of an asynchronous operation while allowing other scripts to continue processing:

* [Coroutines and `IEnumerator` loops](#coroutine-and-ienumerator-operation-handling)
* [Event based operation handling](#event-based-operation-handling)
* [Task based operation handling](#task-based-operation-handling)

> [!NOTE]
> You can block the current thread to wait for the completion of an asynchronous operation. Doing so can introduce performance problems and frame rate hitches. Refer to [Using operations synchronously](#use-operations-synchronously) for more information.

## Release AsyncOperationHandle instances

Methods like [`LoadAssetsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync*) return [`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle) instances that give the results of the operation and a way to release both the results and the operation object itself. 

You must keep the handle object for as long as you want to use the results. Depending on the situation, that might be one frame, until the end of a level, or even the lifetime of the application. Use the [`Addressables.Release`](xref:UnityEngine.AddressableAssets.Addressables.Release*) method to release operation handles and any associated Addressable assets.

Releasing an operation handle decrements the reference count of any assets loaded by the operation and invalidates the operation handle object itself. Refer to [Memory management](MemoryManagement.md) for more information about reference counting in the Addressables system.

If you don’t need to use the results of an operation beyond a limited scope, you can release the handles immediately. Some Addressables methods, such as [`UnloadSceneAsync`](xref:UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync*) allow you to automatically release the operation handle when it's complete.

If an operation is unsuccessful, you should still release the operation handle. Addressables releases any assets that it loaded during a failed operation, but releasing the handle still clears the handle’s instance data. Some methods which load multiple assets, like [`LoadAssetsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync*), give you the option to either keep any assets that it loaded, or to fail and release everything if any part of the load operation failed.

## Coroutine and IEnumerator operation handling

[`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle) implements the [`IEnumerator`](xref:System.Collections.IEnumerator) interface and continues iteration until the operation is complete. 

In a coroutine, you can yield the operation handle to wait for the next iteration. When complete, the execution flow continues to the following statements. You can implement the [MonoBehaviour `Start`](https://docs.unity3d.com/ScriptReference/MonoBehaviour.Start.html) method as a coroutine, which is a good way to have a GameObject load and instantiate the assets it needs.

The following script loads a prefab as a child of its GameObject using a `Start` method in a coroutine. It yields the `AsyncOperationHandle` until the operation finishes and then uses the same handle to instantiate the prefab.

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadWithIEnumerator.cs#doc_LoadWithIEnumerator)]

You can't cancel [`Addressables.LoadAssetsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync*) once started. However, releasing the handle before it has finished decrements the handle reference count and automatically releases it when the load is complete.

Refer to the Unity User Manual documentation on [Coroutines](xref:Coroutines) for more information.

### Group operations in a coroutine

To perform several operations before moving on to the next step in your game logic, such as to load prefabs and other assets before you start a level, you can combine them with a single call to the [`Addressables.LoadAssetsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync*) method, if all the operations load assets. 

The `AsyncOperationHandle` for this method works the same as [`LoadAssetAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetAsync*). You can yield the handle in a coroutine to wait until all the assets in the operation load. You can also pass a callback method to `LoadAssetsAsync` and the operation calls that method when it finishes loading a specific asset. Refer to [Loading multiple assets](load-assets.md#load-multiple-assets) for an example.

You can also use the [`ResourceManager.CreateGenericGroupOperation`](xref:UnityEngine.ResourceManagement.ResourceManager.CreateGenericGroupOperation*) to create a group operation that completes when all its members finish.

## Event based operation handling

You can add a delegate function to the [`Completed`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed) event of an [`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle). The operation calls the delegate function when it's finished.

The following script performs the same function as the example in [coroutine and IEnumerator operation handling](#coroutine-and-ienumerator-operation-handling), but uses an event delegate instead of a coroutine:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadWithEvent.cs#doc_LoadWithEvent)]

The handle instance passed to the event delegate is the same as that returned by the original method call. You can use either to access the results and status of the operation and to release the operation handle and loaded assets.

## Task-based operation handling

[`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle) provides a [`Task`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Task) object that you can use with the C# `async` and `await` keywords to sequence code that calls asynchronous methods and handles the results.

The following example loads Addressable assets using a list of keys. The differences between this task-based approach and the coroutine or event-based approaches are in the signature of the calling method. This method must include the `async` keyword and use of the `await` keyword with the operation handle’s `Task` property. The calling method, `Start` in this case, suspends operation while the task finishes. Execution then resumes and the example instantiates all the loaded prefabs in a grid pattern.

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadWithTask.cs#doc_LoadWithTask)]

> [!IMPORTANT]
> The [`AsyncOperationHandle.Task`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Task) property isn't available on the WebGL platform, which doesn't support multitasking.

When you use `Task`-based operation handling, you can use the C# `Task` class methods such as [`WhenAll`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.whenall) to control which operations you run in parallel and which you want to run in sequence. The following example illustrates how to wait for more than one operation to finish before moving onto the next task:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadWithTask.cs#doc_useWhenAll)]

## Use operations synchronously

You can wait for an operation to finish without yielding, waiting for an event, or using `async await` by calling an operation’s [`WaitForCompletion`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion*) method. This method blocks the current program execution thread while it waits for the operation to finish before continuing in the current scope.

Avoid calling `WaitForCompletion` on operations that can take a significant amount of time, such as those that must download data. Calling `WaitForCompletion` can cause frame hitches and interrupt UI responsiveness.

The following example loads a prefab asset by address, waits for the operation to complete, and then instantiates the prefab:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadSynchronously.cs#doc_LoadSynchronously)]

## Custom operations

To create a custom operation, extend the [`AsyncOperationBase`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase`1) class and override its virtual methods.

You can pass the derived operation to the [`ResourceManager.StartOperation`](xref:UnityEngine.ResourceManagement.ResourceManager.StartOperation*) method to start the operation and receive an [`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle) struct. The [`ResourceManager`](xref:UnityEngine.ResourceManagement.ResourceManager) registers operations started this way.

### Execute a custom operation

The [`ResourceManager`](xref:UnityEngine.ResourceManagement.ResourceManager) invokes the [`AsyncOperationBase.Execute`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase`1.Execute*) method for the custom operation once the optional dependent operation completes.

### Completion handling

When the custom operation completes, call [`AsyncOperationBase.Complete`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase`1.Complete*) on the custom operation object. You can call this in the [`Execute`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase`1.Execute*) method or defer it to outside the call. `AsyncOperationBase.Complete` notifies the `ResourceManager` that the operation has finished. `The ResourceManager` invokes the associated [`AsyncOperationHandle.Completed`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed) events for the relevant instances of the custom operation.

### Terminate the custom operation

`ResourceManager` invokes the [`AsyncOperationBase.Destroy`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase`1.Destroy*) method for your custom operation when the operation [`AsyncOperationBase.ReferenceCount`]( xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase`1.DecrementReferenceCount*) reaches zero. `AsyncOperationBase.ReferenceCount` is decreased when the [`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle) that references it is released using [`Addressables.Release`](xref:UnityEngine.AddressableAssets.Addressables.Release*) or when [`AsyncOperationBase.DecrementReferenceCount`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase`1.DecrementReferenceCount*) is called by a custom operation internally. `AsyncOperationBase.Destroy` is where you should release any memory or resources associated with your custom operation.


## Typed and typeless operation handles

Most `Addressables` methods that start an operation return a generic [`AsyncOperationHandle<T>`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle`1) struct, allowing type safety for the [`AsyncOperationHandle.Completed`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed) event and for the [`AsyncOperationHandle.Result`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Result) object. You can also use a non-generic [`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle) struct and convert between the two handle types as desired.

A runtime exception happens if you try to cast a non-generic handle to a generic handle of a wrong type. For example:

[!code-cs[sample](../Tests/Editor/DocExampleCode/OperationHandleTypes.cs#doc_ConvertTypes)]

## Report operation progress

`AsyncOperationHandle` has the following methods that you can use to monitor and report the progress of the operation:

* [`GetDownloadStatus`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.GetDownloadStatus*): Returns a [`DownloadStatus`](xref:UnityEngine.ResourceManagement.AsyncOperations.DownloadStatus) struct. This struct contains information about how many bytes have been downloaded and how many bytes still need to be downloaded. [`DownloadStatus.Percent`](xref:UnityEngine.ResourceManagement.AsyncOperations.DownloadStatus.Percent) reports the percentage of bytes downloaded.
* [`AsyncOperationHandle.PercentComplete`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.PercentComplete): Returns an equally weighted total percentage of all the sub-operations that are complete. For example, if an operation has five sub-operations, each of them represents 20% of the total. The value doesn't factor in the amount of data that must be downloaded by the individual sub-operations.

For example, if you call [`Addressables.DownloadDependenciesAsync`](xref:UnityEngine.AddressableAssets.Addressables.DownloadDependenciesAsync*) and five AssetBundles need to be downloaded, `GetDownloadStatus` tells you what percentage of the total number of bytes for all sub-operations has been downloaded. `PercentComplete` tells you what percentage of the number of operations had finished, regardless of their size.

If you call [`LoadAssetAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetAsync*), and one bundle has to be downloaded before an asset can be loaded from it, the download percentage might be misleading. The values obtained from `GetDownloadStatus` reach 100% before the operation finishes, because the operation has additional sub-operations to conduct. The value of `PercentComplete` is 50% when the download sub-operation is finished and 100% when the actual load into memory is complete.