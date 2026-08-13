# Enable remote distribution

To enable remote distribution of your application's content, you must enable the remote catalog and set up the [groups](groups-intro.md) containing the assets you want to host remotely.

## Enable the remote catalog

Enable the following settings in the [__Addressable Asset Settings__](AddressableAssetSettings.md) Inspector (menu: __Window > Asset Management > Addressables > Settings__):

* __Build Remote Catalog__: Enabled
* __Build & Load Paths__: Set to remote

Unity builds the catalog and its accompanying hash file to the folder specified in the __Build Path__ setting. You must upload these files so that they can be accessed at the URL specified by the __Load Path__ setting. Unless you have a specific reason not to, use the __Remote__ location so that Unity builds and loads the catalog from the same paths as the remote AssetBundles.

## Set up a remote group

To set up a group so that the assets in it can be hosted remotely, set the [Build & Load Paths](AddressableAssetSettings.md) to the __Remote__ location.

If you plan to publish content updates between publishing full rebuilds of your application, set the [__Content Update Restriction__](ContentPackingAndLoadingSchema.md#content-update-restriction) value according to how often you expect to update content in a group:

* Enable **Prevent Updates** for groups that produce larger AssetBundles, especially if you're not going to change most of the assets in the group. If you do change assets in a group with this setting, the Addressables tools move the changed assets to a new group for the update. Only the new AssetBundles are downloaded by installed applications.
* Disable **Prevent Updates** for groups containing assets that you expect to change frequently. If you change assets in a group with this setting, the AssetBundles containing those assets are rebuilt as a whole and are downloaded again by installed applications. To reduce the amount of data that needs to be downloaded after an update, try to keep the AssetBundles produced by groups with this setting as small as possible.

Refer to [Content update builds](content-update-builds-overview.md) for more information about updating remote content.

The __Advanced Options__ section contains some options that affect remote hosting and downloads but aren't necessary to enable remote hosting. Refer to [Advanced Options](ContentPackingAndLoadingSchema.md#advanced-options) for more information.

## Additional resources

* [Define remote content profiles](remote-content-profiles.md)
* [Addressables Assets Settings reference](AddressableAssetSettings.md)
* [Group Inspector settings reference](ContentPackingAndLoadingSchema.md)
* [Content update builds](content-update-builds-overview.md)