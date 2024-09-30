---
uid: addressables-content-packing-and-loading-schema
---

## Content Packing & Loading schema reference

The Content Packing & Loading schema is the main Addressables schema used by the default build script, and it defines the settings for building and loading Addressable assets.

To open the Content Packing & Loading schema, open the [Addressables Groups window](GroupsWindow.md) (**Window &gt; Asset Management &gt; Addressables &gt; Groups**), then select a group. The group's settings are displayed in the Inspector.

## Build and Load Paths

The Build and Load Paths settings determine where the artifacts for your content builds are created and where the Addressables system should look for them at runtime.

![](images/groups-build-load.png)<br/>*Building and loading paths*

| **Property**|| **Description** |
|:---|---|:---|
| __Build & Load Paths__ || The [Profile path](xref:addressables-profiles) pair that defines where the Addressables build system creates artifacts for this group and where the Addressables system loads those artifacts at runtime. Choose a path pair from the list or select `<custom>` if you want to set the build and load paths separately.|
| __Build Path__<br/><br/>__Load Path__<br/><br/>Only available if you set __Build & Load Paths__ to `<custom>`.|| A Profile variable that defines where the Addressables build system creates artifacts for this group, or loads the build artifacts of the group. You can also set a custom string. Use one of the following:
||__LocalBuildPath__| Use for assets that you plan to distribute as part of your application installation. 
||__RemoteBuildPath__| Use for assets that you plan to distribute using a remote hosting service such Unity Cloud Content Delivery or other Content Delivery Network.
||__Custom__| Specify a string as the path for this group.|

The build and load path options are defined by variables in a [Profile](xref:addressables-profiles). Note that only variables intended for a given purpose should be used for a setting. For example, choosing a load path variable for a build path setting wouldn't give you a useful result.

When you choose a Profile variable, a preview of the path is displayed in the __Path Preview__. Components of the path in braces, such as `{UnityEngine.AddressableAssets.Addressable.RuntimePath}`, indicate that a static variable is used to construct the final path at runtime. That portion of the path is replaced by the current value of the static variable when the Addressables system initializes at runtime.

> [!WARNING]
> Usually, you shouldn't change the local build or load paths from their default values. If you do, you must copy the local build artifacts from your custom build location to the project's `StreamingAssets` folder before making a Player build. Altering these paths also precludes building your Addressables as part of the Player build.

## Advanced Options

![](images/groups-advanced.png)<br/>*The Advanced Options section*

