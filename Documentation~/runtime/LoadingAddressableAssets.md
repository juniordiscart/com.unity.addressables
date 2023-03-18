---
uid: addressables-api-load-asset-async
---

# Loading Addressable assets

The [Addressables] class provides several methods for loading Addressable assets. You can load assets one at a time or in batches. To identify the assets to load, you pass either a single key or a list of keys to the loading function. A key can be one of the following objects:

* Address: a string containing the address you assigned to the asset
* Label: a string containing a label assigned to one or more assets
* AssetReference object: an instance of [AssetReference]
* [IResourceLocation] instance: an intermediate object that contains information to load an asset and its dependencies. 

When you call one of the asset loading functions, the Addressables system begins an asynchronous operation that carries out the following tasks:

1. Looks up the resource locations for the specified keys (except IResourceLocation keys)
2. Gathers the list of dependencies
3. Downloads any remote AssetBundles that are required
4. Loads the AssetBundles into memory
5. Sets the [Result] object of the operation to the loaded objects
6. Updates the [Status] of the operation and calls any  [Completed] event listeners

If the load operation succeeds, the Status is set to Succeeded and the loaded object or objects can be accessed from the [Result] object.

If an error occurs, the exception is copied to the [OperationException] member of the operation object and the Status is set to Failed. By default, the exception is not thrown as part of the operation. However, you can assign a handler function to the [ResourceManager.ExceptionHandler] property to handle any exceptions. Additionally, you can enable the [Log Runtime Exceptions] option in your Addressable system settings to record errors to the Unity [Console]. 

When you call loading functions that can load multiple Addressable assets, you can specify whether the entire operation should abort if any single load operation fails or whether the operation should load any assets it can. In both cases, the operation status is set to failed. (Set the `releaseDependenciesOnFailure` parameter to true in the call to the loading function to abort the entire operation on any failure.)

See [Operations] for more information about asynchronous operations and writing asynchronous code in Unity scripts.

### Correlating loaded assets to their keys

When you load multiple assets in one operation, the order in which individual assets are loaded is not necessarily the same as the order of the keys in the list you pass to the loading function.

If you need to associate an asset in a combined operation with the key used to load it, you can perform the operation in two steps:

1. Load the [IResourceLocation] instances with the list of asset keys.
2. Load the individual assets using their IResourceLocation instances as keys.

The IResourceLocation object contains the key information so you can, for example, keep a dictionary to correlate the key to an asset. Note that when you call a loading function, such as [LoadAssetsAsync], the operation first looks up the [IResourceLocation] instances that correspond to a key and then uses that to load the asset. When you load an asset using an IResourceLocation, the operation skips the first step. Thus, performing the operation in two steps does not add significant additional work.

The following example loads the assets for a list of keys and inserts them into a dictionary by their address ([PrimaryKey]). The example first loads the resource locations for the specified keys. When that operation is complete, it loads the asset for each location, using the Completed event to insert the individual operation handles into the dictionary. The operation handles can be used to instantiate the assets, and, when the assets are no longer needed, to release them.

