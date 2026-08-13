---
uid: addressables-migration
---

# Introduction to converting existing projects to Addressables

You can use the Addressables package in an existing Unity project that uses one of the other [asset management options](xref:um-assets-managing-introduction) available in Unity. Once you [install Addressables](installation-guide.md) you need to assign addresses to the assets in your project and then refactor any runtime loading code.

You can integrate the Addressables package into your project at any stage of development, but it's best practice to use Addressables from the start to avoid code refactoring and content planning changes.

## Convert assets to use Addressables

Assets that use Addressables only reference other assets built in that Addressables build. If there are Addressable assets used or referenced in the [scene data](convert-scene-data.md) or [Resources system](convert-resources-system.md), then Unity duplicates those assets on disk and in memory if they're both loaded.

To avoid this duplication, you can convert all scene data and `Resources` folder data to the Addressables build system. This reduces the memory overhead from the duplicated assets and means you can manage all content with Addressables. This also means that the content can be either local or remote, and you can update it through [content update](xref:addressables-content-update-builds) builds.

To convert your project to Addressables, you need to perform different steps depending on how your current project references and loads assets:

* [Convert prefabs to use Addressables](convert-prefabs.md).
* [Convert scenes to use Addressables](convert-scene-data.md).
* [Move assets from the Resources system](convert-resources-system.md).
* [Convert AssetBundles to Addressables](convert-assetbundles.md).

### Files in StreamingAssets

You can continue to load files from the [`StreamingAssets` folder](xref:um-streaming-assets) when you use the Addressables system. However, the files in this folder can't be Addressable and can't reference other assets in your project.

The Addressables system places its runtime configuration files and local AssetBundles in the `StreamingAssets` folder during a build. Addressables removes these files at the end of the build process and you won't find them in the Unity Editor.

## Additional resources

* [Create and organize Addressable assets introduction](organize-addressable-assets.md)
* [Building Addressable assets](Builds.md)