---
uid: addressables-unloading
---


# Unloading Addressable assets

The Addressables system uses reference counting to determine whether an asset is in use - as a result, you must release every asset that you load or instantiate when you're done with it. See [Memory Management] for more information.

When you unload a Scene, the AssetBundle it belongs to is unloaded. This unloads assets associated with the Scene, including any GameObjects moved from the original Scene to a different Scene.

Unity automatically calls `UnloadUnusedAssets` when it loads a Scene using the [LoadSceneMode.Single] mode. To prevent the Scene and its assets from being unloaded, maintain a reference to the scene load operation handle until the Scene should be unloaded manually. You can do this by using [ResourceManager.Acquire] on the load operation handle. Conventional methods of preserving the assets such as [Object.DontDestroyOnLoad] or [HideFlags.DontUnloadUnusedAsset] will not work.

Individual Addressables and their operation handles that you loaded separately from the Scene are not released. You must call [Resources.UnloadUnusedAssets] or [UnloadAsset] to free these assets. (The exception to this is that any Addressable assets that you instantiated using [Addressables.InstantiateAsync] with `trackHandle` set to true, the default, are automatically released.)

[Memory Management]: xref:addressables-memory-management
[LoadSceneMode.Single]: xref:UnityEngine.SceneManagement.LoadSceneMode.Single
[ResourceManager.Acquire]: xref:UnityEngine.ResourceManagement.ResourceManager.Acquire(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle)
[Object.DontDestroyOnLoad]: xref:UnityEngine.Object.DontDestroyOnLoad(UnityEngine.Object)
[HideFlags.DontUnloadUnusedAsset]: xref:UnityEngine.HideFlags.DontUnloadUnusedAsset
[Resources.UnloadUnusedAssets]: xref:UnityEngine.Resources.UnloadUnusedAssets
[UnloadAsset]: xref:UnityEngine.Resources.UnloadAsset(UnityEngine.Object)
[Addressables.InstantiateAsync]: xref:UnityEngine.AddressableAssets.Addressables.InstantiateAsync*