# Addressables - Multi-Catalog

The Addressables package by Unity provides a novel way of managing and packing assets for your build. It replaces the
Asset Bundle system, and in certain ways, also seeks to dispose of the Resources folder.

This variant, forked from the original Addressables-project, adds support for building your assets across several
catalogs, ideal for managing additional content or DLC for your game.

This package currently tracks version `1.22.2` of the vanilla Addressables packages. Checkout a `multi-catalog` tag if
you require a specific version.

## Notes before you begin

1. This multi-catalog version of Addressables **does not support catalog and group updates for subsequent content
   builds!** If
   your project requires content to be updated regularly and downloaded by your users without a new player build, then
   this package will not work for you. If you do require it, please consider implementing this feature in a fork from
   this repository.
2. This repository does not track every available version of the _vanilla_ Addressables package. It's only kept
   up-to-date sporadically.
3. For additional features found in this fork of Addressables, check the [Additional features](#additional-features)
   section.

## Upgrades notes

When upgrading from Addressables version `1.21.2` to `1.21.9` or later, please read the upgrade notes
for [migration help](#from-1212-to-1219-and-later) with breaking changes.

## Why does this exist?

### The problem

A frequently recurring question is:

> Can Addressables be used for DLC bundles/assets?

The answer to that question largely depends on how you define what DLC means for your project, but for clarity's sake,
let's define it this way:

> A package that contains assets and features to expand the base game. Such a package holds unique assets that _may_
> rely on assets already present in the base game.

So, does Addressables support DLC packages as defined above? Yes, it's supported, but in a very crude and inefficient
way.

The vanilla implementation of Addressables assumes you have a single content catalog file, and DLC could be added to
this catalog by labeling the DLC-specific content in your catalog. However, it will require you to constantly dance
around the DLC content addresses available in the catalog by implementing entitlement checks and preventing them from
being loaded. This is not only cumbersome and error-prone, but it also allows your players to easily mine the data and
assume
content might be available but is just soft-locked behind some simple checks. If you actually have the DLC content data
shipped with the base game all the time as well, it can cause even more frustration as it's taking up disk space for
something
that player will not even be able to use.

This sparked the idea of splitting off the actual DLC content to its separate content bundles along with a catalog that
works independently of the base catalog shipped with the base game binary. This is largely possible in the vanilla
implementation, but comes with a serious caveat: the Addressables system implicitly pulls in all of its dependencies in
its generated bundles, and the build cache assumes only a single catalog is built per project, invalidating the build
cache everytime a different catalog is built. This causes huge build times, and large output files. What's even worse is
duplicated assets in memory when running the game because the implicitly copied assets that are duplicates from those in
the base game's catalog are now seen as new and unique items when loaded. And finally, due to the fickle nature of the
build process
and its constant invalidation of the build cache, it can also trigger larger uploads and consequently larger downloads
for players when using services such as Valve's Steampipe.

This is unacceptable from a developer's as well as a player's perspective.

### The goal

So we're looking for a system that overcomes the frustrations outlined above. The system should allow the following:

* Split off DLC-specific content to its own content bundles and content catalog.
    * Easy development - no dancing around entitlement checks.
    * Only load content the player is entitled to access in the game.
    * Not to waste disk space when not needed on the player's system.
    * Not to duplicate assets in memory.
* Retain the build cache for faster builds, smaller uploads and consequently smaller updates.

### The solution

The solution comes in the form of setting up a DLC catalog object per DLC package you want to have available for your
game. These DLC catalog objects are added to the content build pipeline, which will analyse which addresses and content
belongs to their respective DLC package.

When everything has been built by the content build processor, the content is
separated based on which catalog it belongs to. DLC content is stripped away from the main catalog and copied to the
location defined by the DLC catalog configuration. After that's done, everything that remains is part of the main
catalog and ready to be shipped with the base game.

## Installation

This package is best installed using Unity's Package Manager. Fill in the URL found below in the package manager's input
field for git-tracked packages:

> <https://github.com/juniordiscart/com.unity.addressables.git>

### Updating a vanilla installation

When you've already set up Addressables in your project and adjusted the settings to fit your project's needs, it might
be cumbersome to set everything back. In that case, it might be better to update your existing settings with the new
objects rather than starting with a clean slate:

1. Remove the currently tracked Addressables package from the Unity Package manager and track this version instead as
   defined by the [Installation section](#installation). However, **don't delete** the `Assets/AddressableAssetsData`
   folder from your project!

2. In your project's `Assets/AddressableAssetsData/DataBuilders` folder, create a new 'multi-catalog' data builder:

   > Create → Addressables → Content Builders → Multi-Catalog Build Script

   ![Create multi-catalog build script](Documentation~/images/multi_catalogs/CreateDataBuilders.png)

3. Select your existing Addressable asset settings object, navigate to the `Build and Play Mode Scripts` property and
   add your newly created multi-catalog data builder to the list.

   ![Assign data builder to Addressable asset settings](Documentation~/images/multi_catalogs/AssignDataBuilders.png)

4. Optionally, if you have the Addressables build set to be triggered by the player build, or have a custom
   build-pipeline, you will have to set the `ActivePlayerDataBuilderIndex` property. This value must either be set
   through the debug-inspector view (it's not exposed by the custom inspector), or set it through script.

   ![Set data builder index](Documentation~/images/multi_catalogs/SetDataBuilderIndex.png)

### Setting up multiple catalogs

With the multi-catalog system installed, additional catalogs can now be created and included in build:

1. Create a new `ExternalCatalogSetup` object, one for each DLC package:

   > Create → Addressables → new External Catalog

2. In this object, fill in the following properties:
    * Catalog name: the name of the catalog file produced during build.
    * Build path: where this catalog and it's assets will be exported to after the build is done. This supports the same
      variable syntax as the build path in the Addressable Asset Settings.
    * Runtime load path: when the game is running, where should these assets be loaded from. This should depend on how
      you will deploy your DLC assets on the systems of your players. It also supports the same variable syntax.

   ![Set external catalog properties](Documentation~/images/multi_catalogs/SetCatalogSettings.png)

3. Assign the Addressable asset groups that belong to this package.

   **Note**: Addressable asset groups that are assigned to an external catalog, but still have their `BuildPath` and
   `LoadPath` values set to point to the main/default catalog's build and load path, will have them replaced with that
   of the external catalog during build time. So you don't have to perform specific actions with regards to the
   Addressable asset groups themselves, unless you wish to have them build to a specific other location other than next
   to the external catalog file.

4. Now, select the `BuildScriptPackedMultiCatalogMode` data builder object and assign your external catalog object(s).

   ![Assign external catalogs to data builder](Documentation~/images/multi_catalogs/AssignCatalogsToDataBuilder.png)

## Building

With everything set up and configured, it's time to build the project's contents!

In your Addressable Groups window, tick all 'Include in build' boxes of those groups that should be built. From the
build tab, there's a new `Default build script - Multi-Catalog` option. Select this one to start a content build with
the multi-catalog setup.

**Note**: built-in content is automatically included along with the player build as a post-build process. External
catalogs and their content are built and moved to their location when they are build. It's up to the user to configure
the build and load paths of these external catalogs so that they are properly placed next to the player build or into a
location that can be picked up by the content distribution system, e.g. Valve's SteamPipe for Steam.

## Loading the external catalogs

When you need to load in the assets put aside in these external packages, you can do so using:

> `Addressables.LoadContentCatalogAsync("path/to/dlc/catalogName.json");`

## Additional Features

Below you'll find additional features in this fork of Addressables that were considered missing in the vanilla flavour
of Addressables.

### Addressables Scene Merging

When merging scenes using `SceneManager.MergeScenes`, the source scene will be unloaded by Unity. If this source scene
is a scene loaded by Addressables, then its loading handle will be disposed off and releasing all assets associated with
the scene. This will cause all merged assets from the source scene that were handled by this single handle be unloaded
as well. This may cause several assets to not show up properly anymore, e.g. the well known pink missing material, no
meshes, audio clips, etc. will all be missing.

This is resolved by adding a `MergeScenes` method to `Addressables`, similar to `SceneManager.MergeScenes`, but will
keep the Addressable scene's loading handle alive until the destination scene is unloaded. This process can be repeated
multiple times, passing the loading handle until it's current bearer is unloaded.

## Migration Notes

### From 1.21.2 to 1.21.9 and later

If you're updating from a multi-catalog version of Addressables with version number `1.21.2` or earlier to version
`1.21.9` or later, then please read the notes below carefully to restore your project's build.

* `ExternalCatalogSetup` has had its `buildPath` and `runtimeLoadPath` values be changed in type. This will result in
  empty values on your external catalog objects upon upgrading, and content builds failing. The types have been updated
  to work with Addressables' `ProfileValueReference` framework. This allows to work with the built-in string evaluation
  functions and profile-defined variables in a more transparent way as it also properly previews the result in the
  inspector window.
