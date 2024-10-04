# Distribute remote content overview

Distributing content remotely can reduce initial app download size and install time. You can also update remotely distributed assets without republishing your app or game

When you assign a remote URL as the Load Path of a group, the Addressables system loads assets in the group from that URL. When you enable the Build Remote Catalog option, Addressables looks up the addresses of any remote assets in the remote catalog, allowing you to make changes to Addressable assets without forcing users to update and reinstall the entire game or application.

After [enabling remote distribution](remote-content-enable.md), you can build your content in two ways:

* A full [content build](builds-full-build.md) using the __New Build > Default Build Script__: builds all content bundles and catalogs. Always perform a full build before rebuilding your player when preparing to publish or update your full application.
* A [content update build](builds-update-build.md) using the __Update a Previous Build__ script: builds all content bundles and catalogs, but sets up the remote catalog so that installed applications only need to download the changed bundles. Run the [Check for Content Update Restrictions](content-update-build-create.md#check-for-content-update-restrictions-tool) tool to identify changes and prepare your groups before building an update.

After building a full build or an update, you must upload your remote catalog, catalog hash file, and remote bundles to your hosting service.

Refer to [Remote content profiles](remote-content-profiles.md) for tips on setting up Addressables Profiles to help you develop, test, and publish remote content.

## Custom URL evaluation

There are several scenarios where you might need to customize the path or URL of an Asset (an AssetBundle generally) at runtime. The most common example is creating signed URLs. Another is dynamic host determination.

See [ID transform function](xref:addressables-api-transform-internal-id) for more information.
