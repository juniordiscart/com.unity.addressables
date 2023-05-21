---
uid: addressables-content-packing-and-loading-schema
---

<a name="content-packing-loading-settings"></a>
## Content Packing & Loading Schema

<a name="build-and-load-paths"></a>
### Build and Load Paths

The Build and Load Paths settings of the Content Packing & Loading schema determine where the artifacts for your content builds are created and where the Addressables system should look for them at runtime.

![](../../images/addr_groups_3.png)<br/>*Building and loading paths*

| Setting| Purpose |
|:---|:---| 
| __Build & Load Paths__ | The Profile path pair that defines where the Addressables build system creates artifacts for this group and where the Addressables system loads those artifacts at runtime. Choose a path pair from the list or select `<custom>` if you want to set the build and load paths separately.|
| __Build Path__| A Profile variable that defines where the Addressables build system creates artifacts for this group. You can also set a custom string. Use one of the following for the build path:<br/>- __LocalBuildPath__: use for assets that you plan to distribute as part of your application installation.<br/>- __RemoteBuildPath__: use for assets that you plan to distribute using a remote hosting service such Unity Cloud Content Delivery or other Content Delivery Network.<br/>- __\<custom\>__: specify a string as the build path for this group.<br/></br/>Only shown if you set __Build & Load Paths__ to `<custom>`.|
| __Load Path__| A Profile variable that defines where the Addressables system loads the build artifacts for this group at runtime. You can also set a custom string. Use one of the following for the load path:<br/>- __LocalLoadPath__: use for assets that you plan to distribute as part of your application installation.<br/>- __RemoteLoadPath__: use for assets that you plan to distribute using a remote hosting service such Unity Cloud Content Delivery or other Content Delivery Network.<br/>- __\<custom\>__: specify a string as the load path for this group.<br/></br/>Only shown if you set __Build & Load Paths__ to `<custom>`.|

The build and load path options are defined by variables in your [Profiles]. Note that only variables intended for a given purpose should be used for a setting. For example, choosing a load path variable for a build path setting wouldn't give you a useful result.

When you choose a Profile variable, the current evaluation of the path is shown in the __Path Preview__. Components of the path in braces, such as `{UnityEngine.AddressableAssets.Addressable.RuntimePath}`, indicate that static variable is used to construct the final path at runtime. That portion of the path is replaced by the current value of the static variable when the Addressables system initializes at runtime.

> [!WARNING]
> In most cases, you should not change the local build or load paths from their default values. If you do, you must copy the local build artifacts from your custom build location to the project's [StreamingAssets] folder before making a Player build. Altering these paths also precludes building your Addressables as part of the Player build. 

See [Profiles] for more information.

<a name="advanced-options"></a>
### Advanced Options

![](../../images/addr_groups_4.png)<br/>*The Advanced Options section* 

