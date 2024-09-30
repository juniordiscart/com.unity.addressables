---
uid: addressables-groups-window
---

# Addressables Groups window reference

Use the Addressables Groups window to manage your groups and Addressable assets. To open the window, go to **Window** &gt; **Asset Management** &gt; **Addressables** &gt; **Groups**.

The Groups window also serves as a central location for starting content builds and accessing the tools and settings of the Addressables system.

A group is the main organizational unit of the Addressables system. Use this window to create and manage your groups and the assets they contain.

![](images/addressables-groups-window.png)<br/><br/>*The Addressables Groups window showing the toolbar and list of groups and assets.*

## Group list

The Group list displays the Addressable groups in your Project. Expand a group in the list to display the assets that it contains. You can also expand composite assets, such as sprite sheets, to display the sub-objects they contain.

When you first install the Addressables package, the Groups window displays two groups of assets:

* __Default Local Group (Default)__: Initially empty, Unity adds any assets you make Addressable to this group. The group is set up so that its assets are built to your local build path and included in your Project builds. You can change the name, settings, and make another group the default group, if desired. The settings of the default group are also used to create [shared AssetBundles](xref:addressables-build-artifacts).

The list columns contain the following information:

| **Column**|**Description** |
|---|---|
| __Notifications__| Any notifications about a Group, or asset, that's flagged during the build.
| __Group Name \ Addressable Name__| The name of the item. For groups, this is an arbitrary name that you can assign. For assets, this is the Addressable address. You can edit the name or address using the context menu. |
| __Icon__| The Unity asset icon based on asset type. |
| __Path__| The path to the source asset in your project. |
| __Labels__| Displays any labels assigned to the asset. Click on a label entry to change the assigned labels or to manage your label definitions. |

To sort the assets displayed in the group list, select one of the column headers. This sorts the assets within each group, but doesn't reorder the groups. To change the order that the groups are displayed, drag them into the desired position.

## Groups window toolbar

The toolbar at the top of the Addressables Group window provides the following settings:

|**Setting**|**Description**|
|---|---|
|**New**|Choose a template to create a new group, or blank for no schema. Refer to [Group templates](xref:group-templates) for information about creating your own templates.|
|**Profile**|Set the active Profile to select the paths used for building and loading Addressables. Choose an existing profile or select __Manage Profiles__ to open the [Profiles window](xref:addressables-profiles).|
|**Tools**|Open the various Addressables tools available. Choose from:<br/><br/> - __Inspect System Settings__: Open the [Addressables Settings](xref:addressables-asset-settings) Inspector.<br/>- __Check for Content Update Restrictions__: Run a pre-update content check. Refer to [Update Restrictions](xref:addressables-content-update-builds) for more information.<br/>- __Window__: Open other Addressables system windows: [Profiles](xref:addressables-profiles), [Labels](xref:addressables-labels), or the [Addressables Report](addressables-report.md)  window.<br/>- __Groups View__: Set Group window display options:<br/>- __Show Sprite and Subobject Addresses__: Enable to display sprite and sub-objects in the Group list or just the parent object<br/>- __Group Hierarchy with Dashes__: Enable to display groups that contain dashes `-` in their names as if the dashes represented a group hierarchy. For example, if you name two groups `x-y-z` and `x-y-w`, the window displays an entry called `x` with a child called `y`, which contains two groups, called `x-y-z` and `x-y-w`. Enabling this option affects the group display only.|
|**Play mode Script**|Set the active Play mode Script. The active Play mode Script determines how Addressables are loaded in the Editor Play mode. Refer to [Play mode Scripts](#play-mode-scripts) for more information.|
|**Build**|Select a content build command:<br/><br/> - __New Build__: Choose a build script to run a full content build.<br/>- __Update a Previous Build__: Run a differential update based on an earlier build.<br/>- __Clear Build Cache__: choose a command to clean existing build artifacts.<br/><br/>Refer to [Builds](xref:addressables-builds) for more information.|

## Play mode Scripts

The active Play mode script determines how the Addressable system accesses Addressable assets when you run your game in Play mode. When you select a Play mode script, it remains the active script until you choose a different one. The Play mode Script has no effect on asset loading when you build and run your application outside the Unity Editor.

The Play mode Scripts include:

* __Use Asset Database__: Loads assets directly from the Editor Asset Database, which is also used for all non-Addressable assets. You don't have to build your Addressable content when using this option.
* __Use Existing Build__: Loads assets from bundles created by an earlier content build. You must run a full build using a Build Script such as [Default Build Script](xref:addressables-builds) before using this option. Remote content must be hosted at the __RemoteLoadPath__ of the Profile used to build the content.

## Find an asset

To locate an Addressable Asset in the Groups window, type all or part of its address, path, or a label into the filter control on the Groups window toolbar.

![](images/addressables-groups-find.png)<br/>*Filtering the group list by the string "NP" to find all assets labeled NPC*

To locate the asset in your project, select it in the Groups window. Unity then selects the asset in the Project window and displays the asset's details in the Inspector window.

> [!TIP]
> * To view the groups of the assets found, enable __Hierarchical Search__. Disable this option to only display groups if they match the search string. Select the magnifying glass icon in the search box to enable or disable __Hierarchical Search__.
> * To view sub-object addresses, such as the Sprites in a Sprite Atlas, enable the __Show Sprite and Subobject Addresses__ option using the __Tools__ menu on the Groups window toolbar.


## Group context menu

To open the Group context menu and access group-related commands, right-click on a group name.

| **Command**| **Description** |
|:---|:---|
| __Remove Group(s)__| Removes the Group and deletes its associated ScriptableObject asset. Unity reverts any assets in the group into non-Addressable assets.  |
| __Simplify Addressable Names__| Shortens the name of assets in the group by removing path-like components and extensions. |
| __Set as Default__| Sets the group as the default group. When you mark an asset as Addressable without explicitly assigning a group, Unity adds the asset to the default group. |
| __Inspect Group Settings__| Selects the group asset in the Unity Project window and in the Inspector window so that you can view the settings. |
| __Rename__| Enables you to edit the name of the group. |
| __Create New Group__| Creates a new group based on a group template. |

## Asset context menu

To open the Addressable asset context menu and access asset-related commands, right-click on an asset.

| **Command**| **Description** |
|:---|:---|
| __Move Addressables to Group__| Move the selected assets to a different, existing group. |
| __Move Addressables to New Group__| Create a new group with the same settings as the current group and move the selected assets to it. |
| __Remove Addressables__| Remove the selected assets from the Group and make the assets non-Addressable.  |
| __Simplify Addressable Names__| Shortens the names of the selected assets by removing path-like components and extensions. |
| __Copy Address to CLipboard__| Copies the asset's assigned address string to your system Clipboard. |
| __Change Address__| Edit the asset's name. |
| __Create New Group__| Create a new group based on a group template. This doesn't move the selected assets. |
