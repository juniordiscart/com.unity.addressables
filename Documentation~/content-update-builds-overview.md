# Content update builds overview

When you distribute content remotely, you can make content changes without needing to rebuild and republish your entire application. When the Addressables system initializes at runtime, it checks for an updated content catalog. If one exists, the system downloads the new catalog and, when it loads assets, downloads the newer versions of all your AssetBundles.

However, when you rebuild all your content with a new [content catalog](build-content-catalogs.md), installed players must also download all the remote AssetBundles again, whether the assets in them have changed or not. If you have a large amount of content, then downloading everything again can take a significant amount of time and might hurt player retention. To make this process more efficient, the Addressables package provides tools that you can run to identify changed assets and to produce a content update build. 

The following diagram illustrates how you can use the Addressables tools to produce smaller content updates that only require your players to download new or changed content:

![](images/addressables-update-builds.png)<br/>*The workflow for reducing the size of content updates*

## Content update build workflow

When you release your full application, you first build the Addressables content and then make a player build. The player build contains local AssetBundles and you upload the remote AssetBundles to the Content Delivery Network (CDN) or other hosting service.

The default build script that produces the Addressables content build always creates the `addressables_content_state.bin` file, which is required to efficiently publish content-only updates. You must save this file for each published full application release on every platform.

Between full application releases, which require your users to download and install a new player build, you can make changes to your Addressable assets in the project. Because AssetBundles don't include code, don't make code changes in the version of your project that you use to develop your asset changes. You can change both local and remote assets. 

## Content update tools

The Addressables package includes tools that you can use to reduce the size of updates to the content you distribute remotely. 

The content update tools include:

* [Check for Content Update Restrictions tool](content-update-build-create.md#check-for-content-update-restrictions-tool): Prepares your group organization for a content update build based on group settings
* [Update a Previous Build script](): A build script that performs the content update build 

You must save the `addressables_content_state.bin` file produced by the Default Build Script for each build that you intend to update in the future. This file is updated every time you run the build script. Make sure that you save the version produced for the content build that you publish. Refer to [Settings](content-update-build-settings.md) for relevant Addressable settings that handle the use of the previous content state file.

> [!IMPORTANT]
> On platforms that have their own patching systems or that don't support remote content distribution, do not use content update builds. Every build of your game should be a complete fresh content build. In this case you can discard or ignore the `addressables_content_state.bin` file that's generated after each build for the platform.

When you want publish a content update, run the __Check Content Update Restrictions__ tool manually, or make sure the check is run as part of the update build process. This check examines the `addressables_content_state.bin` file and moves changed assets to a new remote group, according to the settings of the group they are in.

## Build updated content

To build the updated AssetBundles, run the __Update a Previous Build__ script. This tool also uses the `addressables_content_state.bin` file. It rebuilds all of your content, but produces a modified catalog that accesses unchanged content from their original AssetBundles and changed content from the new AssetBundles.

The final step is to upload the updated content to your CDN. You can upload all the new AssetBundles produced or just those with changed names. Bundles that haven't changed use the same names as the originals and will overwrite them.

You can make additional content updates following the same process. Always use the `addressables_content_state.bin` file from your original release.

Refer to [Building content updates](content-update-build-create.md) for step-by-step instructions.

## When to perform a full rebuild

Addressables can only distribute content, not code. As such, a code change requires a fresh player build, and usually a fresh build of content. Although a new player build can sometimes reuse old, existing content from a CDN, you must analyze whether the type trees in the existing AssetBundles are compatible with your new code.

Note that Addressables itself is code, so updating Addressables or the Unity version requires that you create a new player build and fresh content builds.
