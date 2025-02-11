---
uid: addressables-build-layout-report
---

# Build layout report

The build layout report provides detailed information and statistics about your Addressables builds, including:

* Description of AssetBundles
* Sizes of each Asset and AssetBundle
* Explanation of non-Addressable Assets implicitly included in AssetBundles as dependencies. Refer to [Asset and AssetBundle dependencies](xref:addressables-asset-dependencies) for more information.
* [AssetBundle dependencies](xref:AssetBundles-Dependencies)

When enabled, the Addressables build script creates the report whenever you build Addressables content. You can enable the report in the Addressables section of the [Preferences window](addressables-preferences.md). You can find the report in your project folder at `Library/com.unity.addressables/buildlayout`. Producing the report increases build time.

Refer to [Building your Addressable content](xref:addressables-building-content) for more information about building content.

## Create a build report

To create a build report:

1. Open the Unity Preferences window (menu: Edit > Preferences).
1. Select __Addressables__ from the list of preference types.
1. Enable the __Debug Build Layout__ option.
1. Perform a full build of your Addressables content. Refer to [Full builds](builds-full-build.md) for more information.
1. In a file system window, navigate to the `Library/com.unity.addressables/` folder of your Unity project.
1. Open the `buildlayout` file in a text editor.

## Report data

The build layout report contains the following information:

* [Summary](#summary-section): Provides an overview of the build.
* [Group](#group-section): Provides information for each group.
* [Asset bundle](#assetbundle-information): Provides information about each bundle built for a group.
* [Asset](#asset-information): Provides information about each explicit asset in a bundle.
* [File](#file-information): Provides information about each serialized file in an AssetBundle archive.
* [Built-in bundles](#built-in-bundles): Provides information about bundles created for assets, such as the default shader, that are built into Unity.

### Summary section

Provides a summary of the build.

| **Name**| **Description** |
|:---|:---|
| **Addressable Groups**| The number of groups included in the build. |
| **Explicit Assets Addressed**| The number of Addressable assets in the build (this number doesn't include assets in the build that are referenced by an Addressable asset, but which aren't marked as Addressable). |
| **Total Bundle**| The number of AssetBundles created by the build, including how many contain Scene data. |
| **Total Build Size**| The combined size of all AssetBundles. |
| **Total MonoScript Size**| The size of serialized MonoBehaviour and SerializedObject instances. |
| **Total AssetBundle Object Size**| The size of all AssetBundle object instances|

### Group section

Reports how Addressables packed the assets in a group into AssetBundles.

| **Name**| **Description** |
|:---|:---|
| **Group summary**| Name, number of bundles created for group, total size, and number of explicit assets built for the group. |
| **Schemas**| Schemas and settings for the group. |
| **Asset bundles**| See [AssetBundle information](#assetbundle-information). |

### AssetBundle information

Reports details for each AssetBundle built for a group.

| **Name**| **Description** |
|:---|:---|
| **File name**| The file name of the AssetBundle. |
| **Size**| The size of the AssetBundle |
| **Compression**| The compression setting used for the bundle. |
| **Object size**|  |
| **Bundle Dependencies**| The list of other AssetBundles the current bundle depends upon. These bundles are always loaded with the current bundle. |
| **Expanded Bundle Dependencies**|  |
| **Explicit Assets**| [Asset information](#asset-information) about Addressables included in the bundle. |
| **Files**| [File information](#file-information) about the files in the AssetBundle archive. Scene bundles contain up to two files per Scene, non-Scene bundles contain only one file. |

### Asset information

Provides Information for each asset in the Explicit Assets section.

| **Name**| **Description** |
|:---|:---|
| **Asset path**| The path to the asset in your project |
| **Total Size**|  |
| **Size from Objects**|  |
| **Size from Streamed Data**|  |
| **File Index**| The index of the file in the AssetBundle in which this asset is located. |
| **Addressable Name**| The address of the asset. |
| **External References**|  |
| **Internal References**|  |

### File information

Provides details about each serialized file in an AssetBundle archive

| **Name**| **Description** |
|:---|:---|
| **File summary**| Index in file list, number and size of serialized MonoScripts in the file |
| **File sections**| A serialized file can have one or more of the following sections: <br/> &#8226; No extension<br/> &#8226; .resS<br/> &#8226; .resource<br/> &#8226; .sharedAssets |
| **Data from Other Assets**| Dependent assets referenced by assets in the file. |

### Built-in bundles

Lists any bundles that Addressables created from assets, such as the default shaders, that are provided as part of the Unity Engine. The Addressables build places such assets in the separate bundles listed here when needed to avoid duplicating the assets across multiple bundles as implicit dependencies.

