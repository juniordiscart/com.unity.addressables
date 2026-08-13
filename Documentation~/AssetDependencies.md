---
uid: addressables-asset-dependencies
---

# Addressable asset dependencies

Understanding how assets reference each other can help you optimize the Addressables implementation in your project. Asset dependencies can affect build size, memory usage, and runtime performance.

Unity packages assets differently depending on how you configure them:

- **Addressable assets**: Depending on how you configure the Addressables settings in your project you can either:
    - Build Addressable assets into your application as an additional set of local assets.
    - Keep Addressable assets external to the build as remote assets hosted on a server and downloaded when they're needed. You can update remote assets independently from the application itself, although remote assets can't include code, so you can only change assets and serialized data.
- **Scene assets**: Unity includes scenes from your project's [Scene List](xref:um-build-profile-scene-list) and their dependencies in the application's built-in data.
- **Resources assets**: Unity packages assets in [`Resources` folders](xref:um-loading-resources-at-runtime) as a separate, built-in collection that you can load independently.

![How different types of project assets are exported to a Player build.](images/addressables-assets-overview.png)<br/>*How different types of project assets are exported to a Player build.*

## Asset organization

To avoid duplication of content between the player build and Addressables you can minimize the amount of data in the Player build by moving data from `Resources` folders, and scenes into [Addressable groups](groups-intro.md). Small amounts of data in `Resources` folders typically don't cause performance issues. You don't need to move third-party package assets unless they cause problems. You can't store Addressable assets in `Resources` folders.

Keep at least one scene in your project's Scene List and create a minimal initialization scene if needed.

## Sub object references

Unity determines build content partly based on how your project's assets and scripts reference each other. Sub object references affect the process in the following ways:

- **AssetReference to sub object**: If an [`AssetReference`](asset-reference-intro.md) points to a sub object of an Addressable asset, Unity builds the entire object into the AssetBundle.
- **AssetReference to main object**: If the `AssetReference` points to an Addressable object (GameObject, ScriptableObject, or scene) that references a sub object, Unity builds only the sub object as an implicit dependency.

An explicit asset is one you directly add to an [Addressables group](groups-intro.md). Unity packs these into AssetBundles during a [content build](xref:addressables-builds).

An implicit asset is a dependency that Unity automatically includes. If an explicit asset references other assets:
- **Addressable dependencies**: Unity packs these according to their group settings (same or different AssetBundle).
- **Non-Addressable dependencies**: Unity includes these in the referencing asset's AssetBundle.

> [!TIP]
> Use the [Build Layout Report](xref:addressables-build-layout-report) tool to view detailed information about AssetBundles and their dependencies.

## Avoiding asset duplication

When multiple Addressables reference the same non-Addressable asset, Unity creates copies in each AssetBundle:

![Non-Addressable assets are copied to each bundle with a referencing Addressable. This concept is illustrated as Prefab X and Prefab Y both referencing the same non-addressable Material P. The result is Bundle A containing Prefab X plus Bundle A's own copy of Material P, and Bundle B containing Prefab Y and Bundle B's own copy of Material P.](images/addressables-multiple-assets.png)<br/>*Non-Addressable assets are copied to each bundle with a referencing Addressable.*

As a result, at runtime the following happens:

- Multiple instances of the same asset exist at runtime instead of a single shared instance.
- Changes to one instance don't affect other instances.
- Increased memory usage and build size.

To avoid this problem, make the shared asset Addressable and place it in its own AssetBundle or group it with one of the referencing assets. This creates an AssetBundle dependency that Unity loads automatically when needed.

When you load any asset from an AssetBundle, Unity must also load all dependent AssetBundles. This loading affects runtime performance even if you don't use the dependent assets directly. For more information, refer to [Memory implications of loading AssetBundle dependencies](memory-assetbundles.md).

## Additional resources

* [Addressables initialization process](InitializeAsync.md)
* [Memory management](MemoryManagement.md)