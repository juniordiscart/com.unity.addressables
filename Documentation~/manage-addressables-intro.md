# Manage Addressables introduction

Familiarize yourself with how assets create dependencies on other assets before you decide how you want to manage the assets in your project. For more information about dependencies, refer to [Asset dependencies overview](xref:addressables-asset-dependencies).

For more information on strategies to consider when deciding how to organize your assets, refer to [Organizing Addressable assets](xref:addressables-assets-development-cycle).

## Organize Addressables with groups

Addressable [groups](xref:addressables-groups) are the main unit of organization that you use to manage Addressable assets. Use the Addressables Groups window to create Addressables groups, move assets between groups, and assign addresses and labels to assets.

When you first install and set up the Addressables package, it creates a default group for Addressable assets. The Addressables system assigns any assets you mark as Addressable to this group by default. At the start of a project, you might find it acceptable to keep your assets in this single group. As you add more content, you should create additional groups so that you have better control over which resources your application loads and keeps in memory at any given time.

Key group settings include:

* **Build path**: Where to save your content after a content build.
* **Load path**: Where your application looks for built content at runtime.
* **Bundle mode**: How to package the content in the group into a bundle. You can choose the following options:
    * One bundle containing all group assets
    * A bundle for each entry in the group (useful if you mark entire folders as Addressable and want their contents built together)
    * A bundle for each unique combination of labels assigned to group assets
* **Content update restriction**: Setting this value allows you to publish smaller content updates. Refer to [Content update builds](xref:addressables-content-update-builds) for more information. If you always publish full builds to update your app and don't download content from a remote source, you can ignore this setting.

Alongside group settings, you can use the following to control how Addressables work in a project:

* [Addressable asset settings](xref:addressables-asset-settings): The project-level settings of the Addressable assets.
* [Profiles](xref:addressables-profiles): Defines collections of build path settings that you can swap between, depending on the purpose of a build. This is most useful if you plan to distribute content remotely.
* [Labels](xref:addressables-labels): Edit the Addressable asset labels used in your project.
* [Play mode scripts](xref:addressables-groups-window): Choose how the Addressables system loads assets when you enter Play mode in the Editor.

> [!NOTE]
> You can use Profile variables to set the build and load paths. Refer to [Profiles](AddressableAssetsProfiles.md) for more information.

You can also control how Unity packs groups of assets into AssetBundles. You can keep groups together in one AssetBundle, separate groups into different bundles, or pack together assets that share a label. Refer to [Pack groups into AssetBundles](xref:addressables-packing-groups) for more information.

## Manage assets through the Editor

[AssetReferences](xref:addressables-asset-references) offer a user interface (UI) compatible way to use Addressable assets. You can include `AssetReference` fields in `MonoBehaviour` and `ScriptableObject` classes. Then, you can assign assets to them in the Unity Editor either by dragging an Addressable asset into the `AssetReference` field in the Inspector window, or using the object picker dialog.

## Tools

The Addressables system provides the following additional tools to help development:

* [Build layout report](xref:addressables-build-layout-report): provides a description of the AssetBundles produced by a build.
* [Build profile log](xref:addressables-build-profile-log): provides a log of information about the performance of the build process.
