# Manage and create groups

To manage your groups and Addressables assets, open the Addressables Groups window by going to **Window** &gt;**Asset Management** &gt; **Addressables** &gt; **Groups**. Refer to [Addressables Groups window](xref:addressables-groups-window) for details about the features of this window.

## Create a group

To create a group:

1. Go to **Window** &gt; **Asset Management** &gt; **Addressables** and select **Groups** to open the Addressables Groups window.
1. Select **New** &gt; **Packed Asset** to create a new group. If you've created your own [group templates](xref:group-templates), they are also displayed in the menu.
1. Context click the new group and select **Rename** to rename the group.
1. Open the context menu again and select **Inspect Group Settings**.
1. Adjust the group settings as desired.

For groups that contain assets that you plan to distribute with your main application, the default settings are a reasonable starting point. For groups containing assets that you plan to distribute remotely, you must change the build and load paths to use the remote versions of the [Profile](xref:addressables-profiles) path variables. To build AssetBundles for remote distribution, you must also enable the __Build Remote Catalog__ option in the [Addressable System Settings](xref:addressables-asset-settings).

Refer to [Group settings](xref:addressables-group-schemas) for more information about individual settings.

## Add assets to a group

Use one of the following methods to add an asset to a group:

* Drag the assets from the Project window into the Group window and drop them into the desired group.
* Drag the assets from one group into another.
* Select the asset to open it in the Inspector window and enable the **Addressables** option. This adds the asset to the default group. Use the group context menu to change which group is the default group.
* Add the folder containing the assets to a group. All assets added to the folder are included in the group.

> [!NOTE]
> If you add assets in a Resources folder to a group, the Addressables system first moves the assets to a non-Resource location. You can move the assets elsewhere, but you can't store Addressable assets in a Resources folder in your project.

## Remove assets from a group

Select one or more assets in the Groups window and right-click to open the context menu, then select **Remove Addressables**. You can also select the assets and press the Delete key to remove the assets from the group.

## Add or remove labels

Select one or more assets in the Groups window, then select the label field for one of the selected assets.

To assign labels, enable or disable the checkboxes for the desired labels.

To add, remove or rename your labels, select the __+__ button, then select __Manage Labels__. To only add a new label, select the __+__ button and then select __New Label__. Refer to [Labels](xref:addressables-labels) for more information on how to use labels.