| Setting| Purpose |
|:---|:---| 
| __Asset Bundle Compression__| The compression type for all bundles produced from the group. LZ4 is usually the most efficient option, but other options can be better in specific circumstances. For more information, see the [AssetBundle compression manual page]. |
| __Include In Build__| Whether to include assets in this group in a content build.  |
| __Force Unique Provider__| Whether Addressables uses unique instances of Resource Provider classes for this group. Enable this option if you have custom Provider implementations for the asset types in this group and instances of those Providers must not be shared between groups. |
| __Use Asset Bundle Cache__| Whether to cache remotely distributed bundles. |
| __Asset Bundle CRC__| Whether to verify a bundle's integrity before loading it.<br/>&#8226; __Disabled__: Never check bundle integrity.<br/> &#8226; __Enabled, Including Cached__: Always check bundle integrity.<br/> &#8226; __Enabled, Excluding Cached__: Check integrity of bundles when downloading.<br/> |
|__Use UnityWebRequest for Local Asset Bundles__|Load local AssetBundle archives from this group using [UnityWebRequestAssetBundle.GetAssetBundle] instead of [AssetBundle.LoadFromFileAsync]. |
| __Request Timeout__| The timeout interval for downloading remote bundles. |
| __Use Http Chunked Transfer__| Whether to use the HTTP/1.1 chunked-transfer encoding method when downloading bundles. <br/><br/> Deprecated and ignored in Unity 2019.3+. |
| __Http Redirect Limit__| The number of redirects allowed when downloading bundles. Set to -1 for no limit. |
| __Retry Count__| The number of times to retry failed downloads. |
|__Include Addresses in Catalog__|Whether to include the address strings in the catalog. If you don't load assets in the group using their address strings, you can decrease the size of the catalog by not including them.|
|__Include GUIDs in Catalog__|Whether to include GUID strings in the catalog. You must include GUID strings to access an asset with an [AssetReference]. If you don't load assets in the group using AssetReferences or GUID strings, you can decrease the size of the catalog by not including them.|
|__Include Labels in Catalog__|Whether to include label strings in the catalog. If you don't load assets in the group using labels, you can decrease the size of the catalog by not including them. |
|__Internal Asset Naming Mode__|Determines the identification of assets in AssetBundles and is used to load the asset from the bundle. This value is used as the internalId of the asset Location. Changing this setting affects a bundles CRC and Hash value. <br/><br/>**Warning**: Do not modify this setting for [Content update builds]. The data stored in the [content state file] will become invalid.<br/><br/>The different modes are:<br/>- __Full Path__: the path of the asset in your project. This mode is recommended to use during development because it allows you to identify Assets being loaded by their ID if needed. <br/>- __Filename__: the asset's filename. This can also be used to identify an asset. **Note**: You cannot have multiple assets with the same name.<br/>- __GUID__: a deterministic value for the asset.<br/>- __Dynamic__: the shortest id that can be constructed based on the assets in the group. This mode is recommended to use for release because it can reduce the amount of data in the AssetBundle and catalog, and lower runtime memory overhead.|
|__Internal Bundle Id Mode__|Determines how an AssetBundle is identified internally. This affects how an AssetBundle locates dependencies that are contained in other bundles. Changing this value affects the CRC and Hash of this bundle and all other bundles that reference it.<br/><br/>**Warning**: Do not modify this setting for [Content update builds]. The data stored in the [content state file] will become invalid.<br/><br/>The different modes are:<br/>- __Group Guid__: unique identifier for the Group. This mode is recommended to use as it does not change. <br/>- __Group Guid Project Id Hash__: uses a combination of the Group GUID and the Cloud Project Id (if Cloud Services are enabled). This changes if the Project is bound to a different Cloud Project Id. This mode is recommended when sharing assets between multiple projects because the id constructed is deterministic and unique between projects.<br/>- __Group Guid Project Id Entries Hash__: uses a combination of the Group GUID, Cloud Project Id (if Cloud Services are enabled), and asset entries in the Group. Note that using this mode can easily cause bundle cache version issues. Adding or removing entries results in a different hash.| 
|__Cache Clear Behavior__| Determines when an installed application clears AssetBundles from the cache.|
| __Bundle Mode__| How to pack the assets in this group into bundles:<br/>- __Pack Together__: create a single bundle containing all assets.<br/>- __Pack Separately__: create a bundle for each primary asset in the group. Subassets, such as Sprites in a Sprite sheet are packed together. Assets within a folder added to the group are also packed together. <br/>- __Pack Together by Label__: create a bundle for assets sharing the same combination of labels.  |
| __Bundle Naming Mode__| How to construct the file names of AssetBundles:<br/>- __Filename__: the filename is a string derived from the group name. No hash is appended to it.<br/>- __Append Hash to Filename__: the filename is a string derived from the group name with bundle hash appended to it. The bundle hash is calculated using the contents of the bundle.<br/>- __Use Hash of AssetBundle__: the filename is the bundle hash.<br/>- __Use Hash of Filename__: the filename is a hash calculated from the a string derived from the group name.|
|__Asset Load Mode__|Whether to load assets individually as you request them (the default) or always load all assets in the group together. It is recommended to use __Requested Asset and Dependencies__ for most cases. See [Asset Load Mode] for more information. |
| __Asset Provider__| Defines which Provider class Addressables uses to load assets from the AssetBundles generated from this group. Set this option to __Assets from Bundles Provider__ unless you have a custom Provider implementation to provide assets from an AssetBundle. |
| __Asset Bundle Provider__| Defines which Provider class Addressables uses to load AssetBundles generated from this group. Set this option to __AssetBundle Provider__ unless you have a custom Provider implementation to provide AssetBundles. |

