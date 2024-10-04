# Incremental content updates

When you distribute content remotely, you can reduce the amount of data your users need to download for an update by publishing incremental content update builds. An incremental update build allows you to publish remote bundles which contain only the assets that have changed since you last published an update rather than republishing everything. The assets in these smaller, updated bundles override the existing assets. 

> [!IMPORTANT]
> You must enable the [Build Remote Catalog](xref:addressables-asset-settings) option before you publish a player build if you want to have the option to publish incremental updates. Without a remote catalog, an installed application doesn't check for updates.

For more detailed information about content updates, including examples, refer to [Content update builds](xref:addressables-content-update-builds).

## Start a content update build

To make a content update, rather than a full build:

1. In the __Build Settings__ window, set the __Platform Target__ to match the target of the previous content build that you are now updating.
1. Open the __Addressables Groups__ window (menu: __Asset Management > Addressables > Groups__).
1. From the __Tools__ menu, run the __Check for Content Update Restrictions__ command. The __Build Data File__ browser window opens. 
1. Locate the `addressables_content_state.bin` file produced by the previous build. This file is in a subfolder of `Assets/AddressableAssestsData` named for the target platform. 
1. Select __Open__. The __Content Update Preview__ window searches for changes and identifies assets that must be moved to a new group for the update. If you have not changed any assets in groups set to "Cannot Change Post Release," then no changes will be listed in the preview. (When you change an asset in a group set to "Can Change Post Release," then Addressables rebuilds all the AssetBundles for the group; Addressables does not move the changed assets to a new group in this case.)
1. Select __Apply Changes__ to accept any changes.
1. From the __Build__ menu, run the __Update a Previous Build__ command.
1. Open the `addressables_content_state.bin` file produced by the previous build.

The build process starts.

After the build is complete, you can upload the files from your __RemoteBuildPath__ to your hosting server.

> [!IMPORTANT]
> Addressables uses the `addressables_content_state.bin` file to identify which assets you changed. You must preserve a copy of this file for each published build. Without the file, you can only create a full content build, not an update.