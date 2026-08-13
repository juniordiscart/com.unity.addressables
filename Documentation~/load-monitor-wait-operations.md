# Monitor wait operations

`AsyncOperationHandle` provides the following methods to monitor and report operation progress:

* [`GetDownloadStatus`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.GetDownloadStatus*): Returns a [`DownloadStatus`](xref:UnityEngine.ResourceManagement.AsyncOperations.DownloadStatus) struct that contains information about downloaded bytes and remaining bytes to download. [`DownloadStatus.Percent`](xref:UnityEngine.ResourceManagement.AsyncOperations.DownloadStatus.Percent) reports the percentage of downloaded bytes.
* [`AsyncOperationHandle.PercentComplete`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.PercentComplete): Returns an equally weighted total percentage of all completed sub operations. For example, if an operation has five sub operations, each one represents 20% of the total. This value doesn't factor in the amount of data that individual sub operations must download.

For example, when you call [`Addressables.DownloadDependenciesAsync`](xref:UnityEngine.AddressableAssets.Addressables.DownloadDependenciesAsync*) and five AssetBundles require downloading, `GetDownloadStatus` tells you what percentage of the total bytes for all sub operations Unity has downloaded. `PercentComplete` tells you what percentage of operations have finished, regardless of their size.

If you call [`LoadAssetAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetAsync*), and one AssetBundle requires downloading before Unity can load an asset from it, the download percentage might be misleading. The values that `GetDownloadStatus` provides reach 100% before the operation finishes, because the operation has additional sub operations to conduct. The value of `PercentComplete` reaches 50% when the download sub operation finishes and 100% when the actual load into memory completes.

## Additional resources

* [`GetDownloadStatus` API reference](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.GetDownloadStatus*)
* [`AsyncOperationHandle.PercentComplete`API reference](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.PercentComplete)
* [Wait for asynchronous loads to complete](AddressableAssetsAsyncOperationHandle.md)