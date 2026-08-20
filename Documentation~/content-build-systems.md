# Choose a content build system

Addressables supports the following content build systems in Unity:

* [Content directories](xref:um-content-directories)
* [AssetBundles](xref:um-asset-bundles)

In versions of Addressables prior to 4.0, the AssetBundle system was the only content build system available. [Content directories](xref:um-content-directories) are intended to be a replacement to the AssetBundle system. If you upgrade to content directories, most workflows remain the same, such as creating groups, assigning labels, and using `AssetReference` to refer to assets at runtime.

The key differences between the content build systems are as follows:

|**Feature**|**Content directories**|**AssetBundles**|
|---|---|---|
|**Schema name**|[**Content Directory**](groups-create.md)|[**Content Packing & Loading**](groups-create.md)|
|**Loading and unloading**|Loads and unloads assets as needed along with their direct dependencies, and unloads assets as soon as their direct dependencies are released.|Loads assets as needed, and loads their dependent AssetBundles automatically. When unloading, assets are only unloaded once all [dependent AssetBundles](AssetDependencies.md) are released.|
|**Dependencies**|Tracks dependencies per asset. Unity automatically removes duplicated content in a build, and handles dependencies automatically.|Tracks dependencies per AssetBundle. Loading an asset requires loading its AssetBundle, and recursively loading all the dependent AssetBundles, even if the loaded asset itself doesn't reference them.|
|**Layout**|Granular file layout with hash-based names, optionally in a Unity archive.|Individual Unity archive files for each defined AssetBundle. Referenced content can be duplicated in multiple AssetBundles.|
|**Organization**|By default, all groups build to a single content directory, regardless of how you organize assets in the Groups window.|Groups you create determine which AssetBundle the assets are assigned to.|
|**Compression options**|LZ4 compression when ArchiveContentDirectories is enabled.|Options per-group: Uncompressed, LZ4, LZMA|
|**Remote content delivery**|Local content only.|Supports local and remote content.|

> [!TIP]
> For new projects that don't need to serve content remotely, use the content directory system. Choose AssetBundles if you need remote content, content updates, or are using an Editor version lower than Unity 6.6.

## Defining the content build system

The schemas [assigned to a group](groups-create.md) define the content build system and the settings used to build the assets in a group. The default schemas determine which content build system Addressables uses to create a content build of the assets in your project, as follows:

* **Content Directories**: Uses [content directories](xref:um-content-directories) to create content builds.
* **Content Packing & Loading**: Uses [AssetBundles](xref:um-asset-bundles) to create content builds.

You can also implement your own [`IResourceProvider`](xref:UnityEngine.ResourceManagement.ResourceProviders.IResourceProvider) class to support other ways to access assets.

You can use a mixture of both schemas in your project, and the default build script produces two content builds: one for AssetBundles, and one for content directories.

## Additional resources

* [Convert Addressables projects to content directories](convert-content-directories.md)
* [Add assets to groups](groups-create.md)