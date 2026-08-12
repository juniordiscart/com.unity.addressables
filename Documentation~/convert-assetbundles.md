# Convert AssetBundles to Addressables

When you first open the __Addressables Groups__ window, Unity displays a prompt to convert all AssetBundles into Addressables groups. This is the easiest way to migrate your AssetBundle setup to the Addressables system. You must still update your runtime code to load and release assets using the [`Addressables`](xref:UnityEngine.AddressableAssets.Addressables) API.

If you want to convert your AssetBundle setup manually, select __Ignore__ and manually migrate the AssetBundles to Addressables:

1. Make the assets Addressable by enabling the __Addressable__ option on each asset's Inspector window or by dragging the asset to a group in the [Addressables Groups](GroupsWindow.md) window. The Addressables system ignores existing AssetBundle and label settings for an asset.
1. Change any runtime code that loads assets using the [`AssetBundle`](xref:UnityEngine.AssetBundle) or [`UnityWebRequestAssetBundle`](xref:UnityEngine.Networking.UnityWebRequestAssetBundle) APIs to load them with the [`Addressables`](xref:UnityEngine.AddressableAssets.Addressables) API. You don't need to explicitly load AssetBundle objects themselves or the dependencies of an asset because the Addressables system handles those aspects automatically.
1. Add code to release loaded assets when no longer needed.

> [!NOTE]
> The default path for the address of an asset is its file path. If you use the path as the asset's address, you load the asset in the same way that you load from an AssetBundle. The Addressable asset system handles the loading of the AssetBundle and all its dependencies.

If you chose the automatic conversion option or manually added your assets to equivalent Addressables groups, then, depending on your group settings, you end up with the same set of AssetBundles containing the same assets. The AssetBundle files themselves aren't identical.

## Additional resources

* [Define how groups are packed into AssetBundles](PackingGroupsAsBundles.md)
* [Load AssetBundles](LoadingAssetBundles.md)
* [Introduction to AssetBundles](xref:um-asset-bundles-intro)