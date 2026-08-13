---
uid: addressables-overview
---

# Addressables introduction

The Addressables package provides a user interface for organizing assets in your project. You can organize assets into [groups](groups-intro.md), which define how Unity packages assets into AssetBundles and loads them.

By default, Addressables uses [AssetBundles](xref:um-asset-bundles-intro) to package your assets. You can also implement your own [`IResourceProvider`](xref:UnityEngine.ResourceManagement.ResourceProviders.IResourceProvider) class to support other ways to access assets.

## Addressables groups and labels

Use Addressables [groups](groups-intro.md) to organize your content. All Addressable assets belong to a group. If you don't explicitly assign an asset to a group, Unity adds it to the default group.

You can set the [group settings](ContentPackingAndLoadingSchema.md) to specify how the Addressables build system packages the assets in a group into bundles. For example, you can choose whether to pack all the assets in a group together in a single AssetBundle file.

Use [labels](Labels.md) to tag content that you want to treat together in some way. For example, if you had labels defined for `red`, `hat`, and `feather`, you can load all red hats with feathers in a single operation, whether they're part of the same AssetBundle or not. You can also use labels to decide how assets in a group are packed into AssetBundles.

Add an asset to a group and move assets between groups using the [Addressables Groups](xref:addressables-groups-window) window. You can also assign labels to your assets in the Groups window.

### Group schemas

The schemas assigned to a group define the settings used to build the assets in a group. Different schemas can define different groups of settings. For example, one standard schema defines the settings for how to pack and compress your assets into AssetBundles (among other options). You also can define your own schemas to use with custom build scripts.

For more information, refer to [Define group settings](GroupSchemas.md).

## Asset addresses

In the Addressables system, assets are assigned addresses that can be used to load the assets at runtime. For example, an asset at `Assets/Boss1/Materials/MainMaterial.material` could be assigned an address like `boss1_material_main`. The Addressables resource manager looks up the address in the content catalog to find out where the asset is stored. Assets can be built-in to your application, cached locally, or hosted remotely. The resource manager loads the asset and any dependencies, downloading the content first, if necessary.

![An overview of the Addressables system retrieving assets from different locations. The locally-installed application includes both non-addressable assets and local addressable assets. It communicates with both a device cache and a remote host, which each have their own addressable assets that the application can retrieve.](images/addressables-overview-addresses.png)<br/>*An overview of the Addressables system retrieving assets from different locations.*

Because an address isn't tied to the physical location of the asset, you have several options to manage and optimize your assets, both in the Unity Editor and at runtime. [Catalogs](#content-catalogs) map addresses to physical locations.

Although it's best practice to assign unique addresses to your assets, an asset address doesn't have to be unique. You can assign the same address string to more than one asset when useful. For example, if you have variants of an asset, you can assign the same address to all the variants and use labels to distinguish between the variants:

* Asset 1: address: `"plate_armor_rusty"`, label: `"hd"`
* Asset 2: address: `"plate_armor_rusty"`, label: `"sd"`

The `Addressables` API methods that only load a single asset, such as [`LoadAssetAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetAsync*), load the first instance found if you call them with an address assigned to multiple assets. Other methods, like [`LoadAssetsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync*), load multiple assets in one operation and load all the assets with the specified address.

> [!TIP]
> You can use the [`MergeMode`](xref:UnityEngine.AddressableAssets.Addressables.MergeMode) parameter of `LoadAssetsAsync` to load the intersection of two keys.
>
>In the earlier example, you can specify the address, `"plate_armor_rusty"`, and the label, `"hd"`, as keys and intersection as the merge mode to load Asset 1. You can then change the label value to `"sd"` to load Asset 2.

For more information on how to assign addresses to assets, refer to [Making an asset Addressable](xref:addressables-getting-started). For information on how to load assets by keys, including addresses, refer to [Loading assets](xref:addressables-api-load-asset-async).

## Asset loading and unloading

