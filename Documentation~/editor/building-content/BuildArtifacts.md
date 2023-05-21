---
uid: addressables-build-artifacts
---
# Build artifacts

A [content build] creates files in several locations; Unity doesn't include every file in a built player. Typically, Unity includes files associated with local content in the built player and excludes files associated with remote content.

Most of the files associated with local content are located in the `Library/com.unity.addressables` folder. This is a special subfolder in the `Library` folder which Unity uses to store Addressables files. For more information about the `Library` folder, see [Importing assets].

## Artifacts included in the player

During a player build, the Addressables system copies the following files from the `Library/com.unity.addressables/aa/<AddressablesPlatform>` folder to the [StreamingAssets] folder:

* Local AssetBundles (`.bundle`): according to your group, profile, and platform settings. By default, these files are located in the [BuildTarget] subfolder. To change the build location of the bundle files produced by a group, modify the [Build & Load Paths] setting.
* `settings.json`: contains Addressables configuration data used at runtime.
* `catalog.json`: the content catalog used to locate and load assets at runtime (if no newer remote catalog is available).  For more information about catalogs, see [Content catalogs].
* `AddressablesLink/link.xml`: prevents the Unity linker from stripping types used by your assets. For more information about code stripping, see [Managed Code Stripping]. In Unity version 2021.2 and later, this file temporarily copied the [AddressableAssetSettings.ConfigFolder], or the `Assets/Addressables_Temp` folder if no settings file exists.

For a full list of platform names, see [AddressablesPlatform].

## Artifacts not included in the player

### Remote content
Files used for remote content should be uploaded to a hosting server. By default these files are located in the `ServerData` folder. 

The files are:

* Remote AssetBundles (`.bundle`): according to your group, profile, and platform settings. By default these files are located in the [BuildTarget] subfolder. To change the build location of the files produced by a group, modify the [Build & Load Paths] setting.
* `catalog_{timestamp or player version}.json`: a remote catalog which when downloaded will override the local catalog. This file is only created if the __Build Remote Catalogs__ option in [Catalog settings] is enabled. To change the build location of this file modify the __Build & Load Paths__ in [Catalog settings]. By default, the filename includes the timestamp of the build. To use a version number instead, specify the value of the __Player Version Override__ in [Catalog settings]. For more information about catalogs, see [Content catalogs].
* `catalog_{timestamp or player version}.hash`: a file used to check whether the remote catalog has changed since the last time a client app downloaded it. Just like the remote catalog file, this file is only created if the __Build Remote Catalogs__ option in [Catalog settings] is enabled.  To change the build location of this file, modify the __Build & Load Paths__ in [Catalog settings]. By default, the filename includes the timestamp of the build. To use a version number instead, specify the value of the __Player Version Override__ in [Catalog settings]. For more information about catalogs, see [Content catalogs].

### Content State File

The `addressables_content_state.bin` file is used for making a [content update build]. If you are supporting dynamic content updates, you must save this file after each full content build that you release. Otherwise, you can ignore this file. 

By default this file is located in `Assets/AddressableAssetsData/<AddressablesPlatform>`. See [AddressablesPlatform] for all platform names. To change the build location of the file specify the value of the __Content State Build Path__ in [Update a Previous Build].

> [!NOTE]
> It is recommended to check this file into version control and create a new branch each time a player build is released.

### Diagnostic Data

Additional files can be created to collect data about the content build. 

The files are:
* `Library/com.unity.addressables/AddressablesBuildTEP.json`: build performance data. See [Build profiling]. 
* `Library/com.unity.addressables/buildlayoutreport`: information about AssetBundles produced by the build. See [Build layout report].

## Content catalogs

Content catalogs are the data stores Addressables uses to look up an asset's physical location based on the key(s) provided to the system. Addressables builds a single catalog for all Addressable assets. The catalog is placed in the [StreamingAssets] folder when you build your application player. The local catalog can access remote as well as local assets, but if you want to update content between full builds of your application, you must create a remote catalog.

The remote catalog is a separate copy of the catalog that you host along with your remote content. 
Ultimately, Addressables only uses one of these catalogs. A hash file contains the hash (a mathematical fingerprint) of the catalog. If a remote catalog is built and it has a different hash than the local catalog, it is downloaded, cached, and used in place of the built-in local catalog. When you produce a [content update build], the hash is updated and the new remote catalog points to the changed versions of any updated assets.