<a name="assetBundle-compression"></a>
### AssetBundle Compression

Addressables provides three different options for bundle compression: Uncompressed, LZ4, and LZMA.  Generally speaking, LZ4 should be used for local content, and LZMA for remote, but more details are outlined below as there can be exceptions to this.

You can set the compression option using the Advanced settings on each group. Compression does not affect in-memory size of your loaded content. 

* Uncompressed - This option is largest on disk, and generally fastest to load.  If your game happens to have space to spare, this option should at least be considered for local content.  A key advantage of uncompressed bundles is how they handle being patched.  If you are developing for a platform where the platform itself provides patching (such as Steam or Switch), uncompressed bundles provide the most accurate (smallest) patching.  Either of the other compression options will cause at least some bloat of patches.
* LZ4 - If Uncompressed is not a viable option, then LZ4 should be used for all other local content.  This is a chunk-based compression which provides the ability to load parts of the file without needing to load it in its entirety. 
* LZMA - LZMA should be used for all remote content, but not for any local content.  It provides the smallest bundle size, but is slow to load. If you were to store local bundles in LZMA you could create a smaller player, but load times would be significantly worse than uncompressed or LZ4. For downloaded bundles, we avoid the slow load time by recompressing the downloaded bundle when storing it in the AssetBundle cache.  By default, bundles will be stored in the cache with LZ4 compression.

> [!NOTE] 
> LZMA AssetBundle compression is not available for AssetBundles on WebGL. LZ4 compression can be used instead. For more WebGL AssetBundle information, see [Building and running a WebGL project].

Note that the hardware characteristics of a platform can mean that uncompressed bundles are not always the fastest to load.  The maximum speed of loading uncompressed bundles is gated by IO speed, while the speed of loading LZ4-compressed bundles can be gated by either IO speed or CPU, depending on hardware.  On most platforms, loading LZ4-compressed bundles is CPU bound, and loading uncompressed bundles will be faster. On platforms that have low IO speeds and high CPU speeds, LZ4 loading can be faster. It is always a good practice to run performance analysis to validate whether your game fits the common patterns, or needs some unique tweaking.

More information on Unity's compression selection is available in the [AssetBundle compression manual page].

### AssetBundle CRC

Different CRC settings are best used depending upon different circumstances. Checking for a change in the file requires the entire AssetBundle to be decompressed and the check processed on the uncompressed bytes. This can impact performance negatively and may not be a worth while trade off.
Corruption is likely to only happen during a download, disk storage is generally reliable and unlikely to have corrupted files after saving to disk. if your AssetBundle contains data that may be tampered with such as settings values, then you may want to consider enabling CRC checks on saved AssetBundles.

For __local__ AssetBundles, if the application download performs a check on the download before saving to disk. Then consider setting this to __Disabled__ as the download will have already been checked.

For __remote__ AssetBundles, __Enabled, Excluding cache__ is a good default. When downloading and caching an AssetBundle to disk, the bytes are decompressed and a CRC calculation is done during file saving. This will not impact performance and the corruption is most likely to occur during this phase from the download. __including cache__ is good to use where the data needs to be checked every time such as settings values.

### Asset Load Mode

For most platforms and collection of content, it is recommended to use __Requested Asset and Dependencies__. This mode will only load what is required for the Assets requested with [LoadAssetAsync] or [LoadAssetsAsync]. Objects are loaded based in the order that they appear in a bundle file, which can result in reading the same file multiple times. Enabling the __Contiguous Bundles__ option in [Addressables Build settings] can help reduce the number of extra file reads.

This prevents situations where Assets are loaded into memory that are not used.

Performance in situations where you will load all Assets that are packed together, such as a loading screen. Most types of content will have either have similar or improved performance when loading each individually using __Requested Asset and Dependencies__ mode. This mode sequentially reads entire bundle files, which may be more preferrable in some platforms like the Switch.

