---
uid: addressables-building-content
---

# Create a content-only build

To create a content-only build, you need to use the [Addressables Groups window](GroupsWindow.md), which allows you to run a build script to create a build that only contains the Addressables assets in your project. You can also use the Addressables Groups window to run [your own custom build scripts](build-scripting-builds.md).

To create a content-only build:

1. Configure your project's [group settings](GroupSchemas.md).
1. Optionally, [set up a remote build](#set-up-remote-content-builds) if you're distributing the content on a remote server.
1. [Launch the content build from the Groups window](#create-a-content-build).

> [!TIP]
> If you encounter build or runtime loading issues during development, in the [Groups Window](GroupsWindow.md) run the __Clear Build Cache > All__ command from the __Build__ menu before you rebuild your content.

## Set up remote content builds

To set up a remote content build:

1. Open the [Addressable Asset Settings asset](AddressableAssetSettings.md) (menu: __Window > Asset Management > Addressables > Settings__).
2. In the __Catalog__ section, enable the __Build Remote Catalog__ setting. The __BuildPath__ and __LoadPath__ settings for the catalog must be the same as those you use for remote groups. You can usually use the RemoteBuildPath and RemoteLoadPath profile variables.
3. For each group that you want to build as remote content, set the __BuildPath__ and __LoadPath__ to the RemoteBuildPath and RemoteLoadPath profile variables (or a custom value if desired).
4. Open the [Profiles window](addressables-profiles-window.md)  (menu: __Window > Asset Management > Addressables > Profiles__).
5. Set the RemoteLoadPath variable to the URL where you plan to host your remote content. If you require different URLs for different types of builds, create a new Profile for each build type. For more information, refer to [Create a profile](profiles-create.md).

For information on distributing remote content, refer to [Distribute remote content](RemoteContentDistribution.md).

## Create a content build

After you have group and Addressables system settings configured, you can run a content build:

1. Open the [Addressables Groups window](GroupsWindow.md) (menu: __Windows > Asset Management > Addressables > Groups__).
2. Select the desired profile from the __Profile__ menu on the toolbar.
3. Select __Build > New Build > Default Build Script__. If you have created your own build scripts they are also available from this menu.

The Default Build Script creates one or more AssetBundles for each group and saves them to either the local or the remote build path. By default, the build creates files in the locations defined in your [Profile](xref:addressables-profiles) settings for the __LocalBuildPath__ and __RemoteBuildPath__ variables. The files that Unity uses for your Player builds include AssetBundles (.bundle), catalog JSON and hash files, and settings files.

If you've already created a build, the __Update a Previous Build__ option is also available, and you can use it to create an [update build](builds-update-build.md).

> [!WARNING]
> Don't change the local build or load paths from their default values. If you do, you must copy the local build artifacts from your custom build location to the project's [StreamingAssets](xref:StreamingAssets) folder before making a Player build. Altering these paths also precludes building your Addressables as part of the Player build.

A content build also creates the following files that Addressables doesn't use directly in a Player build:

* `addressables_content_state.bin`: used to make a content update build. If you support dynamic content updates, you must save this file after each content release. Otherwise, you can ignore this file.
* `AddressablesBuildTEP.json`: logs build performance data. For more information, refer to [Build Profiling](xref:addressables-build-profile-log).

> [!IMPORTANT]
> If you plan to publish remote content updates without rebuilding your application, you must preserve the `addressables_content_state.bin` file for each published build. Without this file, you can only create a full content build and player build, not an update. Refer to [Content update builds](xref:addressables-content-update-builds) for more information.

After the build is complete, you can perform a Player build and upload any remote files from your __RemoteBuildPath__ to your hosting server.

## Additional resources

* [Distribute remote content](RemoteContentDistribution.md)
* [Build Addressable assets with a player build](build-player-builds.md)
* [Build Addressable assets from scripts](build-scripting-builds.md)
