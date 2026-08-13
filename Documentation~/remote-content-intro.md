# Introduction to distributing remote content

Distributing content remotely can reduce the initial download size and install time of your application. You can also update remotely distributed assets without republishing your application.

When you assign a remote URL as the [load path of a group](profiles-build-load-paths.md), the Addressables system loads assets in the group from that URL. When you enable the [Build Remote Catalog](AddressableAssetSettings.md#catalog) option, Addressables looks up the addresses of any remote assets in the remote catalog, allowing you to make changes to Addressable assets without forcing users to update and reinstall the entire application.

After [enabling remote distribution](remote-content-enable.md), you can build your content in two ways:

* A [content-only build](builds-full-build.md): Builds all content AssetBundles and catalogs. Always perform a full build before rebuilding your Player when preparing to publish or update your application.
* A [content update build](builds-update-build.md): Builds all content AssetBundles and catalogs, but sets up the remote catalog so that installed applications only need to download the changed AssetBundles. Run the [Check for Content Update Restrictions](builds-update-build.md) tool to identify changes and prepare your groups before building an update.

After building a full build or an update, you must upload your remote catalog, catalog hash file, and remote AssetBundles to your hosting service.

Refer to [Remote content profiles](remote-content-profiles.md) for tips on setting up Addressables Profiles to help you develop, test, and publish remote content.

## Custom URL evaluation

There are several scenarios where you might need to customize the path or URL of an asset (an AssetBundle generally) at runtime. The most common example is creating signed URLs. Another is dynamic host determination. For more information, refer to [Change Addressable load URLs](TransformInternalId.md) for more information.


---

# Remote content distribution

You can use Addressables to support remote distribution of content through a Content Delivery Network (CDN) or other hosting service. Unity provides the [Unity Cloud Content Delivery (CCD) service](AddressablesCCD.md) for this purpose, but you can use any CDN or host you prefer.

Before building content for remote distribution, you must:

* Enable the __Build Remote Catalog__ option in your AddressableAssetSettings (access using menu: __Windows > Asset Management > Addressables > Settings__).
* Configure the __RemoteLoadPath__ in the [Profile](xref:addressables-profiles) you use to publish content to reflect the remote URL at which you plan to access the content.
* For each Addressables group containing assets you want to deliver remotely, set the __Build Path__ to __RemoteBuildPath__ and the __Load Path__ to __RemoteLoadPath__.
* Set desired __Platform Target__ on the Unity __Build Settings__ window.

After you make a content build (using the Addressables __Groups__ window) and a player build (using the __Build Settings__ window), you must upload the files created in the folder designated by your profile's __RemoteBuildPath__ to your hosting service. The files to upload include:

* AssetBundles (name.bundle)
* Catalog (catalog_timestamp.json)
* Hash (catalog_timestamp.hash)

Refer to [Distributing remote content](RemoteContentDistribution.md) for more information.

## Additional resources

* [Enable remote content](remote-content-enable.md)
* [Define remote content profiles](remote-content-profiles.md)
* [Remote content AssetBundle caching](remote-content-assetbundle-cache.md)