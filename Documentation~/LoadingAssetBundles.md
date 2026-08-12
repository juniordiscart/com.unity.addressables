---
uid: addressables-loading-bundles
---

# Load AssetBundles

The Addressables system packs assets into AssetBundles and loads these AssetBundles as you load individual assets. You can control how AssetBundles load with the [`BundledAssetGroupSchema`](xref:UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema) class. You can set these options through the scripting API or under the [Advanced Options](ContentPackingAndLoadingSchema.md#advanced-options) in the Inspector of the Addressables Asset Group Inspector.

## UnityWebRequestForLocalBundles

Addressables can load AssetBundles from the following engine APIs:

* [`UnityWebRequestAssetBundle.GetAssetBundle`](xref:UnityEngine.Networking.UnityWebRequest.GetAssetBundle(System.String,System.UInt32))
* [`AssetBundle.LoadFromFileAsync`](xref:UnityEngine.AssetBundle.LoadFromFileAsync(System.String,System.UInt32,System.UInt64)).

The default behavior is to use `AssetBundle.LoadFromFileAsync` when the AssetBundle is in local storage and use `UnityWebRequestAssetBundle` when the AssetBundle path is a URL.

You can override this behavior to use `UnityWebRequestAssetBundle` for local AssetBundles by setting [`UseUnityWebRequestForLocalBundles`](xref:UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema.UseUnityWebRequestForLocalBundles) to true, or enabling the [**Use UnityWebRequest for Local Asset Bundles**](AddressableAssetSettings.md#downloads) property.

A few of these situations include:

* You're shipping local AssetBundles that use LZMA compression because you want your shipped game package to be as small as possible. In this case, you can use `UnityWebRequestAssetBundle` to recompress those AssetBundles LZ4 into the local disk cache.
* You're shipping an Android game and the APK contains AssetBundles that are compressed with the default APK compression.
* You want the entire local AssetBundle to be loaded into memory to avoid disk seeks. If you use `UnityWebRequestAssetBundle` and have caching disabled, the entire AssetBundle file is loaded into the memory cache. This increases runtime memory usage, but might improve loading performance because it prevents disk seeking after the initial AssetBundle load.

The first two situations result in the AssetBundle existing on the player device twice (original and cached representations). This means the initial loads (decompressing and copying to cache) are slower than subsequent loads (loading from cache)

## Handle download errors

When a download fails, the `RemoteProviderException` contains errors that can be used to determine how to handle the failure.
The `RemoteProviderException` is either the `AsyncOperationHandle.OperationException` or an inner exception. For example:

[!code-cs[sample](../Tests/Editor/DocExampleCode/DownloadError.cs#doc_DownloadError)]

Possible error strings:

* Request aborted
* Unable to write data
* Malformed URL
* Out of memory
* No Internet Connection
* Encountered invalid redirect (missing Location header?)
* Cannot modify request at this time
* Unsupported Protocol
* Destination host has an erroneous SSL certificate
* Unable to load SSL Cipher for verification
* SSL CA certificate error
* Unrecognized content-encoding
* Request already transmitted
* Invalid HTTP Method
* Header name contains invalid characters
* Header value contains invalid characters
* Cannot override system-specified headers
* Backend Initialization Error
* Cannot resolve proxy
* Cannot resolve destination host
* Cannot connect to destination host
* Access denied
* Generic/unknown HTTP error
* Unable to read data
* Request timeout
* Error during HTTP POST transmission
* Unable to complete SSL connection
* Redirect limit exceeded
* Received no data in response
* Destination host does not support SSL
* Failed to transmit data
* Failed to receive data
* Login failed
* SSL shutdown failed
* Redirect limit is invalid
* Not implemented
* Data Processing Error, see Download Handler error
* Unknown Error

## Additional resources

* [Define how groups are packed into AssetBundles](PackingGroupsAsBundles.md)
* [Addressable AssetBundle memory considerations](memory-assetbundles.md)
* [Load assets](load-assets.md)
* [Load assets by location](load-assets-location.md)