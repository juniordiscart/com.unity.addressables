---
uid: group-templates
---

## Group templates

A Group template defines which types of schema objects Unity creates for a new group. The Addressables system includes the __Packed Assets__ template, which includes all the settings needed to build and load Addressables using the default build scripts. 

If you want to create your own build scripts or utilities that need additional settings, you can define these settings in your own schema objects and create your own group templates. The following instructions describe how to do this:

1. Navigate to the desired location in your Assets folder using the Project panel.
2. Create a Blank Group Template (menu: **Assets** &gt; **Addressables** &gt; **Group Templates** &gt; **Blank Group Templates**).
3. Assign a suitable name to the template.
4. In the Inspector window, add a description, if desired.
5. Click the **Add Schema** button and choose from the list of schemas.

Repeat the above steps to add as many new schemas as needed.

> [!NOTE]
> If you use the default build script, a group must use the __Content Packing & Loading__ schema. If you use content update builds, a group must include the __Content Update Restrictions__ schema. See [Builds] for more information.

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
