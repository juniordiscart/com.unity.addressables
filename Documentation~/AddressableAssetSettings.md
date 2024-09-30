---
uid: addressables-asset-settings
---

# Addressable Asset Settings reference

To manage how Addressable assets work in your project, use the **Addressable Asset Settings** Inspector. To open this Inspector, go to **Window** &gt; **Asset Management** &gt; **Addressables** &gt; **Settings**.

The Addressables system stores the settings asset in the `AddressableSettingsData` folder in the project's `Assets` folder. If this folder doesn't exist yet, you must initialize the Addressables system from the [Groups window](xref:addressables-groups-window) (menu: **Window** &gt; **Asset Management** &gt; **Addressables** &gt; **Groups**).

![](images/addressable-assets-settings.png)<br/>*The Addressable Asset Settings Inspector*

The Inspector contains the following sections:

* [Profile](#profile)
* [Diagnostics](#diagnostics)
* [Catalog](#catalog)
* [Update a Previous Build](#update-a-previous-build)
* [Downloads](#downloads)
* [Build](#build)
* [Build and Play Mode Scripts](#build-and-play-mode-scripts)
* [Asset Group Templates](#asset-group-templates)
* [Initialization object list](#initialization-object-list)
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

|**Property**||**Description**|
|---|---|---|
| __Player Version Override__|| Overrides the timestamp used to create the remote catalog name. If set, the remote catalog is named, `Catalog_<Player Version Override>.json`. If left blank, then Unity uses the timestamp.<br/><br/>If you use a unique remote catalog name for every new build, you can host multiple versions of your content at the same base URL. If you use the same override string for every build, then all players load the new catalog. Player update builds also always use the same remote catalog name as the build they're updating. For more information, refer to [Content update builds](xref:addressables-content-update-builds). |
| __Compress Local Catalog__|| Enable this property to build the catalog in a compressed AssetBundle file. This property reduces the storage size of the catalog, but increases the time to build and to load the catalog. |
| __Build Remote Catalog__|| Enable this property to create a copy of the content catalog for storage on a remote server. to build a remote catalog. When you enable this property the following options are available: |
| | __Build & Load Paths__| Set where to build and load the remote catalog. Choose a Profile path pair from the list or select `<custom>` if you want to set the build and load paths separately.|
| | __Build Path__| Only displayed if you set __Build & Load Paths__ to `<custom>`. Set where to build the remote catalog. Typically, you should use the __RemoteBuildPath__ Profile variable.|
| | __Load Path__| Only displayed if you set __Build & Load Paths__ to `<custom>`. Set the URL at which to access the remote catalog. Typically, you should use the __RemoteLoadPath__ Profile variable.|
|__Only update catalogs manually__|| Enable this property to disable the automatic check for an updated remote catalog when the Addressables system initializes at runtime. You can manually [check for an updated catalog](xref:addressables-api-load-content-catalog-async).|

## Update a Previous Build

To use the properties in the **Update a Previous Build** section, you must enable the __Build Remote Catalog__ property.

|**Property**|**Description**|
|---|---|
|**Check for Update Issues**|Choose whether to perform a [content update restriction](ContentUpdateWorkflow.md) as part of the update, and how to handle the result.|
|__Content State Build Path__|Set where to build the content state file that the default build script builds.|

## Downloads

|**Property**|**Description**|
|---|---|
| __Custom certificate handler__| Set the class to use for custom certificate handling. The list has all classes in the project that extend [`UnityEngine.Networking.CertificateHandler`](xref:UnityEngine.Networking.CertificateHandler). |
| __Max Concurrent Web Requests__| Set the maximum amount of concurrent web requests. The system queues any requests beyond this limit. 2 to 4 concurrent downloads are recommended to reach the best download speeds. |
| __Catalog Download Timeout__ | Set how many seconds to wait for a catalog file to download. If you set this to 0 there will be no timeout.|

## Build

|**Property**||**Description**|
|---|---|---|
| **Build Addressables on Player Build** || Select how Unity builds Addressables content as part of the Player build. <br/><br/>The __Build Addressables content on Player Build__ and __Do not Build Addressables content on Player Build__ properties override the global Preference for the current project and affect all contributors who build the Project. Otherwise, the global Preferences value applies to all Unity projects. Refer to [Building content](BuildingContent.md) for more information.|
|| __Build Addressables content on Player Build__| Always build Addressables content when building the Player.|
||__Do not Build Addressables content on Player Build__| Never build Addressables content when building the Player. If you modify Addressables content, you must rebuild it manually before building the Player.|
|| __Use global Settings (stored in preferences)__| Use the value specified in the [Unity Editor Preferences](addressables-preferences.md) under __Addressables__.|
| __Ignore Invalid/Unsupported Files in Build__|| Enable this property to exclude invalid or unsupported files from the build script rather than aborting the build. |
| __Unique Bundle IDs__|| Enable this property to make a unique name for a bundle in every build. Refer to [Unique Bundle IDs](xref:addressables-content-update-builds) for more information. |
| __Contiguous Bundles__|| Enable this property to produce a more efficient bundle layout. If you have bundles produced by Addressables 1.12.1 or earlier, disable this property to minimize bundle changes. |
| __Non-Recursive Dependency Calculation__ || Enable this property to improve build times and reduce runtime memory overhead when assets have circular dependencies. <br/><br/>For example, A prefab assigned to Bundle A references a material assigned to Bundle B. If this property is disabled, Unity needs to calculate the material's dependencies twice, once for each bundle. If this option is enabled, Unity only needs to calculate the material's dependencies once, for Bundle B.<br/><br/> In an example where many scenes reference the same material, if this property is disabled, Unity opens each scene to calculate shader usage, which is a costly operation. If this property is enabled, Unity only loads the material and doesn't need to open any scenes for dependency calculation.<br/><br/>This option is enabled by default when using Unity version 2021.2 or later. Disabling this option invalidates previously built bundles because the rebuilt bundles have a different build layout. Therefore, leave this property enabled unless you've shipped a build. <br/><br/>Some circular dependencies might fail to load when the option is enabled because the referenced asset is always assigned to the same bundle location, even when more content is added to the build. This issue often occurs for Monoscripts. Building the MonoScript bundle can help resolve these load failures. |
| __Shader Bundle Naming Prefix__ || Choose how to name the bundle produced for Unity shaders. |
| __MonoScript Bundle Naming Prefix__ || Choose how to name the bundle that contains all MonoScripts. The bundle ensures that Unity loads all Monoscripts before any MonoBehaviours can reference them. It also decreases the number of duplicated or complex Monoscript dependencies and so, reduces runtime memory overhead. |
| __Strip Unity Version From AssetBundles__ || Enable this property to remove the Unity version from the bundle header. |
| __Disable Visible Sub Asset Representations__ || Enable this property to improve build times if you don't use sub-objects directly (such as sprites, or sub-meshes). |

## Build and Play Mode Scripts

Configures the [`IDataBuilder`](xref:UnityEditor.AddressableAssets.Build.IDataBuilder) scripts available in the project. If you create a custom Build or Play mode script, you must add it to this list before you can use it.

The Addressables packages has some default build scripts that handle the default build processes and offer different ways to access data in Play mode. You can find these scripts in the `AddressableAssetData/DataBuilders` folder.

> [!NOTE]
> Build and Play mode scripts are `ScriptableObject` assets. To create a ScriptableObject asset for a Build or Play mode script, follow the instructions in the [ScriptableObject manual page](xref:class-ScriptableObject).

To add a custom script, select the __+__ button and choose the ScriptableObject asset which represents the desired script from the file panel.

Refer to [Custom Build Scripting](xref:addressables-api-build-player-content) for more information about custom scripts.

## Asset Group Templates

Defines the list of templates that you can use to create new groups. When you create a new template, you must add it to this list before you can use it.

The Addressables package has one template that includes the schemas that the default build scripts uses. You can find the template in the `AddressableAssetData/AssetGroupTemplates` folder.

> [!NOTE]
> Group templates are ScriptableObject assets. To create a ScriptableObject asset for a group template, follow the instructions in the [ScriptableObject manual page](xref:class-ScriptableObject).

To add a custom template, select the __+__ button and choose the ScriptableObject asset which represents the desired template from the file panel.

Refer to [Group templates](xref:group-templates) for information on creating custom templates.

## Initialization object list

Configures the initialization objects for the project. Initialization objects are ScriptableObject classes that implement the [`IObjectInitializationDataProvider`](xref:UnityEngine.ResourceManagement.Util.IObjectInitializationDataProvider) interface. You can create these objects to pass data to the Addressables initialization process at runtime.

> [!NOTE]
> Initialization objects are ScriptableObject assets. To create a ScriptableObject asset for an initialization object, follow the instructions in the [ScriptableObject manual page](xref:class-ScriptableObject).

To add an initialization object, select the __+__ button and choose the ScriptableObject asset which represents the desired object from the file panel.

Refer to [Customizing initialization](xref:addressables-api-initialize-async) for more information.

## Cloud Content Delivery

| **Property**                 | **Description**                                                                                           |
|------------------------------|-----------------------------------------------------------------------------------------------------------|
| **Enable CCD Features**      | Enable this property to enable [CCD features](AddressablesCCD.md).                                        |
| **Log HTTP Requests**        | Enable this property to log http requests to the CCD Management API.                                      |
| **Log HTTP Request Headers** | Enable this property to additionally log request headers when logging requests to the CCD Management API. |
