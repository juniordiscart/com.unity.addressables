---
uid: samples-custom-build-and-playmode-scripts
---

# Custom Build and Playmode Scripts Sample

This sample includes two [custom scripts]: a custom play mode script (located in `Editor/CustomPlayModeScript.cs` of the Sample) and a custom build script (located in `Editor/CustomBuildScript.cs` of the Sample).  This custom build script creates a build that only includes the currently open scene.  A bootstrap scene is automatically created and a script is added that loads the built scene on startup.  The custom play mode script works similarly to the Use Existing Build (requires built groups) play mode script already included.  The methods added to accomplish this are `CreateCurrentSceneOnlyBuildSetup` and `RevertCurrentSceneSetup` on the `CustomBuildScript`.

For these examples, the build and load paths used by default are `[UnityEngine.AddressableAssets.Addressables.BuildPath]/[BuildTarget]` and `{UnityEngine.AddressableAssets.Addressables.RuntimePath}/[BuildTarget]` respectively.

The `ScriptableObject` of the class has already been created, but the Create menu can be used to make another `ScriptableObject` if you desire.  For this `CustomPlayModeScript` the create menu path is **Addressables/Content Builders/Use CustomPlayMode Script**.  By default, this creates a CustomPlayMode.asset ScriptableObject.  The same goes for the `CustomBuildScript`.

[custom scripts]: xref:addressables-api-build-player-content#custom-build-scripting