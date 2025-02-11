# Build content introduction

A content build processes Addressables [groups](groups-intro.md) to produce the [content catalog](build-content-catalogs.md), runtime settings, and the [AssetBundles](xref:AssetBundlesIntro) that contain your assets. Addressables uses these files to load content at runtime.

You can configure the Addressables system to build your Addressables content as part of every Player build or you can build your content separately before making a Player build. Refer to [Building Addressables content with Player builds](build-player-builds.md) for more information about configuring these options.

## Configure builds

If you configure Unity to build your content as part of the Player build, use the __Build__ or __Build and Run__ buttons in the Unity Editor [Build Settings](xref:PublishingBuilds) window to start a build. You can also invoke the Editor on the command line, passing in one of the `-buildPlatformPlayer` options or use an API such as [`BuildPipeline.BuildPlayer`](xref:UnityEditor.BuildPipeline.BuildPlayer(UnityEditor.BuildPlayerOptions)) to start the build. In all cases, Unity builds your Addressables content as a pre-build step before building the Player.

If you configure Unity to build your content separately, you must start the Addressables build using the __Build__ menu on the [Addressables Groups window](GroupsWindow.md) as described in [Making builds](xref:addressables-building-content). The next time you build the Player for your project, it uses the artifacts produced by the last Addressables content build run for the current platform. Refer to[Build scripting](xref:addressables-api-build-player-content) for information about automating your Addressables build process.

## Content build types

Your content build can produce two general categories of content:

* __Local content__: Content that's included directly in your player build. The Addressables system manages local content automatically as long as you use the default build path for your local content. If you change the local build path, you must copy the artifacts from the local build path to the project's `Assets/StreamingAssets` folder before making a Player build.
* __Remote content__: Content that's downloaded from a URL after your application is installed. It's your responsibility to upload remote content to a hosting server so your application can access it the designated URL specified by a [`RemoteLoadPath`](xref:addressables-profiles).

Refer to [Build artifacts](xref:addressables-build-artifacts) for more information about files produced by a content build.

## Groups and profiles

Your project's [group settings](xref:addressables-group-schemas) determine which category a group belongs to. The active [Profile](xref:addressables-profiles) determines the specific paths and URLs that the Addressables system uses to build and load the content. The [Addressable Asset settings](xref:addressables-asset-settings) also contain options that affect your content builds, such as whether to build remote content at all.

## Start a build

You can start builds from a script or from the __Groups__ window. Refer to [Build scripting](xref:addressables-api-build-player-content) for more information on how to extend building Addressable content.

The Addressables system includes the following build scripts:

* __Default Build Script__: Performs a full content build based on Group, Profile, and Addressables system settings.
* __Update a Previous Build__: Performs a differential content build to update a previously created build.
* __Play Mode scripts__: The Play Mode scripts are technically build scripts and control how the Editor accesses your content in Play Mode. Refer to [Play Mode Scripts](xref:addressables-groups-window) for more information.

The build scripts also provide a function to clear the cached files they create. You can run these functions from the __Build > Clean Build__ menu of the [Groups window](GroupsWindow.md).

