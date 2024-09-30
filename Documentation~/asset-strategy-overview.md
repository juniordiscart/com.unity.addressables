# Asset organization strategy overview

To maximize the benefits of the Addressables package, you need to organize your assets in a way that makes sense for your project. Each project has unique requirements and one strategy won't works across all project types.

This section outlines strategies for organizing Addressables assets for some common types of projects.

## Scale implications as your project grows larger

As your project grows larger, consider the following aspects of your assets and bundles:

*  Total bundle size: Historically Unity hasn't supported files larger than 4GB. This has been fixed in some recent editor versions, but there can still be issues. You should keep the content of a given bundle under this limit for best compatibility across all platforms.
* __Bundle layout at scale__: The memory and performance trade-offs between the number of AssetBundles produced by your content build and the size of those bundles can change as your project grows larger.
* __Bundle dependencies__: When an Addressable asset is loaded, all of its bundle dependencies are also loaded. Be aware of any references between assets when creating Addressable groups. Refer to [Asset and AssetBundle dependencies](xref:addressables-asset-dependencies) for more information.
* __Sub assets affecting UI performance__: There is no hard limit here, but if you have many assets, and those assets have many subassets, it might be best to disable sub-asset display. This option only affects how the data is displayed in the Groups window, and does not affect what you can and cannot load at runtime. The option is available in the groups window under __Tools__&gt; __Show Sprite and Subobject Addresses__. Disabling this will make the UI more responsive.
* __Group hierarchy display__: Another UI-only option to help with scale is __Group Hierarchy with Dashes__. This is available within the inspector of the top level settings. With this enabled, groups that contain dashes `-` in their names will display as if the dashes represented folder hierarchy. This does not affect the actual group name, or the way things are built. For example, two groups called `x-y-z` and `x-y-w` would display as if inside a folder called `x`, there was a folder called `y`. Inside that folder were two groups, called `x-y-z` and `x-y-w`. This doesn't affect UI responsiveness, but simply makes it easier to browse a large collection of groups.
