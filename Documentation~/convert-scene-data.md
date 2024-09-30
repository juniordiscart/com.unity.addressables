# Convert scene data

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


## Use Addressable assets in non-Addressable scenes

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