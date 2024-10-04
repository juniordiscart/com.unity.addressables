# Load assets

You can use `LoadAssetAsync` or `LoadAssetsAsync` to load one, or multiple assets at runtime.

## Load a single asset

Use the [`LoadAssetAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync*) method to load a single Addressable asset, typically with an address as the key:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadSingle.cs#doc_Load)]

You can use a label or other key type when you call `LoadAssetAsync`, not just an address. However, if the key resolves to more than one asset, only the first asset found is loaded. For example, if you call this method with a label applied to several assets, Addressables returns whichever one of those assets that happens to be located first.

## Load multiple assets

Use the [`LoadAssetsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync*) method to load more than one Addressable asset in a single operation. When using this method, you can specify a single key, such as a label, or a list of keys.

When you specify multiple keys, you can specify a [merge mode](xref:UnityEngine.AddressableAssets.Addressables.MergeMode) to set how the assets that match each key are combined:

* `Union`: Include assets that match any key
* `Intersection`: Include assets that match every key
* `UseFirst`: Include assets only from the first key that resolves to a valid location

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadMultiple.cs#doc_Load)]

To specify how to handle loading errors, use the `releaseDependenciesOnFailure` parameter. If `true`, then the operation fails if it encounters an error loading any single asset. The operation and any assets that loaded are released.

If `false`, then the operation loads any objects that it can and doesn't release the operation. If it fails, the operation still completes with a status of `Failed`. Also, the list of assets returned has null values where the failed assets would otherwise appear.

Set `releaseDependenciesOnFailure` to true when loading a group of assets that must be loaded as a set to be used. For example, if you load the assets for a game level, you might fail the operation as a whole rather than load only some of the required assets.
