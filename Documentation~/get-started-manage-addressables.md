# Manage Addressable assets

To manage your Addressable assets, use the [Addressables Groups](Groups.md) window. Use this window to create Addressables groups, move assets between groups, and assign addresses and labels to assets.

When you first install and set up the Addressables package, it creates a default group for Addressable assets. The Addressables system assigns any assets you mark as Addressable to this group by default. At the start of a project, you might find it acceptable to keep your assets in this single group, but as you add more content, you should create additional groups so that you have better control over which resources your application loads and keeps in memory at any given time.

Key group settings include:

* **Build path**: Where to save your content after a content build.
* **Load path**: Where your application looks for built content at runtime.

> [!NOTE]
> You can use Profile variables to set these paths. Refer to [Profiles](AddressableAssetsProfiles.md) for more information.

* **Bundle mode**: How to package the content in the group into a bundle. You can choose the following options:
    * One bundle containing all group assets
    * A bundle for each entry in the group (useful if you mark entire folders as Addressable and want their contents built together)
    * A bundle for each unique combination of labels assigned to group assets
* **Content update restriction**: Setting this value allows you to publish smaller content updates. Refer to [Content update builds](xref:addressables-content-update-builds) for more information. If you always publish full builds to update your app and don't download content from a remote source, you can ignore this setting.

For more information on strategies to consider when deciding how to organize your assets, refer to [Organizing Addressable assets](xref:addressables-assets-development-cycle).

For more information on using the Addressables Groups window, refer to [Groups](Groups.md).