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

### Replace Resources.LoadAll

Previously, if you had assets in a folder named `Resources/MyPrefabs/`, you could run [`Resources.LoadAll\<SampleType\>("MyPrefabs")`](https://docs.unity3d.com/ScriptReference/Resources.LoadAll.html) to load every asset of type `SampleType` in that folder. To get the same result with Addressables:

1. Rename or move the `Resources/MyPrefabs` folder out of any `Resources` folder (Addressables ignores assets that remain under a `Resources` folder path) &mdash; for example to `Assets/MyPrefabs`.
2. In the Project window enable the __Addressable__ checkbox on the folder itself (not each file individually). This makes every asset under the folder Addressable, addressed as `<folder address>/<relative path>`.
3. Update any keys used for loading. If you move Resources/MyPrefabs/ into Assets, you'll need to change the loading key to Assets/MyPrefabs. It should exeactly match what is listed in the Addressables Group window.
3. Confirm __Include Folder Keys in Catalog__ is enabled on the group's [Content Packing & Loading schema](ContentPackingAndLoadingSchema.md) (enabled by default). This makes the folder's own address load every asset inside it.
4. Replace the load call:

   ```csharp
   // Before
   var prefabs = Resources.LoadAll<SampleType>("MyPrefabs");

   // After
   var handle = Addressables.LoadAssetsAsync<SampleType>("Assets/MyPrefabs", prefab => { /* use prefab */ });
   await handle.Task;
   // or, to get the list of locations first:
   var locations = await Addressables.LoadResourceLocationsAsync("Assets/MyPrefabs", typeof(SampleType)).Task;
   ```

5. Release the handle when the assets are no longer needed. Refer to [Unloading Addressable assets](UnloadingAddressableAssets.md).

If you need finer-grained grouping than "everything in one folder" (for example, a subset of files scattered across folders), use [Addressable labels](xref:addressables-labels) instead. A label behaves the same way (one key, many assets) but isn't tied to folder structure.

## Additional resources

* [Load asset references](LoadingAssetReferences.md)
* [Labelling assets](Labels.md)
* [Organize assets into groups](groups-intro.md)
* [Content Packing & Loading schema](ContentPackingAndLoadingSchema.md)
* [Resources system](xref:um-loading-resources-at-runtime)
