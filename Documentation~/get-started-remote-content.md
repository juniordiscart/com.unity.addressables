# Remote content distribution

You can use Addressables to support remote distribution of content through a Content Delivery Network (CDN) or other hosting service. Unity provides the Unity Cloud Content Delivery (CCD) service for this purpose, but you can use any CDN or host you prefer.

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
