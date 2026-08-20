# Introduction to distributing remote content

>[!IMPORTANT]
>The following workflow is only applicable if you're using [AssetBundles as the content build system](content-build-systems.md) for your project.

Distributing content remotely can reduce the initial download size and install time of your application. You can also update remotely distributed assets without republishing your application.

When you assign a remote URL as the [load path of a group](profiles-build-load-paths.md), the Addressables system loads assets in the group from that URL. When you enable the [Build Remote Catalog](AddressableAssetSettings.md#catalog) option, Addressables looks up the addresses of any remote assets in the remote catalog, allowing you to make changes to Addressable assets without forcing users to update and reinstall the entire application.

After [enabling remote distribution](remote-content-enable.md), you can build your content in the following ways:

* A [content-only build](builds-full-build.md): Builds all content AssetBundles and catalogs. Always perform a full build before rebuilding your Player when preparing to publish or update your application.
* A [content update build](builds-update-build.md): Builds all content AssetBundles and catalogs, but sets up the remote catalog so that installed applications only need to download the changed AssetBundles. Run the [Check for Content Update Restrictions](builds-update-build.md) tool to identify changes and prepare your groups before building an update.

After building a full build or an update, you must upload your remote catalog, catalog hash file, and remote AssetBundles to your hosting service.

Refer to [Remote content profiles](remote-content-profiles.md) for tips on setting up Addressables Profiles to help you develop, test, and publish remote content.

## Custom URL evaluation

There are several scenarios where you might need to customize the path or URL of an asset (an AssetBundle generally) at runtime. The most common example is creating signed URLs. Another is dynamic host determination. For more information, refer to [Change Addressable load URLs](TransformInternalId.md) for more information.

## Additional resources

* [Enable remote content](remote-content-enable.md)
* [Define remote content profiles](remote-content-profiles.md)
* [Remote content AssetBundle caching](remote-content-assetbundle-cache.md)