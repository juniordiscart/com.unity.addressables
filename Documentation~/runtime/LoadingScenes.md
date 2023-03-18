---
uid: addressables-loading-scenes
---

# Loading Scenes

Use the [Addressables.LoadSceneAsync] method to load an Addressable Scene asset by address or other Addressable key object. 

> [!NOTE]
> [Addressables.LoadSceneAsync] uses the Unity Engine [SceneManager.LoadSceneAsync] method internally. API that affects the behaviour of [SceneManager.LoadSceneAsync] will most likely affect [Addressables.LoadSceneAsync] in the same way, for example [Application.backgroundLoadingPriority].

The remaining parameters of the method correspond to those used with the [SceneManager.LoadSceneAsync] method:

* __loadMode__: whether to add the loaded Scene into the current Scene or to unload and replace the current Scene. 
* __loadSceneParameters__: includes loadMode in addition to localPhysicsMode, used when loading the Scene to specify whether a 2D and/or 3D physics Scene should be created
* __activateOnLoad__: whether to activate the scene as soon as it finishes loading or to wait until you call the SceneInstance object's [ActivateAsync] method. Corresponds to the [AsyncOperation.allowSceneActivation] option. Defaults to true.
* __priority__: the priority of the AsyncOperation used to load the Scene. Corresponds to the [AsyncOperation.priority] option. Defaults to 100.

> [!WARNING]
> Setting the `activateOnLoad` parameter to false blocks the AsyncOperation queue, including the loading of any other Addressable assets, until you activate the scene. To activate the scene, call the [ActivateAsync] method of the [SceneInstance] returned by [LoadSceneAsync]. See [AsyncOperation.allowSceneActivation] for additional information.

The following example loads a scene additively. The Component that loads the Scene, stores the operation handle and uses it to unload and release the Scene when the parent GameObject is destroyed.

