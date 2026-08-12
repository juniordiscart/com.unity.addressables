# Move assets from the Resources system

If your project uses the [Resources system](xref:um-loading-resources-at-runtime) to load assets, you can migrate those assets to the Addressables system:

1. Make the assets Addressable. To do this, either enable the __Addressable__ option in each asset's Inspector window or drag the assets to groups in the [Addressables Groups](GroupsWindow.md) window.
1. Change any runtime code that loads assets using the [`Resources`](xref:UnityEngine.Resources) API to load them with the [`Addressables`](xref:UnityEngine.AddressableAssets.Addressables) API. For more information, refer to [Load asset references](LoadingAssetReferences.md).
1. Add code to release loaded assets when no longer needed.

If you keep all the former Resources assets in one group, the loading and memory performance is equivalent.

When you mark an asset in a Resources folder as Addressable, the system automatically moves the asset to a new folder in your project named `Resources_moved`. The default address for a moved asset is the old path, omitting the folder name. For example, your loading code might change from:

```
Resources.LoadAsync\<GameObject\>("desert/tank.prefab");
```
to:

```
Addressables.LoadAssetAsync\<GameObject\>("Resources_moved/tank.prefab");.
```

## Update Resources code

You might have to implement some functionality of the `Resources` class differently after modifying your project to use the Addressables system.

For example, consider the [`Resources.LoadAll`](https://docs.unity3d.com/ScriptReference/Resources.LoadAll.html) method. Previously, if you had assets in a folder named `Resources/MyPrefabs/`, and ran `Resources.LoadAll\<SampleType\>("MyPrefabs");`, Unity loads all the assets in `Resources/MyPrefabs/` matching type `SampleType`. The Addressables system doesn't support this exact functionality, but you can achieve similar results using [Addressable labels](xref:addressables-labels).

## Additional resources

* [Load asset references](LoadingAssetReferences.md)
* [Labelling assets](Labels.md)
* [Organize assets into groups](groups-intro.md)
* [Resources system](xref:um-loading-resources-at-runtime)