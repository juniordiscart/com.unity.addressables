# Convert AssetBundles

When you first open the **Addressables Groups** window, Unity offers to convert all AssetBundles into Addressables groups. This is the easiest way to migrate your AssetBundle setup to the Addressables system. You must still update your runtime code to load and release assets using the `Addressables` API.

If you want to convert your AssetBundle setup manually, click the **Ignore** button. The process for manually migrating your AssetBundles to Addressables is similar to that described for scenes and the Resources folder:

1. Make the assets Addressable by enabling the **Addressable** property on each asset’s Inspector window or by dragging the asset to a group in the Addressables Groups window. The Addressables system ignores existing AssetBundle and label settings for an asset.
2. Change any runtime code that loads assets using the AssetBundle or UnityWebRequestAssetBundle APIs to load them with the Addressables API. You don't need to explicitly load AssetBundle objects themselves or the dependencies of an asset; the Addressables system handles those aspects automatically.
3. Add code to release loaded assets when no longer needed.

>[!NOTE]
> The default path for the address of an asset is its file path. If you use the path as the asset's address, you'd load the asset in the same manner as you would load from a bundle. The Addressable asset system handles the loading of the bundle and all its dependencies.

If you chose the automatic conversion option or manually added your assets to equivalent Addressables groups, then, depending on your group settings, you end up with the same set of bundles containing the same assets. The bundle files themselves won't be identical.
