# Create a full build

To create a full build:

1. Configure your project's [group settings](GroupSchemas.md).
1. If you're distributing content remotely, configure the [Profile](profiles-introduction.md) and [Addressables system settings](AddressableAssetSettings.md) to enable remote content distribution.
3. Select the correct Profile.
4. Launch the build from the [Groups window](GroupsWindow.md).

> [!TIP]
> If you encounter build or runtime loading issues during development, run the __Clean > All__ command from the __Build__ menu before you rebuild your content.

## Set up build and load paths

A [Profile](profiles-introduction.md) defines separate variables for the build and load paths of local versus remote content. You can create multiple profiles to use different paths for different kinds of builds. For example, you might have a profile to use while you develop your Project in the Unity Editor and another for when you publish your final content builds.

For most projects, you only need multiple profiles when you support remote content distribution. You don't typically need to change the local paths at different stages of your development process. Most projects should build local content to the default local build path and load it from the default local load path (which resolves to the StreamingAssets folder).

### Windows file path limit

Windows has a file path limit of 260 characters. If the build path of your content ends up creating a path that meets or exceeds the limit on Windows, the build fails.

You might run into a file path limit issue if your project is located in a directory that is close to the character limit. The Scriptable Build Pipeline creates AssetBundles in a temporary directory during the build. This temporary path is a sub-directory of your project and can end up generating a `string` that goes over the Windows limit.
If the Addressables content build fails with a `Could not find a part of the path` error, and you're on Windows, this might be the cause.

### Default local paths

The local build path defaults to the path provided by `Addressables.BuildPath`, which is in the Library folder of your Unity project. Addressables appends a folder to the local build path based on your current platform build target setting. When you build for multiple platforms, the build places the artifacts for each platform in a different subfolder.

Likewise, the local load path defaults to that provided by `Addressables.RuntimePath`, which resolves to the StreamingAssets folder. Again Addressables adds the platform build target to the path.

When you build your local bundles to the default build path, then the build code temporarily copies the artifacts from the build path to the StreamingAssets folder when you build your player, and removes them after the build.

> [!WARNING]
> If you build to, or load from custom local paths, then you must copy your build artifacts to the correct place in your project before making a player build and to make sure your application can access those artifacts at runtime.

### Default remote paths

Addressables sets the default remote build path to an arbitrarily chosen folder name, `ServerData`, which is created under your Project folder. The build adds the current platform target to the path as a subfolder to separate the unique artifacts for different platforms.

The default remote load path is `http://localhost/` appended with the current profile BuildTarget variable. You must change this path to the base URL at which you plan to load your Addressable assets.

Use different [profiles](profiles-introduction.md) to set up the remote load path as appropriate for the type of development, testing, or publishing you are doing. For example, you could have a profile that loads assets from a localhost server for general development builds, a profile that loads assets from a staging environment for QA builds, and one that loads assets from your Content Delivery Network (CDN) for release builds.

> [!NOTE]
> When running your game in the Editor, you can use the __Use Asset Database__ Play Mode Script to bypass loading assets through the remote or local load paths. This can be convenient, especially when you don't have a localhost server set up. However, it can hide group configuration and asset assignment mistakes.

## Set up remote content builds

To set up a remote content build:

1. Navigate to your AdressablesSystemSetting asset (menu: __Window > Asset Management > Addressables > Settings__).
2. Under __Catalog__, enable the __Build Remote Catalog__ option.
The __BuildPath__ and __LoadPath__ settings for the catalog must be the same as those you use for your remote groups. In most cases, use the RemoteBuildPath and RemoteLoadPath profile variables.
3. For each group that you want to build as remote content, set the __BuildPath__ and __LoadPath__ to the RemoteBuildPath and RemoteLoadPath profile variables (or a custom value if desired).
4. Open the [Profiles window]  (menu: __Window > Asset Management > Addressables > Profiles__).
5. Set the RemoteLoadPath variable to the URL where you plan to host your remote content.
If you require different URLs for different types of builds, create a new Profile for each build type. See [Profiles] and [Hosting] for more information.

Refer to [Remote content distribution](RemoteContentDistribution.md) for additional information.

## Perform the build

After you have your group and Addressables system settings configured, you can run a content build:

1. Open the [Groups window](GroupsWindow.md) (menu: __Windows > Asset Management > Addressables > Groups__).
2. Select the desired profile from the __Profile__ menu on the toolbar.
3. Select the __Default Build Script__ from the __Build > New Build__ menu. (If you have created your own build scripts they will also be available from this menu.)

The Default Build Script creates one or more AssetBundles for each group and saves them to either the local or the remote build path.

