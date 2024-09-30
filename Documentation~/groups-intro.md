# Groups introduction

A group is the main organizational unit of the Addressables system. Create and manage your groups and the assets they contain with the [Addressables Groups window](xref:addressables-groups-window).

To control how Unity handles assets during a content build, organize Addressables into groups and assign different settings to each group as required. Refer to [Organizing Addressable Assets](xref:addressables-assets-development-cycle) for information about how to organize your assets.

When you begin a content build, the build scripts create AssetBundles that contain the assets in a group. The build determines the number of bundles to create and where to create them from both the [settings of the group](xref:addressables-group-schemas) and your overall [Addressables system settings](xref:addressables-asset-settings). Refer to [Builds](xref:addressables-builds) for more information.

> [!NOTE]
> Addressable Groups only exist in the Unity Editor. The Addressables runtime code doesn't use a group concept. However, you can assign a label to the assets in a group if you want to find and load all the assets that were part of that group. Refer to [Loading Addressable assets](xref:addressables-api-load-asset-async) for more information about selecting the assets to load using labels.