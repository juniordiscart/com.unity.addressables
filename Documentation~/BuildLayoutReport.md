---
uid: addressables-build-layout-report
---

# Create a build report

The build layout report provides detailed information and statistics about Addressables builds. The format of the report depends on whether you're using AssetBundles or content directories as the [content build system](content-build-systems.md):

* **Content directories**: Uses the [**Build Analysis** window](xref:um-build-analysis-window-reference) to display the details of the content build.
* **AssetBundles**: Uses the [**Addressables Report** window](addressables-report-window.md) to display the details of the content build at  `Library/com.unity.addressables/buildlayout.json`.

When the **Debug Build Layout** setting is enabled in the [**Preferences** window](addressables-preferences.md), Unity creates the report whenever you build Addressables content.

## Create a build report

To create a build report, you must enable the **Debug Build Layout** setting, which creates a build report whenever you create a content build:

1. Open the [**Preferences** window](addressables-preferences.md) (menu: **Edit > Preferences**, macOS: **Unity > Settings**).
1. Select __Addressables__ from the list of preference types.
1. Enable the __Debug Build Layout__ option.
1. [Perform a build](builds-full-build.md) of Addressables content.
1. Open the [**Addressables Report** window](addressables-report-window.md) (**Window** > **Asset Management** > **Addressables** > **Addressables Report**) to view the report.

>[!TIP]
> Enable the [**Open Addressables Report**](addressables-preferences.md) setting to automatically open the report in the **Addressables Report** window after the build completes. If the content build contains content directories, the **Addressables Report** window displays a button to open the [**Build Analysis** window](xref:um-build-analysis-window-reference) to inspect the build further.

## Additional resources

* [Addressables Report window reference](addressables-report-window.md)
* [Addressables Preferences reference](addressables-preferences.md)
* [Build Analysis window reference](xref:um-build-analysis-window-reference)

