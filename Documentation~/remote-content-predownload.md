# Pre-download remote content

In situations where you want to pre-download content so that it is cached on disk and faster to access when the application needs it, you can use the [`Addressables.DownloadDependenciesAsync`](xref:UnityEngine.AddressableAssets.Addressables.DownloadDependenciesAsync*)  method. This method downloads an Addressable entity and any dependencies as a background task.

Calling the `Addressables.DownloadDependenciesAsync` method loads the dependencies for the address or label that you pass in. Typically, this is the AssetBundle.

The [`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle) struct returned by this call includes a PercentComplete attribute that you can use to monitor and display download progress. You can also have the app wait until the content has loaded.

## PercentComplete

PercentComplete takes into account several aspects of the underlying operations being handled by a single `AsyncOperationHandle`. There may be instances where the progression isn't linear, or some semblance of linear. This can be due to quick operations being weighted the same as operations that will take longer.

For example, given an asset you wish to load from a remote location that takes a non-trivial amount of time to download and is reliant on a local bundle as a dependency you'll see your PercentComplete jump to 50% before continuing. This is because the local bundle is able to be loaded much quicker than the remote bundle. However, all the system is aware of is the need for two operations to be complete.

If you wish to ask the user for consent prior to download, use [`Addressables.GetDownloadSize`](xref:UnityEngine.AddressableAssets.Addressables.GetDownloadSize*) to return how much space is needed to download the content from a given address or label. Note that this takes into account any previously downloaded bundles that are still in Unity's AssetBundle cache.

While it can be advantageous to download assets for your app in advance, there are instances where you might choose not to do so. For example:

* If your application has a large amount of online content, and you generally expect users to only ever interact with a portion of it.
* You have an app that must be connected online to function. If all your app's content is in small bundles, you might choose to download content as needed.

Rather than using the percent complete value to wait until the content is loaded, you can use the preload functionality to show that the download has started, then continue on. This implementation would require a loading or waiting screen to handle instances where the asset has not finished loading by the time it's needed.
