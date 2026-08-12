# Create a custom build script

To create a custom build script, you can create a class that extends the [`BuildScriptBase`](xref:UnityEditor.AddressableAssets.Build.DataBuilders.BuildScriptBase) class, or implements the [`IDataBuilder`](xref:UnityEditor.AddressableAssets.Build.IDataBuilder) interface.

You can then add the script to the [Build and Play Mode Scripts](AddressableAssetSettings.md#build-and-play-mode-scripts) section of the Addressable Asset Settings Inspector.

## Implementing build scripts

You can override the `ClearCachedData` and `CanBuildData<T>` methods in `BuildScriptBase` and `IDataBuilder` to define how to create an Addressables build. If you're extending the `BuildScriptBase` class, you can also override the [`BuildDataImplementation`](xref:UnityEditor.AddressableAssets.Build.DataBuilders.BuildScriptBase.BuildDataImplementation*) method to setup or build content.

You must define a custom script as either a Build Script or a Play Mode Script with the `CanBuildData<T>` method. Build scripts can only build data of the type `AddressablesPlayerBuildResult`, so you must implement it like the following example:

[!code-cs[sample](../Tests/Editor/DocExampleCode/CustomBuildScript.cs#doc_CustomBuildScript)]

When you implement a script in this way, it appears in the **Build** menu of the [Addressables Groups window](GroupsWindow.md).

Play Mode Scripts can also only build data of the type `AddressablesPlayModeBuildResult`. The following is an example of how to implement this method:

[!code-cs[sample](../Tests/Editor/DocExampleCode/CustomBuildScript.cs#doc_CustomPlayModeScript)]

When you implement a script in this way, it appears in the **Play Mode Scripts** menu of the [Addressables Groups window](GroupsWindow.md).

For further examples, refer to the [Custom build and Play mode scripts sample](SamplesOverview.md).

## Extend the default build script

You can extend and override the default build script [`BuildScriptPackedMode`](xref:UnityEditor.AddressableAssets.Build.DataBuilders.BuildScriptPackedMode) to treat specific groups or types of assets differently. If the group or asset the build script processes is one that you want to treat differently, you can run your own code. Otherwise, you can call the base class version of the method to use the default algorithm.

## Save the content state

If you support [remote content distribution](xref:addressables-remote-content-distribution) and update your content between player releases, you must record the state of your Addressables groups at the time of the build. Recording the state lets you perform a differential build using the [Update a Previous Build](xref:addressables-content-update-builds) script.

For more information, refer to the implementation of [`BuildScriptPackedMode`](xref:UnityEditor.AddressableAssets.Build.DataBuilders.BuildScriptPackedMode) and [`ContentUpdateScript`](xref:UnityEditor.AddressableAssets.Build.ContentUpdateScript).

## Additional resources

* [`BuildScriptBase` API reference](xref:UnityEditor.AddressableAssets.Build.DataBuilders.BuildScriptBase)
* [`IDataBuilder` API reference](xref:UnityEditor.AddressableAssets.Build.IDataBuilder)
* [Build and Play Mode Scripts reference](AddressableAssetSettings.md#build-and-play-mode-scripts)
* [Addressables Groups window reference](GroupsWindow.md)
* [Start a build from a script](build-scripting-start-build.md)
