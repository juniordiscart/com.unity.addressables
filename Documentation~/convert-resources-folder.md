# Convert the Resources folder

If your project loads assets in Resources folders, you can migrate those assets to the Addressables system:

1. Make the assets Addressable. To do this, either enable the **Addressable** option in each asset's Inspector window or drag the assets to groups in the [Addressables Groups](xref:addressables-groups) window.
1. Change any runtime code that loads assets using the [Resources](xref:UnityEngine.Resources) API to load them with the [Addressables](xref:UnityEngine.AddressableAssets.Addressables) API.
3. Add code to release loaded assets when no longer needed.

If you keep all the assets that were previously in the Resources folder in one group, you can expect similar loading and memory performance.

When you mark an asset in the Resources folder as Addressable, Unity automatically moves the asset to a new folder in your project named `Resources_moved`. The default address for a moved asset is the old path, omitting the folder name. For example, your loading code might change from:

```c#
Resources.LoadAsync<GameObject>("desert/tank.prefab");
```
to:

```c#
Addressables.LoadAssetAsync<GameObject>("desert/tank.prefab");
```

You might have to implement some functionality of the `Resources` class differently after you change your project to use the Addressables system.

For example, if you run a command like `Resources.LoadAll\<SampleType\>("MyPrefabs");` to load assets from a `Resources/MyPrefabs/` folder, Unity loads all the assets, Unity loads all assets in `Resources/MyPrefabs/` that match the type `SampleType`.

Because the Addressables system doesn't support this exact functionality, you need to change your workflow to accommodate the Addressables system. In this case, you can achieve a similar effect with [Addresssable labels](xref:addressables-labels).
