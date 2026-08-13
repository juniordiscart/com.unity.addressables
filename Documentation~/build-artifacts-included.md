# Player artifacts

During a player build, the Addressables system copies the following files from the `Library/com.unity.addressables/aa/<AddressablesPlatform>` folder to the [StreamingAssets](xref:um-streaming-assets) folder:

|**File**|**Description**|
|---|---|
|Local AssetBundles| `.bundle` files according to [group](GroupSchemas.md), [profile](profiles-create.md), and platform settings. By default, these files are located in the [`BuildTarget`](xref:UnityEditor.EditorUserBuildSettings.activeBuildTarget) subfolder. To change the build location of the AssetBundle files produced by a group, modify the [Build & Load Paths](AddressableAssetSettings.md#catalog) setting.|
|`settings.json`| Contains Addressables configuration data used at runtime.|
|`catalog.json`| The content catalog used to locate and load assets at runtime if no newer remote catalog is available. For more information about catalogs, refer to [Content catalogs](build-content-catalogs.md).|
|`AddressablesLink/link.xml`| Prevents the Unity linker from stripping types used by your assets. For more information about code stripping, refer to [Managed Code Stripping](xref:um-managed-code-stripping). In Unity versions 2021.2 and later, this file temporarily copies the [`AddressableAssetSettings.ConfigFolder`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ConfigFolder), or the `Assets/Addressables_Temp` folder if no settings file exists.|

For a full list of platform names, refer to [`AddressablesPlatform`](xref:UnityEngine.AddressableAssets.AddressablesPlatform).

## Artifacts not included in the player

The following artifacts aren't included in a built player.

### Remote content

Upload files used for remote content to a hosting server. By default these files are located in the `ServerData` folder.

|**File**|**Description**|
|---|---|
|Remote AssetBundles|  `.bundle` files according to [group](GroupSchemas.md), [profile](profiles-create.md), and platform settings. By default, these files are located in the [`BuildTarget`](xref:UnityEditor.EditorUserBuildSettings.activeBuildTarget) subfolder. To change the build location of the AssetBundle files produced by a group, modify the [Build & Load Paths](AddressableAssetSettings.md#catalog) setting.|
|`catalog_{timestamp or player version}.json`| A remote catalog which when downloaded overrides the local catalog. This file is only created if the __Build Remote Catalogs__ option in [Catalog settings](AddressableAssetSettings.md#catalog) is enabled. By default the file name includes the timestamp of the build. To use a version number instead, specify the value of the __Player Version Override__ in [Catalog settings](AddressableAssetSettings.md#catalog). For more information about catalogs, refer to [Content catalogs](build-content-catalogs.md).|
|`catalog_{timestamp or player version}.hash`| A file used to check whether the remote catalog has changed since the last time a client app downloaded it. Similar to the remote catalog file, this file is only created if the __Build Remote Catalogs__ option in [Catalog settings](AddressableAssetSettings.md#catalog) is enabled. To change the build location of this file modify the __Build & Load Paths__ in Catalog settings. By default, the file name includes the timestamp of the build. To use a version number instead, specify the value of the __Player Version Override__ in [Catalog settings](AddressableAssetSettings.md#catalog). For more information about catalogs, refer to [Content catalogs](build-content-catalogs.md).|

### Content state file

The `addressables_content_state.bin` file is used to make a [content update build](xref:addressables-content-update-builds). If you support dynamic content updates, you must save this file after each full content build that you release. Otherwise, you can ignore this file.

By default this file is located in `Assets/AddressableAssetsData/<AddressablesPlatform>`. Refer to [`AddressablesPlatform`](xref:UnityEngine.AddressableAssets.AddressablesPlatform) for all platform names. To change the build location of the file specify the value of the __Content State Build Path__ in [Catalog settings](AddressableAssetSettings.md#catalog).

> [!NOTE]
> Check this file into version control and create a new branch each time a player build is released.

### Diagnostic data

Additional files can be created to collect data about the content build.

The files are:
* `Library/com.unity.addressables/AddressablesBuildTEP.json`: build performance data. Refer to [Build profiling](BuildProfileLog.md) for more information.
* `Library/com.unity.addressables/buildlayoutreport`: information about AssetBundles produced by the build. Refer to [Build layout report](BuildLayoutReport.md) for more information.

## Additional resources

* [Addressable Asset Settings reference](AddressableAssetSettings.md)
* [Content catalogs](build-content-catalogs.md)
* [Shared AssetBundles](build-shared-assetbundles.md)