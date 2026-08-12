# Start a build from a script

To start a build from another script, call the [`AddressableAssetSettings.BuildPlayerContent`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent*) method.

Before starting the build, set the active [Profile](AddressableAssetsProfiles.md) and the active build script. You can also set a different [`AddressableAssetSettings`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings) object than the default.

`BuildPlayerContent` takes into consideration the following information when performing the build:

* [`AddressableAssetSettingsDefaultObject`](xref:UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject)
* [`ActivePlayerDataBuilder`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ActivePlayerDataBuilder)
* The `addressables_content_state.bin` file.

## Set the AddressableAssetSettings

The settings defined by [`AddressableAssetSettings`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings) include the list of [groups](groups-intro.md) and the [profile](profiles-introduction.md) to use.

To access the settings displayed in the Unity Editor (menu: __Window > Asset Management > Addressables > Settings__), use the static [`AddressableAssetSettingsDefaultObject.Settings`](xref:UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings) property. However, if desired, you can use a different settings object for a build.

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

To check for success, use `BuildPlayerContent(out AddressablesPlayerBuildResult result)`. `result.Error` contains any error message returned if the Addressables build failed. If `string.IsNullOrEmpty(result.Error)` is true, the build was successful.

### Example script to launch build

The following example adds two menu commands to the **Window > Asset Management > Addressables** menu in the Editor. The first command builds the Addressable content using the preset profile and build script. The second command builds the Addressable content, and if it succeeds, builds the Player.

[!code-cs[sample](../Tests/Editor/DocExampleCode/BuildLauncher.cs#doc_BuildLauncher)]

If your build script makes setting changes that require a domain reload, you should run the build script using Unity command line options, instead of running it interactively in the Editor. Refer to [Handle domain reloads](build-scripting-recompiling.md) for more information.

## Additional resources

* [`AddressableAssetSettings.BuildPlayerContent` API reference](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent*)
* [Handle domain reloads](build-scripting-recompiling.md)
* [`AddressableAssetSettings` API reference](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings)