# Start a build from a script

To start a build from another script, call the [`AddressableAssetSettings.BuildPlayerContent`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent*) method.

Before starting the build, set the active [Profile](AddressableAssetsProfiles.md) and the active build script. You can also set a different [`AddressableAssetSettings`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings) object than the default.

`BuildPlayerContent` takes into consideration the following information when performing the build:

* [`AddressableAssetSettingsDefaultObject`](xref:UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject)
* [`ActivePlayerDataBuilder`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ActivePlayerDataBuilder)
* The `addressables_content_state.bin` file.

## Set the AddressableAssetSettings

The settings defined by [`AddressableAssetSettings`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings) include the list of [groups](Groups.md) and the [profile](profiles-introduction.md) to use.

To access the settings that displayed in the Editor (menu: __Window > Asset Management > Addressables > Settings__), use the static [`AddressableAssetSettingsDefaultObject.Settings`](xref:UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings) property. However, if desired, you can use a different settings object for a build.

To load a custom settings object in a build:

[!code-cs[sample](../Tests/Editor/DocExampleCode/BuildLauncher.cs#getSettingsObject)]

## Set the active profile

A build started with `BuildContent` uses the variable settings of the active profile. To set the active profile as part of your customized build script, assign the ID of the desired profile to the [`activeProfileId`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.activeProfileId) field of the [`AddressableAssetSettingsDefaultObject.Settings`](xref:UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings) object.

The [`AddressableAssetSettings`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings) object contains the list of profiles. Use the name of the desired profile to look up its ID value and then assign the ID to the `activeProfileId` variable:

[!code-cs[sample](../Tests/Editor/DocExampleCode/BuildLauncher.cs#setProfile)]

## Set the active build script

The `BuildContent` method launches the build based on the current [`ActivePlayerDataBuilder`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ActivePlayerDataBuilder) setting. To use a specific build script, assign the index of the `IDataBuilder` object in the [`AddressableAssetSetting.DataBuilders`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.DataBuilders) list to the [`ActivePlayerDataBuilderIndex`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ActivePlayerDataBuilderIndex) property.

The build script must be a `ScriptableObject` that implements [`IDataBuilder`]((xref:UnityEditor.AddressableAssets.Build.IDataBuilder)) and you must add it to the [`DataBuilders`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.DataBuilders) list in the [`AddressableAssetSettings`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings) instance. Once added to the list, use the standard [`List.IndexOf`](xref:System.Collections.Generic.List`1.IndexOf*) method to get the index of the object.

[!code-cs[sample](../Tests/Editor/DocExampleCode/BuildLauncher.cs#setBuilder)]

## Launch a build

After setting the profile and builder to use, you can launch the build:

[!code-cs[sample](../Tests/Editor/DocExampleCode/BuildLauncher.cs#buildAddressableContent)]

To check for success, use `BuildPlayerContent(out AddressablesPlayerBuildResult result)`. `result.Error` contains any error message returned if the Addressables build failed. If s`tring.IsNullOrEmpty(result.Error)` is true, the build was successful.

### Example script to launch build

The following example adds a couple of menu commands to the **Window > Asset Management > Addressables** menu in the Editor. The first command builds the Addressable content using the preset profile and build script. The second command builds the Addressable content, and, if it succeeds, builds the Player.

If your build script makes setting changes that require a domain reload, you should run the build script using Unity command line options, instead of running it interactively in the Editor. Refer to [Domain reloads](#domain-reloads) for more information.

[!code-cs[sample](../Tests/Editor/DocExampleCode/BuildLauncher.cs#doc_BuildLauncher)]

## Domain reloads

If your scripted build process involves changing settings that trigger a domain reload before it makes an Addressables build, then you should script such builds to use [Unity's command line arguments](xref:CommandLineArguments) rather than running a script in the Editor. These types of settings include:

* Changing the defined compiler symbols
* Changing platform target or target group

When you run a script that triggers a domain reload interactively in the Editor, such as using a menu command, your Editor script finishes executing before the domain reload happens. Therefore, if you immediately start an Addressables build, both your code and imported assets are still in their original state. You must wait for the domain reload to complete before you start the content build.

It's best practice to wait for the domain reload to finish when you run the build from the command line, because it can be difficult or impossible to carry out reliably in an interactive script.

The following example script defines two functions that can be invoked when running Unity on the command line. The `ChangeSettings` example sets the specified define symbols. The `BuildContentAndPlayer` function runs the Addressables build and the Player build.

[!code-cs[sample](../Tests/Editor/DocExampleCode/BatchBuild.cs#doc_BatchBuild)]

To call these functions, use [Unity's command line arguments](xref:CommandLineArguments) in a terminal or command prompt or in a shell script:

```
D:\Unity\2020.3.0f1\Editor\Unity.exe -quit -batchMode -projectPath . -executeMethod BatchBuild.ChangeSettings -defines=FOO;BAR -buildTarget Android
D:\Unity\2020.3.0f1\Editor\Unity.exe -quit -batchMode -projectPath . -executeMethod BatchBuild.BuildContentAndPlayer -buildTarget Android
```

> [!NOTE]
> If you specify the platform target as a command line parameter, you can perform an Addressables build in the same command. However, if you wanted to change the platform in a script, you should do it in a separate command, such as the `ChangeSettings` function in this example.
