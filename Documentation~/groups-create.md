# Add assets to groups

Create groups, add and remove assets from groups, and manage group organization using the **Addressables Groups** window.

To manage groups and Addressables assets, open the **Addressables Groups** window by going to **Window** &gt;**Asset Management** &gt; **Addressables** &gt; **Groups**. Refer to [Addressables Groups window](xref:addressables-groups-window) for details about the features of this window.

![The Addressables Groups window showing the toolbar and list of groups and assets.](images/addressables-groups-window.png)<br/><br/>*The Addressables Groups window showing the toolbar and list of groups and assets.*

## Create a group

To create a group:

1. Open the Addressables Groups window: **Window** &gt; **Asset Management** &gt; **Addressables** &gt; **Groups**
1. Select **New** and choose from:
    * **Content Directory**: Builds assets into a content directory. Select this option for new projects with assets that you plan on distributing locally.
    * **Packed Asset**: Builds assets into AssetBundles. Select this option if you're using an older project that uses AssetBundles, or you want to distribute assets remotely.
    * **Blank**: Creates a group with [no schema](GroupSchemas) attached to it. The default build script can't process assets in a blank group.
    * **Custom template**: If you've created a custom [group template](GroupTemplates.md) it appears in this dropdown.

    You can also right-click in the window and select **Create New Group** to create a new group.
1. Right click the new group and select **Rename** to rename the group.
1. Select the group to view its [group settings](group-inspector-settings-reference.md) in the Inspector.

For groups that contain assets that you plan to distribute with your main application, use the default settings.

> [!TIP]
> You can optionally use the **[Auto Group Generator window](groups-auto-group-generator.md)** to automatically generate optimized groups for assets and their dependencies.

### Groups for remote distribution

> [!IMPORTANT]
> Remote distribution is only compatible with the AssetBundle system, so you must use the **Packed Asset** group for assets you want to distribute remotely.

For groups containing assets that you plan to distribute remotely, you must do the following:

* Use the **Packed Asset** group type.
* Change the build and load paths to use the remote versions of the [profile](AddressableAssetsProfiles) path variables.
* To build content for remote distribution, enable the __Build Remote Catalog__ option in the [Addressable System Settings](AddressableAssetSettings.md).

## Add assets to a group

To add an asset to a group, perform one of the following steps:

* Open the Groups window (**Window** &gt; **Asset Management** &gt; **Addressables** &gt; **Groups**) and drag the assets from the Project window into the desired group.
* Drag the assets from one group into another.
* Select an asset, or a folder, and in its Inspector window, enable the **Addressables** option. This adds the asset, or the contents of the folder to the default group. You can then use the object picker to select a different group.

> [!NOTE]
> If you add assets in a Resources folder to a group, the Addressables system first moves the assets to a non-Resource location. You can move the assets elsewhere, but you can't store Addressable assets in a Resources folder in your project.

## Remove assets from a group

Select one or more assets in the Groups window and right-click to open the context menu, then select **Remove Addressables**. You can also select the assets and press the Delete key to remove the assets from the group.

## Add or remove labels

Select one or more assets in the Groups window, then select the label field for one of the selected assets.

To assign labels, enable or disable the checkboxes for the desired labels.

To add, remove or rename your labels, select the __+__ button, then select __Manage Labels__. To only add a new label, select the __+__ button and then select __New Label__. For more information on how to use labels, refer to [Labelling assets](Labels.md).

## Additional resources

* [Labelling assets](Labels.md)
* [Define how groups are packed into AssetBundles](PackingGroupsAsBundles.md)
* [Addressables Groups window reference](GroupsWindow.md)
* [Content packing settings reference](group-inspector-settings-reference.md)
