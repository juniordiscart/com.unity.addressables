# Content update build settings

To publish content updates, your application must already use a remote catalog and host its remote content on an accessible server. Refer to [Enabling remote distribution](xref:addressables-remote-content-distribution) for information about setting up content hosting and distribution.

You should also consider how to set each group's __Update Restriction__ settings. These settings determine how the __Check for Content Update Restriction__ tool treats changed content in your groups. Choose appropriate settings to help minimize the download size of your content updates. 

For more information, refer to [Group Update Restriction settings](#group-update-restriction-settings).

## Update a Previous Build setting

The [Addressable Asset Settings](AddressableAssetSettings.md) also has a section for updating a previous build:  

![](images/update-a-previous-build.png)

|**Setting**|**Description**|
|---|---|
| __Check For Update Issues__| Informs the system on whether to run the __Check For Content Update Restrictions__ check automatically, and how to handle any issues the system detects.|
|__Content State Build Path__| This location serves two purposes:<br/><br/>- Indicates where new Content Builds put the previous state file.<br/>- This is the location that the __Update a Previous Build__ process attempts to pull the previous state file from automatically.|

The __Content State Build Path__ can be a remote location, if you want to have a shared previous state file on a server. The system handles remote locations for the previous state file differently:

* New content builds place the previous state file to the `ContentUpdateScript.PreviousContentStateFileCachePath`, which is `Library/com.unity.addressables/AddressablesBinFileDownload/` by default.
* Update a Previous Build downloads the remote previous state file to `ContentUpdateScript.PreviousContentStateFileCachePath` and then reads the file like normal. If the file doesn't exist at the remote location, but one has already been placed in the cache path, the system loads the local file.

## Group Update Restriction settings

For each group in your project, the __Update Restriction__ schema determines how a content update handles the group and its assets as follows:

* __Prevent Updates__: When enabled, the system treats assets in that group as static content that you expect to update infrequently, if at all. Use this setting for all local content.

Choose the setting based on the content type in a group and how often you expect to update that content between full player builds of your application.

You can change content in a group no matter which setting you choose. The difference is how the __Check for Content Update Restrictions__ and __Update Previous Build__ tools treat the assets in the group and  how the installed applications access the updated content.

> [!IMPORTANT]
> Don't change the __Update Restriction__ setting of a group unless you are performing a full build. If you change your group settings before a content update, the Addressables system can't generate the correct changes needed for the update build.

### Prevent Updates Enabled (static content)

When you enable __Prevent Updates__, the __Check for Content Update Restrictions__ tool moves any changed assets to a new group, which is set to build and load from your remote paths. This is the same check that can be automatically integrated with Updating a Previous Build. Regardless of if you manually run the tool, or let __Update a Previous Build__ handle the check automatically, the content update sets up the remote catalog so that the changed assets are accessed from the new bundles, but the unchanged assets are still accessed from the original bundles. 

> [!NOTE]
> Although the update build produces versions of the original bundles without the changed assets, installed applications don't download these bundles unless the locally cached version is deleted for some reason.  

Organize content that you don't expect to update often into groups with the __Prevent Updates__ property enabled. You can safely set up these groups to produce fewer, larger bundles because your users usually won't need to download these bundles more than once.  

Enable the __Prevent Updates__ property for any groups that you intend to load from the local load path and for any groups that produce large, remote bundles. This only requires your users to download changed assets when assets in the group change.

### Prevent Updates Disabled (dynamic content)

When a group doesn't have __Prevent Updates__ enabled, then a content update rebuilds the entire bundle if any assets inside the group have changed. The __Update a Previous Build__ script sets up the catalog so that installed applications load all assets in the group from the new bundles.

Organize content you expect to change often into groups with __Prevent Updates__ disabled. The Addressables system republishes all the assets in these groups when any single asset changes. To minimize the number of assets that need republishing, set up these groups to produce smaller bundles containing fewer assets. 

## Unique Bundle IDs setting

If you want to update content at runtime rather than at application startup, use the __Unique Bundle IDs__ setting. Enabling this setting can make it easier to load updated AssetBundles in the middle of an application session, but can make builds slower and updates larger.

With this setting enabled, you can load a changed version of an AssetBundle while the original bundle is still in memory. Building your AssetBundles with unique internal IDs makes it easier to update content at runtime without creating AssetBundle ID conflicts. 

However, when enabled, any AssetBundles containing assets that reference a changed asset must also be rebuilt. More bundles must be updated for a content update and all builds are slower.

You typically only need to use unique bundle IDs when you update content catalogs after the Addressable system has already initialized and you have started loading assets.

You can avoid AssetBundle loading conflicts and the need to enable unique IDs using one of the following methods:

* Update the content catalog as part of Addressables initialization. By default, Addressables checks for a new catalog at initialization as long as you don't enable the [Only update catalogs manually](xref:addressables-asset-settings) option in Addressable Asset settings. Choosing this method does preclude updating your application content in mid-session.
* Unload all remote AssetBundles before updating the content catalog. Unloading all your remote bundles and assets also avoids bundle name conflicts, but could interrupt your user's session while they wait for the new content to load. 
