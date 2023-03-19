# Addressables - Multi-Catalog

The Addressables package by Unity provides a novel way of managing and packing assets for your build. It replaces the Asset Bundle system, and in certain ways, also seeks to dispose of the Resources folder.

This variant forked from the original Addressables-project adds support for building your assets across several catalogs in one go and provides several other benefits, e.g. reduced build times and build size, as well as keeping the buildcache intact.

This package currently tracks version `1.21.9` of the vanilla Addressables packages. Checkout a `multi-catalog` tag if you require a specific version.

**Note**: this repository does not track every available version of the _vanilla_ Addressables package. It's only kept up-to-date sporadically.

For additional features found in this fork of Addressables, check the [Additional features](#additional-features) section.

## The problem

A frequently recurring question is:

> Can Addressables be used for DLC bundles/assets?

The answer to that question largely depends on how you define what DLC means for your project, but for clarity's sake, let's define it this way:

> A package that contains assets and features to expand the base game. Such a package holds unique assets that _may_ rely on assets already present in the base game.

So, does Addressables support DLC packages as defined above? Yes, it's supported, but in a very crude and inefficient way.

In the vanilla implementation of Addressables, only one content catalog file is built at a time, and only the assets as defined to be included in the current build will be packed and saved. Any implicit dependency of an asset will get included in that build too, however. This creates several major problems:

* Each content build (one for the base game, and one for each DLC package) will include each and every asset it relies on. This is expected for the base game, but not for the DLC package. This essentially creates unnecessary large DLC packages, and in the end, your running game will include the same assets multiple times on the player's system, and perhaps even in memory at runtime.
* No build caching can be used, since each build done for a DLC package is considered a whole new build for the Addressables system, and will invalidate any prior caching done. This significantly increases build times.
* Build caching and upload systems as those used by Steam for example, can't fully differentiate between the changes properly of subsequent builds. This results in longer and larger uploads, and consequently, in bigger updates for players to download.

## The solution

The solution comes in the form of performing the build process of the base game and all DLC packages in one step. In essence, what this implementation does is, have the Addressables build-pipeline perform one large build of all assets tracked by the base game and each of the included DLC packages.

Afterwards, the contents for each external catalog are extracted to their proper location and their content catalog file is created based on the content they require.

## Installation

This package is best installed using Unity's Package Manager. Fill in the URL found below in the package manager's input field for git-tracked packages:

> <https://github.com/juniordiscart/com.unity.addressables.git>

### Updating a vanilla installation

When you've already set up Addressables in your project and adjusted the settings to fit your project's needs, it might be cumbersome to set everything back. In that case, it might be better to update your existing settings with the new objects rather than starting with a clean slate:

1. Remove the currently tracked Addressables package from the Unity Package manager and track this version instead as defined by the [Installation section](#installation). However, **don't delete** the `Assets/AddressableAssetsData` folder from your project!

2. In your project's `Assets/AddressableAssetsData/DataBuilders` folder, create a new 'multi-catalog' data builder:

   > Create → Addressables → Content Builders → Multi-Catalog Build Script

   ![Create multi-catalog build script](Documentation~/images/multi_catalogs/CreateDataBuilders.png)

3. Select your existing Addressable asset settings object, navigate to the `Build and Play Mode Scripts` property and add your newly created multi-catalog data builder to the list.

   ![Assign data builder to Addressable asset settings](Documentation~/images/multi_catalogs/AssignDataBuilders.png)

4. Optionally, if you have the Addressables build set to be triggered by the player build, or have a custom build-pipeline, you will have to set the `ActivePlayerDataBuilderIndex` property. This value must either be set through the debug-inspector view (it's not exposed by the custom inspector), or set it through script.

   ![Set data builder index](Documentation~/images/multi_catalogs/SetDataBuilderIndex.png)

### Setting up multiple catalogs

With the multi-catalog system installed, additional catalogs can now be created and included in build:

1. Create a new `ExternalCatalogSetup` object, one for each DLC package:

   > Create → Addressables → new External Catalog

2. In this object, fill in the following properties:
   * Catalog name: the name of the catalog file produced during build.
   * Build path: where this catalog and it's assets will be exported to after the build is done. This supports the same variable syntax as the build path in the Addressable Asset Settings.
   * Runtime load path: when the game is running, where should these assets be loaded from. This should depend on how you will deploy your DLC assets on the systems of your players. It also supports the same variable syntax.

   ![Set external catalog properties](Documentation~/images/multi_catalogs/SetCatalogSettings.png)

3. Assign the Addressable asset groups that belong to this package.

   **Note**: Addressable asset groups that are assigned to an external catalog, but still have their `BuildPath` and `LoadPath` values set to point to the main/default catalog's build and load path, will have them replaced with that of the external catalog during build time. So you don't have to perform specific actions with regards to the Addressable asset groups themselves, unless you wish to have them build to a specific other location other than next to the external catalog file.

4. Now, select the `BuildScriptPackedMultiCatalogMode` data builder object and assign your external catalog object(s).

   ![Assign external catalogs to data builder](Documentation~/images/multi_catalogs/AssignCatalogsToDataBuilder.png)

## Building

With everything set up and configured, it's time to build the project's contents!

In your Addressable Groups window, tick all 'Include in build' boxes of those groups that should be built. From the build tab, there's a new `Default build script - Multi-Catalog` option. Select this one to start a content build with the multi-catalog setup.

**Note**: built-in content is automatically included along with the player build as a post-build process. External catalogs and their content are built and moved to their location when they are build. It's up to the user to configure the build and load paths of these external catalogs so that they are properly placed next to the player build or into a location that can be picked up by the content distribution system, e.g. Valve's SteamPipe for Steam.

## Loading the external catalogs

When you need to load in the assets put aside in these external packages, you can do so using:

> `Addressables.LoadContentCatalogAsync("path/to/dlc/catalogName.json");`

## Additional Features

Below you'll find additional features in this fork of Addressables that were considered missing in the vanilla flavour of Addressables.

### Addressables Scene Merging

When merging scenes using `SceneManager.MergeScenes`, the source scene will be unloaded by Unity. If this source scene is a scene loaded by Addressables, then its loading handle will be disposed off and releasing all assets associated with the scene. This will cause all merged assets from the source scene that were handled by this single handle be unloaded as well. This may cause several assets to not show up properly anymore, e.g. the well known pink missing material, no meshes, audio clips, etc. will all be missing.

This is resolved by adding a `MergeScenes` method to `Addressables`, similar to `SceneManager.MergeScenes`, but will keep the Addressable scene's loading handle alive until the destination scene is unloaded. This process can be repeated multiple times, passing the loading handle until it's current bearer is unloaded.
