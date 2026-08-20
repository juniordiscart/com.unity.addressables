---
uid: addressables-home
---

# Addressables package

The Addressables package provides a user interface in the Unity Editor to organize and manage the assets in your project, to create content builds that you can ship along with a Player build. It also has an API that you can use to load and release assets at runtime.

The Addressables package was originally designed on top of Unity's [AssetBundle](xref:um-asset-bundles-intro) system, but is also compatible with the newer [content directories system](xref:um-content-directories). Addressables automatically manages dependencies, asset locations, and provides simpler workflows for memory management which you otherwise have to handle manually in the AssetBundle and content directories systems.

When you make an asset Addressable, you can use that asset's address to load it locally or from a content delivery network, rather than using its file name, AssetBundle location, or content directory location. This means you can change the location of assets in a project without needing to rewrite code.

|**Topic**|**Description**|
|---|---|
|**[Addressables introduction](AddressableAssetsOverview.md)**|Understand the core concepts of the Addressables system.|
|**[Choose a content build system](content-build-systems.md)**|Choose between the content directory or AssetBundle system to create content builds. |
|**[Addressables package set up](AddressableAssetsGettingStarted.md)**|Install and configure the Addressables package in your Unity project.|
|**[Create and organize Addressable assets](AddressableAssetsDevelopmentCycle.md)**|Make assets Addressable and organize them into groups for efficient management.|
|**[Build Addressable assets](Builds.md)**|Build and package Addressable assets for deployment.|
|**[Load Addressable assets](LoadingAddressableAssets.md)**|Control how to load assets with the Addressables API.|
|**[Distribute and update remote content](RemoteContentDistribution.md)**|Host and deliver assets from remote servers and content delivery networks.|
|**[Optimization tools](optimization-tools.md)**|Use analysis tools to optimize Addressables.|
|**[Known issues](known-issues.md)**|Review known issues in the Addressables package and their workarounds.|

## Additional resources

* [Introduction to runtime asset management](xref:um-assets-managing-introduction)
* [Convert existing projects to Addressables](convert-existing-projects.md)