# Content update examples

The following examples illustrate how Addressables handles content during an update build.

## Content marked as Prevent Updates

In this example, consider a shipped application built with the following Addressables groups:

| **Local_Static**| **Remote_Static** | **Remote_NonStatic** |
|:---|:---|:---|
| AssetA| AssetL | AssetX |
| AssetB| AssetM | AssetY |
| AssetC| AssetN | AssetZ |

`Local_Static` and `Remote_Static` have the [**Prevent Updates**](ContentPackingAndLoadingSchema.md#content-update-restriction) property in their group settings enabled.

Existing players have `Local_Static` on their devices, and might have both of the remote AssetBundles cached locally.

If you modify one asset from each group (AssetA, AssetL, and AssetX), then run [__Check for Content Update Restrictions__](builds-update-build.md#check-for-content-update-restrictions), the results in your local Addressable settings are as follows:

| **Local_Static**| **Remote_Static** | **Remote_NonStatic** | **content_update_group (non-static)** |
|:---|:---|:---|:---|
| |  | AssetX | AssetA |
| AssetB| AssetM | AssetY | AssetL |
| AssetC| AssetN | AssetZ |  |

Because `Local_Static` and `Remote_Static` have the **Prevent Updates** property enabled, Unity moves the modified assets to a new group. The system builds the previous layout, but discards the build results for any new groups.

The example has the following implications:

* Any changed local assets remain unused on the user's device forever.
* If the user already cached a non-static AssetBundle, they need to redownload the AssetBundle, including the unchanged assets (in this instance, for example, AssetY and AssetZ). If the user hasn't cached the AssetBundle, they only need to download the new `Remote_NonStatic` AssetBundle.
* If the user has already cached the `Remote_Static` AssetBundle, they only need to download the updated asset (in this instance, AssetL via `content_update_group`). This is ideal in this case. If the user hasn't cached the AssetBundle, they must download both the new AssetL via `content_update_group` and the now-defunct AssetL via the untouched `Remote_Static` AssetBundle. Regardless of the initial cache state, at some point the user have the defunct AssetL on their device, cached indefinitely despite never being accessed.

As such, you end up with the following from a user's perspective:

* The `Local_Static` AssetBundle is already on the user's device, which you can't change. The old version of AssetA is no longer referenced and remains on the user's device as unused data.
* The `Remote_Static` AssetBundle is unchanged. If it isn't already cached on the user's device, it downloads when AssetM or AssetN is requested. Like AssetA, this old version of AssetL is no longer referenced.
* The `Remote_NonStatic` AssetBundle is outdated and replaced with a new `Remote_NonStatic` AssetBundle with a different hash file. The modified version of AssetX is updated with the new AssetBundle.
You can optionally delete the old `Remote_NonStatic` AssetBundle from the server, and it isn't downloaded from this point forward. If cached, it remains on the user's device indefinitely unless you remove it. For more information, refer to [AssetBundle caching](xref:addressables-remote-content-distribution).


## Content update dependencies

If you change an asset's dependencies, Unity rebuilds it as part of a content update build.

As an example, consider the `Local_Static` group from the previous example. The assets in this group have a dependency chain that looks like this:

* AssetA depends on Dependency1, which depends on Dependency2,
* AssetB depends on Dependency2,
* AssetC depends on Dependency3

All three dependencies are a mix of Addressable and non-Addressable assets. If you modify any of the assets in this group, when a dependency is changed the entire dependency tree needs to be rebuilt. Additionally, because `Local_Static` has the **Prevent Updates** property enabled, Unity moves any modified assets to a new group.

For example, If only Dependency1 is changed and then you run [__Check for Content Update Restrictions__](builds-update-build.md#check-for-content-update-restrictions), the resulting project structure looks like the following:

| **Local_Static**| **content_update_group** |
|:---|:---|
| | AssetA |
| AssetB|  |
| AssetC|  |

If only Dependency2 is changed:

| **Local_Static**| **content_update_group** |
|:---|:---|
| | AssetA |
| | AssetB |
| AssetC|  |

If only Dependency3 is changed:

| **Local_Static**| **content_update_group** |
|:---|:---|
| AssetA|  |
| AssetB|  |
| | AssetC |

## Additional resources

* [Content update build settings](content-update-build-settings.md)
* [Create a script to check for content updates](content-update-builds-check.md)
* [Group Inspector settings reference](ContentPackingAndLoadingSchema.md#content-update-restriction)