To load an Addressable asset, you can use its address or other key such as a label or `AssetReference`. For more information, refer to [Loading Addressable Assets](xref:addressables-api-load-asset-async). You only need to load the main asset and Addressables loads any dependent assets automatically.

When your application no longer needs access to an Addressable asset at runtime, you must release it so that Addressables can free the associated memory. The Addressables system keeps a reference count of loaded assets, and doesn't unload an asset until the reference count returns to zero. As such, you don't need to keep track of whether an asset or its dependencies are still in use. You only need to make sure that any time you explicitly load an asset, you release it when your application no longer needs that instance. Refer to [Releasing Addressable assets](xref:addressables-unloading) for more information.

### Control loading with asset references

An [`AssetReference`](xref:UnityEngine.AddressableAssets.AssetReference) is a type that you can set to any kind of Addressable asset. Unity doesn't automatically load the asset assigned to the reference, so you have more control over when to load and unload it.

Use fields of type `AssetReference` in a `MonoBehaviour` or `ScriptableObject` to reference an Addressable asset. You can drag and drop in the Editor Inspector to assign an Asset to an `AssetReference` field.

Addressables also provide specialized types, such as `AssetReferenceGameObject` and `AssetReferenceTexture`. You can use these specialized subclasses to prevent the possibility of assigning the wrong asset type to an `AssetReference` field. You can also use the `AssetReferenceUILabelRestriction` attribute to limit assignment to assets with specific labels.

For more information, refer to [Using AssetReferences](xref:addressables-asset-references).

## Dependency and resource management

One asset in Unity can depend on another. A scene might reference one or more prefabs, or a prefab might use one or more materials. One or more prefabs can use the same material, and those prefabs can exist in different AssetBundles. When you load an Addressable asset, the system automatically finds and loads any dependent assets that it references. When the system unloads an asset, it also unloads its dependencies, unless a different asset is still using them.

As you load and release assets, the Addressables system keeps a reference count for each item. When an asset is no longer referenced, Addressables unloads it. If the asset was in a bundle that no longer has any assets that are in use, Addressables also unloads the bundle.

Refer to [Memory management](xref:addressables-memory-management) for more information.

## Content builds

The Addressables system separates the building of Addressable content from the build of your player. A content build produces the content catalog, catalog hash, and the AssetBundles containing your assets. You can build Addressable assets in a separate, [content-only build](builds-full-build.md), or build them at the [same time as the Player](build-player-builds.md).

Because asset formats are platform-specific, you must make a content build for each platform before building a player.

Refer to [Building Addressable content](xref:addressables-builds) for more information.

## Content catalogs

The Addressables system produces a content catalog file that maps the addresses of assets to their physical locations. It can also create a hash file containing the hash of the catalog. If you're hosting Addressable assets remotely, the system uses this hash file to decide if the content catalog has changed and needs to download it. Refer to [Content catalogs](xref:addressables-build-artifacts) for more information.

The Profile selected when you perform a content build determines how the addresses in the content catalog map to resource loading paths. Refer to [Profiles](xref:addressables-profiles) for more information.

For information about hosting content remotely, refer to [Distributing content remotely](xref:addressables-remote-content-distribution).


## Addressables tools

The Addressables system provides the following tools and windows to help you manage your Addressable assets:

* [Addressable Groups window](xref:addressables-groups-window): The main interface for managing assets, group settings, and making builds.
* [Profiles window](xref:addressables-profiles): Helps set up paths used by your builds.
* [Build layout report](xref:addressables-build-layout-report): Describes the AssetBundles produced by a content build.
* [Analyze tool](xref:addressables-analyze-tool): the Analyze tool runs analysis rules that check whether your Addressables content conforms to the set of rules you have defined. The Addressables system provides some basic rules, such as checking for duplicate assets; you can add your own rules using the [AnalyzeRule] class.

## Additional resources

* [AssetBundles introduction](xref:um-asset-bundles-intro)
* [Organize Addressable assets](AddressableAssetsDevelopmentCycle.md)
* [Referencing Addressable assets in code](AssetReferences.md)