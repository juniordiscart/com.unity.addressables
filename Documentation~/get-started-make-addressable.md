# Make an asset Addressable

You can make an asset Addressable in the following ways:

* Enable the **Addressable** checkbox in the Inspector window for either the asset itself or for its parent folder.
* Drag or assign the asset to an [AssetReference](AssetReferences.md) field in the Inspector window.
* Drag the asset into a group on the [Addressables Groups](GroupsWindow.md) window.

Once you make an asset Addressable, the Addressables system adds it to a default group, unless you place it in a specific group. Addressables packs assets in a group into [AssetBundles](xref:AssetBundlesIntro) according to your group settings when you make a [content build](xref:addressables-builds). You can load these assets using the [Addressables API](xref:addressables-api-load-asset-async).

> [!NOTE]
> If you make an asset in a [Resources folder](xref:SpecialFolders) Addressable, Unity moves the asset out of the Resources folder. You can move the asset to a different folder in your Project, but you cannot store Addressable assets in a Resources folder.
