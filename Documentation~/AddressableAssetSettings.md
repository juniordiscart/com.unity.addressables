---
uid: addressables-asset-settings
---

# Addressable Asset Settings reference

Reference for project-level Addressables settings, including profiles, catalogs, downloads, build options, and system configuration.

To manage how Addressable assets work in your project, use the **Addressable Asset Settings** Inspector. To open this Inspector, go to **Window** &gt; **Asset Management** &gt; **Addressables** &gt; **Settings**.

The Addressables system stores the settings asset in the `AddressableSettingsData` folder in the project's `Assets` folder. If this folder doesn't exist yet, you must initialize the Addressables system from the [Groups window](xref:addressables-groups-window) (menu: **Window** &gt; **Asset Management** &gt; **Addressables** &gt; **Groups**).

![The Addressable Asset Settings Inspector displays the full collapsed list of its different sections.](images/addressable-assets-settings.png)<br/>*The Addressable Asset Settings Inspector*

The Inspector contains the following sections:

* [Profile](#profile)
* [Diagnostics](#diagnostics)
* [Catalog](#catalog)
* [Update a Previous Build](#update-a-previous-build)
* [Downloads](#downloads)
* [Build](#build)
* [Build and Play Mode Scripts](#build-and-play-mode-scripts)
* [Asset Group Templates](#asset-group-templates)
* [Initialization Objects](#initialization-objects)
* [Cloud Content Delivery](#cloud-content-delivery)

To open the [Groups window](xref:addressables-groups-window), select __Manage Groups__.

## Profile

|**Property**|**Description**|
|---|---|
|**Profile In Use**|Choose the active profile, which determines the value of the variables that the Addressables build scripts use. For more information, refer to the [Profiles](xref:addressables-profiles) documentation.|
|**Manage Profiles**|Opens the [Profiles](xref:addressables-profiles) window|

## Diagnostics

|**Property**|**Description**|
|---|---|
| __Log Runtime Exceptions__| Enable this property to log runtime exceptions for asset loading operations and record the error to the [`AsyncOperationHandle.OperationException`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.OperationException) property. |

By default, Addressable Assets only logs warnings and errors. To enable detailed logging, open the **Player** settings window (menu: **Edit** &gt; **Project Settings** &gt; **Player**), go to **Other Settings** &gt; **Configuration** section, and add `ADDRESSABLES_LOG_ALL` to the **Scripting Define Symbols** field.

## Catalog

|**Property**|**Description**|
|---|---|
| __Player Version Override__| Overrides the timestamp used to create the remote catalog name. If set, the remote catalog is named, `Catalog_<Player Version Override>.json`. If left blank, then Unity uses the timestamp.<br/><br/>If you use a unique remote catalog name for every new build, you can host multiple versions of your content at the same base URL. If you use the same override string for every build, then all players load the new catalog. Player update builds also always use the same remote catalog name as the build they're updating. For more information, refer to [Content update builds](xref:addressables-content-update-builds). |
| __Compress Local Catalog__| Builds the catalog in a compressed AssetBundle file. This property reduces the storage size of the catalog, but increases the time to build and to load the catalog. |
| __Build Remote Catalog__| Creates a copy of the content catalog for storage on a remote server. When you enable this property the following options are available:<ul><li> __Build & Load Paths__: Set where to build and load the remote catalog. Choose a Profile path pair from the list or select `<custom>` if you want to set the build and load paths separately.</li><li> __Build Path__: Only displayed if you set __Build & Load Paths__ to `<custom>`. Set where to build the remote catalog. Typically, you should use the __RemoteBuildPath__ Profile variable.</li><li> __Load Path__: Only displayed if you set __Build & Load Paths__ to `<custom>`. Set the URL at which to access the remote catalog. Typically, you should use the __RemoteLoadPath__ Profile variable.</li></ul>|
|__Enable Json Catalog__|Use JSON catalogs instead of binary catalogs. JSON catalogs are more human readable, but slower to load and larger in size.|
|__Only update catalogs manually__| Disables the automatic check for an updated remote catalog when the Addressables system initializes at runtime. You can manually [check for an updated catalog](xref:addressables-api-load-content-catalog-async).|
|__Internal Asset Naming Mode__|Determines the identification of assets in AssetBundles and is used to load the asset from the bundle. This value is used as the `internalId` of the asset location. Changing this setting affects a bundles CRC and Hash value. <br/><br/>**Warning**: Don't modify this setting for [Content update builds](xref:addressables-content-update-builds) because the data stored in the [content state file](xref:addressables-build-artifacts) becomes invalid.<br/><br/>The different modes are:<ul><li> __Full Path__: The path of the asset in your project. Recommended during development because you can identify assets being loaded by their ID if needed.</li><li> __Filename__: The asset's file name. This can also be used to identify an asset. **Note**: You can't have multiple assets with the same name.</li><li>__GUID__: A deterministic value for the asset.</li><li> __Dynamic__: The shortest ID that can be constructed based on the assets in the group. Recommended for release because it can reduce the amount of data in the AssetBundle and catalog, and lower runtime memory overhead.</li></ul>|
|__Internal Bundle Id Mode__|Determines how an AssetBundle is identified internally. This affects how an AssetBundle locates dependencies that are contained in other bundles. Changing this value affects the CRC and Hash of this bundle and all other bundles that reference it.<br/><br/>**Warning**: Don't modify this setting for [Content update builds](xref:addressables-content-update-builds) because the data stored in the [content state file](xref:addressables-build-artifacts) becomes invalid.<br/><br/>The different modes are:<ul><li> __Group Guid__: A unique identifier for the group. This mode is recommended because it doesn't change.</li><li> __Group Guid Project Id Hash__: Uses a combination of the Group GUID and the Cloud Project ID, if Cloud Services are enabled. This changes if the Project is bound to a different Cloud Project ID. This mode is recommended when sharing assets between multiple projects because the ID constructed is deterministic and unique between projects.</li><li> __Group Guid Project Id Entries Hash__: Uses a combination of the Group GUID, Cloud Project ID (if Cloud Services are enabled), and asset entries in the Group. Using this mode can cause bundle cache version issues. Adding or removing entries results in a different hash.</li></ul>|
|__Asset Load Mode__|Set whether to load assets individually as you request them (the default) or always load all assets in the group together. Choose from:<ul><li>__Requested Asset and Dependencies__: Only loads what's required for the assets requested with [`LoadAssetAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetAsync*) or [`LoadAssetsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync*). Objects are loaded based in the order that they appear in a bundle file, which can result in reading the same file multiple times. Enabling the __Contiguous Bundles__ option in [Addressables Build settings](xref:addressables-asset-settings) can help reduce the number of extra file reads.</li><li>__All Packed Assets and Dependencies__: Loads all assets in the group together. This setting is useful if you have large counts of serialized data such as prefabs or ScriptableObjects with direct references to other serialized data.</li></ul>|
| __Asset Provider__| Defines which Provider class Addressables uses to load assets from the AssetBundles generated from this group. Set this option to __Assets from Bundles Provider__ unless you have a custom Provider implementation to provide assets from an AssetBundle. |
| __Asset Bundle Provider__| Defines which Provider class Addressables uses to load AssetBundles generated from this group. Set this option to __AssetBundle Provider__ unless you have a custom Provider implementation to provide AssetBundles. |

## Update a Previous Build

To use the properties in the **Update a Previous Build** section, you must enable the __Build Remote Catalog__ property in the [Catalog](#catalog) section.

|**Property**|**Description**|
|---|---|
|**Check for Update Issues**|Choose whether to perform a content update restriction as part of the update, and how to handle the result. For more information, refer to [Content update build settings](content-update-build-settings.md).|
|__Content State Build Path__|Set where to build the content state file that the default build script builds.|

## Downloads

|**Property**|**Description**|
|---|---|
| __Custom certificate handler__| Set the class to use for custom certificate handling. The list has all classes in the project that extend [`UnityEngine.Networking.CertificateHandler`](xref:UnityEngine.Networking.CertificateHandler). |
| __Max Concurrent Web Requests__| Set the maximum amount of concurrent web requests. The system queues any requests beyond this limit. To reach the best download speeds, set this value between 2 and 4. |
| __Catalog Download Timeout__ | Set how many seconds to wait for a catalog file to download. If you set this to 0 there will be no timeout.|
|__Use UnityWebRequest for Local Asset Bundles__|Load local AssetBundle archives from this group using [`UnityWebRequestAssetBundle.GetAssetBundle`](xref:UnityEngine.Networking.UnityWebRequest.GetAssetBundle(System.String,System.UInt32)) instead of [`AssetBundle.LoadFromFileAsync`](xref:UnityEngine.AssetBundle.LoadFromFileAsync(System.String,System.UInt32,System.UInt64)). |
| __Bundle Request Timeout__| The timeout interval for downloading remote bundles. |
| __Bundle Retry Count__| The number of times to retry failed downloads. |
| __Bundle Http Redirect Limit__| The number of redirects allowed when downloading bundles. |

## Build

| **Property** | **Description** |
|---|---|
| **Build Addressables on Player Build** | Select how Unity builds Addressables content as part of the Player build. <br/><br/>The __Build Addressables content on Player Build__ and __Do not Build Addressables content on Player Build__ properties override the global Preference for the current project and affect all contributors who build the project. Otherwise, the global Preferences value applies to all Unity projects. Refer to [Build Addressable assets](builds-full-build.md) for more information. Choose from the following:<ul><li>__Use global Settings (stored in preferences)__: Use the value specified in the [Unity Editor Preferences](addressables-preferences.md) under __Addressables__. </li><li>__Build Addressables content on Player Build__: Always build Addressables content when building the Player.</li><li> __Do not Build Addressables content on Player Build__: Never build Addressables content when building the Player. If you modify Addressables content, you must rebuild it manually before building the Player.</li></ul>|
| __Ignore Invalid/Unsupported Files in Build__ | Exclude invalid or unsupported files from the build script rather than aborting the build. |
| __Unique Bundle IDs__ | Creates a unique name for an AssetBundle in every build. For more information, refer to [Unique Bundle IDs](content-update-build-settings.md#unique-bundle-ids-setting). |
| __Contiguous Bundles__ | Produces an efficient AssetBundle layout. If you have bundles produced by Addressables 1.12.1 or earlier, disable this property to minimize AssetBundle changes. |
| __Non-Recursive Dependency Calculation__ | Improves build times and reduces runtime memory overhead when assets have circular dependencies. <br/><br/>For example, A prefab assigned to Bundle A references a material assigned to Bundle B. If this property is disabled, Unity needs to calculate the material's dependencies twice, once for each bundle. If this option is enabled, Unity only needs to calculate the material's dependencies once, for Bundle B.<br/><br/> In an example where many scenes reference the same material, if this property is disabled, Unity opens each scene to calculate shader usage, which is a costly operation. If this property is enabled, Unity only loads the material and doesn't need to open any scenes for dependency calculation.<br/><br/>This option is enabled by default when using Unity version 2021.2 or later. Disabling this option invalidates previously built bundles because the rebuilt bundles have a different build layout. Therefore, leave this property enabled unless you've shipped a build. <br/><br/>Some circular dependencies might fail to load when the option is enabled because the referenced asset is always assigned to the same bundle location, even when more content is added to the build. This issue often occurs for Monoscripts. Building the MonoScript bundle can help resolve these load failures. |
| __Strip Unity Version From AssetBundles__ | Removes the Unity version from the AssetBundle header. |
| __Disable Visible Sub Asset Representations__ | Improves build times if you don't use sub objects directly (such as sprites, or sub meshes). |
| __Shared Bundle Settings__ | Define which group settings to use for for shared AssetBundles (Monoscript and UnityBuiltInAssets). By default this is the default group. |
| __Shared Bundle Settings Group__ | Define which group settings to use for shared AssetBundles (Monoscript and UnityBuiltInAssets). |
| __Built In Bundle Naming Prefix__ | Choose how to name the AssetBundle produced for Unity built in resources. |
| __MonoScript Bundle Naming Prefix__ | Choose how to name the AssetBundle that contains all MonoScripts. The bundle ensures that Unity loads all Monoscripts before any MonoBehaviours can reference them. It also decreases the number of duplicated or complex Monoscript dependencies and so, reduces runtime memory overhead. |

## Build and Play Mode Scripts

Configures the [`IDataBuilder`](xref:UnityEditor.AddressableAssets.Build.IDataBuilder) scripts available in the project. If you create a custom Build or Play mode script, you must add it to this list before you can use it.

The Addressables packages has some default build scripts that handle the default build processes and offer different ways to access data in Play mode. You can find these scripts in the `AddressableAssetData/DataBuilders` folder.

> [!NOTE]
> Build and Play mode scripts are `ScriptableObject` assets. To create a ScriptableObject asset for a Build or Play mode script, follow the instructions in the [ScriptableObject manual page](xref:um-class-scriptable-object).

To add a custom script, select the __+__ button and choose the ScriptableObject asset which represents the desired script from the file panel.

For more information about custom scripts, refer to [Create a custom build script](build-scripting-custom.md).

## Asset Group Templates

Defines the list of templates that you can use to create new groups. When you create a new template, you must add it to this list before you can use it.

The Addressables package has one template that includes the schemas that the default build scripts uses. You can find the template in the `AddressableAssetData/AssetGroupTemplates` folder.

> [!NOTE]
> Group templates are ScriptableObject assets. To create a ScriptableObject asset for a group template, follow the instructions in the [ScriptableObject manual page](xref:um-class-scriptable-object).

To add a custom template, select the __+__ button and choose the ScriptableObject asset which represents the desired template from the file panel.

For information on creating custom templates, refer to [Group templates](xref:group-templates).

## Initialization objects

Configures the initialization objects for the project. Initialization objects are ScriptableObject classes that implement the [`IObjectInitializationDataProvider`](xref:UnityEngine.ResourceManagement.Util.IObjectInitializationDataProvider) interface. You can create these objects to pass data to the Addressables initialization process at runtime.

To create a ScriptableObject asset for an initialization object, follow the instructions in the [ScriptableObject manual page](xref:um-class-scriptable-object).

To add an initialization object, select the __+__ button and choose the ScriptableObject asset which represents the desired object from the file panel.

For more information, refer to [Customizing initialization](xref:addressables-api-initialize-async).

## Cloud Content Delivery

| **Property**                 | **Description** |
|------------------------------|---------------|
| **Enable CCD Features**      | Enable this property to enable [CCD features](AddressablesCCD.md).  |

## Additional resources

* [Addressables Groups window reference](GroupsWindow.md)
* [Group Inspector settings reference](ContentPackingAndLoadingSchema.md)
* [Addressables Preferences reference](addressables-preferences.md)
