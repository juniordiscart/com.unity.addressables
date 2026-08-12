---
uid: addressables-content-packing-and-loading-schema
---

# Group Inspector settings reference

Reference for Group Inspector settings that control build paths, load paths, bundle modes, and content update restrictions.

When you select an Addressable group, you can control its settings in the Inspector. To open a group's settings, select the group from its containing folder in the **Project** window. Alternatively, open the [Addressables Groups window](GroupsWindow.md) (**Window &gt; Asset Management &gt; Addressables &gt; Groups**), then select a group. The group's settings are displayed in the Inspector.

![The Inspector window for the Default Local Group.](images/groups-default-settings.png)<br/>*The Inspector window for the Default Local Group.*

## Inspect Top Level Settings

Displays the [Addressables Asset Settings](AddressableAssetSettings.md) window for this group.

## Content Packing & Loading

The Content Packing & Loading schema is the main Addressables schema used by the default build script, and it defines the settings for building and loading Addressable assets.

### Build and Load Paths

Determine where the artifacts for your content builds are created and where the Addressables system loads them at runtime. The build and load path options are defined by variables in a [Profile](xref:addressables-profiles).

| **Property**| **Description** |
|:---|---|
| __Build & Load Paths__ | The [Profile path](xref:addressables-profiles) pair that defines where the Addressables build system creates artifacts for this group and where the Addressables system loads those artifacts at runtime. Choose a path pair from the list or select `<custom>` if you want to set the build and load paths separately.|
| __Build Path__<br/><br/>__Load Path__|Only available if you set __Build & Load Paths__ to `<custom>`. A Profile variable that defines where the Addressables build system creates artifacts for this group, or loads the build artifacts of the group. You can also set a custom string. Use one of the following:<ul><li>__LocalBuildPath__: Use for assets that you plan to distribute as part of your application installation.</li><li>__RemoteBuildPath__: Use for assets that you plan to distribute using a remote hosting service such as Unity Cloud Content Delivery or other Content Delivery Network.</li><li>__Custom__: Specify a string as the path for this group.</li></ul>|

When you choose a Profile variable, a preview of the path is displayed in the __Path Preview__. Components of the path in braces, such as `{UnityEngine.AddressableAssets.Addressable.RuntimePath}`, indicate that a static variable is used to construct the final path at runtime. That portion of the path is replaced by the current value of the static variable when the Addressables system initializes at runtime.

> [!WARNING]
> If you change the local build or load paths from their default values, you must copy the local build artifacts from your custom build location to the project's `StreamingAssets` folder before making a Player build. Changing these paths also prevents building your Addressables as part of the Player build.

## Advanced Options

