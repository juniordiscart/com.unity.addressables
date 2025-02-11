---
uid: addressables-unloading
---

# Unload Addressable assets

The Addressables system uses reference counting to check whether an asset is in use. This means that you must release every asset that you load or instantiate when you're finished with it. Refer to [Memory Management](MemoryManagement.md) for more information.

When you unload a scene, the AssetBundle it belongs to is unloaded. This unloads assets associated with the scene, including any GameObjects moved from the original scene to a different scene.

Unity automatically calls `UnloadUnusedAssets` when it loads a scene using the [`LoadSceneMode.Single`](xref:UnityEngine.SceneManagement.LoadSceneMode.Single) mode. To prevent the scene and its assets from being unloaded, keep a reference to the scene load operation handle until you want to unload the scene manually. To do this, use [`ResourceManager.Acquire`](xref:UnityEngine.ResourceManagement.ResourceManager.Acquire(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle)) on the load operation handle.

>[!IMPORTANT]
>Conventional methods of preserving the assets such as [`Object.DontDestroyOnLoad`](xref:UnityEngine.Object.DontDestroyOnLoad(UnityEngine.Object)) or [`HideFlags.DontUnloadUnusedAsset`](xref:UnityEngine.HideFlags.DontUnloadUnusedAsset) don't work.

Individual Addressables and their operation handles that you load separately from the scene aren't released. You must call [`Addressables.Release`](xref:UnityEngine.AddressableAssets.Addressables.Release*) to free these assets. The exception to this is that any Addressable assets that you instantiate using [`Addressables.InstantiateAsync`](xref:UnityEngine.AddressableAssets.Addressables.InstantiateAsync*) with `trackHandle` set to true, the default, are automatically released.
