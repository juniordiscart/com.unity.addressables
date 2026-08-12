# Introduction to Addressable asset groups

Understand how to use groups to organize Addressables, control build paths, load paths, and AssetBundle packaging strategies.

A group is the main organizational unit of the Addressables system. Create and manage your groups and the assets they contain with the [Addressables Groups window](GroupsWindow.md).

To control how Unity handles assets during a content build, organize Addressables into groups and assign different settings to each group as required.

![The Addressables Groups window showing the toolbar and list of groups and assets.](images/addressables-groups-window.png)<br/><br/>*The Addressables Groups window showing the toolbar and list of groups and assets.*

When you begin a content build, the build scripts create AssetBundles that contain the assets in a group. The build determines the number of AssetBundles to create and where to create them from both the [settings of the group](GroupSchemas.md) and the [Addressables system settings](AddressableAssetSettings.md). For more information, refer to [Builds](Builds.md).

> [!NOTE]
> Addressable Groups only exist in the Unity Editor. The Addressables runtime code doesn't use a group concept. However, you can [assign a label](Labels.md) to the assets in a group if you want to find and load all the assets that were part of that group. For more information, refer to [Loading Addressable assets](LoadingAddressableAssets.md).

Unity saves the groups you create in the `AssetGroups` subfolder of `AddressableAssetsData`. When you select a group in this folder, you can use the Inspector to define the following:

* **Build paths**: Where to save your content after a content build.
* **Load paths**: Where your application looks for built content at runtime.
* **Bundle mode**: How to package the content in the group into a bundle. You can choose the following options:
    * One bundle containing all group assets
    * A bundle for each entry in the group (useful if you mark entire folders as Addressable and want their contents built together)
    * A bundle for each unique combination of labels assigned to group assets
* **Content update restriction**: Restrict groups when creating content update builds. For more information, refer to [Content update builds](ContentUpdateWorkflow.md).

For full details of each setting, refer to [Content packing settings reference](ContentPackingAndLoadingSchema.md).

You can also use profile variables to automatically set these paths. For more information, refer to [Profiles](AddressableAssetsProfiles.md).

## Additional resources

* [Add assets to groups](groups-create.md)
* [Define group settings](GroupSchemas.md)
* [Labelling assets](Labels.md)
* [Addressables Groups window reference](GroupsWindow.md)
