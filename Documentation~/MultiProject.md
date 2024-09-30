---
uid: addressables-multiple-projects
---

# Load content from multiple projects

If you're working with multiple projects, such as a large project broken up across multiple Unity projects, you can use [`Addressables.LoadContentCatalogAsync`](LoadContentCatalogAsync.md) to link together code and content across the various projects.

## Set up multiple projects

To create a multi-project setup make sure of the following:

* Each project uses the same version of the Unity Editor
* Each project uses the same version of the Addressables package

Projects can contain whatever assets and code you need for your given situation. One of your projects must be your main or source project. This is the project that you'll build and deploy your game binaries from. Typically, this source project is comprised of code and little to no content. The main piece of content in the primary project is a bootstrap scene at minimum. You might want to include any scenes that need to be local for performance purposes before any AssetBundles have had a chance to be downloaded and cached.

Secondary projects are the opposit and contain content and little to no code. These projects need to have all remote Addressable Groups and Build Remote Catalog enabled. Any local data built into these projects can't be loaded in your source project's application. Non-critical scenes can live in these projects and be downloaded by the primary project when requested.

## Work with multiple project

Once you have your projects setup, the workflow generally is as follows:

1. Build remote content for all secondary projects
2. Build Addressables content for source project
3. Start the source project's Play mode or build the source project's binaries
4. In source project, use [`Addressables.LoadContentCatalogAsync`](LoadContentCatalogAsync.md) to load the remote catalogs of your other various projects
5. Proceed with game runtime as normal. Now that the catalogs are loaded, Addressables can load assets from any of these locations.

It might be worth having a minimal amount of content built locally in the source project. Each project is unique, and has unique needs, but having a small set of content needed to run your game in the event of internet connection issues or other various problems is advisable. 

## Handle shaders

Addressables builds a Unity built in shader bundle for each set of Addressables player data that gets built. This means that when multiple AssetBundles are loaded that were built in secondary projects, there might be multiple built in shader bundles loaded at the same time.

Depending on your specific situation, you might need to use the Shader Bundle Naming Prefix on the `AddressableAssetSettings` object. Each built in shader bundle needs to be named different from others built in your other projects. If they're not named differently you'll get `The AssetBundle [bundle] can't be loaded because another AssetBundle with the same files is already loaded.` errors.