[!code-cs[sample](../../Tests/Editor/DocExampleCode/LoadLocation.cs#doc_Load)]

<!--
``` csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class LoadWithLocation : MonoBehaviour
{
    public Dictionary<string, AsyncOperationHandle<GameObject>> operationDictionary;
    public List<string> keys;
    public UnityEvent Ready;

    IEnumerator LoadAndAssociateResultWithKey(IList<string> keys) {
        if(operationDictionary == null)
            operationDictionary = new Dictionary<string, AsyncOperationHandle<GameObject>>();

        AsyncOperationHandle<IList<IResourceLocation>> locations 
            = Addressables.LoadResourceLocationsAsync(keys, 
                Addressables.MergeMode.Union, typeof(GameObject));

        yield return locations;

        var loadOps = new List<AsyncOperationHandle>(locations.Result.Count); 

        foreach (IResourceLocation location in locations.Result) {
            AsyncOperationHandle<GameObject> handle =
                Addressables.LoadAssetAsync<GameObject>(location);
            handle.Completed += obj => operationDictionary.Add(location.PrimaryKey, obj);
            loadOps.Add(handle);
        }

        yield return Addressables.ResourceManager.CreateGenericGroupOperation(loadOps, true);

        Ready.Invoke();
    }

    void Start() {
        Ready.AddListener(OnAssetsReady);
        StartCoroutine(LoadAndAssociateResultWithKey(keys));
    }

    private void OnAssetsReady() {
        float x = 0, z = 0;
        foreach (var item in operationDictionary) {
            Debug.Log($"{item.Key} = {item.Value.Result.name}");
            Instantiate(item.Value.Result,
                        new Vector3(x++ * 2.0f, 0, z * 2.0f),
                        Quaternion.identity, transform);
            if (x > 9) {
                x = 0;
                z++;
            }
        }
    }

    private void OnDestroy() {
        foreach(var item in operationDictionary) {
            Addressables.Release(item.Value);
        }
    }
}
```
-->

Note that the loading function creates a group operation with [ResourceManager.CreateGenericGroupOperation]. This allows the function to continue after all the loading operations have finished. In this case, the function dispatches a "Ready" event to notify other scripts that the loaded data can be used.

## Loading assets by location

When you load an Addressable asset by address, label, or AssetReference, the Addressables system first looks up the resource locations for the assets and uses these [IResourceLocation] instances to download the required AssetBundles and any dependencies. You can perform the asset load operation in two steps by first getting the IResourceLocation objects with [LoadResourceLocationsAsync] and then using those objects as keys to load or instantiate the assets.

[IResourceLocation] objects contain the information needed to load one or more assets. 

The [LoadResourceLocationsAsync] method never fails. If it cannot resolve the specified keys to the locations of any assets, it returns an empty list. You can restrict the types of asset locations returned by the function by specifying a specific type in the `type` parameter.

The following example loads locations for all assets labeled with "knight" or "villager":

[!code-cs[sample](../../Tests/Editor/DocExampleCode/LoadLocation.cs#doc_LoadLocations)]

<!--
```csharp
AsyncOperationHandle<IList<IResourceLocation>> handle 
    = Addressables.LoadResourceLocationsAsync(new string[]
    {
        "knight",
        "villager"
    }, Addressables.MergeMode.Union);

    yield return handle;

    //...

    Addressables.Release(handle);
```
-->

## Loading locations of subobjects 

Locations for SubObjects are generated at runtime to reduce the size of the content catalogs and improve runtime performance. When you call [LoadResourceLocationsAsync] with the key of an asset with subobjects and don't specify a type, then the function generates IResourceLocation instances for all of the subobjects as well as the main object (if applicable). Likewise, if you do not specify which subobject to use for an AssetReference that points to an asset with subobjects, then the system generates IResourceLocations for every subobject.

For example, if you load the locations for an FBX asset, with the address, "myFBXObject", you might get locations for three assets: a GameObject, a Mesh, and a Material. If, instead, you specified the type in the address, "myFBXObject[Mesh]", you would only get the Mesh object. You can also specify the type using the `type` parameter of the LoadResourceLocationsAsync function.

## Asynchronous Loading

The Addressables system API is asynchronous and returns an [AsyncOperationHandle] for use with managing operation progress and completion.
Addressables is designed to content location agnostic. The content may need to be downloaded first or use other methods that can take a long time. To force synchronous execution, See [Synchronous Addressables] for more information.

When loading an asset for the first time, the handle is done after a minimum of one frame. You can wait until the load has completed using different methods as shown below.
If the content has already been loaded, execution times may differ between the various asynchronous loading options shown below.
* [Coroutine]: Always be delayed at minimum of one frame before execution continues.
* [Completed callback]: Is a minimum of one frame if the content has not already been loaded, otherwise the callback is invoked in the same frame.
* Awaiting [AsyncOperationHandle.Task]: Is a minimum of one frame if the content has not already been loaded, otherwise the execution continues in the same frame.

[!code-cs[sample](../../Tests/Editor/DocExampleCode/AsynchronousLoading.cs#doc_asyncload)]

## Unloading Addressable assets

See [Unloading Addressables]. 

## Using Addressables in a Scene

If a Scene is itself Addressable, you can use Addressable assets in the scene just as you would any assets. You can place Prefabs and other assets in the Scene, assign assets to component properties, and so on. If you use an asset that is not Addressable, that asset becomes an implicit dependency of the Scene and the build system packs it in the same AssetBundle as the Scene when you make a content build. (Addressable assets are packed into their own AssetBundles according to the group they are in.)  

> [!NOTE]
> Implicit dependencies used in more than one place can be duplicated in multiple AssetBundles and in the built-in scene data. Use the [Check Duplicate Bundle Dependencies] rule in the Analyze tool to find unwanted duplication of assets.

If a Scene is NOT Addressable, then any Addressable assets you add directly to the scene hierarchy become implicit dependencies and Unity includes copies of those assets in the built-in scene data even if they also exist in an Addressable group. The same is true for any assets, such as Materials, assigned to a component on a GameObject in the scene. 

In custom component classes, you can use [AssetReference] fields to allow the assignment of Addressable assets in non-Addressable scenes. Otherwise, you can use [addresses] and [labels] to load assets at runtime from a script. Note that you must load an AssetReference in code whether or not the Scene is Addressable. 

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
[Check Duplicate Bundle Dependencies]: xref:addressables-analyze-tool#check-duplicate-bundle-dependencies
[GetDownloadStatus]: xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.GetDownloadStatus*
[Instantiate]: xref:UnityEngine.Object.Instantiate*
[InstantiateAsync]: xref:UnityEngine.AddressableAssets.Addressables.InstantiateAsync*
[InstantiationParameters]: xref:UnityEngine.ResourceManagement.ResourceProviders.InstantiationParameters
[IResourceLocation]: xref:UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation
[AsyncOperationHandle]: xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle
[AsyncOperationHandle.Task]: xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Task
[Completed callback]: xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed
[Coroutine]: xref:UnityEngine.Coroutine
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
