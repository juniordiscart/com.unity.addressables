# Addressable AssetBundle memory considerations

When you organize Addressable groups and AssetBundles, you must make trade-offs between the size and the number of AssetBundles you create and load. Fewer, larger AssetBundles can minimize the total memory usage of AssetBundles. However, using many small AssetBundles can minimize the peak memory usage because Unity can quickly unload assets and AssetBundles.

The size of an AssetBundle on disk isn't the same as its size at runtime. However, you can use the disk size as a guide to the memory overhead of the AssetBundles in a build. You can get AssetBundle size and other information to help analyze AssetBundles from the [Build Layout Report](BuildLayoutReport.md).

For information on AssetBundle memory overhead, refer to [Optimizing AssetBundles](xref:um-asset-bundles-optimization).

## TypeTree management

You can [remove TypeTrees](xref:um-asset-bundles-optimization) to optimize memory. However it is recommended to only use this approach for content that you can rebuild each time you release a new Player. This is a suitable optimization for Addressable content that you include directly with the player build. In that case you must always rebuild local Addressable groups before building a new Player.

If your project [distributes content remotely](remote-content-intro.md) it becomes more complicated. The ability to add new content after the Player has already shipped means that you exactly match the version of Unity and that there are no serialization changes in your code and in the code of all the packages that contribute content. If you use multiple player versions, updates, and versions of Unity it can become difficult to manage matching the AssetBundles with compatible Player builds because you are giving up Unity's tolerance to load AssetBundles with slightly mismatched Players. You might not find the memory savings from disabling TypeTrees to be worth the trouble.

## Loading AssetBundle dependencies

Loading an Addressable asset also loads all the AssetBundles containing its dependencies. An AssetBundle dependency happens when an asset in one AssetBundle references an asset in another AssetBundle. For example, when a material references a texture. For more information refer to [Asset and AssetBundle dependencies](AssetDependencies.md).

Addressables calculates dependencies between AssetBundles at the AssetBundle level. If one asset references an object in another AssetBundle, then the entire AssetBundle has a dependency on that AssetBundle. This means that even if you load an asset in the first AssetBundle that has no dependencies of its own, the second AssetBundle is still loaded into memory.

For example,`BundleA` contains Addressable assets `RootAsset1` and `RootAsset2`. `RootAsset2` references `DependencyAsset3`, which is in `BundleB`. Even though `RootAsset1` has no reference to `BundleB`, `BundleB` is still a dependency of `RootAsset1` because `RootAsset1` is in `BundleA`, which has a reference to `BundleB`.

To avoid loading more bundles than you need, keep the dependencies between AssetBundles as simple as possible. You can use the [Build Layout Report](xref:addressables-build-layout-report) to check dependencies.

## Additional resources

* [Optimizing AssetBundles](xref:um-asset-bundles-optimization)
* [Asset and AssetBundle dependencies](AssetDependencies.md)