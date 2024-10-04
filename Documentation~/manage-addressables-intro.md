# Manage Addressables introduction

Before you decide how you want to manage the assets in your project, refer to [How Addressables interacts with your project assets](xref:addressables-asset-dependencies).

Addressable [groups](xref:addressables-groups) are the main unit of organization that you use to manage Addressable assets. An important consideration are your options for [packing groups into AssetBundles](xref:addressables-packing-groups).

Alongside group settings, you can use the following to control how Addressables work in a project:

* [Addressable asset settings](xref:addressables-asset-settings): The project-level settings of Addressable assets.
* [Profiles](xref:addressables-profiles): Defines collections of build path settings that you can switch between depending on the purpose of a build. Primarily of interest if you plan to distribute content remotely.
* [Labels](xref:addressables-labels): Edit the Addressable asset labels used in your project.
* [Play mode scripts](xref:addressables-groups-window): Choose how the Addressables system loads assets when you enter Play mode in the Editor.

[AssetReferences](xref:addressables-asset-references) offer a UI-friendly way to use Addressable assets. You can include `AssetReference` fields in `MonoBehaviour` and `ScriptableObject` classes and then assign assets to them in the Unity Editor using drag-and-drop or the object picker dialog.

The Addressables system provides the following additional tools to help development:

* [Build layout report](xref:addressables-build-layout-report): provides a description of the AssetBundles produced by a build.
* [Build profile log](xref:addressables-build-profile-log): provides a log profiling the build process itself so that you can see which parts take the longest.

## Further resources

* [Organize Addressable assets](organize-addressable-assets.md)