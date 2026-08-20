# Convert Addressables projects to content directories

If you created a project using Addressables before version 4.0, you can convert it to use [content directories](xref:um-content-directories) as the content build system. For more information about using content directories in Addressables, refer to [Choose a content build system](content-build-systems.md). The workflow to update to content directories is as follows:

1. [Convert existing groups to content directories](#convert-existing-groups-to-content-directories).
1. [Validate the build](#validate-the-build).
1. [Optionally clean up redundant Addressable entries](#clean-up-redundant-addressable-entries).

## Prerequisites

Install Addressables version 4.0 from the **Package Manager** window.

## Convert existing groups to content directories

To convert existing AssetBundle groups to content directories, perform the following steps:

1. Open the **Groups** window (**Window** > **Asset Management** > **Addressables** > **Groups**).
1. Select all groups that you want to convert to content directories. To select multiple groups hold down Ctrl (Command on macOS), or Shift to select a range of groups.
1. Right-click on the groups and select **Convert schema(s) to Content Directory**.

>[!NOTE]
>If you're using localization packages, then read-only localization package group schemas can't be edited in the Inspector directly.

## Build content directories

To create a content directory build, perform the following steps:

1. Open the **Groups** window (**Window** > **Asset Management** > **Addressables** > ***Groups**).
1. Select **Build** > **Clear Build Cache** > **All**. This step is only necessary the first time you build content directories, to remove any old AssetBundle content in the cache.
1. Select **Build** > **New Build** > **Default Build Script**.

The Default Build Script builds both AssetBundles and content directories at the same time, so if you still have AssetBundles in your project, it produces both an AssetBundle build, and a content directory build.

### Read-only warnings and errors

Some restrictions are in place for read-only files to optimize the content build process. These files are no longer automatically modified at build time to save processing overhead. You therefore might need to update read-only files manually. You can use the [**Project Auditor**](xref:um-project-auditor) window to fix these issues as follows:

1. Open the **Project Auditor** window (**Window** > **Analysis** > **Project Auditor**). If it's your first time using Project Auditor, you might be prompted to download the Project Auditor Rules package.
1. Select **Start Analysis**.
1. Expand the **Top Ten Issues** panel.
1. Select **Quick Fix** on any issues that say **Mesh requires Read/Write access** or **Texture requires Read/Write access**.

You can also use the [**Build Analysis** window](xref:um-build-analysis-window-reference) to inspect the output of a build.

## Validate the build

* To validate that the Editor works in Play mode, set the **Play Mode Script** to **Use Existing Build** (**Window** > **Asset Management** > **Addressables** > **Groups** > **Play Mode Script**), and then enter Play mode.
* To validate the Player build, create a build from the [**Build Profiles** window](xref:um-create-build-profile).

## Clean up redundant Addressable entries

In projects that use the AssetBundle system, some assets might be marked as Addressable to control how Unity bundles assets together, for example, to avoid asset duplication. The content directory system loads and unloads assets granularly so you no longer need to mark these assets as Addressable. Only assets that are loaded by address, asset reference, or label need to be marked as Addressable, because their dependencies are included automatically in the content directory system. Make any other assets non-Addressable to remove redundant catalog entries and reduce startup overhead.

## Additional resources

* [Introduction to content directories](xref:um-content-directories-introduction)
* [Choose a content build system](content-build-systems.md)
* [Add assets to groups](groups-create.md)
* [Create a content build](builds-full-build.md)