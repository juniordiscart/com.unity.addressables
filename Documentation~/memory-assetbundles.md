# AssetBundle memory overhead

When you load an AssetBundle, Unity allocates memory to store the bundle's internal data, and memory for the assets contained in the bundle. The main types of internal data for a loaded AssetBundle include:

* **Loading cache**: Stores recently accessed pages of an AssetBundle file. Use [`AssetBundle.memoryBudgetKB`](xref:UnityEngine.AssetBundle.memoryBudgetKB) to control its size.
* [TypeTrees](#typetrees): Defines the serialized layout of objects.
* [Table of contents](#table-of-contents): Lists the assets in a bundle.
* [Preload table](#preload-table): Lists the dependencies of each asset.

When you organize Addressable groups and AssetBundles, you must make trade-offs between the size and the number of AssetBundles you create and load. Fewer, larger bundles can minimize the total memory usage of your AssetBundles. However, using many small bundles can minimize the peak memory usage because Unity can easily unload assets and AssetBundles.

The size of an AssetBundle on disk isn't the same as its size at runtime. However, you can use the disk size as a guide to the memory overhead of the AssetBundles in a build. You can get bundle size and other information to help analyze AssetBundles from the [Build Layout Report](xref:addressables-build-layout-report).

## TypeTrees

A TypeTree describes the field layout of one of the data types in your project.

Each serialized file in an AssetBundle has a TypeTree for each object type within the file. You can use TypeTree information to load objects that are deserialized slightly differently from the way they were serialized. TypeTree information isn't shared between AssetBundles and each bundle has a complete set of TypeTrees for the objects it contains.

Unity loads all the TypeTrees when it loads the AssetBundle, and holds it in memory for the lifetime of the AssetBundle. The memory overhead associated with TypeTrees is proportional to the number of unique types in the serialized file and the complexity of those types.

### Reduce TypeTree memory

You can reduce the memory requirements of AssetBundle TypeTrees in the following ways:

* Keep assets of the same types together in the same bundles.
* Disable TypeTrees, excludes TypeTree information from a bundle, and  makes the AssetBundles smaller. However, without TypeTree information, when you load older bundles with a newer version of Unity or make script changes in your project, you might get serialization errors or undefined behavior.
*  Use simple data types to reduce TypeTree complexity.

To test the impact that TypeTrees have on the size of AssetBundles, build them with and without TypeTrees disabled and compare their sizes. Use [`BuildAssetBundleOptions.DisableWriteTypeTree`](xref:UnityEditor.BuildAssetBundleOptions.DisableWriteTypeTree) to disable TypeTrees in your AssetBundles.

>[!NOTE]
>Some platforms require TypeTrees and ignore the `DisableWriteTypeTree` setting. Additionally, not all platforms support TypeTrees.

If you disable TypeTrees in a project, always rebuild local Addressable groups before building a new player. If your project distributes content remotely, use the same version (including patch number) of Unity that you used to produce the player and don't make minor code changes. If you're using multiple player versions, updates, and versions of Unity, you might not find the memory savings from disabling TypeTrees to be worth the trouble.

## Table of contents

The table of contents is a map in the bundle that you can use to look up each explicitly included asset by name. It scales linearly with the number of assets and the length of the string names by which they are mapped.

The size of the table of contents data is based on the total number of assets. To minimize the amount of memory dedicated to holding table of content data, minimize the number of AssetBundles loaded at a given time.

## Preload table

The preload table is a list of all the other objects that an asset references. Unity uses the preload table to load these referenced objects when you load an asset from the AssetBundle.

For example, a prefab has a preload entry for each of its components and any other assets it might reference such as materials or textures. Each preload entry is 64 bits and can reference objects in other AssetBundles.

When an asset references another asset that in turn references other assets, the preload table can become large because it contains the entries needed to load both assets. If two assets both reference a third asset, then the preload tables of both assets contain entries to load the third asset, whether the referenced asset is Addressable or in the same AssetBundle.

For example, a project has two assets in an AssetBundle (`PrefabA` and `PrefabB`) and both of these prefabs reference a third prefab (`PrefabC`), which is large and has several components and references to other assets. This AssetBundle has two preload tables, one for `PrefabA` and one for `PrefabB`. Those tables contain entries for all the objects of their respective prefab, but also entries for all the objects in `PrefabC` and any objects that `PrefabC` references. The information required to load `PrefabC` ends up duplicated in both `PrefabA` and `PrefabB`. This happens whether `PrefabC` is explicitly added to an AssetBundle or not.

Depending on how you organize the assets in a project, the preload tables in AssetBundles might be large and contain many duplicate entries. This is true if you have several loadable assets that all reference a complex asset, such as `PrefabC` in the example. If you decide that the memory overhead from the preload table is a problem, you can structure the loadable assets in your project so that they have fewer complex loading dependencies.

## Loading AssetBundle dependencies

Loading an Addressable asset also loads all the AssetBundles containing its dependencies. An AssetBundle dependency happens when an asset in one bundle references an asset in another bundle. For example, when a material references a texture. For more information refer to [Asset and AssetBundle dependencies](xref:addressables-asset-dependencies).

Addressables calculates dependencies between bundles at the bundle level. If one asset references an object in another bundle, then the entire bundle has a dependency on that bundle. This means that even if you load an asset in the first bundle that has no dependencies of its own, the second AssetBundle is still loaded into memory.

For example,`BundleA` contains Addressable assets `RootAsset1` and `RootAsset2`. `RootAsset2` references `DependencyAsset3`, which is in `BundleB`. Even though `RootAsset1` has no reference to `BundleB`, `BundleB` is still a dependency of `RootAsset1` because `RootAsset1` is in `BundleA`, which has a reference to `BundleB`.

To avoid loading more bundles than you need, keep the dependencies between AssetBundles as simple as possible. You can use the [Build Layout Report](xref:addressables-build-layout-report) to check dependencies.
