# Content update build settings reference

There are several settings that affect how Unity performs content update builds.

To publish content updates, your application must already use a remote catalog and host its remote content on an accessible server. Refer to [Distribute remote content](RemoteContentDistribution.md) for information about setting up content hosting and distribution.

## Update a Previous Build setting

The [Addressable Asset Settings](AddressableAssetSettings.md#update-a-previous-build) contains a section for updating a previous build.

![The Update a Previous Build section of the Addressable Asset Settings Inspector window.](images/update-a-previous-build.png)*The Update a Previous Build section of the Addressable Asset Settings Inspector window.*

The __Content State Build Path__ can be a remote location, if you want to have a shared previous state file on a server. The system handles remote locations for the previous state file differently:

* New content builds place the previous state file to the `ContentUpdateScript.PreviousContentStateFileCachePath`, which is `Library/com.unity.addressables/AddressablesBinFileDownload/` by default.
* Update a Previous Build downloads the remote previous state file to `ContentUpdateScript.PreviousContentStateFileCachePath` and then reads the file like normal. If the file doesn't exist at the remote location, but one is already in the cache path, the system loads the local file.

## Group Update Restriction settings

For each group in your project, the [__Update Restriction__](ContentPackingAndLoadingSchema.md#content-update-restriction) schema determines how Unity handles a group and its assets in a content update as follows. When you enable **Prevent Updates**, Unity treats assets in that group as static content that you expect to update infrequently, if at all. All local content should use this setting.

Choose the setting based on the content type in a group and how frequently you expect to update that content between full player builds of your application.

You can change content in a group no matter which setting you choose. The difference is how the [__Check for Content Update Restrictions__](builds-update-build.md#check-for-content-update-restrictions) and [__Update Previous Build__](builds-update-build.md) tools treat the assets in the group and how the installed applications access the updated content.

> [!IMPORTANT]
> Don't change the __Update Restriction__ setting of a group unless you're performing a full build. If you change your group settings before a content update, Addressables can't generate the correct changes needed for the update build.

### Enable Prevent Updates

When you enable __Prevent Updates__, the __Check for Content Update Restrictions__ tool moves any changed assets to a new group, which is set to build and load from your remote paths. This is the same check that can be automatically integrated when updating a previous build. The content update sets up the remote catalog so that the changed assets are accessed from the new AssetBundles, but the unchanged assets are still accessed from the original AssetBundles.

> [!NOTE]
> Although the update build produces versions of the original AssetBundles without the changed assets, installed applications don't download these AssetBundles unless the locally cached version is deleted.

Organize content that you don't expect to update often in groups set to groups with __Prevent Updates.__ enabled. You can safely set up these groups to produce fewer, larger AssetBundles because your users usually don't need to download these AssetBundles more than once.

Set any groups that you intend to load from the local load path, or any groups that produce large, remote AssetBundles to __Prevent Updates__. This means that your users only need to download the changed assets if you end up changing assets in these groups.

### Disable Prevent Updates

When a group doesn't have __Prevent Updates__, then a content update rebuilds the entire AssetBundle if any assets inside the group have changed. The __Update a Previous Build__ script sets the catalog up so that installed applications load all assets in the group from the new bundles.

Organize content you expect to change often into groups with __Prevent Updates__ disabled. Because all the assets in these groups are republished when any single asset changes, you should set up these groups to produce smaller AssetBundles containing fewer assets.

## Unique Bundle IDs setting

If you want to update content on the fly rather than at application startup, enable the [__Unique Bundle IDs__](AddressableAssetSettings.md) setting. Enabling this option can make it easier to load updated AssetBundles in the middle of an application session, but typically makes builds slower and updates larger.

Enabling the __Unique Bundle IDs__ option allows you to load a changed version of an AssetBundle while the original AssetBndle is still in memory. Building AssetBundles with unique internal IDs makes it easier to update content at runtime without running into AssetBundle ID conflicts.

However, when enabled, any AssetBundles containing assets that reference a changed asset must also be rebuilt. More AssetBundles must be updated for a content update and all builds are slower.

You typically only need to enable __Unique Bundle IDs__ when you update content catalogs after the Addressable system has already initialized and your application starts loading assets.

You can avoid AssetBundle loading conflicts and the need to enable unique IDs using one of the following methods:

* Update the content catalog as part of Addressables initialization. By default, Addressables checks for a new catalog at initialization as long as you don't enable the [Only update catalogs manually](AddressableAssetSettings.md) option. Choosing this method means updating your application content in mid-session.
* Unload all remote AssetBundles before updating the content catalog. Unloading all remote AssetBundles and assets avoids AssetBundle name conflicts, but might interrupt your user's session while they wait for the new content to load.

## Additional resources

* [Distribute remote content](RemoteContentDistribution.md)
* [Addressables Asset Settings reference](AddressableAssetSettings.md)