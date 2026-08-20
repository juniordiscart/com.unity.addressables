---
uid: addressables-memory-management
---

# Managing Addressable asset memory

The Addressables system keeps a reference count of every item it loads to manage the memory it uses to load assets, AssetBundles, and content directories.

When Unity loads an Addressable asset, the system increments the reference count. When Unity releases the asset, the system decrements the reference count. When the reference count of an Addressable returns to zero, it can be unloaded. When you explicitly load an Addressable asset, you must also release the asset when you're finished using it.

## Memory leaks

To avoid memory leaks, where assets remain in memory after they're no longer needed, mirror every call to a load method with a call to a release method. You can release an asset with a reference to the asset instance itself or with the result handle that the original load operation returns.

Use the [Addressables Profiler module](ProfilerModule.md) to monitor loaded content. The module displays when assets and their dependencies are loaded and unloaded.

You can also read the current reference count of an individual operation at runtime through [`AsyncOperationHandle.ReferenceCount`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.ReferenceCount). This is useful for logging or leak diagnostics on devices where the Profiler module isn't available. The property is read-only and returns 0 for an invalid or released handle.

### AssetBundle reference counting

Unity doesn't unload released assets from memory immediately, because the memory that an asset uses isn't freed until the AssetBundle it belongs to is also unloaded.

AssetBundles have their own reference count, and the system treats them like Addressables with the assets they contain as dependencies. When you load an asset from an AssetBundle, the AssetBundle's reference count increases and when you release the asset, the AssetBundle reference count decreases. When an AssetBundle's reference count returns to zero, that means none of the assets contained in the AssetBundle are in use. Unity then unloads the AssetBundle and all the assets contained in it from memory.

### Content directory reference counting

Unity keeps reference counts for assets loaded from a content directory and increments or decrements the count when the content directory is loaded or released. Content directories can unload individual assets when their reference count reaches zero, so releasing one asset doesn't depend on the others being released.

However, Addressables doesn't keep a reference count for the content directory itself. It registers a content directory the first time you load an asset from it and keeps it registered for the lifetime of the Addressables system. Addressables only unregisters all content directories when the Addressables system shuts down, and doesn't automatically unregister the content directory when the reference counts of its assets reach zero.

## Memory clearance

If an asset is no longer referenced, indicated by the **Released** status in the [Addressables Profiler module](ProfilerModule.md)), this means that Unity might not have unloaded the asset from memory. A common scenario involves multiple assets in an AssetBundle. For example:

* You have three assets (`tree`, `tank`, and `cow`) in an AssetBundle (`stuff`).
* When `tree` loads, the Profiler displays a single ref-count for `tree`, and one for `stuff`.
* Later, when `tank` loads, the Profiler displays a single ref-count for both `tree` and `tank`, and two ref-counts for the `stuff` AssetBundle.
* If you release `tree`, its ref-count becomes zero, and the blue bar goes away.

In this example, the `tree` asset isn't unloaded at this point. You can load an AssetBundle, or its partial contents, but you can't unload part of an AssetBundle. No asset in `stuff` unloads until the AssetBundle is unloaded.

## Avoid asset churn

Asset churn happens if you release an object that's the last item in an AssetBundle, and then immediately reload either that asset or another asset in the AssetBundle. Asset churn can affect the performance of your application negatively if you unload and reload assets in succession. You should keep assets for as long as possible before unloading them.

For example, if you have two materials, `boat` and `plane` that share a texture, `cammo`, which is in its own AssetBundle. Level 1 uses `boat` and level 2 uses `plane`. As you exit level 1 Unity releases `boat`, and immediately loads `plane`. When Unity releases `boat`, Addressables unloads texture `cammo`. Then, when Unity load `plane`, Addressables immediately reloads `cammo`.

You can use the [Addressables Profiler module](ProfilerModule.md) to help detect asset churn by monitoring asset loading and unloading.

## TypeTree management

You can [remove TypeTrees](xref:um-asset-bundles-optimization) to optimize memory. However, this approach is only recommended for content you can rebuild each time you release a new Player. This is a suitable optimization for Addressable content that you include directly with the Player build. In that case you must always rebuild local Addressable groups before building a new Player.

[Distributing content remotely](remote-content-intro.md) adds additional considerations. The ability to add new content after the Player has shipped means the content must exactly match the Unity Editor version the Player was built with. There must also be no serialization changes in your code and in the code of all the packages that contribute content. If you use multiple Player versions, updates, and Editor versions, it can become difficult to manage matching the AssetBundles or content directories with compatible Player builds. The memory savings from disabling TypeTrees might not be worth this extra trouble.

If you're using AssetBundles, you can enable the **[Extract TypeTree Data](AddressableAssetSettings.md#build)** setting to place TypeTree data in a separate file, which can reduce the file size of a build. To use this setting in an existing project you need to create a new Player build and rebuild all remote content because the setting adjusts any existing AssetBundles.

## Additional resources

* [Addressables Profiler module reference](ProfilerModule.md)
* [Memory in Unity](xref:um-performance-memory)