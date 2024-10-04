# Enable remote distribution

To enable remote distribution of your content, you must enable the remote catalog and set up the [groups](Groups.md) containing the assets you want to host remotely.  

## Enabling the remote catalog

Enable the remote catalog in the __Addressable Asset Settings__ Inspector (menu: __Window > Asset Management > Addressables > Settings__).

* __Build Remote Catalog__: Enabled
* __Build & Load Paths__: Set to remote

The catalog and its accompanying hash file are built to the folder specified by the __Build Path__ setting. You must upload these files so that they can be accessed at the URL specified by your __Load Path__ setting. Unless you have a specific reason not to, use the __Remote__ location so that the catalog is built to and loaded from the same paths as your remote bundles.

## Set up a remote group

To set up a group so that the assets in it can be hosted remotely, set the [Build & Load Paths](AddressableAssetSettings.md) to the __Remote__ location.

If you plan to publish content updates between publishing full rebuilds of your application, set the __Update Restriction__ value according to how often you expect to update content in a group.

Choose __Cannot Change Post Release__ for groups that produce larger bundles, especially if you do not anticipate changing most of the assets in the group. If you do change assets in a group with this setting, the Addressables tools move the changed assets to a new group for the update. Only the new bundles are downloaded by installed applications.

Choose __Can Change Post Release__ for groups containing assets that you expect to change frequently. If you change assets in a group with this setting, the bundles containing those assets are rebuilt as a whole and will be redownloaded by installed applications. To reduce the amount of data that needs to be downloaded after an update, try to keep the bundles produced by groups with this setting as small as possible.

Refer to [Content update builds](content-update-builds-overview.md) for more information about updating remote content.

The __Advanced Options__ section contains some options that affect remote hosting and downloads but aren't necessary to enable remote hosting. Refer to [Advanced Options](GroupSchemas.md) for more information.