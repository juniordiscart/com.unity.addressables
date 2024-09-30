# Organize Addressable assets

The best way to organize your assets depends on the specific requirements of each project. Aspects to consider when planning how to manage your assets in a project include:

* **Logical organization**: Keep assets in logical categories to make it easier to understand your organization and discover items that are out of place.
* **Runtime performance**: Performance bottlenecks can happen if your AssetBundles grow in size, or if you have many AssetBundles.
* **Runtime memory management**: Keep assets together that you use together to help lower peak memory requirements.
* **Scale**: Some ways of organizing assets might work well in small games, but not large ones, and vice versa.
* **Platform characteristics**: The characteristics and requirements of a platform can be a large consideration in how to organize your assets. Some examples:
  * Platforms that offer abundant virtual memory can handle large bundle sizes better than those with limited virtual memory.
  * Some platforms don't support downloading content, ruling out remote distribution of assets entirely.
  * Some platforms don't support AssetBundle caching, so putting assets in local bundles, when possible, is more efficient.
* **Distribution**: Whether you distribute your content remotely or not means that you must separate your remote content from your local content.
* **How often assets are updated**: Keep assets that you expect to update often separate from those that you don't intend to update often.
* **Version control**: The more people who work on the same assets and asset groups, the greater the chance for version control conflicts to happen in a project.

## Common strategies

Typical strategies for organizing assets include:

* **Concurrent usage**: Group assets that you load at the same time together, such as all the assets for a given level. This strategy is often the most effective in the long term and can help reduce peak memory use in a project.
* **Logical entity**: Group assets belonging to the same logical entity together. For example, user interface (UI) layout assets, textures, sound effects. Or character models and animations.
* **Type**: Group assets of the same type together. For example, music files, textures.

Depending on the needs of your project, one of these strategies might make more sense than the others. For example, in a game with many levels, organizing according to concurrent usage might be the most efficient both from a project management and from a runtime memory performance standpoint. 

At the same time, you might use different strategies for different types of assets. For example, you might group your UI assets for menu screens together in a level-based game that otherwise groups its level data separately. You might also pack a group that has the assets for a level into bundles that contain a particular asset type.

Refer to [Preparing Assets for AssetBundles](xref:AssetBundles-Preparing) for additional information.

## Strategies for common platforms

The Addressables system creates AssetBundles based on the [Groups](Groups.md) you create. Your choices about the size and number of those AssetBundles, and when you choose to load and unload them, can have a significant impact on your project's install time, load time, and performance.
For this reason, it's important to choose a strategy that takes into account the metrics that might affect your project the most, when organizing your Addressables assets into Groups.

The information below describes some of these metrics, how they differ by platform, and how to optimize your strategy accordingly.

While there is no single solution that works for all projects, you should always try to limit AssetBundles to only contain assets that you expect to unload together. This is important to consider because your application can’t unload an asset that isn’t needed anymore if the application still needs any other asset in the same AssetBundle.

## Mobile apps

The key performance metrics to consider when you target mobile devices are app installation size, load times and download speed. Smaller builds take less time to download and install, so it’s important to minimize the size of your app. Mobile apps also need to perform well on low-powered devices at runtime.

### Reduce app size

Smaller final builds download faster and use less of the device’s storage after installation. Both are important to consider on low-powered mobile devices because a faster download and install time can improve user retention.

You can use remote content distribution to deliver some of your content after a user installs your app. Keep as much content as possible remote to reduce the initial download and installation size of your app.

You can use the Build report window to measure the impact of features on the size.

### Choose an appropriate bundled content size

The size of your AssetBundles affects how fast they load and unload.. For a mobile app where you might need to load many different levels in one play session, a larger number of smaller bundles can ensure that each bundle loads faster.

There’s a small performance impact for every AssetBundle you load because each one increases the size of the catalog. Using too many small AssetBundles can create a large catalog that has a greater performance impact than using fewer large AssetBundles. Check the size of the catalog in the [Streaming assets](https://docs.unity3d.com/2022.3/Documentation/Manual/StreamingAssets.html) folder for a variety of bundle sizes to decide on the best balance of size and number of bundles for your mobile app.

## Desktop applications

For desktop devices, download times are usually less important than runtime performance and loading times. A larger application on higher-end devices can justify a long download time, but not poor performance at runtime or long delays while content loads.

### Reduce loading times

For projects where you can use more runtime memory, you can preload AssetBundles that your project will need in the future. You can do this during existing waiting times (loading screens, transitions between content) or when the application starts. This increases the loading time once, but reduces or eliminates any load times in future for the preloaded AssetBundles.

### Optimize runtime performance

Desktop platforms have fewer constraints around device memory and storage. To take advantage of this, use group settings that don’t compress your AssetBundles. Uncompressed AssetBundles provide faster loading at runtime and more efficient patches, but increase application size.

## XR platforms

Requirements and constraints for XR platforms are often similar to mobile platforms. A key difference is in how important a consistent frame rate at runtime is. Any stuttering or reduction in frame rate in an XR application can cause discomfort for your user.

To help compensate for this, minimize the loading that your XR application needs to do at runtime. Preload AssetBundles wherever possible in the same way as described in the [Reduce loading times](#reduce-loading-times) section earlier on this page.
