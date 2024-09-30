# Make an asset Addressable

You can make an asset Addressable in the following ways:

* Enable the __Addressable__ property in the asset's **Inspector** window.
* Assign the asset to an AssetReference field in the **Inspector** window.
* Drag the asset into a group on the [Addressables Groups](GroupsWindow.md) window.
* In the Project window, move the asset into a folder that's marked as Addressable.

<!-- 
* Enable the __Addressable__ setting in the asset's Inspector:
  ![](images/get-started-addressable-setting.png)
* Drag or assign the asset to an AssetReference field in an Inspector:
  ![](images/get-started-assetreference.png)
* Drag the asset into a group on the [Addressables Groups](GroupsWindow.md) window:
  ![](images/get-started-addressables-groups.png)
* Put the asset in a Project folder that's marked as Addressable:
  ![](images/get-started-addressables-folder.png) -->

Once you make an asset Addressable, the Addressables system adds it to a default group, unless you place it in a specific group. Addressables packs assets in a group into [AssetBundles](xref:AssetBundlesIntro) according to your group settings when you make a [content build](xref:addressables-builds). You can load these assets using the [Addressables API](xref:addressables-api-load-asset-async).

> [!NOTE] 
> If you make an asset in the [Resources folder](xref:SpecialFolders) Addressable, Unity displays a prompt asking whether you want Unity to move the asset out of the Resources folder. You can move the asset to a different folder in your Project, but you can't store Addressable assets in the Resources folder.
