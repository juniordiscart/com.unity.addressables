---
uid: addressables-api-build-player-content
---

# Build scripting

You can use the `Addressables` API to customize your project build in the following ways:

* Start a build from a script
* Override an existing script
* Extend [`BuildScriptBase`](xref:UnityEditor.AddressableAssets.Build.DataBuilders.BuildScriptBase) or implement [`IDataBuilder`](xref:UnityEditor.AddressableAssets.Build.IDataBuilder)

When you customize a build script to handle different asset types or handle assets in a different way, you might need to customize the [Play Mode Scripts](xref:addressables-asset-settings) so that the Unity Editor can handle those assets in the same way during Play mode.

## Addtional resources

* [Start a build from a script](build-scripting-start-build.md)
* [Custom build scripting](build-scripting-custom.md)
* [Build while recompiling](build-scripting-recompiling.md)