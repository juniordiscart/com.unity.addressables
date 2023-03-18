---
uid: addressables-update-restriction-schema
---

## Prevent Updates Schema

The **Prevent Updates** options determine how the [Check for Content Update Restrictions] tool treats assets in the group. To prepare your groups for a differential content update build rather than a full content build, on the [Groups window], go to **Tools** and run the **Check for Content Update Restrictions** command. The tool moves modified assets in any groups with __Prevent Updates__ toggled on, to a new group.

The **Prevent Updates** options includes:

* **On**: The tool doesn't move any assets. When you make the update build, if any assets in the bundle have changed, then the entire bundle is rebuilt.
* **Off**: If any assets in the bundle have changed, then the [Check for Content Update Restrictions] tool moves them to a new group created for the update. When you make the update build, the assets in the AssetBundles created from this new group override the versions found in the existing bundles.

See [Content update builds] for more information.

[Addressable System Settings]: xref:addressables-asset-settings
[AddressableAssetGroup]: xref:UnityEditor.AddressableAssets.Settings.AddressableAssetGroup
[AddressableAssetGroupSchema]: xref:UnityEditor.AddressableAssets.Settings.AddressableAssetGroupSchema
[Addressables Build settings]: xref:addressables-asset-settings#build
[Addressables Groups window]: xref:addressables-groups-window
[Addressables Settings]: xref:addressables-asset-settings
[Addressables system settings]: xref:addressables-asset-settings
[Analyze]: xref:addressables-analyze-tool
[Asset Load Mode]: #asset-load-mode
[AssetBundle Compression]: addressables-content-packing-and-loading-schema#assetBundle-compression
[AssetBundle compression manual page]: xref:AssetBundles-Cache
[AssetReference]: xref:addressables-asset-references
[Build scripts]: xref:addressables-builds#build-commands
[Builds]: xref:addressables-builds
[Building and running a WebGL project]: xref:webgl-building#AssetBundles
[content state file]: xref:addressables-build-artifacts#content-state-file
[Content update builds]: xref:addressables-content-update-builds
[Content Workflow: Update Restrictions]: xref:addressables-content-update-builds#settings
[Check for Content Update Restrictions]: xref:addressables-content-update-builds#check-for-content-update-restrictions-tool
[Custom Inspector scripts]: xref:VariablesAndTheInspector
[Default Build Script]: xref:addressables-builds
[Event Viewer]: xref:addressables-event-viewer
[Group settings]: #addressables-group-schemas
[Group Templates]: #group-templates
[Group templates]: #group-templates
[Hosting]: xref:addressables-asset-hosting-services
[Labels]: xref:addressables-labels
[Loading Addressable assets]: xref:addressables-api-load-asset-async
[LoadAssetAsync]:  xref:UnityEngine.AddressableAssets.Addressables.LoadAssetAsync*
[LoadAssetsAsync]: xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync``1(System.Collections.Generic.IList{System.Object},System.Action{``0},UnityEngine.AddressableAssets.Addressables.MergeMode)
[LoadSceneMode.Single]: xref:UnityEngine.SceneManagement.LoadSceneMode.Single
[Organizing Addressable Assets]: xref:addressables-assets-development-cycle#organizing-addressable-assets
[Play Mode Scripts]: #play-mode-scripts
[Profile]: xref:addressables-profiles
[Profiles]: xref:addressables-profiles
[ProjectConfigData]: xref:UnityEditor.AddressableAssets.Settings.ProjectConfigData
[Resources.UnloadUnusedAssets]: xref:UnityEngine.Resources.UnloadUnusedAssets
[Schema]: #schemas
[settings of the group]: #addressables-group-schemas
[Synchronous Addressables]: xref:synchronous-addressables
[template]: #group-templates
[UnityWebRequestAssetBundle.GetAssetBundle]: xref:UnityEngine.Networking.UnityWebRequest.GetAssetBundle(System.String,System.UInt32)
[AssetBundle.LoadFromFileAsync]: xref:UnityEngine.AssetBundle.LoadFromFileAsync(System.String,System.UInt32,System.UInt64)
