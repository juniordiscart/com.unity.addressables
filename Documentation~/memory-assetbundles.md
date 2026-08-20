# Addressable AssetBundle memory considerations

>[!IMPORTANT]
>The following workflow is only applicable if you're using [AssetBundles as the content build system](content-build-systems.md) for your project.

When you organize Addressable groups and AssetBundles, you must make trade-offs between the size and the number of AssetBundles you create and load. Fewer, larger AssetBundles can minimize the total memory usage of AssetBundles. However, using many small AssetBundles can minimize the peak memory usage because Unity can quickly unload assets and AssetBundles.

The size of an AssetBundle on disk isn't the same as its size at runtime. However, you can use the disk size as a guide to the memory overhead of the AssetBundles in a build. You can get AssetBundle size and other information to help analyze AssetBundles from the [Build Layout Report](BuildLayoutReport.md).

For information on AssetBundle memory overhead, refer to [Optimizing AssetBundles](xref:um-asset-bundles-optimization).

## Loading AssetBundle dependencies

Loading an Addressable asset also loads all the AssetBundles containing its dependencies. An AssetBundle dependency happens when an asset in one AssetBundle references an asset in another AssetBundle. For example, when a material references a texture. For more information refer to [Asset and AssetBundle dependencies](AssetDependencies.md).

Addressables calculates dependencies between AssetBundles at the AssetBundle level. If one asset references an object in another AssetBundle, then the entire AssetBundle has a dependency on that AssetBundle. This means that even if you load an asset in the first AssetBundle that has no dependencies of its own, the second AssetBundle is still loaded into memory.

For example,`BundleA` contains Addressable assets `RootAsset1` and `RootAsset2`. `RootAsset2` references `DependencyAsset3`, which is in `BundleB`. Even though `RootAsset1` has no reference to `BundleB`, `BundleB` is still a dependency of `RootAsset1` because `RootAsset1` is in `BundleA`, which has a reference to `BundleB`.

To avoid loading more bundles than you need, keep the dependencies between AssetBundles as simple as possible. You can use the [Build Layout Report](xref:addressables-build-layout-report) to check dependencies.

## Additional resources

* [Optimizing AssetBundles](xref:um-asset-bundles-optimization)
* [Asset and AssetBundle dependencies](AssetDependencies.md)