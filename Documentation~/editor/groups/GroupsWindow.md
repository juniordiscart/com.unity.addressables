---
uid: addressables-groups-window
---

## Groups window

Use the Groups window to manage your groups and Addressable assets.

The Groups window also serves as a central location for starting content builds and accessing the tools and settings of the Addressables system.

A group is the main organizational unit of the Addressables system. Use this window to create and manage your groups and the assets they contain.

![](../../images/addr_groups_5.png)

*The Addressables Groups window showing the toolbar and list of groups and assets.*

### Group list

The Group list displays the Addressable groups in your Project. Expand a group in the list to show the assets it contains. You can also expand composite assets, such as Sprite sheets, to show the subobjects they contain.

When you first install the Addressables package, the Groups window displays two groups of assets:

* __Built In Data__: contains assets in any Project Resource folders and any Scenes included in the Build Settings list. (None of these assets can be Addressable unless removed from Resources or the Scene list.)

* __Default Local Group (Default)__: Initially empty, any assets you make Addressable are added to this group. The group is set up so that its assets are built to your local build path and included in your Project builds. You can change the name, settings, and make another group the default group, if desired. Note that the settings of the default group are also used to create [shared AssetBundles].

The list columns contain the following information:

| Column| Purpose |
|:---|:---|
| __Notifications__| Any notifications regarding a Group, or asset, that is flagged during the build.
| __Group Name__ \ __Addressable Name__| The name of the item. For groups, this is an arbitrary name that you can assign. For assets, this is the Addressable address. You can edit the name or address using the context menu. |
| __Icon__| The Unity asset icon based on asset type. |
| __Path__| The path to the source asset in your Project. |
| __Labels__| Shows any labels assigned to the asset. Click on a Label entry to change the assigned labels or to manage your label definitions. |

You can sort the assets shown in the Group list by clicking one of the column headers. This sorts the assets within each group, but does not reorder the groups themselves. You can change the order in which your groups are displayed by dragging them into the desired position.

## Groups window toolbar

The toolbar at the top of the Addressables Group window provides access to the following commands and tools:

### Create
Create a group.

![](../../images/addr_groups_template.png)

Choose a template for the group or Blank for no schema.

See [Group templates] for information about creating your own templates.

### Profile
Set the active Profile to determine the paths used for building and loading Addressables.

![](../../images/addr_groups_profile.png)

Select an existing profile or choose __Manage Profiles__ to open the Profiles window.

See [Profiles] for more information.

### Tools
Choose from a menu of settings windows and tools.

![](../../images/addr_groups_tools.png)

* __Inspect System Settings__: open the [Addressables Settings] Inspector.
* __Check for Content Update Restrictions__: run a pre-update content check. See [Content Workflow: Update Restrictions] for more information.
* __Window__: open other Addressables system windows:
  * __Profiles__: open the [Profiles] window.
  * __Labels__: open the [Labels] window.
  * __Analyze__: open the [Analyze] tool
  * __Hosting Services__: open the [Hosting] window.
  * __Event Viewer__: open the [Event Viewer] window.
* __Groups View__: set Group window display options: 
  * __Show Sprite and Subobject Addresses__: whether to show Sprite and subobjects in the Group list or just the parent object. 
  * __Group Hierarchy with Dashes__: when enabled, the Groups window displays groups that contain dashes '-' in their names as if the dashes represented a group hierarchy. For example, if you name two groups "x-y-z" and "x-y-w", the the window shows an entry called "x" with a child called "y", which contains two groups, called "x-y-z" and "x-y-w". Enabling this option affects the group display only.
* __Convert Legacy AssetBundles__: Assigns non-Addressable assets to Addressable groups based on their current AssetBundle settings.

### Play Mode Script 
Set the active Play Mode Script.

![](../../images/addr_groups_pms.png)

The active Play Mode Script determines how Addressables are loaded in the Editor Play mode. See [Play Mode Scripts] for more information.

### Build Script
Select a content build command.

![](../../images/addr_groups_bs.png)

* __New Build__: choose a build script to run a full content build.
* __Update a Previous Build__: run a differential update based on an earlier build.
* __Clean Build__: choose a command to clean existing build artifacts.

See [Builds] for more information.

### Filter list
Find items in the group list matching the specified string.

![](../../images/addr_groups_filter.png)

