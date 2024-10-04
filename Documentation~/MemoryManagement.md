---
uid: addressables-memory-management
---

# Memory management overview

The Addressables system keeps a reference count of every item it loads to manage the memory it uses to load assets and bundles.

When Unity loads an Addressable, the system increments the reference count. When Unity releases the asset, the system decrements the reference count. When the reference count of an Addressable returns to zero, it can be unloaded. When you explicitly load an Addressable asset, you must also release the asset when you're finished using it.

## Memory leaks

To avoid memory leaks, where assets remain in memory after they're no longer needed, mirror every call to a load method with a call to a release method. You can release an asset with a reference to the asset instance itself or with the result handle that the original load operation returns.

However, Unity doesn't unload released assets from memory immediately, as the memory that an asset uses isn't freed until the AssetBundle it belongs to is also unloaded.

AssetBundles have their own reference count, and the system treats them like Addressables with the assets they contain as dependencies. When you load an asset from a bundle, the bundle's reference count increases and when you release the asset, the bundle reference count decreases. When a bundle's reference count returns to zero, that means none of the assets contained in the bundle are still in use. Unity then unloads the bundle and all the assets contained in it from memory.

Use the [Profiler module](ProfilerModule.md) to monitor your loaded content. The module displays when assets and their dependencies are loaded and unloaded.

## Memory clearance

If an asset is no longer referenced, indicated by the released status and disabled text in the [Profiler module](ProfilerModule.md), this doesn't mean that Unity unloaded that asset. A common applicable scenario involves multiple assets in an AssetBundle. For example:

* You have three assets (`tree`, `tank`, and `cow`) in an AssetBundle (`stuff`).
* When `tree` loads, the Profiler displays a single ref-count for `tree`, and one for `stuff`.
* Later, when `tank` loads, the Profiler displays a single ref-count for both `tree` and `tank`, and two ref-counts for the `stuff` AssetBundle.
* If you release `tree`, its ref-count becomes zero, and the blue bar goes away.

In this example, the `tree` asset isn't unloaded at this point. You can load an AssetBundle, or its partial contents, but you can't unload part of an AssetBundle. No asset in `stuff` unloads until the AssetBundle is unloaded.

## Avoid asset churn

Asset churn happens if you release an object that's the last item in an AssetBundle, and then immediately reload either that asset or another asset in the bundle.

For example, if you have two materials, `boat` and `plane` that share a texture, `cammo`, which is in its own AssetBundle. Level 1 uses `boat` and level 2 uses `plane`. As you exit level 1 Unity releases `boat`, and immediately loads `plane`. When Unity releases `boat`, Addressables unloads texture `cammo`. Then, when Unity load `plane`, Addressables immediately reloads `cammo`.

You can use the [Profiler module](ProfilerModule.md) to help detect asset churn by monitoring asset loading and unloading.