> [!NOTE]
> You must enable the remote catalog for the full player build that you publish. Otherwise, the Addressables system does not check for a remote catalog and thus cannot detect any content updates. See [Enabling the remote catalog]. 

Although Addressables produces one content catalog per project, you can load catalogs created by other Unity Projects to load Addressable assets produced by those Projects. This allows you to use separate Projects to develop and build some of your assets, which can make iteration and team collaboration easier on large productions. See [Managing catalogs at runtime] for information about loading catalogs.

### Catalog settings

Use [Catalog settings] to configure local and remote catalogs.

To minimize the catalog size, use the following settings:
1. Compress the local catalog.  If your primary concern is how big the catalog is in your build, there is an option in [Catalog settings] called **Compress Local Catalog**. This option builds catalog that ships with your game into an AssetBundle. Compressing the catalog makes the file itself smaller, but note that this does increase catalog load time.  
2. Disable built-in scenes and Resources.  Addressables provides the ability to load content from Resources and from the built-in scenes list. By default this feature is on, which can bloat the catalog if you do not need this feature.  To disable it, select the "Built In Data" group within the Groups window (**Window** > **Asset Management** > **Addressables** > **Groups**). From the settings for that group, you can uncheck "Include Resources Folders" and "Include Build Settings Scenes". Unchecking these options only removes the references to those asset types from the Addressables catalog.  The content itself is still built into the player you create, and you can still load it via legacy API. 
3. There are several group settings that can help reduce the catalog size, such as __Internal Asset Naming Mode__. For more information see [Advanced Group settings].

## Shared AssetBundles

In addition to the bundles created from your `AddressableAssetGroups`, a build can produce specialized bundles called "shared AssetBundles". These are the `unitybuiltinshaders` AssetBundle and the `MonoScript` AssetBundle. 

The former is generated if any built-in shaders are used by assets included in the build. All Addressable assets that reference a shader that is built-in with the Unity Editor, such as the Standard Shader, do so by referencing this specialized shader AssetBundle. The naming method of the built-in shader bundle can be changed using the __Shader Bundle Naming Prefix__ option in [Addressables Build settings].

The latter can be toggled on or off by changing the __MonoScript Bundle Naming Prefix__ option in [Addressables Build settings]. The `MonoScript` bundle has naming options listed here, which are typically used in multi-project situations. It is used to build `MonoScript` behaviors into AssetBundles that can be referenced as a dependency.

Shared AssetBundles derive their build options from the default `AddressableAssetGroup`. By default this group is named **Default Local Group (Default)** and uses local build and load paths. In this case the shared bundles cannot be updated as part of a Content Update, and can only be changed in a new player build. The Check for Content Update Restrictions tool will fail to detect the changes to the bundle because it is only generated during the content build. Therefore if you plan on making content changes to the shared bundles in the future, set the default group to use remote build and load paths and set its [Update Restriction] to **Can Change Post Release**.

[Addressables Build settings]: xref:addressables-asset-settings#build
[AddressablesPlatform]: xref:UnityEngine.AddressableAssets.AddressablesPlatform
[AddressableAssetSettings.ConfigFolder]: xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ConfigFolder
[Advanced Group settings]: xref:addressables-content-packing-and-loading-schema#build-and-load-paths
[BuildTarget]: xref:UnityEditor.EditorUserBuildSettings.activeBuildTarget
[Build profiling]: xref:addressables-build-profile-log
[Build layout report]: xref:addressables-build-layout-report
[Build & Load Paths]: xref:addressables-content-packing-and-loading-schema#build-and-load-paths
[Catalog settings]: xref:addressables-asset-settings#catalog
[content build]: xref:addressables-builds
[Content catalogs]: #content-catalogs
[content update build]: xref:addressables-content-update-builds
[Enabling the remote catalog]: xref:addressables-remote-content-distribution#enabling-the-remote-catalog
[Importing assets]: xref:ImportingAssets
[Managed Code Stripping]: xref:ManagedCodeStripping	
[Managing catalogs at runtime]: xref:addressables-api-load-content-catalog-async
[Profiles]: xref:addressables-profiles
[StreamingAssets]: xref:StreamingAssets
[Update a Previous Build]: xref:addressables-asset-settings#update-a-previous-build
[Update Restriction]: xref:addressables-content-update-builds#group-update-restriction-settings
