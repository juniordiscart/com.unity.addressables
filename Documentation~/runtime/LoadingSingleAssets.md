---
uid: addressables-loading-single-asset
---

# Loading a single asset

Use the [LoadAssetAsync] method to load a single Addressable asset, typically with an address as the key:

[!code-cs[sample](../../Tests/Editor/DocExampleCode/LoadSingle.cs#doc_Load)]

<!--
``` csharp
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class LoadAddress : MonoBehaviour
{
  public string key;
  AsyncOperationHandle<GameObject> opHandle;

  public IEnumerator Start()
  {
      opHandle = Addressables.LoadAssetAsync<GameObject>(key);
      yield return opHandle;

      if (opHandle.Status == AsyncOperationStatus.Succeeded) {
          GameObject obj = opHandle.Result;
          Instantiate(obj, transform);
          GameObject.Destroy(gameObject, 12);
      }
  }

  void OnDestroy()
  {
      Addressables.Release(opHandle);
  }
}
```
-->

> [!NOTE]
> You can use a label or other type of key when you call [LoadAssetAsync], not just an address. However, if the key resolves to more than one asset, only the first asset found is loaded. For example, if you call this method with a label applied to several assets, Addressables returns whichever one of those assets that happens to be located first.

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