> [!NOTE] 
> The examples below apply to Desktop and Mobile platforms. Performance may differ between platforms. The __All Packed Assets and Dependencies__ mode typically performs better than loading assets individually on the Nintendo Switch due its hardware and memory reading limitations. 
> It is recommended to profile loading performance for your specific content and platform to see what works for your Application.

Loading performance can vary between content type. As an example, large counts of serialized data such as Prefabs or ScriptableObjects with direct references to other serialized data will load faster using __All Packed Assets and Dependencies__. With some other Assets like Textures, you can often achieve better performance when you load each Asset individually.

If using [Synchronous Addressables], there is little performance between between Asset load modes. Because of greater flexibility it is recommended to use __Requested Asset and Dependencies__ where you know the content will be loaded synchronously.

On loading the first Asset with __All Packed Assets and Dependencies__, all Assets are loaded into memory. Later LoadAssetAsync calls for Assets from that pack will return the preloaded Asset without 
needing to load it. 

Even though all the Assets in a group and any dependencies are loaded in memory when you use the All Packed Assets and Dependencies option, the reference count of an individual asset is not incremented unless you explicitly load it (or it is a dependency of an asset that you load explicitly). If you later call [Resources.UnloadUnusedAssets], or you load a new Scene using [LoadSceneMode.Single], then any unused assets (those with a reference count of zero) are unloaded.

[Addressable System Settings]: xref:addressables-asset-settings
[AddressableAssetGroup]: xref:UnityEditor.AddressableAssets.Settings.AddressableAssetGroup
[AddressableAssetGroupSchema]: xref:UnityEditor.AddressableAssets.Settings.AddressableAssetGroupSchema
[Addressables Build settings]: xref:addressables-asset-settings#build
[Addressables Groups window]: xref:addressables-groups-window
[Addressables Settings]: xref:addressables-asset-settings
[Addressables system settings]: xref:addressables-asset-settings
[Analyze]: xref:addressables-analyze-tool
[Asset Load Mode]: #asset-load-mode
[AssetBundle compression manual page]: xref:AssetBundles-Cache
[AssetReference]: xref:addressables-asset-references
[Build scripts]: xref:addressables-builds#build-commands
[Builds]: xref:addressables-builds
[Building and running a WebGL project]: xref:webgl-building#AssetBundles
[content state file]: xref:addressables-build-artifacts#content-state-file
[Content update builds]: xref:addressables-content-update-builds
[Content Workflow: Update Restrictions]: xref:addressables-content-update-builds#settings
[Custom Inspector scripts]: xref:VariablesAndTheInspector
[Default Build Script]: xref:addressables-builds
[Event Viewer]: xref:addressables-event-viewer
[Group settings]: #addressables-group-schemas
[Group Templates]: #group-templates
[Group templates]: #group-templates
[Hosting]: xref:addressables-asset-hosting-services
[Labels]: xref:addressables-labels
[Loading Addressable assets]: xref:addressables-api-load-asset-async
[LoadAssetAsync]:  xref:UnityEngine.AddressableAssets.Addressables.LoadAssetAsync*
[LoadAssetsAsync]: xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync``1(System.Collections.Generic.IList{System.Object},System.Action{``0},UnityEngine.AddressableAssets.Addressables.MergeMode)
[LoadSceneMode.Single]: xref:UnityEngine.SceneManagement.LoadSceneMode.Single
[Organizing Addressable Assets]: xref:addressables-assets-development-cycle#organizing-addressable-assets
[Play Mode Scripts]: #play-mode-scripts
[Profile]: xref:addressables-profiles
[Profiles]: xref:addressables-profiles
[ProjectConfigData]: xref:UnityEditor.AddressableAssets.Settings.ProjectConfigData
[Resources.UnloadUnusedAssets]: xref:UnityEngine.Resources.UnloadUnusedAssets
[Schema]: #schemas
[settings of the group]: #addressables-group-schemas
[Synchronous Addressables]: xref:synchronous-addressables
[template]: #group-templates
[UnityWebRequestAssetBundle.GetAssetBundle]: xref:UnityEngine.Networking.UnityWebRequest.GetAssetBundle(System.String,System.UInt32)
[AssetBundle.LoadFromFileAsync]: xref:UnityEngine.AssetBundle.LoadFromFileAsync(System.String,System.UInt32,System.UInt64)
