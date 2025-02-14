# Content update examples

The following discussion walks through a hypothetical example to illustrate how Addressable content is handled during a content update. In this example, consider a shipped application built with the following Addressables groups:

| Local_Static| Remote_Static | Remote_NonStatic |
|:---|:---|:---|
| AssetA| AssetL | AssetX |
| AssetB| AssetM | AssetY |
| AssetC| AssetN | AssetZ |

`Local_Static` and `Remote_Static` are part of the Cannot Change Post Release groups.

Since this version is live, existing players have `Local_Static` on their devices, and potentially have either or both of the remote bundles cached locally.

If you modify one Asset from each group (AssetA, AssetL, and AssetX), then run __Check for Content Update Restrictions__, the results in your local Addressable settings are now:

| Local_Static| Remote_Static | Remote_NonStatic | content_update_group (non-static) |
|:---|:---|:---|:---|
| |  | AssetX | AssetA |
| AssetB| AssetM | AssetY | AssetL |
| AssetC| AssetN | AssetZ |  |

The prepare operation edits the Cannot Change Post Release groups, which may seem counterintuitive. However, the system builds the above layout, but discards the build results for any such groups. As such, you end up with the following from a player's perspective:

| Local_Static|
|:---|
| AssetA|
| AssetB|
| AssetC|

The `Local_Static` bundle is already on player devices, which you can't change. This old version of AssetA is no longer referenced. Instead, it is stuck on player devices as dead data.

| Remote_Static|
|:---|
| AssetL|
| AssetM|
| AssetN|


The `Remote_Static` bundle is unchanged. If it is not already cached on a player's device, it will download when AssetM or AssetN is requested. Like AssetA, this old version of AssetL is no longer referenced.

| Remote_NonStatic (old)|
|:---|
| AssetX|
| AssetY|
| AssetZ|

The `Remote_NonStatic` bundle is now old. You can delete it from the server or leave it there; either way it will not be downloaded from this point forward. If cached, it remains on player devices indefinitely unless you remove it. See [AssetBundle caching](xref:addressables-remote-content-distribution) for more information. Like AssetA and AssetL, this old version of AssetX is no longer referenced.

| Remote_NonStatic (new)|
|:---|
| AssetX|
| AssetY|
| AssetZ|

The old `Remote_NonStatic` bundle is replaced with a new version, distinguished by its hash file. The modified version of AssetX is updated with this new bundle.

| content_update_group|
|:---|
| AssetA|
| AssetL|

The `content_update_group` bundle consists of the modified Assets that will be referenced moving forward.

The example above has the following implications:

* Any changed local Assets remain unused on the user's device forever.
* If the user already cached a non-static bundle, they will need to redownload the bundle, including the unchanged Assets (in this instance, for example, AssetY and AssetZ). Ideally, the user has not cached the bundle, in which case they simply need to download the new Remote_NonStatic bundle.
* If the user has already cached the `Static_Remote` bundle, they only need to download the updated asset (in this instance, AssetL via `content_update_group`). This is ideal in this case. If the user has not cached the bundle, they must download both the new AssetL via `content_update_group` and the now-defunct AssetL via the untouched `Remote_Static` bundle. Regardless of the initial cache state, at some point the user will have the defunct AssetL on their device, cached indefinitely despite never being accessed.

The best setup for your remote content will depend on your specific use case.

## Content update dependencies

Directly changing an asset is not the only way to have it flagged as needing to be rebuilt as part of a content update. Changing an asset's dependencies is a less obvious factor that gets taken into account when building an update.

As an example, consider the `Local_Static` group from the example above:

| Local_Static|
|:---|
| AssetA|
| AssetB|
| AssetC|

Suppose the assets in this group have a dependency chain that looks like this: AssetA depends on Dependency1, which depends on Dependency2, AssetB depends on Dependency2, and AssetC depends on Dependency3 and all three dependencies are a mix of Addressable and non-Addressable assets.

If only Dependency1 is changed and Check For Content Update Restriction is run, the resulting project structure looks like:

| Local_Static| content_update_group |
|:---|:---|
| | AssetA |
| AssetB|  |
| AssetC|  |

If only Dependency2 is changed:

| Local_Static| content_update_group |
|:---|:---|
| | AssetA |
| | AssetB |
| AssetC|  |

Finally, if only Dependency3 is changed:

| Local_Static| content_update_group |
|:---|:---|
| AssetA|  |
| AssetB|  |
| | AssetC |

This is because when a dependency is changed the entire dependency tree needs to be rebuilt.

The following example has this dependency tree. AssetA depends on AssetB, which depends on Dependency2, AssetB depends on Dependency2, and AssetC depends on Dependency3. Now, if Dependency2 is changed, the project structure looks like the following:

| Local_Static| content_update_group |
|:---|:---|
| | AssetA |
| | AssetB |
| AssetC|  |

This is because AssetA relies on AssetB and AssetB relies on Dependency2. Since the entire chain needs to be rebuilt both AssetA and AssetB will get put into the __content_update_group__.
