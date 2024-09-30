---
uid: convert-to-addressables
---

# Configure your project to use Addressables

You can add Addressables to an existing Unity project by installing the Addressables package. Once you've installed the package, you need to assign addresses to your assets and refactor any runtime loading code.

Although you can integrate Addressables at any stage in a project’s development, it's best practice to start using Addressables immediately in new projects to avoid unnecessary code refactoring and content planning changes later in development.

## Convert to Addressables

Content built using Addressables only references other assets built in that Addressables build. Content that's used or referenced to which is included within both Addressables, and the Player build through the __Scene data__ and __Resource folders__ is duplicated on disk and in memory if they're both loaded. Because of this limitation, you must convert all __Scene data__ and __Resource folders__ to the Addressables build system. This reduces the memory overhead because of duplication and means you can manage all content with Addressables. This also means that the content can be either local or remote, and you can update it through [content update](xref:addressables-content-update-builds) builds.

To convert your project to Addressables, you need to perform different steps depending on how your current project references and loads assets:

* __Prefabs__: Assets you create using GameObjects and components, and save outside a Scene. For information on how to upgrade prefab data to Addressables, refer to [Convert prefabs](convert-prefabs.md).
* __AssetBundles__: Assets you package in AssetBundles and load with the `AssetBundle` API. For information on how to upgrade AssetBundles to Addressables, refer to [Convert AssetBundles](convert-asset-bundles.md)
* __StreamingAssets__: Files you place in the `StreamingAssets` folder. Unity includes any files in the `StreamingAssets` folder in your built player application as is.

## Files in StreamingAssets

You can continue to load files from the `StreamingAssets` folder when you use the Addressables system. However, the files in this folder can't be Addressable nor can they reference other assets in your project.

The Addressables system places its runtime configuration files and local AssetBundles in the StreamingAssets folder during a build. Addressables removes these files at the conclusion of the build process and you won’t see them in the Unity Editor.
