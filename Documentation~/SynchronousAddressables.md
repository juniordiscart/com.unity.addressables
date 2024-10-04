---
uid: synchronous-addressables
---

## Synchronous loading

Synchronous Addressables APIs help to mirror Unity asset loading workflows. `AsyncOperationHandles` have a method called `WaitForCompletion()` that forces the [asynchronous operation](AddressableAssetsAsyncOperationHandle.md) to complete and return the `Result` of the operation.

The result of `WaitForCompletion` is the `Result` of the asynchronous operation it's called on. If the operation fails, this returns `default(TObject)`.

You can get a `default(TObject)` for a result when the operation doesn't fail. Asynchronous operations that auto release their `AsyncOperationHandle` instances on completion are such cases.  `Addressables.InitializeAsync` and any API with a `autoReleaseHandle` parameter set to true return `default(TObject)` even if the operations succeeded.

## Performance considerations

Calling `WaitForCompletion` might have performance implications on your runtime when compared to `Resources.Load` or `Instantiate` calls directly. If the `AssetBundle` is local or has been downloaded before and cached, these performance hits are small.

All active asset load operations are completed when `WaitForCompletion` is called on any asset load operation, because of how Unity handles asynchronous operations. To avoid unexpected stalls, use `WaitForCompletion` when you known the current operation count, and the you want all active operations to complete synchronously.

Don't call `WaitForCompletion` on an operation that's going to fetch and download a remote `AssetBundle`.

## Synchronous loading example

```c#
void Start()
{
    //Basic use case of forcing a synchronous load of a GameObject
    var op = Addressables.LoadAssetAsync<GameObject>("myGameObjectKey");
    GameObject go = op.WaitForCompletion();

    //Do work...

    Addressables.Release(op);
}
```

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

Unity can't unload a scene synchronously. Calling `WaitForCompleted` on a scene unload doesn't unload the scene or any assets, and a warning is logged to the Console.

Because of limitations with scene integration on the main thread through the `SceneManager` API, you can lock the Unity Editor or Player when calling `WaitForCompletion` to load scenes. This issue happens when loading two scenes in succession, with the second scene load request having `WaitForCompletion` called from its `AsyncOperationHandle`.

Scene loading takes extra frames to fully integrate on the main thread, and `WaitForCompletion` locks the main thread, so you might have a situation where Addressables has been informed by the `SceneManager` that the first scene is fully loaded, even though it hasn't finished all operations. At this point, the scene is fully loaded, but the `SceneManager` attempts to call `UnloadUnusedAssets`, on the main thread, if the scene was loaded in `Single` mode. Then, the second scene load request locks the main thread with `WaitForCompletion`, but can't begin loading because `SceneManager` requires the `UnloadUnusedAssets` to complete before the next scene can begin loading.

To avoid this deadlock, either load successive scenes asynchronously, or add a delay between scene load requests.

Another issue is calling `WaitForCompletion` on an asynchronous operation during `Awake` when a scene is not yet fully loaded. This can block the main thread and prevent other asynchronous operations (i.e. unloading a bundle) in progress from completing. To avoid this deadlock, call `WaitForCompletion` during `Start` instead.

Note that Addressables has a callback registered to [`SceneManager.sceneUnloaded`](xref:UnityEngine.SceneManagement.SceneManager.sceneUnloaded(UnityEngine.Events.UnityAction`1<UnityEngine.SceneManagement.Scene>)) that will release any unloaded addressable scenes. This can trigger scene bundle unloading if no other scenes from the bundle are loaded.

## Custom operations

Addressables supports custom `AsyncOperation` instances which support unique implementations of `InvokeWaitForCompletion`. This method can be overridden to implement custom synchronous operations.

Custom operations work with `ChainOperation` and `GroupsOperation` instances. If you want to complete chained operations synchronously, make your custom operations implement `InvokeWaitForCompletion` and create a `ChainOperation` using your custom operations. Similarly, `GroupOperations` are well suited to make a collection of `AsyncOperations`, including custom operations, complete together.

Both `ChainOperation` and `GroupOperation` have their own implementations of `InvokeWaitForCompletion` that relies on the `InvokeWaitForCompletion` implementations of the operations they depend on.

## WebGL support

WebGL doesn't support `WaitForCompletion`. On WebGL, a web request loads all files. On other platforms, a web request gets started on a background thread and the main thread spins in a tight loop while waiting for the web request to finish. This is how Addressables does it for `WaitForCompletion` when a web request is used.

Because WebGL is single-threaded, the tight loop blocks the web request and the operation is never allowed to finish. If a web request finishes the same frame it was created, then `WaitForCompletion` wouldn't have any issue. However, this isn't guaranteed.
