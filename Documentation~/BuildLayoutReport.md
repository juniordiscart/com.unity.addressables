---
uid: addressables-build-layout-report
---

# Create a build report

The build layout report provides detailed information and statistics about your Addressables builds, including:

* Description of AssetBundles
* Sizes of each Asset and AssetBundle
* Explanation of non-Addressable assets implicitly included in AssetBundles as dependencies. Refer to [Asset and AssetBundle dependencies](AssetDependencies.md) for more information.
* [AssetBundle dependencies](xref:um-asset-bundles-dependencies)

When enabled, the Addressables build script creates the report whenever you build Addressables content. You can enable the report in the Addressables section of the [Preferences window](addressables-preferences.md). You can find the report in your project folder at `Library/com.unity.addressables/buildlayout.json`. Producing the report increases build time.

## Create a build report

To create a build report:

1. Open the Unity Preferences window (menu: **Edit > Preferences**, macOS: **Unity > Settings**).
1. Select __Addressables__ from the list of preference types.
1. Enable the __Debug Build Layout__ option.
1. [Perform a build](builds-full-build.md) of Addressables content. If you enable the **Open Addressables Report** setting, then the [Addressables Report window](addressables-report-window.md) automatically opens with the report after the build completes.

The report file is in the`Library/com.unity.addressables/` folder of your Unity project, named `buildlayout.json`.

## Additional resources

* [Addressables Report window reference](addressables-report-window.md)

