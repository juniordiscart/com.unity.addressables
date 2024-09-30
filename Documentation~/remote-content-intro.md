# Distribute remote content overview

You can use Addressables to support remote distribution of content through a Content Delivery Network (CDN) or other hosting service. Unity provides the Unity Cloud Content Delivery (CCD) service for this purpose, but you can use any CDN or host you prefer.

Distributing content remotely can reduce initial app download size and install time. You can also update remotely distributed assets without republishing your application.

When you assign a remote URL as the Load Path of a group, the Addressables system loads assets in the group from that URL. When you enable the **Build Remote Catalog** property, the Addressables system looks up the addresses of any remote assets in the remote catalog. This enables you to make changes to Addressable assets without forcing users to reinstall the updated version of your application.

To build content for remote distribution, you must:

* Go to __Windows > Asset Management > Addressables > Settings__ and enable the __Build Remote Catalog__ property.
* In the [Profile](xref:addressables-profiles) you use to publish content, configure the __RemoteLoadPath__ to reflect the remote URL at which you plan to access the content.
* For each Addressables group containing assets you want to deliver remotely, set the __Build Path__ property to __RemoteBuildPath__ and the __Load Path__ property to __RemoteLoadPath__.
* Set the desired __Platform Target__ property in the __Build Settings__ window.

After you make a content build through the Addressables __Groups__ window and a player build through the __Build Settings__ window, you must upload the files created in the folder designated by your profile's __RemoteBuildPath__ to your hosting service. The files to upload include:

* Any AssetBundles (name.bundle)
* The catalog file (catalog_timestamp.json)
* The catalog hash (catalog_timestamp.hash)

After [enabling remote distribution](remote-content-enable.md), you can build your content in two ways:

* A full [content build](builds-full-build.md) using the **New Build > Default Build Script** builds all content bundles and catalogs. Always perform a full build before rebuilding your player when preparing to publish or update your full application.
* A [content update build](builds-update-build.md) using the **Update a Previous Build** script builds all content bundles and catalogs, but sets up the remote catalog so that installed applications only need to download the changed bundles. Run the [Check for Content Update Restrictions](content-update-build-create.md#check-for-content-update-restrictions-tool) tool to identify changes and prepare your groups before building an update.

After building a full build or an update, you must upload your remote catalog, catalog hash file, and remote bundles to your hosting service.

Refer to [Remote content profiles](remote-content-profiles.md) for tips on setting up Addressables Profiles to help you develop, test, and publish remote content.

## Custom URL evaluation

There are several scenarios where you might need to customize the path or URL of an Asset (often an AssetBundle) at runtime. The most common example is creating signed URLs. Another is dynamic host determination.

Refer to [ID transform function](xref:addressables-api-transform-internal-id) for more information.