| **Property**| **Description** |
|:---|---|
| __Asset Bundle Compression__| The compression type for all bundles produced from the group. LZ4 is usually the most efficient option, but other options can be better in specific circumstances. Refer to [AssetBundle Compression](#assetbundle-compression) for more information. |
| __Include In Build__| Enable this property to include assets in this group in a content build.  |
| __Force Unique Provider__| Enable this property to use unique instances of Resource Provider classes for this group. Enable this option if you have custom Provider implementations for the asset types in this group and instances of those Providers must not be shared between groups. |
| __Use Asset Bundle Cache__| Enable this property to cache remotely distributed bundles. |
| __Asset Bundle CRC__| Enable this property to verify a bundle's integrity before loading it. For more information see [AssetBundle CRC](#assetbundle-crc):<br/>&#8226; __Disabled__: Never check bundle integrity.<br/> &#8226; __Enabled, Including Cached__: Always check bundle integrity.<br/> &#8226; __Enabled, Excluding Cached__: Check integrity of bundles when downloading.<br/> |
|__Use UnityWebRequest for Local Asset Bundles__|Enable this property to load local AssetBundle archives from this group using [`UnityWebRequestAssetBundle.GetAssetBundle`](xref:UnityEngine.Networking.UnityWebRequest.GetAssetBundle(System.String,System.UInt32)) instead of [`AssetBundle.LoadFromFileAsync`](xref:UnityEngine.AssetBundle.LoadFromFileAsync(System.String,System.UInt32,System.UInt64)). |
| __Request Timeout__| The timeout interval for downloading remote bundles. |
| __Use Http Chunked Transfer__| Enable this property to use the HTTP/1.1 chunked-transfer encoding method when downloading bundles. <br/><br/> Deprecated and ignored in Unity 2019.3+. |
| __Http Redirect Limit__| The number of redirects allowed when downloading bundles. Set to -1 for no limit. |
| __Retry Count__| The number of times to retry failed downloads. |
|__Include Addresses in Catalog__|Enable this property to include the address strings in the catalog. If you don't load assets in the group using their address strings, you can disable this property to decrease the size of the catalog.|
|__Include GUIDs in Catalog__|Enable this property to include GUID strings in the catalog. You must include GUID strings to access an asset with an [`AssetReference`](xref:addressables-asset-references). If you don't load assets in the group using an `AssetReference` or GUID strings, you can disable this property to decrease the size of the catalog.|
|__Include Labels in Catalog__|Enable this property to include label strings in the catalog. If you don't load assets in the group using labels, you can disable this property to decrease the size of the catalog. |
|__Internal Asset Naming Mode__|Determines the identification of assets in AssetBundles and is used to load the asset from the bundle. This value is used as the `internalId` of the asset location. Changing this setting affects a bundles CRC and Hash value. <br/><br/>**Warning**: Don't modify this setting for [Content update builds](xref:addressables-content-update-builds) because the data stored in the [content state file](xref:addressables-build-artifacts) becomes invalid.<br/><br/>The different modes are:<br/><br/>- __Full Path__: The path of the asset in your project. Recommended during development because you can identify assets being loaded by their ID if needed. <br/>- __Filename__: The asset's file name. This can also be used to identify an asset. **Note**: You can't have multiple assets with the same name.<br/>- __GUID__: A deterministic value for the asset.<br/>- __Dynamic__: The shortest ID that can be constructed based on the assets in the group. Recommended for release because it can reduce the amount of data in the AssetBundle and catalog, and lower runtime memory overhead.|
|__Internal Bundle Id Mode__|Determines how an AssetBundle is identified internally. This affects how an AssetBundle locates dependencies that are contained in other bundles. Changing this value affects the CRC and Hash of this bundle and all other bundles that reference it.<br/><br/>**Warning**: Don't modify this setting for [Content update builds](xref:addressables-content-update-builds) because the data stored in the [content state file](xref:addressables-build-artifacts) becomes invalid.<br/><br/>The different modes are:<br/><br/>- __Group Guid__: A unique identifier for the Group. This mode is recommended because it doesn't change. <br/>- __Group Guid Project Id Hash__: Uses a combination of the Group GUID and the Cloud Project ID, if Cloud Services are enabled. This changes if the Project is bound to a different Cloud Project ID. This mode is recommended when sharing assets between multiple projects because the ID constructed is deterministic and unique between projects.<br/>- __Group Guid Project Id Entries Hash__: Uses a combination of the Group GUID, Cloud Project ID (if Cloud Services are enabled), and asset entries in the Group. Using this mode can cause bundle cache version issues. Adding or removing entries results in a different hash.|
|__Cache Clear Behavior__| Determines when an installed application clears AssetBundles from the cache.|
| __Bundle Mode__| Set how to pack the assets in this group into bundles:<br/><br/>- __Pack Together__: Create a single bundle containing all assets.<br/>- __Pack Separately__: Create a bundle for each primary asset in the group. Subassets, such as sprites in a sprite sheet are packed together. Assets within a folder added to the group are also packed together. <br/>- __Pack Together by Label__: Create a bundle for assets sharing the same combination of labels.  |
| __Bundle Naming Mode__| Set how to construct the file names of AssetBundles:<br/>- __Filename__: The filename is a string derived from the group name. No hash is appended to it.<br/>- __Append Hash to Filename__: The filename is a string derived from the group name with bundle hash appended to it. The bundle hash is calculated using the contents of the bundle.<br/>- __Use Hash of AssetBundle__: The filename is the bundle hash.<br/>- __Use Hash of Filename__: The filename is a hash calculated from the a string derived from the group name.|
|__Asset Load Mode__|Set whether to load assets individually as you request them (the default) or always load all assets in the group together. It is recommended to use __Requested Asset and Dependencies__ for most cases. Refer to [Asset Load Mode](#asset-load-mode) for more information. |
| __Asset Provider__| Defines which Provider class Addressables uses to load assets from the AssetBundles generated from this group. Set this option to __Assets from Bundles Provider__ unless you have a custom Provider implementation to provide assets from an AssetBundle. |
| __Asset Bundle Provider__| Defines which Provider class Addressables uses to load AssetBundles generated from this group. Set this option to __AssetBundle Provider__ unless you have a custom Provider implementation to provide AssetBundles. |

### AssetBundle compression

Addressables provides three different options for bundle compression:

* **Uncompressed**: This option is largest on disk, and fastest to load. If your application has space to spare, consider this option for local content. An advantage of uncompressed bundles is how they handle being patched. If you're developing for a platform where the platform itself provides patching, uncompressed bundles provide the most accurate (smallest) patching. Either of the other compression options cause some bloat of patches.
* **LZ4**: If Uncompressed is not a viable option, then LZ4 should be used for all other local content. This is a chunk-based compression which provides the ability to load parts of the file without needing to load it in its entirety.
* **LZMA**: Use LZMA for all remote content, but not for any local content. It provides the smallest bundle size, but is slow to load. If you store local bundles in LZMA you can create a smaller player, but load times are significantly worse than uncompressed or LZ4. For downloaded bundles, LZMA avoids the slow load time by recompressing the downloaded bundle when storing it in the AssetBundle cache. By default, bundles are stored in the cache with LZ4 compression.

To set the compression, use the Advanced settings on each group. Compression doesn't affect in-memory size of your loaded content.

> [!NOTE]
> LZMA AssetBundle compression isn't available for AssetBundles on WebGL. You can use LZ4 compression instead. For more WebGL AssetBundle information, see [Building and running a WebGL project](xref:webgl-building).

The hardware characteristics of a platform mean that uncompressed bundles aren't always the fastest to load. The maximum speed of loading uncompressed bundles is gated by IO speed, while the speed of loading LZ4-compressed bundles is gated by either IO speed or CPU, depending on hardware. 

On most platforms, loading LZ4-compressed bundles is CPU bound, and loading uncompressed bundles is faster. On platforms that have low IO speeds and high CPU speeds, LZ4 loading can be faster. It's best practice to run performance analysis to validate whether your game fits the common patterns, or needs some unique tweaking.

More information on Unity's compression selection is available in the [AssetBundle compression manual page](xref:AssetBundles-Cache).

### AssetBundle CRC

Different CRC settings are best used depending upon different circumstances. Checking for a change in the file requires the entire AssetBundle to be decompressed and the check processed on the uncompressed bytes. This can impact performance negatively.

Corruption is likely to only happen during a download, because disk storage is generally reliable and unlikely to have corrupted files after saving to disk. If your AssetBundle contains data that might be tampered with, such as settings values, then you might want to consider enabling CRC checks on saved AssetBundles.

For local AssetBundles, if the application download performs a check on the download before saving to disk, consider setting this property to __Disabled__ because the download will have already been checked.

For remote AssetBundles, __Enabled, Excluding cache__ is a good default. When downloading and caching an AssetBundle to disk, the bytes are decompressed and a CRC calculation is done during file saving. This doesn't impact performance and the corruption is most likely to occur during this phase from the download. __Including cache__ is good to use where the data needs to be checked every time such as settings values.

### Asset Load Mode

For most platforms and collection of content, you should use __Requested Asset and Dependencies__. This mode only loads what's required for the Assets requested with [`LoadAssetAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetAsync*) or [`LoadAssetsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync``1(System.Collections.Generic.IList{System.Object},System.Action{``0},UnityEngine.AddressableAssets.Addressables.MergeMode)). Objects are loaded based in the order that they appear in a bundle file, which can result in reading the same file multiple times. Enabling the __Contiguous Bundles__ option in [Addressables Build settings](xref:addressables-asset-settings) can help reduce the number of extra file reads.

This prevents situations where Assets are loaded into memory that aren't used.

Performance in situations where you load all Assets that are packed together, such as a loading screen. Most types of content have either have similar or improved performance when loading each individually using __Requested Asset and Dependencies__ mode. This mode sequentially reads entire bundle files, which may be more preferable in some platforms.

> [!NOTE]
> The examples below apply to desktop and mobile platforms. Performance might differ between platforms. The __All Packed Assets and Dependencies__ mode typically performs better than loading assets individually on the Nintendo Switch due its hardware and memory reading limitations.
> You should profile loading performance for your specific content and platform to see what works for your application.

Loading performance can vary between content type. As an example, large counts of serialized data such as prefabs or ScriptableObjects with direct references to other serialized data load faster using __All Packed Assets and Dependencies__. With some other Assets like Textures, you can often achieve better performance when you load each Asset individually.

If using [Synchronous Addressables](xref:synchronous-addressables), there is little performance between asset load modes. Because of greater flexibility you should use __Requested Asset and Dependencies__ where you know the content will be loaded synchronously.

On loading the first Asset with __All Packed Assets and Dependencies__, all Assets are loaded into memory. Later `LoadAssetAsync` calls for Assets from that pack return the preloaded asset without needing to load it.

Even though all the Assets in a group and any dependencies are loaded in memory when you use the **All Packed Assets and Dependencies** option, the reference count of an individual asset isn't incremented unless you explicitly load it (or it is a dependency of an asset that you load explicitly). If you later call [`Resources.UnloadUnusedAssets`](xref:UnityEngine.Resources.UnloadUnusedAssets), or you load a new scene using [`LoadSceneMode.Single`](xref:UnityEngine.SceneManagement.LoadSceneMode.Single), then any unused assets (those with a reference count of zero) are unloaded.