| **Property**| **Description** |
|:---|---|
| __Asset Bundle Compression__| The compression type for all AssetBundles produced from the group. Choose from the following:<ul><li>**Uncompressed**:This option is largest on disk, and fastest to load. If your application has space to spare, consider this option for local content. An advantage of uncompressed bundles is how they handle being patched. If you're developing for a platform where the platform itself provides patching, uncompressed bundles provide the most accurate (smallest) patching.</li><li>**LZ4**: A chunk-based compression which provides the ability to load parts of the file without needing to load it in its entirety.</li><li>**LZMA**: Use LZMA for all remote content, but not for any local content. It provides the smallest bundle size, but is slow to load. If you store local bundles in LZMA you can create a smaller player, but load times are significantly worse than uncompressed or LZ4. For downloaded bundles, LZMA avoids the slow load time by recompressing the downloaded bundle when storing it in the AssetBundle cache.</li></ul> For more information about AssetBundle caching, refer to [AssetBundle compression formats](xref:um-asset-bundles-cache).|
| __Include In Build__| Enable this property to include assets in this group in a content build.  |
| __Force Unique Provider__| Enable this property to use unique instances of Resource Provider classes for this group. Enable this option if you have custom Provider implementations for the asset types in this group and instances of those Providers must not be shared between groups. |
| __Use Asset Bundle Cache__| Cache remotely distributed AssetBundles. |
| __Asset Bundle CRC__| Set how to verify a bundle's integrity before loading it: <ul><li>__Disabled__: Never check bundle integrity. If the application download performs a check on the download before saving to disk, consider setting this property.</li><li> __Enabled, Including Cached__: Always check bundle integrity. Use in situations where the data needs to be checked every time such as settings values.</li><li> __Enabled, Excluding Cached__: Check integrity of bundles when downloading. This is useful for remote assets.</li></ul> Checking for a change in the file requires the entire AssetBundle to be decompressed and the check processed on the uncompressed bytes. If your AssetBundle contains data that might be tampered with, such as settings values, then you might want to consider enabling CRC checks on saved AssetBundles.|
|__Use UnityWebRequest for Local Asset Bundles__|Loads local AssetBundle archives from this group using [`UnityWebRequestAssetBundle.GetAssetBundle`](xref:UnityEngine.Networking.UnityWebRequest.GetAssetBundle(System.String,System.UInt32)) instead of [`AssetBundle.LoadFromFileAsync`](xref:UnityEngine.AssetBundle.LoadFromFileAsync(System.String,System.UInt32,System.UInt64)). |
| __Request Timeout__| The timeout interval for downloading remote AssetBundles. |
| __Use Http Chunked Transfer__| Enable this property to use the HTTP/1.1 chunked-transfer encoding method when downloading bundles. <br/><br/> Deprecated and ignored in Unity 2019.3+. |
| __Http Redirect Limit__| The number of redirects allowed when downloading AssetBundles. Set to -1 for no limit. |
| __Retry Count__| The number of times to retry failed downloads. |
|__Include Addresses in Catalog__|Include the address strings in the catalog. If you don't use address strings to load assets in the group, you can disable this property to decrease the size of the catalog.|
|__Include GUIDs in Catalog__|Include GUID strings in the catalog. You must include GUID strings to access an asset with an [`AssetReference`](xref:addressables-asset-references). If you don't use `AssetReference` or GUID strings to load assets, you can disable this property to decrease the size of the catalog.|
|__Include Labels in Catalog__|Include label strings in the catalog. If you don't use labels to load assets, you can disable this property to decrease the size of the catalog. |
|__Internal Asset Naming Mode__|Determines the identification of assets in AssetBundles and is used to load the asset from the AssetBundle. This value is used as the `internalId` of the asset location. Changing this setting affects the AssetBundle's CRC and Hash value. <br/><br/>**Warning**: Don't modify this setting for [Content update builds](xref:addressables-content-update-builds) because the data stored in the [content state file](xref:addressables-build-artifacts) becomes invalid.<br/><br/>The different modes are:<ul><li> __Full Path__: The path of the asset in your project. Recommended during development because you can identify assets being loaded by their ID if needed.</li><li> __Filename__: The asset's file name. This can also be used to identify an asset. **Note**: You can't have multiple assets with the same name.</li><li> __GUID__: A deterministic value for the asset.</li><li> __Dynamic__: The shortest ID that can be constructed based on the assets in the group. Recommended for release because it can reduce the amount of data in the AssetBundle and catalog, and lower runtime memory overhead.</li></ul>|
|__Internal Bundle Id Mode__|Determines how an AssetBundle is identified internally. This affects how an AssetBundle locates dependencies that are contained in other bundles. Changing this value affects the CRC and Hash of this AssetBundle and all other AssetBundles that reference it.<br/><br/>**Warning**: Don't modify this setting for [Content update builds](xref:addressables-content-update-builds) because the data stored in the [content state file](xref:addressables-build-artifacts) becomes invalid.<br/><br/>The different modes are:<ul><li> __Group Guid__: A unique identifier for the Group. This mode is recommended because it doesn't change. </li><li> __Group Guid Project Id Hash__: Uses a combination of the Group GUID and the Cloud Project ID, if Cloud Services are enabled. This changes if the Project is bound to a different Cloud Project ID. This mode is recommended when sharing assets between multiple projects because the ID constructed is deterministic and unique between projects.</li><li>__Group Guid Project Id Entries Hash__: Uses a combination of the Group GUID, Cloud Project ID (if Cloud Services are enabled), and asset entries in the Group. Using this mode can cause bundle cache version issues. Adding or removing entries results in a different hash.</li></ul>|
|__Cache Clear Behavior__|Controls how old AssetBundles are cleared. Choose from the following:<ul><li>**Clear When Space Is Needed In Cache**</li><li>**Clear When New Version Loaded**</li></ul>|
| __Bundle Mode__| Select how to pack the assets in this group into AssetBundles:<ul><li>__Pack Together__: Create a single AssetBundle containing all assets.</li><li>__Pack Separately__: Create an AssetBundle for each primary asset in the group. Subassets, such as sprites in a sprite sheet are packed together. Assets within a folder added to the group are also packed together.</li><li>__Pack Together by Label__: Create an AssetBundle for assets sharing the same combination of labels.</li></ul>|
| __Bundle Naming Mode__| Set how to construct the file names of AssetBundles:<ul><li>__Append Hash__: The file name is a string derived from the group name with bundle hash appended to it. The bundle hash is calculated using the contents of the bundle.</li><li>__No Hash__</li><li>__Only Hash__</li><li> __File Name Hash__: The file name is a hash calculated from the a string derived from the group name.</li></ul>|
|__Asset Load Mode__|Set whether to load assets individually as you request them (the default) or always load all assets in the group together. It is recommended to use __Requested Asset and Dependencies__ for most cases. |
| __Asset Provider__| Defines which Provider class Addressables uses to load assets from the AssetBundles generated from this group. Set this option to __Assets from Bundles Provider__ unless you have a custom Provider implementation to provide assets from an AssetBundle. |
| __Asset Bundle Provider__| Defines which Provider class Addressables uses to load AssetBundles generated from this group. Set this option to __AssetBundle Provider__ unless you have a custom Provider implementation to provide AssetBundles. |

## Content Update Restriction

The Content Update Restriction schema determines how [Check for Content Update Restrictions](GroupsWindow.md#tools) treats assets in the group. To prepare your groups for a differential content update build rather than a full content build, open the [Addressables Groups window](xref:addressables-groups-window), go to **Tools** and run the **Check for Content Update Restrictions** command. The tool moves modified assets in any groups with the __Prevent Updates__ property enabled to a new group.

The **Prevent Updates** property acts in the following way:

* **Enabled**: The tool doesn't move any assets. When you make the update build, if any assets in the bundle have changed, then the entire bundle is rebuilt.
* **Disabled**: If any assets in the bundle have changed, then the [Check for Content Update Restrictions](builds-update-build.md#check-for-content-update-restrictions) tool moves them to a new group created for the update. When you make the update build, the assets in the AssetBundles created from this new group override the versions found in the existing bundles.

## Additional resources

* [AssetBundle compression formats](xref:um-asset-bundles-cache)
* [Addressables Groups window](GroupsWindow.md)