---
uid: addressables-loading-asset-reference
---

# Loading an AssetReference

The [AssetReference] class has its own load method, [LoadAssetAsync].

[!code-cs[sample](../../Tests/Editor/DocExampleCode/LoadReference.cs#doc_Load)]

<!--
``` csharp
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class LoadFromReference : MonoBehaviour
{
  // Assign in Editor
  public AssetReference reference;

  // Start the load operation on start
  void Start() {
    AsyncOperationHandle handle = reference.LoadAssetAsync<GameObject>();
    handle.Completed += Handle_Completed;
  }

  // Instantiate the loaded prefab on complete
  private void Handle_Completed(AsyncOperationHandle obj) {
    if (obj.Status == AsyncOperationStatus.Succeeded) {
      Instantiate(reference.Asset, transform);
    } else {
      Debug.LogError("AssetReference failed to load.");
    }
    Destroy(gameobject, 12); // Destroy object to demonstrate release
  }

  // Release asset when parent object is destroyed
  private void OnDestroy() {
    reference.ReleaseAsset();
  }
}
```
-->

You can also use the AssetReference object as a key to the [Addressables.LoadAssetAsync] methods. If you need to spawn multiple instances of the asset assigned to an AssetReference, use [Addressables.LoadAssetAsync], which gives you an operation handle that you can use to release each instance.  

See [AssetReference] for more information about using AssetReferences.

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
[Releasing Addressable assets]: #releasing-addressable-assets
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