An item is shown if the specified string matches any part of the text in any column in the list.

> [!TIP]
> Click the magnifying glass icon to enable or disable __Hierarchical Search__, which shows results within their assigned group rather than as a flat list.

## Play Mode Scripts

The active Play Mode Script determines how the Addressable system accesses Addressable assets when you run your game in the Editor Play mode. When you select a Play Mode Script, it remains the active script until you choose a different one. The Play Mode Script has no effect on asset loading when you build and run your application outside the Editor.

The Play Mode Scripts include:

* __Use Asset Database__: loads assets directly from the Editor asset database (which is also used for all non-Addressable assets). You do not have to build your Addressable content when using this option. 
* __Simulate Groups__: analyzes content for layout and dependencies without creating AssetBundles. Loads assets from the asset database through the ResourceManager as if they were loaded through bundles. Simulates download speeds for remote AssetBundles and file loading speeds for local bundles by introducing a time delay. You can use the [Event Viewer] with this Play Mode script. See [ProjectConfigData] for configuration options.
* __Use Existing Build__: loads Assets from bundles created by an earlier content build. You must run a full build using a Build Script such as [Default Build Script] before using this option. Remote content must be hosted at the __RemoteLoadPath__ of the [Profile] used to build the content.

#### To find an asset

To locate an Addressable Asset in the Groups window, type all or part of its address, path, or a label into the filter control on the Groups window toolbar. 

![](../../images/addr_groups_0.png)<br/>*Filtering the group list by the string "NP" to find all assets labeled NPC*

To locate the asset in your project, select it in the Groups window. Unity then selects the asset in the Project window and displays the asset's details in the Inspector window.

> [!TIP]
> * To view the groups of the assets found, enable __Hierarchical Search__; disable this option to only show groups if they match the search string. Click the magnifying glass icon in the search box to enable or disable __Hierarchical Search__.
> * To view subobject addresses, such as the Sprites in a Sprite Atlas, enable the __Show Sprite and Subobject Addresses__ option using the __Tools__ menu on the Groups window toolbar.

[Addressable System Settings]: xref:addressables-asset-settings
[AddressableAssetGroup]: xref:UnityEditor.AddressableAssets.Settings.AddressableAssetGroup
[AddressableAssetGroupSchema]: xref:UnityEditor.AddressableAssets.Settings.AddressableAssetGroupSchema
[Addressables Groups window]: xref:addressables-groups-window
[Addressables Settings]: xref:addressables-asset-settings
[Addressables system settings]: xref:addressables-asset-settings
[Analyze]: xref:addressables-analyze-tool
[AssetBundle Compression]: xref:AssetBundles-Cache
[AssetReference]: xref:addressables-asset-references
[Build scripts]: xref:addressables-builds#build-commands
[Builds]: xref:addressables-builds
[Content update builds]: xref:addressables-content-update-builds
[Content Workflow: Update Restrictions]: xref:addressables-content-update-builds#settings
[Custom Inspector scripts]: xref:VariablesAndTheInspector
[Default Build Script]: xref:addressables-builds
[Event Viewer]: xref:addressables-event-viewer
[Group settings]: xref:addressables-group-schemas
[Group Templates]: xref:group-templates
[Group templates]: xref:group-templates
[Hosting]: xref:addressables-asset-hosting-services
[Labels]: xref:addressables-labels
[Loading Addressable assets]: xref:addressables-api-load-asset-async
[Organizing Addressable Assets]: xref:addressables-assets-development-cycle#organizing-addressable-assets
[Play Mode Scripts]: #play-mode-scripts
[Profile]: xref:addressables-profiles
[Profiles]: xref:addressables-profiles
[ProjectConfigData]: xref:UnityEditor.AddressableAssets.Settings.ProjectConfigData
[Schema]: xref:addressables-group-schemas#schemas
[settings of the group]: xref:addressables-group-schemas
[shared AssetBundles]: xref:addressables-build-artifacts#shared-assetbundles
[template]: xref:addressables-group-schemas#group-templates
[UnityWebRequestAssetBundle.GetAssetBundle]: xref:UnityEngine.Networking.UnityWebRequest.GetAssetBundle(System.String,System.UInt32)
[AssetBundle.LoadFromFileAsync]: xref:UnityEngine.UnityEngine.AssetBundle.LoadFromFileAsync(System.String,System.UInt32,System.UInt64)