[!code-cs[sample](../../Tests/Editor/DocExampleCode/LoadScene.cs#doc_Load)]

<!--
``` csharp
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class LoadSceneByAddress : MonoBehaviour
{
    public string key;
    private AsyncOperationHandle<SceneInstance> loadHandle;

    void Start()
    {
      loadHandle = Addressables.LoadSceneAsync(key, LoadSceneMode.Additive);
      Destroy(this, 12); // Trigger unload to release Scene
    }

  void OnDestroy() {
    Addressables.UnloadSceneAsync(loadHandle);
  }
}
```
-->

See the [Scene loading project] in the [Addressables-Sample] repository for additional examples.

If you load a Scene with [LoadSceneMode.Single], the Unity runtime unloads the current Scene and calls [Resources.UnloadUnusedAssets]. See [Releasing Addressable assets] for more information.

> [!NOTE]
> In the Editor, you can always load scenes in the current project, even when they are packaged in a remote bundle that is not available and you set the Play Mode Script to __Use Existing Build__. The Editor loads the Scene using the asset database.

[ActivateAsync]: xref:UnityEngine.ResourceManagement.ResourceProviders.SceneInstance.ActivateAsync*
[Addressables.ClearDependencyCacheAsync]: xref:UnityEngine.AddressableAssets.Addressables.ClearDependencyCacheAsync*
[Addressables.DownloadDependenciesAsync]: xref:UnityEngine.AddressableAssets.Addressables.DownloadDependenciesAsync*
[Addressables.GetDownloadSizeAsync]: xref:UnityEngine.AddressableAssets.Addressables.GetDownloadSizeAsync*
[Addressables.InstantiateAsync]: xref:UnityEngine.AddressableAssets.Addressables.InstantiateAsync*
[Addressables.LoadAssetAsync]: xref:UnityEngine.AddressableAssets.Addressables.LoadAssetAsync*
[Addressables.LoadSceneAsync]: xref:UnityEngine.AddressableAssets.Addressables.LoadSceneAsync*
[Addressables.ReleaseInstance]: xref:UnityEngine.AddressableAssets.Addressables.ReleaseInstance*
[Addressables]: xref:UnityEngine.AddressableAssets.Addressables
[Application.backgroundLoadingPriority]: xref:UnityEngine.Application.backgroundLoadingPriority
[AssetReference]: xref:UnityEngine.AddressableAssets.AssetReference
[AssetReferences]: xref:addressables-asset-references
[AsyncOperation.priority]: xref:UnityEngine.AsyncOperation.priority
[cache settings]: xref:UnityEngine.Cache
[Check Duplicate Bundle Dependencies]: AnalyzeTool.md#check-duplicate-bundle-dependencies
[GetDownloadStatus]: xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.GetDownloadStatus*
[Instantiate]: xref:UnityEngine.Object.Instantiate*
[InstantiateAsync]: xref:UnityEngine.AddressableAssets.Addressables.InstantiateAsync*
[InstantiationParameters]: xref:UnityEngine.ResourceManagement.ResourceProviders.InstantiationParameters
[IResourceLocation]: xref:UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation
[AsyncOperationHandle]: xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle-1
[AsyncOperationHandle.Task]: xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle-1.Task.html
[Completed callback]: xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle-1.Completed.html
[Coroutine]: xref:UnityEngine.Coroutine*
[HideFlags.DontUnloadUnusedAsset]: xref:UnityEngine.HideFlags.DontUnloadUnusedAsset
[LoadAssetAsync]: xref:UnityEngine.AddressableAssets.Addressables.LoadAssetAsync*
[LoadAssetsAsync]: xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync*
[LoadResourceLocationsAsync]: xref:UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync*
[LoadSceneMode.Single]: xref:UnityEngine.SceneManagement.LoadSceneMode.Single
[Memory Management]: xref:addressables-memory-management
[merge mode]: xref:UnityEngine.AddressableAssets.Addressables.MergeMode
[Object.DontDestroyOnLoad]: xref:UnityEngine.Object.DontDestroyOnLoad(UnityEngine.Object)
[OperationException]: xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.OperationException
[Operations]: xref:addressables-async-operation-handling
[PrimaryKey]: xref:UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation.PrimaryKey
[Releasing Addressable assets]: xref:addressables-unloading
[ResourceManager.Acquire]: xref:UnityEngine.ResourceManagement.ResourceManager.Acquire(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle)
[ResourceManager.CreateGenericGroupOperation]: xref:UnityEngine.ResourceManagement.ResourceManager.CreateGenericGroupOperation*
[Resources.UnloadUnusedAssets]: xref:UnityEngine.Resources.UnloadUnusedAssets
[Result]: xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Result
[SceneManager.LoadSceneAsync]: xref:UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(System.String,UnityEngine.SceneManagement.LoadSceneMode)
[Status]: xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Status
[UnityEngine.Caching]: xref:UnityEngine.Caching
[ResourceManager.ExceptionHandler]: xref:UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler
[Log Runtime Exceptions]: xref:addressables-asset-settings#diagnostics
[Console]: xref:Console
[Object.Instantiate]: xref:UnityEngine.Object.Instantiate*
[addresses]: xref:addressables-overview#asset-addresses
[labels]: xref:addressables-labels
[Completed]: xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed
[AsyncOperation.allowSceneActivation]: xref:UnityEngine.AsyncOperation.allowSceneActivation
[SceneInstance]: xref:UnityEngine.ResourceManagement.ResourceProviders.SceneInstance
[LoadSceneAsync]: xref:UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(System.String,UnityEngine.SceneManagement.LoadSceneMode)
[UnloadAsset]: xref:UnityEngine.Resources.UnloadAsset(UnityEngine.Object)
[Addressables.InstantiateAsync]: xref:UnityEngine.AddressableAssets.Addressables.InstantiateAsync*
[Scene loading project]: https://github.com/Unity-Technologies/Addressables-Sample/tree/master/Basic/Scene%20Loading
[Addressables-Sample]: https://github.com/Unity-Technologies/Addressables-Sample
[Synchronous Addressables]: xref:synchronous-addressables
[Unloading Addressables]: xref:addressables-unloading