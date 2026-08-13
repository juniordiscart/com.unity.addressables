---
uid: addressables-asset-dependencies
---

# Asset dependencies overview

When you include a scene in your Project Build Settings and build a player, Unity includes that scene and any assets used in the scene in your application's built-in data. Similarly, Unity includes any assets in your project's Resources folders in a separate, built-in collection of assets. The difference is that assets in a scene are only loaded as part of a scene, whereas assets in Resources can be loaded independently.

Addressable assets can either be built into your application as an additional set of local assets, or kept external to the build as remote assets hosted on a server and downloaded when they're needed. You can update remote assets independently from the application itself, although remote assets can't include code, so you can only change assets and serialized data.

![How different types of project assets are exported to a Player build.](images/addressables-assets-overview.png)<br/>*How different types of project assets are exported to a Player build.*

If you use the same asset both in a scene and the Resources folder, then Unity makes copies of the asset when building rather than sharing a single instance. For example, if you use a material in a built-in scene and also use it in a prefab located in a Resources folder, you end up with two copies of that material in your build, even if the material asset itself isn't in the Resources folder. If you then mark that same material as Addressable, you end up with three copies. Files in the project's StreamingAssets folder can never be referenced by assets outside that folder.

> [!NOTE]
> Before building a player, you must make a content build of your Addressable assets. During the player build, Unity copies your local Addressables to the StreamingAssets folder so that they're included in the build along with any assets you placed in StreamingAssets. Unity removes these assets at the end of the build process. You must upload the remote Addressables files produced by the content build to your hosting service. Refer to [Builds](xref:addressables-builds) for more information.

When you use Addressables in a project, it's best practice to move any scenes and data in the Resources folders into [Addressable groups](Groups.md) and manage them as Addressables.

The Build Settings scene list must contain at least one scene. You can create a minimal scene that initializes your application.

A small amount of data in Resources folders typically doesn't cause performance issues. If you use third party packages that place assets there, you don't need to move them unless they cause problems. Addressable assets can't be stored in Resources folders.

## Reference sub-objects

Unity partially determines what to include in a content build based on how your assets and scripts reference each other. Sub-object references make the process more complicated.

If an `AssetReference` points to a sub-object of an asset that's Addressable, Unity builds the entire object into the `AssetBundle` at build time. If the `AssetReference` points to an Addressable object such as a `GameObject`, `ScriptableObject`, or `Scene`, which directly references a sub-object, Unity only builds the sub-object into the `AssetBundle` as an implicit dependency.

## Asset and AssetBundle dependencies

When you add an asset to an [Addressables group](Groups.md), that asset is packed into an AssetBundle when you make a [content build](xref:addressables-builds). In this case the asset is explicitly included in the bundle, which is called an explicit asset.

If an asset references other assets, then the referenced assets are dependencies of the original asset. This is called an asset dependency. For example, if the asset is packed into AssetBundle A and the referenced assets are packed into AssetBundle B, then bundle B is a dependency of bundle A. This is called an AssetBundle dependency. Refer to the [AssetBundle dependencies manual page](xref:AssetBundles-Dependencies) for more information.

Asset dependencies are treated depending on whether or not they are also Addressable. Dependencies that are Addressable are packed into AssetBundles according to the settings of the group they're in. This might be the same bundle as the referencing asset or a different bundle. A dependency that isn't Addressable is included in the bundle of its referencing asset. The referenced asset is implicitly included in the bundle, which is called an implicit asset.

> [!TIP]
> Use the [Build Layout Report](xref:addressables-build-layout-report) tool to display more detailed information about AssetBundles produced by a content build.

## Reference multiple implicit assets

If more than one Addressable references the same implicit asset, then copies of the implicit asset are included in each bundle containing a referencing Addressable.

![Non-Addressable assets are copied to each bundle with a referencing Addressable. This concept is illustrated as Prefab X and Prefab Y both referencing the same non-addressable Material P. The result is Bundle A containing Prefab X plus Bundle A's own copy of Material P, and Bundle B containing Prefab Y and Bundle B's own copy of Material P.](images/addressables-multiple-assets.png)<br/>*Non-Addressable assets are copied to each bundle with a referencing Addressable.*

When an implicit asset is included in more than one bundle, multiple instances of that asset can be instantiated at runtime rather than the single instance that your game logic expects. If you change the instance state at runtime, only the object from the same bundle can detect the change because all the other assets now have their own individual instance rather than sharing the common one.

To stop this duplication, you can make the implicit asset an Addressable asset and include it in one of the existing bundles or add it to a different bundle. The bundle the asset is added to is loaded whenever you load one of the Addressables that reference it. If the Addressables are packed into a different AssetBundle than the referenced asset, then the bundle containing the referenced asset is an AssetBundle dependency.

The dependent bundle must be loaded when you load any asset in the current bundle, not just the asset containing the reference. Although none of the assets in this other AssetBundle are loaded, loading a bundle has its own runtime cost. Refer to [Memory implications of loading AssetBundle dependencies](memory-assetbundles.md) for more information.
