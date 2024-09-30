---
uid: addressables-loading-scenes
---

# Load a scene

Use the [`Addressables.LoadSceneAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadSceneAsync*) method to load an Addressable scene asset by address or other Addressable key object.

`Addressables.LoadSceneAsync` uses the Unity Engine [`SceneManager.LoadSceneAsync`](xref:UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(System.String,UnityEngine.SceneManagement.LoadSceneMode)) method internally. APIs that affect the behavior of `SceneManager.LoadSceneAsync` also affect `Addressables.LoadSceneAsync` in the same way, such as [`Application.backgroundLoadingPriority`](xref:UnityEngine.Application.backgroundLoadingPriority).

The remaining parameters of the `Addressables.LoadSceneAsync` method correspond to those used with the `SceneManager.LoadSceneAsync` method:

* `loadMode`: Whether to add the loaded scene into the current scene, or to unload and replace the current scene.
* `loadSceneParameters`: Includes `loadMode` and `localPhysicsMode`. This is used when loading the scene to specify whether to create a 2D or 3D physics scene.
* `activateOnLoad`: Whether to activate the scene as soon as it finishes loading or to wait until you call the `SceneInstance` object's [`ActivateAsync`](xref:UnityEngine.ResourceManagement.ResourceProviders.SceneInstance.ActivateAsync*) method. Corresponds to the [`AsyncOperation.allowSceneActivation`](xref:UnityEngine.AsyncOperation.allowSceneActivation) option. Defaults to true.
* `priority`: The priority of the `AsyncOperation` used to load the Scene. Corresponds to the [`AsyncOperation.priority`](xref:UnityEngine.AsyncOperation.priority) option. Defaults to 100.

> [!WARNING]
> Setting the `activateOnLoad` parameter to false blocks the `AsyncOperation` queue, including the loading of any other Addressable assets, until you activate the scene. To activate the scene, call the [`ActivateAsync`](xref:UnityEngine.ResourceManagement.ResourceProviders.SceneInstance.ActivateAsync*) method of the [`SceneInstance`](xref:UnityEngine.ResourceManagement.ResourceProviders.SceneInstance) returned by [`LoadSceneAsync`](xref:UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(System.String,UnityEngine.SceneManagement.LoadSceneMode)). Refer to [AsyncOperation.allowSceneActivation](xref:UnityEngine.AsyncOperation.allowSceneActivation) for additional information.

The following example loads a scene additively. The component that loads the scene stores the operation handle and uses it to unload and release the scene when the parent GameObject is destroyed.

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadScene.cs#doc_Load)]

Refer to the [Scene loading project](https://github.com/Unity-Technologies/Addressables-Sample/tree/master/Basic/Scene%20Loading) in the Addressables samples repository for additional examples.

If you load a Scene with [`LoadSceneMode.Single`](xref:UnityEngine.SceneManagement.LoadSceneMode.Single), the Unity runtime unloads the current Scene and calls [`Resources.UnloadUnusedAssets`](xref:UnityEngine.Resources.UnloadUnusedAssets). Refer to [Releasing Addressable assets](xref:addressables-unloading) for more information.

> [!NOTE]
> In the Editor, you can always load scenes in the current project, even when they're packaged in a remote bundle that's not available and you set the Play Mode Script to __Use Existing Build__. The Editor loads the scene using the Asset Database.

## Use Addressables in a scene

If a scene is Addressable, you can use Addressable assets in the scene just like any other assets. You can place prefabs and other assets in the scene, and assign assets to component properties. If you use an asset that isn't Addressable, that asset becomes an implicit dependency of the scene and the build system packs it in the same AssetBundle as the scene when you make a content build. Addressable assets are packed into their own AssetBundles according to the group they're in.

> [!NOTE]
> Implicit dependencies used in more than one place can be duplicated in multiple AssetBundles and in the built-in scene data. Use the [Build Layout Report](xref:addressables-build-layout-report) to identify and resolve unwanted asset duplication resulting from your project content organization.

If a scene isn't Addressable, then any Addressable assets you add directly to the scene hierarchy become implicit dependencies and Unity includes copies of those assets in the built-in scene data even if they also exist in an Addressable group. The same is true for any assets, such as materials assigned to a component on a GameObject in the scene.

In custom component classes, you can use [`AssetReference`](xref:UnityEngine.AddressableAssets.AssetReference) fields to allow the assignment of Addressable assets in non-Addressable scenes. Otherwise, you can use [addresses](xref:addressables-overview) and [labels](Labels.md) to load assets at runtime from a script. You must load an `AssetReference` in code regardless of if the scene is Addressable.
