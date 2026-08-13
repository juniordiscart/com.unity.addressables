# Default path values

The default values for [the build and load paths](ProfileVariables.md) are:

* Local build path: `[UnityEditor.EditorUserBuildSettings.activeBuildTarget]`
* Local load path: `[UnityEngine.AddressableAssets.Addressables.BuildPath]/[BuildTarget]`
* Remote build path: `ServerData/[BuildTarget]`
* Remote load path: <undefined>

Usually, you shouldn't need to change the local path values. The Unity build system expects the AssetBundles and other files to exist in the default location. If you change the local paths, you must copy the files from the build path to the load path before making a Player build. The load path must always be in the Unity `StreamingAssets` folder.

If you distribute content remotely, you must change the remote load path to reflect the URL which you host the remote content. You can set the remote build path to any convenient location and the build system doesn't rely on the default value.

## Default local paths

The local build path defaults to the path provided by `Addressables.BuildPath`, which is in the Library folder of your Unity project. Addressables appends a folder to the local build path based on your current platform build target setting. When you build for multiple platforms, the build places the artifacts for each platform in a different subfolder.

Likewise, the local load path defaults to that provided by `Addressables.RuntimePath`, which resolves to the StreamingAssets folder. Again Addressables adds the platform build target to the path.

When you build your local bundles to the default build path, then the build code temporarily copies the artifacts from the build path to the StreamingAssets folder when you build your player, and removes them after the build.

> [!WARNING]
> If you build to, or load from custom local paths, then you must copy your build artifacts to the correct place in your project before making a player build and to make sure your application can access those artifacts at runtime.

## Default remote paths

Addressables sets the default remote build path to an arbitrarily chosen folder name, `ServerData`, which is created under your Project folder. The build adds the current platform target to the path as a subfolder to separate the unique artifacts for different platforms.

The default remote load path is `http://localhost/` appended with the current profile BuildTarget variable. You must change this path to the base URL at which you plan to load your Addressable assets.

Use different [profiles](profiles-introduction.md) to set up the remote load path as appropriate for the type of development, testing, or publishing you're doing. For example, you can create a profile that loads assets from a localhost server for general development builds, a profile that loads assets from a staging environment for QA builds, and one that loads assets from your Content Delivery Network (CDN) for release builds.

> [!NOTE]
> When running your game in the Editor, you can use the __Use Asset Database__ Play Mode Script to bypass loading assets through the remote or local load paths. This can be convenient, especially when you don't have a localhost server set up. However, it can hide group configuration and asset assignment mistakes.

## Windows file path limit

Windows has a file path limit of 260 characters. If the build path of your content ends up creating a path that meets or exceeds the limit on Windows, the build fails.

You might run into a file path limit issue if your project is located in a directory that is close to the character limit. The Scriptable Build Pipeline creates AssetBundles in a temporary directory during the build. This temporary path is a sub-directory of your project and can end up generating a `string` that goes over the Windows limit.

If the Addressables content build fails with a `Could not find a part of the path` error, and you're on Windows, this might be the cause.

## Additional resources

* [Create a profile](profiles-create.md)
* [Add variables to a profile](ProfileVariables.md)
* [Build Addressable assets](builds-full-build.md)