---
uid: addressables-overview
---

# Addressables overview

Addressables provides a system that can scale with your project. You can start with a simple setup and then reorganize as your project grows in complexity with minimal code changes.

For example, you can start with a single group of Addressable assets, which Unity loads as a set. Then, as you add more content, you can split your assets into multiple groups so that you can load only the ones you need at a given time. As your team grows in size, you can make separate Unity projects to develop different types of assets. These auxiliary projects can produce their own Addressables content builds that you load from the main project.

## Concepts

This overview discusses the following concepts to help you understand how to manage and use your assets with the Addressables system:

|**Concept**|**Description**|
|---|---|
|[Addressables tools](#addressables-tools)| The Addressables package has several windows and tools that you can use to organize, build, and optimize your content.|
|[Asset address](#asset-addresses)| A string ID that identifies an Addressable asset. You can use an address as a key to load the asset.|
|[Asset loading and unloading](#asset-loading-and-unloading)| The `Addressables` API provides its own functions to load and release assets at runtime.|
|__Asset location__| A runtime object that describes how to load an asset and its dependencies. You can use a location object as a key to load the asset.|
|[AssetReferences](#assetreference)| A type you can use to support the assignment of Addressable assets to fields in an Inspector window. You can use an AssetReference instance as a key to load the asset. The [`AssetReference`](xref:addressables-asset-references) class also provides its own loading methods.|
|[Content builds](#content-builds)| Use a content build to collate and package your assets as a separate step before you make a player build.|
|[Content catalogs](#content-catalogs)| Addressables uses catalogs to map your assets to the resources that contain them.|
|[Dependencies](#dependency-and-resource-management)| An asset dependency is one asset used by another, such as a prefab used in a scene asset or a material used in a prefab asset.|
|[Dependency and resource management](#dependency-and-resource-management)| The Addressables system uses reference counting to track which assets and AssetBundles are in use, including whether the system loads or unloads dependencies (other referenced assets).|
|[Group](#addressables-groups-and-labels)| You assign assets to groups in the Editor. The group settings configure how Addressables packages the group assets into AssetBundles and how it loads them at runtime.|
|__Key__| An object that identifies one or more Addressables. Keys include addresses, labels, AssetReference instances and location objects.|
|[Label](#addressables-groups-and-labels)| A tag that you can assign to multiple assets and use to load related assets together as a group. You can use a label as a key to load the asset.|
|__Multiple platform support__| The build system separates content built by platform and resolves the correct path at runtime.|

By default, Addressables uses AssetBundles to package your assets. You can also implement your own [`IResourceProvider`](xref:UnityEngine.ResourceManagement.ResourceProviders.IResourceProvider) class to support other ways to access assets.

## Asset addresses

A key feature of the Addressables system is that you assign addresses to your assets and use those addresses to load them at runtime. The Addressables resource manager looks up the address in the content catalog to find out where the asset is stored. Assets can be built-in to your application, cached locally, or hosted remotely. The resource manager loads the asset and any dependencies, downloading the content first, if necessary.

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

## AssetReference

An [`AssetReference`](xref:UnityEngine.AddressableAssets.AssetReference) is a type that you can set to any kind of Addressable asset. Unity doesn't automatically load the asset assigned to the reference, so you have more control over when to load and unload it.

Use fields of type `AssetReference` in a `MonoBehaviour` or `ScriptableObject` to specify which Addressable asset to use for that field (instead of using the string that specifies the address). `AssetReferences` support drag-and-drop and object picker assignment, which makes them more convenient to use in an Editor Inspector.

Addressables also provides a few more specialized types, such as `AssetReferenceGameObject` and `AssetReferenceTexture`. You can use these specialized subclasses to prevent the possibility of assigning the wrong asset type to an `AssetReference` field. You can also use the `AssetReferenceUILabelRestriction` attribute to limit assignment to assets with specific labels.

Refer to [Using AssetReferences](xref:addressables-asset-references) for more information.

## Asset loading and unloading

To load an Addressable asset, you can use its address or other key such as a label or `AssetReference`. Refer to [Loading Addressable Assets](xref:addressables-api-load-asset-async) for more information. You only need to load the main asset and Addressables loads any dependent assets automatically.

When your application no longer needs access to an Addressable asset at runtime, you must release it so that Addressables can free the associated memory. The Addressables system keeps a reference count of loaded assets, and doesn't unload an asset until the reference count returns to zero. As such, you don't need to keep track of whether an asset or its dependencies are still in use. You only need to make sure that any time you explicitly load an asset, you release it when your application no longer needs that instance. Refer to [Releasing Addressable assets](xref:addressables-unloading) for more information.

## Dependency and resource management

One asset in Unity can depend on another. A scene might reference one or more prefabs, or a prefab might use one or more materials. One or more prefabs can use the same material, and those prefabs can exist in different AssetBundles. When you load an Addressable asset, the system automatically finds and loads any dependent assets that it references. When the system unloads an asset, it also unloads its dependencies, unless a different asset is still using them.

As you load and release assets, the Addressables system keeps a reference count for each item. When an asset is no longer referenced, Addressables unloads it. If the asset was in a bundle that no longer has any assets that are in use, Addressables also unloads the bundle.

Refer to [Memory management](xref:addressables-memory-management) for more information.

## Addressables groups and labels

Use Addressables groups to organize your content. All Addressable assets belong to a group. If you don't explicitly assign an asset to a group, Addressables adds it to the default group.

You can set the group settings to specify how the Addressables build system packages the assets in a group into bundles. For example, you can choose whether to pack all the assets in a group together in a single AssetBundle file.

Use labels to tag content that you want to treat together in some way. For example, if you had labels defined for `red`, `hat`, and `feather`, you can load all red hats with feathers in a single operation, whether they're part of the same AssetBundle or not. You can also use labels to decide how assets in a group are packed into bundles.

Add an asset to a group and move assets between groups using the [Addressables Groups](xref:addressables-groups-window) window. You can also assign labels to your assets in the Groups window.

### Group schemas

The schemas assigned to a group define the settings used to build the assets in a group. Different schemas can define different groups of settings. For example, one standard schema defines the settings for how to pack and compress your assets into AssetBundles (among other options). Another standard schema defines which of the categories, **Can Change Post Release** and **Cannot Change Post Release** the assets in the group belong to.

You can define your own schemas to use with custom build scripts.

Refer to [Schemas](xref:addressables-group-schemas) for more information about group schemas.

## Content catalogs

The Addressables system produces a content catalog file that maps the addresses of your assets to their physical locations. It can also create a hash file containing the hash of the catalog. If you're hosting your Addressable assets remotely, the system uses this hash file to decide if the content catalog has changed and needs to download it. Refer to [Content catalogs](xref:addressables-build-artifacts) for more information.

The Profile selected when you perform a content build determines how the addresses in the content catalog map to resource loading paths. Refer to [Profiles](xref:addressables-profiles) for more information.

For information about hosting content remotely, refer to [Distributing content remotely](xref:addressables-remote-content-distribution).

## Content builds

The Addressables system separates the building of Addressable content from the build of your player. A content build produces the content catalog, catalog hash, and the AssetBundles containing your assets.

Because asset formats are platform-specific, you must make a content build for each platform before building a player.

Refer to [Building Addressable content](xref:addressables-builds) for more information.

## Play mode scripts

When you run your game or application in Play mode, it can be inconvenient and slow to always perform a content build before pressing the Play button. At the same time, you want to be able to run your game in a state as close to a built player as possible. Addressables provides three options that decide how the Addressables system locates and loads assets in Play mode:

* __Use the Asset Database__: Addressables loads assets directly from the Asset Database. This option typically provides the fastest iteration speed if you're making both code and Asset changes, but also least resembles a production build.
* __Use existing build__: Addressables loads content from your last content build. This option most resembles a production build and  provides fast iteration turnaround if you aren't changing assets.

Refer to [Play mode scripts](xref:addressables-groups-window) for more information.

## Addressables tools

The Addressables system provides the following tools and windows to help you manage your Addressable assets:

* [Addressable Groups window](xref:addressables-groups-window): The main interface for managing assets, group settings, and making builds.
* [Profiles window](xref:addressables-profiles): Helps set up paths used by your builds.
* [Build layout report](xref:addressables-build-layout-report): Describes the AssetBundles produced by a content build.
* [Analyze tool](xref:addressables-analyze-tool): the Analyze tool runs analysis rules that check whether your Addressables content conforms to the set of rules you have defined. The Addressables system provides some basic rules, such as checking for duplicate assets; you can add your own rules using the [AnalyzeRule] class.
