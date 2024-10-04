---
uid: addressables-migration
---

# Configure your project to use Addressables

You can add Addressables to an existing Unity project by installing the Addressables package. Once you've installed the package, you need to assign addresses to your assets and refactor any runtime loading code.

Although you can integrate Addressables at any stage in a project’s development, it's best practice to start using Addressables immediately in new projects to avoid unnecessary code refactoring and content planning changes later in development.

## Convert to Addressables

Content built using Addressables only references other assets built in that Addressables build. Content that's used or referenced to which is included within both Addressables, and the Player build through the __Scene data__ and __Resource folders__ is duplicated on disk and in memory if they're both loaded. Because of this limitation, you must convert all __Scene data__ and __Resource folders__ to the Addressables build system. This reduces the memory overhead because of duplication and means you can manage all content with Addressables. This also means that the content can be either local or remote, and you can update it through [content update](xref:addressables-content-update-builds) builds.

To convert your project to Addressables, you need to perform different steps depending on how your current project references and loads assets:

* __Prefabs__: Assets you create using GameObjects and components, and save outside a Scene. For information on how to upgrade prefab data to Addressables, refer to [Convert prefabs](#convert-prefabs).
* __AssetBundles__: Assets you package in AssetBundles and load with the `AssetBundle` API. For information on how to upgrade AssetBundles to Addressables, refer to [Convert AssetBundles](#convert-assetbundles)
* __StreamingAssets__: Files you place in the `StreamingAssets` folder. Unity includes any files in the `StreamingAssets` folder in your built player application as is. For information, refer to [Files in StreamingAssets](#files-in-streamingassets)

## Convert scene data

To convert scene data to Addressable, move the scenes out of the [Build Settings](xref:BuildSettings) list and make those scenes Addressable. You must have one scene in the list, which is the scene Unity loads at application startup. You can make a new scene for this that does nothing else than load your first Addressable scene.

To convert your scenes:

1. Create a new initialization scene.
1. Open the __Build Settings__ window (menu: __File > Build Settings__).
1. Add the initialization scene to the scene list.
1. Remove the other scenes from the list.
1. Select each scene in the project list and enable the Addressable option in its Inspector window. Or, you can drag scene assets to a group in the Addressables Groups window. Don't make your new initialization scene Addressable.
1. Update the code you use to load Scenes to use the [`Addressables`](xref:UnityEngine.AddressableAssets.Addressables) class scene loading methods rather than the `SceneManager` methods.

You can now split your one, large Addressable Scene group into multiple groups. The best way to do that depends on the project goals. To proceed, you can move your Scenes into their own groups so that you can load and unload each of them independently of each other. You can avoid duplicating an asset referenced from two different bundles by making the asset itself Addressable. It's often better to move shared assets to their own group as well to reduce dependencies among AssetBundles.

You can now split your one, large Addressable scene group into multiple groups. The best way to do that depends on the project goals. To proceed, you can move your scenes into their own groups so that you can load and unload each of them independently of each other.

To avoid duplicating an asset referenced from two different bundles, make the asset Addressable. It's often better to move shared assets to their own group to reduce the amount of dependencies among your AssetBundles.


### Use Addressable assets in non-Addressable scenes

For any scenes that you don't want to make Addressable, you can still use Addressable assets as part of the Scene data through [AssetReferences](xref:addressables-asset-references).

When you add an AssetReference field to a custom MonoBehaviour or ScriptableObject class, you can assign an Addressable asset to the field in the Unity Editor in a similar way that you assign an asset as a direct reference. The main difference is that you need to add code to your class to load and release the asset assigned to the AssetReference field (whereas Unity loads direct references automatically when it instantiates your object in the Scene).

> [!NOTE]
> You can't use Addressable assets for the fields of any UnityEngine components in a non-Addressable scene. For example, if you assign an Addressable mesh asset to a MeshFilter component in a non-Addressable Scene, Unity doesn't use the Addressable version of that mesh data for the Scene. Instead, Unity copies the mesh asset and includes two versions of the mesh in your application: one in the AssetBundle built for the Addressable group that contains the mesh, and one in the built-in Scene data of the non-Addressable scene. When used in an Addressable Scene, Unity doesn't copy the mesh data and always loads it from the AssetBundle.

To replace direct references with AssetReferences in your custom classes, follow these steps:

1. Replace your direct references to objects with asset references (for example, `public GameObject directRefMember;` becomes `public AssetReference assetRefMember;`).
1. Drag assets onto the appropriate component’s Inspector, as you would for a direct reference.
1. Add runtime code to load the assigned asset using the [`Addressables`](xref:UnityEngine.AddressableAssets.Addressables) API.
1. Add code to release the loaded asset when no longer needed.

For more information about using AssetReference fields, refer to [Asset references](xref:addressables-asset-references).

For more information about loading Addressable assets, refer to [Loading Addressable assets](xref:addressables-api-load-asset-async).

## Convert prefabs

To convert a prefab into an Addressable asset, enable the __Addressables__ option in its Inspector window or drag it to a group in the [Addressables Groups](xref:addressables-groups) window.

You don't always need to make prefabs Addressable when used in an Addressable scene. Addressables automatically includes prefabs that you add to the scene hierarchy as part of the data contained in the scene's AssetBundle. If you use a prefab in more than one scene, make the prefab into an Addressable asset so that the prefab data isn't duplicated in each scene that uses it. You must also make a prefab Addressable if you want to load and instantiate it dynamically at runtime.

> [!NOTE]
> If you use a Prefab in a non-Addressable Scene, Unity copies the Prefab data into the built-in Scene data whether the Prefab is Addressable or not.

## Convert the Resources folder

If your project loads assets in Resources folders, you can migrate those assets to the Addressables system:

1. Make the assets Addressable. To do this, either enable the __Addressable__ option in each asset's Inspector window or drag the assets to groups in the [Addressables Groups](xref:addressables-groups) window.
1. Change any runtime code that loads assets using the [`Resources`](xref:UnityEngine.Resources) API to load them with the [`Addressables`](xref:UnityEngine.AddressableAssets.Addressables) API.
3. Add code to release loaded assets when no longer needed.

As with scenes, if you keep all the former Resources assets in one group, the loading and memory performance should be equivalent.

When you mark an asset in a Resources folder as Addressable, the system automatically moves the asset to a new folder in your project named `Resources_moved`. The default address for a moved asset is the old path, omitting the folder name. For example, your loading code might change from:

```c#
Resources.LoadAsync\<GameObject\>("desert/tank.prefab");
```
to:

```c#
Addressables.LoadAssetAsync\<GameObject\>("desert/tank.prefab");.
```

You might have to implement some functionality of the `Resources` class differently after modifying your project to use the Addressables system.

For example, consider the [`Resources.LoadAll`](https://docs.unity3d.com/ScriptReference/Resources.LoadAll.html) method. Previously, if you had assets in a folder named `Resources/MyPrefabs/`, and ran `Resources.LoadAll\<SampleType\>("MyPrefabs");`, it would have loaded all the assets in `Resources/MyPrefabs/` matching type `SampleType`. The Addressables system doesn't support this exact functionality, but you can achieve similar results using [Addressable labels](xref:addressables-labels).

## Convert AssetBundles

When you first open the __Addressables Groups__ window, Unity offers to convert all AssetBundles into Addressables groups. This is the easiest way to migrate your AssetBundle setup to the Addressables system. You must still update your runtime code to load and release assets using the [`Addressables`](xref:UnityEngine.AddressableAssets.Addressables) API.

If you want to convert your AssetBundle setup manually, click the __Ignore__ button. The process for manually migrating your AssetBundles to Addressables is similar to that described for scenes and the Resources folder:

1. Make the assets Addressable by enabling the __Addressable__ option on each asset’s Inspector window or by dragging the asset to a group in the [Addressables Groups](xref:addressables-groups) window. The Addressables system ignores existing AssetBundle and label settings for an asset.
1. Change any runtime code that loads assets using the [`AssetBundle`](xref:UnityEngine.AssetBundle) or [`UnityWebRequestAssetBundle`](xref:UnityEngine.Networking.UnityWebRequestAssetBundle) APIs to load them with the [`Addressables`](xref:UnityEngine.AddressableAssets.Addressables) API. You don't need to explicitly load AssetBundle objects themselves or the dependencies of an asset; the Addressables system handles those aspects automatically.
1. Add code to release loaded assets when no longer needed.

> [!NOTE]
> The default path for the address of an asset is its file path. If you use the path as the asset's address, you'd load the asset in the same manner as you would load from a bundle. The Addressable asset system handles the loading of the bundle and all its dependencies.

If you chose the automatic conversion option or manually added your assets to equivalent Addressables groups, then, depending on your group settings, you end up with the same set of bundles containing the same assets. The bundle files themselves won't be identical.

## Files in StreamingAssets

You can continue to load files from the `StreamingAssets` folder when you use the Addressables system. However, the files in this folder can't be Addressable nor can they reference other assets in your project.

The Addressables system places its runtime configuration files and local AssetBundles in the StreamingAssets folder during a build. Addressables removes these files at the conclusion of the build process and you won’t see them in the Editor.
