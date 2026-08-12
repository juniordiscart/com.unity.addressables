---
uid: addressables-api-build-player-content
---

# Build Addressable assets from scripts

You can use the `Addressables` API to start a build from a script, override an existing script, extend [`BuildScriptBase`](xref:UnityEditor.AddressableAssets.Build.DataBuilders.BuildScriptBase), or implement [`IDataBuilder`](xref:UnityEditor.AddressableAssets.Build.IDataBuilder).

|**Topic**|**Description**|
|---|---|
|**[Create a custom build script](build-scripting-custom.md)**|Create custom build scripts to define custom build behavior and handle specific groups or asset types differently.|
|**[Start a build from a script](build-scripting-start-build.md)**|Start a build with the `BuildPlayerContent` API, including setting profiles, build scripts, and handling build results.|
|**[Handle domain reloads](build-scripting-recompiling.md)**|Manage domain reloads that happen when changing compiler symbols or platform targets during scripted builds.|

## Additional resources

* [Create a content-only build](builds-full-build.md)
* [Update builds](ContentUpdateWorkflow.md)
* [Referencing Addressable assets in code](AssetReferences.md)