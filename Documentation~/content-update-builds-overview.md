# Introduction to update builds

When you distribute content remotely, you can make content changes without needing to rebuild and republish your entire application. When the Addressables system initializes at runtime, it checks for an updated content catalog and downloads it. When Addressables loads assets it then downloads any AssetBundles with newer versions.

If you rebuild all the content in your project with a new [content catalog](build-content-catalogs.md), installed Players must download all the remote AssetBundles again, whether the assets in them have changed or not. If your application contains a large amount of content, then downloading everything again can take a significant amount of time. To make this process more efficient, the Addressables package provides tools that you can run to identify changed assets and to produce a content update build.

## Content update build workflow

The following diagram illustrates how you can use the Addressables tools to produce smaller content updates that only require a Player build to download new or changed content:

![The workflow of developing, building, and releasing an application. Illustrates how content-only updates can be published to the Content Delivery Network for consumption by Player installs without needing to rebuild the Player.](images/addressables-update-builds.png)<br/>*The workflow for reducing the size of content updates.*

When you release your full application, you first [build the Addressables content](builds-full-build.md) and then make a Player build. The Player build contains local AssetBundles and you upload remote AssetBundles to a Content Delivery Network (CDN) or other hosting service.

The default build script that produces the Addressables content build always creates the `addressables_content_state.bin` file, which is required to publish content-only updates. You must save this file for each published full application release on every platform.

Between full application releases which require your users to download and install a new Player build, you can make changes to Addressable assets in the project. However, you should only modify assets, not code, because AssetBundles contain only asset data and can't include code changes. You can change both local and remote assets.

## Content update tools

The Addressables package includes tools that you can use to reduce the size of updates to the content you distribute remotely.

The content update tools include:

* [Check for Content Update Restrictions tool](builds-update-build.md): Prepares the Addressables groups for a content update build based on group settings.
* [Update a Previous Build](GroupsWindow.md#build): A build script that performs the content update build from the Addressables Groups window.

You must save the `addressables_content_state.bin` file produced by the default build script for each build that you intend to update in the future. Unity updates this file every time you run the build script. Make sure that you save the version produced for the content build that you publish. Refer to [Content update build settings reference](content-update-build-settings.md) for relevant Addressable settings that handle the use of the previous content state file.

> [!IMPORTANT]
> On platforms that have their own patching systems or that don't support remote content distribution, don't use content update builds. Every build of your application must be a new content build. In this case you can discard or ignore the `addressables_content_state.bin` file that's generated after each build for the platform.

When you want publish a content update, run the __Check Content Update Restrictions__ tool manually, or make sure the check is run as part of the update build process. This check examines the `addressables_content_state.bin` file and moves changed assets to a new remote group, according to the settings of the group they're in.

## Build updated content

To build the updated AssetBundles, run the __Update a Previous Build__ script. This tool also uses the `addressables_content_state.bin` file. It rebuilds all the content, but produces a modified catalog that accesses unchanged content from their original AssetBundles and changed content from the new AssetBundles.

You can make additional content updates following the same process. Always use the `addressables_content_state.bin` file from your original release.

For more information, refer to [Create an update build](builds-update-build.md).

The final step is to upload the updated content to your CDN. You can upload all the new AssetBundles produced or just those with changed names. Bundles that haven't changed use the same names as the originals and will overwrite them.

### Output of an update build

The build generates a content catalog, a hash file, and AssetBundles.

The generated content catalog has the same name as the catalog in the original application build, overwriting the old catalog and hash file. The application loads the hash file at runtime to decide if a new catalog is available. Unity loads unmodified assets from existing AssetBundles that were shipped with the application or that the application has already downloaded.

Unity uses the content version string and location information from the [`addressables_content_state.bin`](build-artifacts-included.md) file to create the AssetBundles. Unity uses the same file name as those in the build for AssetBundles that don't contain updated content. If an AssetBundle contains updated content, Unity generates a new AssetBundle that contains the updated content, with a new file name so that it can exist with the original on your content hosting service. You only need to copy AssetBundles with new file to the location that hosts your content, though you can safely upload them all.

Unity also builds AssetBundles for content that can't change, such as any local AssetBundles, but you don't need to upload them to the content hosting location, because no Addressables asset entries reference them.

Don't change the build scripts between building a new Player and making content updates, such as Player code or Addressables. This might cause unpredictable behavior in your application.


## Additional resources

* [Content update dependencies](content-update-examples.md)
* [Create an update build](builds-update-build.md)
* [Create a script to check for content updates](content-update-builds-check.md)