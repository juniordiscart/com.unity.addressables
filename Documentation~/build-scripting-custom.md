# Custom build scripting

To configure a new custom script, add it to the [Build and Play Mode Scripts](xref:addressables-asset-settings) list.

Custom scripts extend the [`BuildScriptBase`](xref:UnityEditor.AddressableAssets.Build.DataBuilders.BuildScriptBase) class or implement the [`IDataBuilder`](xref:UnityEditor.AddressableAssets.Build.IDataBuilder) interface. There are several methods that you can override, such as `ClearCachedData` and `CanBuildData<T>`. If extending the `BuildScriptBase` class, the most notable method to override is `BuildDataImplementation<TResult>`. This is the method that's used to setup or build content.

A custom script is either a Build Script or a Play Mode Script. This is determined by how the `CanBuildData<T>` method is implemented. Build scripts can only build data of the type `AddressablesPlayerBuildResult`, so the method is implemented in this way:

[!code-cs[sample](../Tests/Editor/DocExampleCode/CustomBuildScript.cs#doc_CustomBuildScript)]

This allows the script to be listed in the **Build** menu.

Play Mode Scripts can only build data of the type `AddressablesPlayModeBuildResult`, so the method is implemented in this way:

[!code-cs[sample](../Tests/Editor/DocExampleCode/CustomBuildScript.cs#doc_CustomPlayModeScript)]

This allows the script to be listed in the **Play Mode Scripts** menu.

See the [Custom Build and Play Mode Scripts Sample](SamplesOverview.md) for an example.

## Extend the default build script

If you want to use the same basic build as the default build script [`BuildScriptPackedMode`](xref:UnityEditor.AddressableAssets.Build.DataBuilders.BuildScriptPackedMode), but want to treat specific groups or types of assets differently, you can extend and override the default build script. If the group or asset the build script is processing is one that you want to treat differently, you can run your own code. Otherwise, you can call the base class version of the function to use the default algorithm.

Refer to the [Addressable variants project](https://github.com/Unity-Technologies/Addressables-Sample/tree/master/Advanced/Addressable%20Variants) for an example.

## Save the content state

If you support [remote content distribution](xref:addressables-remote-content-distribution) and update your content between player releases, you must record the state of your Addressables groups at the time of the build. Recording the state lets you perform a differential build using the [Update a Previous Build](xref:addressables-content-update-builds) script.

Refer to the implementation of [`BuildScriptPackedMode`](xref:UnityEditor.AddressableAssets.Build.DataBuilders.BuildScriptPackedMode) and [`ContentUpdateScript`](xref:UnityEditor.AddressableAssets.Build.ContentUpdateScript) for details.