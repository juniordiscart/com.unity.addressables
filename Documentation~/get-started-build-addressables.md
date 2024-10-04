# Build Addressable assets

The Addressables content build step converts the assets in your Addressables groups into AssetBundles based on the [group settings](xref:addressables-group-schemas) and the current platform set in the Unity Editor.

You can configure the Addressables system to build your Addressables content as part of every Player build or you can build your content separately before making a Player build. Refer to [Building Addressables content with Player builds](xref:addressables-builds) for more information about configuring these options.

If you configure Unity to build your content as part of the Player build, use the normal __Build__ or __Build and Run__ buttons in the Editor [Build Settings](xref:PublishingBuilds) window to start a build. Unity builds your Addressables content as a pre-build step before it builds the Player.

In earlier versions of Unity, or if you configure Unity to build your content separately, you must make an Addressables build using the __Build__ menu on the __Addressables Groups__ window. The next time you build the Player for your project, it uses the artifacts produced by the last Addressables content build run for the current platform.

To start a content build from the Addressables Groups window:

![](images/get-started-build.png)

1. Open the Addressables Groups window (menu: __Windows > Asset Management > Addressables > Groups__).
2. Choose an option from the __Build__ menu:
    * __New Build__: perform a build with a specific build script. Use the __Default Build Script__ if you don't have your own custom one.
    * __Update a Previous Build__: builds an update based on an existing build. To update a previous build, the Addressables system needs the `addressables_content_state.bin` file produced by the earlier build. You can find this file in the `Assets/AddressableAssetsData/Platform` folder of your Unity Project. Refer to [Content Updates](xref:addressables-content-update-builds) for more information about updating content. 
    * __Clean Build__: deletes cached build files. 

By default, the build creates files in the locations defined in your [Profile](xref:addressables-profiles) settings for the __LocalBuildPath__ and __RemoteBuildPath__ variables. The files that Unity uses for your player builds include AssetBundles (.bundle), catalog JSON and hash files, and settings files.

> [!WARNING]
> Don't change the local build or load paths from their default values. If you do, you must copy the local build artifacts from your custom build location to the project's [StreamingAssets](xref:StreamingAssets) folder before making a Player build. Altering these paths also precludes building your Addressables as part of the Player build. 


If you have groups that you build to the __RemoteBuildPath__, you must upload those AssetBundles, catalog, and hash files to your hosting server. If your Project doesn't use remote content, set all groups to use the local build and load paths.

A content build also creates the following files that Addressables doesn't use directly in a player build:

* `addressables_content_state.bin`: used to make a content update build. If you support dynamic content updates, you must save this file after each content release. Otherwise, you can ignore this file.
* `AddressablesBuildTEP.json`: logs build performance data. For more information, refer to [Build Profiling](xref:addressables-build-profile-log).

Refer to [Building Addressable content](xref:addressables-builds) for more information about how to set up and perform a content build.

## Start a full content build

To make a full content build:

1. Set the desired __Platform Target__ on the __Build Settings__ window.
2. Open the __Addressables Groups__ window (menu: __Asset Management > Addressables > Groups__).
3. Choose the __New Build > Default Build Script__ command from the Build menu of the __Groups__ window.

The build process starts.

After the build is complete, you can perform a player build and upload any remote files from your __RemoteBuildPath__ to your hosting server.

> [!Important]
> If you plan to publish remote content updates without rebuilding your application, you must preserve the `addressables_content_state.bin` file for each published build. Without this file, you can only create a full content build and player build, not an update. Refer to [Content update builds](xref:addressables-content-update-builds) for more information.
