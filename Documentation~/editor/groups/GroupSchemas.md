---
uid: addressables-group-schemas
---

# Group Settings

Group settings determine how Unity treats the assets in a group in content builds. Group settings control properties such as the location where AssetBundles are built or bundle compression settings.

A group's settings are declared in Schema objects attached to the group. When you create a group with the __Packed Assets__ [template], the __Content Packing & Loading__ and __Content Update Restriction__ schemas define the settings for the group. The default [Build scripts] expect these settings. 

![](../../images/addr_groups_2.png)<br/>*The Inspector window for the Default Local Group*

> [!NOTE]
> If you create a group with the __Blank__ template, then no schemas are attached to the group. Assets in such a group cannot be processed by the default build scripts.

## Schemas

A group schema is a ScriptableObject that defines a collection of settings for an Addressables group. You can assign any number of schemas to a group. The Addressables system defines a number of schemas for its own purposes. You can also create custom schemas to support your own build scripts and utilities.

The built-in schemas include:

* __Content Packing & Loading__: this is the main Addressables schema used by the default build script and defines the settings for building and loading Addressable assets.
* __Content Update Restrictions__: defines settings for making differential updates of a previous build. See [Builds] for more information about update builds.
* __Resources and Built In Scenes__: a special-purpose schema defining settings for which types of built-in assets to display in the __Built In Data__ group. 

### Defining custom schemas

To create your own schema, extend the [AddressableAssetGroupSchema] class (which is a kind of ScriptableObject).

```csharp
using UnityEditor.AddressableAssets.Settings;

public class __CustomSchema __: AddressableAssetGroupSchema
{
   public string CustomDescription;
}
```

Once you have defined your custom schema object, you can add it to existing groups and group templates using the Add Schema buttons found on the Inspector windows of those entities.

You might also want to create a custom Editor script to help users interact with your custom settings. See [Custom Inspector scripts].

In a build script, you can access the schema settings for a group using its [AddressableAssetGroup] object.

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
[settings of the group]: #addressables-group-schemas
[Synchronous Addressables]: xref:synchronous-addressables
[template]: xref:group-templates
[UnityWebRequestAssetBundle.GetAssetBundle]: xref:UnityEngine.Networking.UnityWebRequest.GetAssetBundle(System.String,System.UInt32)
[AssetBundle.LoadFromFileAsync]: xref:UnityEngine.AssetBundle.LoadFromFileAsync(System.String,System.UInt32,System.UInt64)
