# Convert scenes to use Addressables

Any scenes that are in the **Scene List** in the [Build Profiles](xref:um-build-profiles) window can't be Addressable. To make the scene data Addressable, you must remove the scenes from the **Scene List**, and then create a non-Addressable initialization scene which loads the first Addressable scene.

1. Create a new initialization scene and open it. (**File** > **New Scene**, or right click on the **Project** window and select **Create** > **Scene**)
1. Open the __Build Profiles__ window (__File > Build Profiles__), and select the **Scene List**.
1. Add the initialization scene to the scene list.
1. Remove the other scenes from the list.
1. In the **Project** window, select each scene you want to convert to Addressable. In the **Inspector**, enable the **Addressable** option. Or, you can drag scene assets to a [group](groups-create.md) in the [Addressables Groups window](GroupsWindow.md). Don't make your new initialization scene Addressable.
1. Update the code you use to load scenes to use the [`Addressables.LoadSceneAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadSceneAsync*) method rather than the `SceneManager` methods. For more information, refer to [Load scenes](LoadingScenes.md).

You can now split your one, large Addressable scene group into multiple groups. The best way to do that depends on the project goals. To proceed, you can move your scenes into their own groups so that you can load and unload each of them independently of each other.

## Use Addressable assets in non-Addressable scenes

For any scenes that you don't want to make Addressable, you can still use Addressable assets as part of the scene data through [AssetReferences](asset-reference-intro.md).

When you add an `AssetReference` field to a custom MonoBehaviour or ScriptableObject class, you can assign an Addressable asset to the field in the Unity Editor in a similar way that you assign an asset as a direct reference. The main difference is that you need to add code to your class to load and release the asset assigned to the AssetReference field (whereas Unity loads direct references automatically when it instantiates your object in the scene).

> [!NOTE]
> You can't use Addressable assets for the fields of any `UnityEngine` components in a non-Addressable scene. For example, if you assign an Addressable mesh asset to a MeshFilter component in a non-Addressable scene, Unity doesn't use the Addressable version of that mesh data for the scene. Instead, Unity copies the mesh asset and includes two versions of the mesh in your application: one in the AssetBundle built for the Addressable group that contains the mesh, and one in the built-in scene data of the non-Addressable scene. When used in an Addressable scene, Unity doesn't copy the mesh data and always loads it from the AssetBundle.

To replace direct references with `AssetReferences` in your custom classes, follow these steps:

1. Replace any direct references to objects with asset references (for example, `public GameObject directRefMember;` becomes `public AssetReference assetRefMember;`).
1. Drag assets onto the appropriate component's Inspector.
1. Add runtime code to load the assigned asset using the [`Addressables`](xref:UnityEngine.AddressableAssets.Addressables) API.
1. Add code to release the loaded asset when no longer needed.

For more information about using AssetReference fields, refer to [Create an asset reference field](asset-reference-create.md) and [Load asset references](LoadingAssetReferences.md).

## Additional resources

* [Load scenes](LoadingScenes.md)
* [Referencing Addressable assets in code](AssetReferences.md)
* [Convert prefabs to use Addressables](convert-prefabs.md)
