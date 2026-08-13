# Load assets

You load and use an Addressable asset in the following ways:

* [Load an asset by an AssetReference that references the asset](#use-assetreferences)
* [Load a single asset](#load-a-single-asset)
* [Load multiple assets](#load-multiple-assets)

Loading Addressable assets uses asynchronous operations. Refer to [Operations](xref:addressables-async-operation-handling) for information about the different ways to approach asynchronous programming in Unity scripts.

## Use AssetReferences

To use an `AssetReference`, add an `AssetReference` field to a `MonoBehaviour` or `ScriptableObject`. After you create an object of that type, you can assign an asset to the field in your object's Inspector window.

> [!NOTE]
> If you assign a non-Addressable asset to an AssetReference field, Unity automatically makes that asset Addressable and adds it to your default Addressables group. AssetReferences also let you use Addressable assets in a Scene that isn't itself Addressable.

Unity doesn't load or release the referenced asset automatically. You must load and release the asset using the `Addressables` API:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadWithReference.cs#doc_LoadWithReference)]

Refer to [Loading an AssetReference](xref:addressables-loading-asset-reference) for additional information about loading AssetReferences.

## Load a single asset

Use the [`LoadAssetAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync*) method to load a single Addressable asset, typically with an address as the key:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadSingle.cs#doc_Load)]

You can use a [label](Labels.md) or other key type when you call `LoadAssetAsync`, not just an address. However, if the key resolves to more than one asset, only the first asset found is loaded. For example, if you call this method with a label applied to several assets, Addressables returns whichever one of those assets it finds first.

## Load multiple assets

Use the [`LoadAssetsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync*) method to load more than one Addressable asset in a single operation. When using this method, you can specify a single key, such as a [label](Labels.md), or a list of keys.

When you specify multiple keys, you can specify a [merge mode](xref:UnityEngine.AddressableAssets.Addressables.MergeMode) to set how the assets that match each key are combined:

* `Union`: Include assets that match any key
* `Intersection`: Include assets that match every key
* `UseFirst`: Include assets only from the first key that resolves to a valid location

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadMultiple.cs#doc_Load)]

To specify how to handle loading errors, use the `releaseDependenciesOnFailure` parameter. If `true`, then the operation fails if it encounters an error loading any single asset. The operation and any assets that loaded are released.

If `false`, then the operation loads any objects that it can and doesn't release the operation. If it fails, the operation still completes with a status of `Failed`. Also, the list of assets returned has null values where the failed assets otherwise appear.

Set `releaseDependenciesOnFailure` to true when loading a group of assets that must be loaded as a set to be used. For example, if you load the assets for a game level, you might fail the operation as a whole rather than load only some of the required assets.

### Load by label

You can load sets of assets that have the same label in one operation:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadWithLabels.cs#doc_LoadWithLabels)]

## Safely edit loaded assets

You can safely edit loaded assets in the following situations:

* The asset is loaded from an AssetBundle.
* The application is running in a Player, not in the Editor.
* When you enable the **Use Existing Build (requires built groups)** option in [Play Mode Scripts](GroupsWindow.md#play-mode-script).

In these cases, the assets exist as a copy in active memory. Changes made to these copied assets don't affect the saved AssetBundle on disk and any changes don't persist between sessions.

For other situations, including when you enable the **Use Asset Database (fastest)** property in the Play mode settings, Unity loads the assets directly from the project files. This means that Unity saves any modifications to the asset during runtime to the project asset file and that those changes persist between different sessions.

If you want to make runtime changes to an asset, create a new instance of the GameObject you want to change and use the copy for any runtime changes. This removes the risk that you might  change the original asset file. The following code example demonstrates creating a new copy of a loaded asset:

```c#
var op = Addressables.LoadAssetAsync<GameObject>("myKey");
yield return op;
if (op.Result != null)
{
    GameObject inst = UnityEngine.Object.Instantiate(op.Result);
    // can now use and safely make edits to inst, without the source Project Asset being changed.
}
```

If you use this example method to use a copy of an asset, be aware of the following:

* You must use either the original asset or the `AsyncOperationHandle` when you release the asset, not the current instance of the asset.
* When you instantiate an asset that has references to other assets in this way, Unity doesn't create new instances of the referenced assets. The references for the newly instantiated copy target the original project asset.
* Unity invokes `MonoBehaviour` methods like `Start()`, `OnEnable()`, and `OnDisable()` on the new instance.

## Additional resources

* [Load assets by location](load-assets-location.md)
* [Load scenes](LoadingScenes.md)
* [Load AssetBundles](LoadingAssetBundles.md)
* [Load assets from multiple projects](MultiProject.md)