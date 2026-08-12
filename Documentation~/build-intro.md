# Introduction to building Addressable assets

You can configure Unity to build Addressables assets in the following ways:

* [As part of every Player build.](build-player-builds.md)
* [Separately as a content-only build.](builds-full-build.md)

One of the benefits of using the Addressables system is that you can create separate builds of Addressables assets (content-only builds) to distribute remotely. In particular, this makes it easier to create [updates](builds-update-build.md) for your application.

A content-only build processes Addressables [groups](groups-intro.md) to create the [content catalog](build-content-catalogs.md), runtime settings, and the [AssetBundles](xref:AssetBundlesIntro) that contain your project's assets. Addressables uses these files to load content at runtime.

To build the content as part of the Player build, you can configure this in the [**Build**](AddressableAssetSettings.md#build) section of the Addressable Asset Settings.

You can also create builds from scripts. For more information, refer to [Start a build from a script](build-scripting-start-build.md).

## Defining a build

The Addressables system uses the following to determine how to build the Addressables content in your project:

* [Group settings](xref:addressables-group-schemas): Determines which category a group belongs to.
* [Profiles](xref:addressables-profiles): Determines the specific paths and URLs that the Addressables system uses to build and load the content.
* [Addressable Asset settings](xref:addressables-asset-settings): Contain options that affect content-only builds, such as whether to build remote content.

## Content-only build types

The content-only build can produce two general categories of content:

* __Local content__: Content that's included directly in the player build. The Addressables system manages local content automatically when you use the default build path for local content. If you change the local build path, you must copy the artifacts from the local build path to the project's `Assets/StreamingAssets` folder before making a Player build.
* __Remote content__: Content that's downloaded from a URL after your application is installed. You must upload the remote content to a hosting server so that your application can access the designated URL specified by a [`RemoteLoadPath`](remote-content-intro.md).

For more information about files produced by a content build, refer to [Build artifacts](BuildArtifacts.md).

## Start a content-only build

You can start content-only builds from a script or from the [__Groups__ window](GroupsWindow.md). For information on how to extend building Addressable content, refer to [Build scripting](build-scripting-builds.md).

The Addressables system includes the following build scripts:

* __Default Build Script__: Performs a full content build based on the groups, profiles, and Addressables system settings in your project.
* __Update a Previous Build__: Performs a differential content build to update a previously created build.
* __Play Mode scripts__: The Play Mode scripts are build scripts that control how the Editor accesses your content in Play mode. For more information, refer to [Play Mode Scripts](GroupsWindow.md#play-mode-script).

You can also use the build scripts to clear the cached files they create. You can run these functions from the __Build > Clean Build__ menu of the [Groups window](GroupsWindow.md#build).

For more information on how to start an Addressables build, refer to [Build Addressable assets](builds-full-build.md).

## Additional resources

* [Building Addressables content with Player builds](build-player-builds.md)
* [Build artifacts](BuildArtifacts.md)
* [Build scripting](build-scripting-builds.md)
* [Build Addressable assets](builds-full-build.md)