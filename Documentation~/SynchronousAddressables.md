---
uid: synchronous-addressables
---

## Synchronous loading

You can wait for an operation to finish without yielding, waiting for an event, or using `async await` by calling an operation's [`WaitForCompletion`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion*) method. This method blocks the current program execution thread while it waits for the operation to finish before continuing in the current scope.

Avoid calling `WaitForCompletion` on operations that can take a significant amount of time, such as those that must download data. Calling `WaitForCompletion` can cause frame hitches and interrupt UI responsiveness.

The following example loads a prefab asset by address, waits for the operation to complete, and then instantiates the prefab:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadSynchronously.cs#doc_LoadSynchronously)]

The result of `WaitForCompletion` is the `Result` of the asynchronous operation it's called on. If the operation fails, this returns `default(TObject)`.

You can get a `default(TObject)` for a result when the operation doesn't fail. Asynchronous operations that auto release their `AsyncOperationHandle` instances on completion are such cases. `Addressables.InitializeAsync` and any API with a `autoReleaseHandle` parameter set to true return `default(TObject)` even if the operations succeeded.

## Performance considerations

Calling `WaitForCompletion` might have performance implications on your runtime when compared to `Resources.Load` or `Instantiate` calls directly. If the AssetBundle is local or has been downloaded before and cached, these performance hits are small.

All active asset load operations are completed when `WaitForCompletion` is called on any asset load operation, because of how Unity handles asynchronous operations. To avoid unexpected stalls, use `WaitForCompletion` when you known the current operation count, and the you want all active operations to complete synchronously.

Don't call `WaitForCompletion` on an operation that's going to fetch and download a remote `AssetBundle`.

## Deadlocks caused by scene limitations

Unity can't complete scene loading synchronously. Calling `WaitForCompletion` on an operation returned from [`Addressables.LoadSceneAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadSceneAsync*) doesn't completely load the scene, even if `activateOnLoad `is set to `true`. It waits for dependencies and assets to complete but the scene activation must be done asynchronously.

This can be done using the `sceneHandle`, or by the [`AsyncOperation`](xref:UnityEngine.AsyncOperation) from `ActivateAsync` on the `SceneInstance`:

```c#
IEnumerator LoadScene(string myScene)
{
    var sceneHandle = Addressables.LoadSceneAsync(myScene, LoadSceneMode.Additive);
    SceneInstance sceneInstance = sceneHandle.WaitForCompletion();
    yield return sceneInstance.ActivateAsync();

    //Do work... the scene is now complete and integrated
}
```

Unity can't unload a scene synchronously. Calling [`WaitForCompletion`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion) on a scene unload doesn't unload the scene or any assets, and a warning is logged to the Console.

Because of limitations with scene integration on the main thread through the `SceneManager` API, you can lock the Unity Editor or Player when calling `WaitForCompletion` to load scenes. This issue happens when loading two scenes in succession, with the second scene load request having `WaitForCompletion` called from its `AsyncOperationHandle`.

Scene loading takes extra frames to fully integrate on the main thread, and `WaitForCompletion` locks the main thread, so you might have a situation where `SceneManager` informs Unity that the first scene is fully loaded, even though it hasn't finished all operations. At this point, the scene is fully loaded, but the `SceneManager` attempts to call `UnloadUnusedAssets`, on the main thread, if the scene was loaded in `Single` mode. Then, the second scene load request locks the main thread with `WaitForCompletion`, but can't begin loading because `SceneManager` requires the `UnloadUnusedAssets` to complete before the next scene can begin loading.

To avoid this deadlock, either load successive scenes asynchronously, or add a delay between scene load requests.

Another issue is calling `WaitForCompletion` on an asynchronous operation during `Awake` when a scene isn't fully loaded. This can block the main thread and prevent other asynchronous operations (such as unloading an AssetBundle) in progress from completing. To avoid this deadlock, call `WaitForCompletion` during `Start` instead.

Note that Addressables has a callback registered to [`SceneManager.sceneUnloaded`](xref:UnityEngine.SceneManagement.SceneManager.sceneUnloaded(UnityEngine.Events.UnityAction`1<UnityEngine.SceneManagement.Scene>)) that releases any unloaded Addressable scenes. This can trigger scene AssetBundle unloading if no other scenes from the AssetBundle are loaded.

## Custom operations

Addressables supports custom `AsyncOperation` instances which support unique implementations of [`InvokeWaitForCompletion`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase`1.InvokeWaitForCompletion). This method can be overridden to implement custom synchronous operations.

Custom operations work with `ChainOperation` and `GroupsOperation` instances. If you want to complete chained operations synchronously, make your custom operations implement `InvokeWaitForCompletion` and create a `ChainOperation` using your custom operations. Similarly, `GroupOperations` are well suited to make a collection of `AsyncOperations`, including custom operations, complete together.

Both [`ChainOperation`](xref:UnityEngine.ResourceManagement.ResourceManager.CreateChainOperation*) and [`GroupOperation`](xref:UnityEngine.ResourceManagement.ResourceManager.CreateGroupOperation*) have their own implementations of `InvokeWaitForCompletion` that relies on the `InvokeWaitForCompletion` implementations of the operations they depend on.

## WebGL support

WebGL doesn't support `WaitForCompletion`. On WebGL, a web request loads all files. On other platforms, a web request gets started on a background thread and the main thread spins in a tight loop while waiting for the web request to finish. This is how Addressables does it for `WaitForCompletion` when a web request is used.

Because WebGL is single-threaded, the tight loop blocks the web request and the operation is never allowed to finish. If a web request finishes the same frame it was created, then `WaitForCompletion` wouldn't have any issue. However, this isn't guaranteed.

## Additional resources

* [`WaitForCompletion` API reference](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion*)
* [Wait for asynchronous loads to complete](AddressableAssetsAsyncOperationHandle.md)
* [Monitor wait operations](load-monitor-wait-operations.md)