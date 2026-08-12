---
uid: addressables-build-artifacts
---
# Build artifacts

A [content build](builds-full-build.md) creates files in several locations and Unity doesn't include every file in a built Player. Typically, Unity includes files associated with local content in the built Player and excludes files associated with remote content.

Most of the files associated with local content are in the `Library/com.unity.addressables` folder. This is a special subfolder in the `Library` folder which Unity uses to store Addressables files. For more information about the `Library` folder, refer to [Introduction to importing assets](xref:um-importing-assets).

## Additional resources

* [Player artifacts](build-artifacts-included.md)
* [Content catalogs](build-content-catalogs.md)
* [Shared AssetBundles](build-shared-assetbundles.md)