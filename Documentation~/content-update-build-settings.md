# Content update build settings

To publish content updates, your application must already use a remote catalog and host its remote content on an accessible server. Refer to [Enabling remote distribution](xref:addressables-remote-content-distribution) for information about setting up content hosting and distribution.

You should also consider how to set each group's __Update Restriction__ settings. These settings determine how the __Check for Content Update Restriction__ tool treats changed content in your groups. Choose appropriate settings to help minimize the download size of your content updates.

Refer to [Group Update Restriction settings](#group-update-restriction-settings) for more information.

## Update a Previous Build setting

The [Addressable Asset Settings](AddressableAssetSettings.md) also contains a section for updating a previous build:

![The Update a Previous Build section of the Addressable Asset Settings Inspector window.](images/update-a-previous-build.png)*The Update a Previous Build section of the Addressable Asset Settings Inspector window.*

|**Setting**|**Description**|
|---|---|
| __Check For Update Issues__| Informs the system on whether it should run the __Check For Content Update Restrictions__ check automatically, and how to handle issues that are detected.|
|__Content State Build Path__| This location serves two purposes:<br/><br/>- Indicates where new Content Builds put the previous state file.<br/>- This is the location that Update a Previous Build attempts to pull the previous state file from automatically.|

The __Content State Build Path__ can be a remote location, if you want to have a shared previous state file on a server. The system handles remote locations for the previous state file differently:

* New content builds place the previous state file to the `ContentUpdateScript.PreviousContentStateFileCachePath`, which is `Library/com.unity.addressables/AddressablesBinFileDownload/` by default.
* Update a Previous Build downloads the remote previous state file to `ContentUpdateScript.PreviousContentStateFileCachePath` and then reads the file like normal. If the file does not exist at the remote location, but one has already been placed in the cache path, the system loads the local file.

## Group Update Restriction settings

For each group in your project, the __Update Restriction__ schema determines how a group, and its assets, are handled in a content update as follows:

* **Prevent Updates**: When toggled, the system treats assets in that group as static content that you expect to update infrequently, if at all. All local content should use this setting.

Choose the setting based on the content type in a group and how frequently you expect to update that content between full player builds of your application.

You can change content in a group no matter which setting you choose. The difference is how the __Check for Content Update Restrictions__ and __Update Previous Build__ tools treat the assets in the group and  how the installed applications access the updated content.

> [!IMPORTANT]
> Don't change the __Update Restriction__ setting of a group unless you are performing a full build. If you change your group settings before a content update, Addressables can't generate the correct changes needed for the update build.

### Prevent Updates Enabled (static content)

When you enable __Prevent Updates__, the __Check for Content Update Restrictions__ tool moves any changed assets to a new group, which is set to build and load from your remote paths. This is the same check that can be automatically integrated with Updating a Previous Build. Regardless of if you manually run the tool, or let __Update a Previous Build__ handle the check automatically, the content update sets up the remote catalog so that the changed assets are accessed from the new bundles, but the unchanged assets are still accessed from the original bundles.

> [!NOTE]
> Although the update build produces versions of the original bundles without the changed assets, installed applications don't download these bundles unless the locally cached version is deleted for some reason.

Organize content that you don't expect to update often in groups set to groups with __Prevent Updates.__ enabled. You can safely set up these groups to produce fewer, larger bundles because your users usually won't need to download these bundles more than once.

Set any groups that you intend to load from the local load path to __Prevent Updates__. Likewise, set any groups that produce large, remote bundles sto __Prevent Updates__ so that your users only need to download the changed assets if you do end up changing assets in these groups.

### Prevent Updates Disabled (dynamic content)

When a group doesn't have __Prevent Updates__, then a content update rebuilds the entire bundle if any assets inside the group have changed. The __Update a Previous Build__ script sets the catalog up so that installed applications load all assets in the group from the new bundles.

Organize content you expect to change often into groups with __Prevent Updates__ disabled. Because all the assets in these groups are republished when any single asset changes, you should set up these groups to produce smaller bundles containing fewer assets.

## Unique Bundle IDs setting

If you want to update content on the fly rather than at application startup, use the __Unique Bundle IDs__ setting. Enabling this option can make it easier to load updated AssetBundles in the middle of an application session, but typically makes builds slower and updates larger.

Enabling the __Unique Bundle IDs__ option allows you to load a changed version of an AssetBundle while the original bundle is still in memory. Building your AssetBundles with unique internal IDs makes it easier to update content at runtime without running into AssetBundle ID conflicts.

However, when enabled, any AssetBundles containing assets that reference a changed asset must also be rebuilt. More bundles must be updated for a content update and all builds are slower.

You typically only need to use unique bundle IDs when you update content catalogs after the Addressable system has already initialized and you have started loading assets.

You can avoid AssetBundle loading conflicts and the need to enable unique IDs using one of the following methods:

* Update the content catalog as part of Addressables initialization. By default, Addressables checks for a new catalog at initialization as long as you don't enable the [Only update catalogs manually](xref:addressables-asset-settings) option in Addressable Asset settings. Choosing this method does preclude updating your application content in mid-session.
* Unload all remote AssetBundles before updating the content catalog. Unloading all your remote bundles and assets also avoids bundle name conflicts, but could interrupt your user's session while they wait for the new content to load.
