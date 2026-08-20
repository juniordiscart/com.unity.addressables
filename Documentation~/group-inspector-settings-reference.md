---
uid: addressables-content-packing-and-loading-schema
---

# Group Inspector settings reference

Reference for Addressable Asset Group Inspector settings that control build paths, load paths, and content update restrictions.

When you select an [Addressable group](groups-create.md), you can control its settings in the Inspector. To open a group's settings, select the group from its containing folder in the **Project** window. Alternatively, open the [Addressables Groups window](GroupsWindow.md) (**Window &gt; Asset Management &gt; Addressables &gt; Groups**), then select a group. The group's settings are displayed in the Inspector.

![The Inspector window for a group using the Content Packing & Loading schema (left), and the Content Directory schema (right).](images/groups-settings.png)<br/>*The Inspector window for a group using the Content Packing & Loading schema (left), and the Content Directory schema (right).*

To add schemas to an Addressable Asset Group, select the **Add Schema** button. You can choose from the following:

* __Content Directory__: Defines the settings for building and loading Addressable assets using the content directory system. Select this option for new projects with assets that you plan on distributing locally.
* [__Content Packing & Loading__](#content-packing--loading-schema): Defines the settings for building and loading Addressable assets using the AssetBundle system. Select this option if you're using an older project that uses AssetBundles, or you want to distribute assets remotely.
* [__Content Update Restrictions__](#content-update-restriction): Defines settings for making differential updates of an earlier build.

## Build & Load Paths

Both the __Content Directory__ and __Content Packing & Loading__ schemas contain the same **Build & Load Paths** section. This is the only entry in the __Content Directory__ schema.

>[!IMPORTANT]
>While the __Content Directory__ schema has a __RemoteBuildPath__ option, content directories don't support remote content delivery yet. Use the __Content Packing & Loading__ schema to build remote AssetBundles instead.

The **Build & Load Paths** settings determine where the artifacts for your content builds are created and where the Addressables system loads them at runtime. The build and load path options are defined by variables in a [Profile](xref:addressables-profiles).

| **Property**| **Description** |
|:---|---|
| __Build & Load Paths__ | The [Profile path](xref:addressables-profiles) pair that defines where the Addressables build system creates artifacts for this group and where the Addressables system loads those artifacts at runtime. Choose a path pair from the list or select `<custom>` if you want to set the build and load paths separately.|
| __Build Path__<br/><br/>__Load Path__|Only available if you set __Build & Load Paths__ to `<custom>`. A Profile variable that defines where the Addressables build system creates artifacts for this group, or loads the build artifacts of the group. You can also set a custom string. Use one of the following:<ul><li>__LocalBuildPath__: Use for assets that you plan to distribute as part of your application installation.</li><li>__RemoteBuildPath__: Use for assets that you plan to distribute using a remote hosting service such as Unity Cloud Content Delivery or other Content Delivery Network.</li><li>__Custom__: Specify a string as the path for this group.</li></ul>|

When you choose a Profile variable, a preview of the path is displayed in the __Path Preview__. Components of the path in braces, such as `{UnityEngine.AddressableAssets.Addressable.RuntimePath}`, indicate that a static variable is used to construct the final path at runtime. That portion of the path is replaced by the current value of the static variable when the Addressables system initializes at runtime.

> [!WARNING]
> If you change the local build or load paths from their default values, you must copy the local build artifacts from your custom build location to the project's `StreamingAssets` folder before making a Player build. Changing these paths also prevents building your Addressables as part of the Player build.

## Content Packing & Loading schema

Defines the settings for building and loading Addressable assets using the AssetBundle system.

### Advanced Options

| **Property**| **Description** |
|:---|---|
|__Use Defaults__|Uses the default compression settings, which is LZ4.|
| __Asset Bundle Compression__| The compression type for all bundles produced from the group. Choose from the following:<ul><li>**Uncompressed**:This option is largest on disk, and fastest to load. If your application has space to spare, consider this option for local content. An advantage of uncompressed bundles is how they handle being patched. If you're developing for a platform where the platform itself provides patching, uncompressed bundles provide the most accurate (smallest) patching.</li><li>**LZ4**: A chunk-based compression which provides the ability to load parts of the file without needing to load it in its entirety.</li><li>**LZMA**: Use LZMA for all remote content, but not for any local content. It provides the smallest bundle size, but is slow to load. If you store local bundles in LZMA you can create a smaller player, but load times are significantly worse than uncompressed or LZ4. For downloaded bundles, LZMA avoids the slow load time by recompressing the downloaded bundle when storing it in the AssetBundle cache.</li></ul> For more information about AssetBundle caching, refer to [AssetBundle compression formats](xref:um-asset-bundles-cache).|
| __Use Asset Bundle Cache__| Cache remotely distributed bundles. |
|__Cache Clear Behavior__|Controls how old AssetBundles are cleared. Choose from the following:<ul><li>**Clear When Space Is Needed In Cache**</li><li>**Clear When New Version Loaded**</li></ul>|
| __Asset Bundle CRC__| Set how to verify an AssetBundle's integrity before loading it: <ul><li>__Disabled__: Never check AssetBundle integrity. If the application download performs a check on the download before saving to disk, consider setting this property.</li><li> __Enabled, Including Cached__: Always check AssetBundle integrity. Use in situations where the data needs to be checked every time such as settings values.</li><li> __Enabled, Excluding Cached__: Check the integrity of AssetBundles when downloading. This is useful for remote assets.</li></ul> Checking for a change in the file requires the entire AssetBundle to be decompressed and the check processed on the uncompressed bytes. If an AssetBundle contains data that might have changed, such as settings values, then you might want to consider enabling CRC checks on saved AssetBundles.|
| __Bundle Naming Mode__| Set how to construct the file names of AssetBundles:<ul><li>__Filename__: Leave the file name unchanged.</li><li>__Append Hash to Filename__: Append the AssetBundle content hash to the file name. (default)</li><li>__Use Hash of AssetBundle__: Replace the file name with the AssetBundle hash.</li><li> __Use Hash of Filename__: Replace the file name with a hash of the file name.</li></ul>|
|__Strip Bundle Download Options__|Strips unused AssetBundle download data from the catalog to reduce its size. Only enable this for local groups. It applies only to binary catalogs, and is unavailable if [__Use UnityWebRequest for Local Asset Bundles__](AddressableAssetSettings.md#downloads) is enabled.|
|__Include Addresses in Catalog__|Include the address strings in the catalog. If you don't use address strings to load assets in the group, you can disable this property to decrease the size of the catalog.|
|__Include GUIDs in Catalog__|Include GUID strings in the catalog. You must include GUID strings to access an asset with an [`AssetReference`](xref:addressables-asset-references). If you don't use `AssetReference` or GUID strings to load assets, you can disable this property to decrease the size of the catalog.|
|__Include Labels in Catalog__|Include label strings in the catalog. If you don't use labels to load assets, you can disable this property to decrease the size of the catalog. |
|__Include Folder Keys in Catalog__|Include the address of each addressable folder as a shared key on every asset inside it. Enable this to load every asset in an addressable folder with one call, for example `Addressables.LoadAssetsAsync<GameObject>(folderAddress, ...)`, similar to `Resources.LoadAll`. If you don't need to load a whole folder at once, you can disable this property to decrease the size of the catalog.|
|__Exclude Individual Addresses for Folder Assets__|Only shown when __Include Folder Keys in Catalog__ is enabled. If enabled, assets inside an addressable folder don't get their own individual address added to the catalog &mdash; only the folder's shared key is added. GUIDs are unaffected, so [`AssetReference`](xref:addressables-asset-references) into folder assets keeps working. Use this if you always load these assets via the folder and never reference an individual asset by its own full address, to further decrease the size of the catalog.|
| __Bundle Mode__| Select how to pack the assets in this group into AssetBundles:<ul><li>__Pack Together__: Create a single AssetBundle containing all assets.</li><li>__Pack Separately__: Create an AssetBundle for each primary asset in the group. Subassets, such as sprites in a sprite sheet are packed together. Assets within a folder added to the group are also packed together.</li><li>__Pack Together by Label__: Create an AssetBundle for assets sharing the same combination of labels.</li></ul>|

## Content Update Restriction

>[!IMPORTANT]
>The following setting is only applicable if you're using [AssetBundles as the content build system](content-build-systems.md) for your project.

The Content Update Restriction schema determines how [Check for Content Update Restrictions](GroupsWindow.md#tools) treats assets in the group. To prepare your groups for a differential content update build rather than a full content build, open the [Addressables Groups window](xref:addressables-groups-window), go to **Tools** and run the **Check for Content Update Restrictions** command. The tool moves modified assets in any groups with the __Prevent Updates__ property enabled to a new group.

The **Prevent Updates** property acts in the following way:

* **Enabled**: If any assets in the AssetBundle have changed, then the [Check for Content Update Restrictions](builds-update-build.md#check-for-content-update-restrictions) tool moves them to a new group created for the update. When you make the update build, the assets in the AssetBundles created from this new group override the versions found in the existing AssetBundles.
* **Disabled**: The tool doesn't move any assets. When you make the update build, if any assets in the AssetBundle have changed, then the entire AssetBundle is rebuilt.

## Additional resources

* [AssetBundle compression formats](xref:um-asset-bundles-cache)
* [Addressables Groups window](GroupsWindow.md)
