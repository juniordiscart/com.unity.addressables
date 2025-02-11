---
uid: addressables-home
---

# Addressables package

The Addressables package provides tools and scripts to organize and package content for your application, and an API to load and release assets at runtime.

When you make an asset Addressable, you can use that asset's address to load it from anywhere. Whether that asset resides in the local application or on a content delivery network, the Addressable system locates and returns it.

Adopt the Addressables system to help improve your project in the following areas:

* __Flexibility__: Addressables give you the flexibility to adjust where you host your assets. You can install assets with your application or download them on demand. You can change where you access a specific asset at any stage in your project without rewriting any game code.
* __Dependency management__: The system automatically loads all dependencies of any assets you load, so that all meshes, shaders, animations, and other assets load before the system returns the content to you.
* __Memory management__: The system unloads and loads assets, and counts references automatically. The Addressables package also provides a Profiler to help you identify potential memory problems.
* __Content packing__: Because the system maps and understands complex dependency chains, it package AssetBundles efficiently, even when you move or rename assets. You can prepare assets for both local and remote deployment, to support downloadable content and reduced application size.

> [!NOTE]
> The Addressables system builds on Unity's [AssetBundles](xref:AssetBundlesIntro). If you want to use AssetBundles in your projects without writing your own detailed management code, you should use Addressables.

