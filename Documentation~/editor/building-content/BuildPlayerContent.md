---
uid: addressables-api-build-player-content
---

# Build scripting

There are a few ways in which you can use the Addressables API to customize your project build:

* Start a build from a script
* Override an existing script
* Extend [BuildScriptBase] or implement [IDataBuilder]

When you customize a build script to handle different asset types or handle assets in a different way, you might also need to customize the [Play Mode Scripts] so that the Editor can handle those assets in the same way during Play mode.

### Starting an Addressables build from a script

To start an Addressables build from another script, call the [AddressableAssetSettings.BuildPlayerContent] method.

Before starting the build, you should set the active [Profile] and the active build script. You can also set a different [AddressableAssetSettings] object than the default, if desired. 

There are a few pieces of information that BuildPlayerContent takes into consideration when performing the build: the [AddressableAssetSettingsDefaultObject], [ActivePlayerDataBuilder], and the `addressables_content_state.bin` file.

#### Set the AddressableAssetSettings

The settings defined by [AddressableAssetSettings] include the list of groups and the profile to use.

To access the settings that you see in the Editor (menu: __Window > Asset Management > Addressables > Settings__), use the static [AddressableAssetSettingsDefaultObject.Settings] property. However, if desired, you can use a different settings object for a build.

To load a custom settings object in a build:

[!code-cs[sample](../../../Tests/Editor/DocExampleCode/BuildLauncher.cs#getSettingsObject)]

<!--
```csharp
static void getSettingsObject(string settingsAsset)
{
    // This step is optional, you can also use the default settings:
    //settings = AddressableAssetSettingsDefaultObject.Settings;

    settings 
        = AssetDatabase.LoadAssetAtPath<ScriptableObject>(settingsAsset) 
            as AddressableAssetSettings;

    if (settings == null)
        Debug.LogError($"{settingsAsset} couldn't be loaded.");
}
```
 -->

 #### Set the active Profile

A build started with BuildContent uses the variable settings of the active Profile. To set the active Profile as part of your customized build script, assign the ID of the desired profile to the [activeProfileId] field of the [AddressableAssetSettingsDefaultObject.Settings] object.

The [AddressableAssetSettings] object contains the list of profiles. Use the name of the desired profile to look up its ID value and then assign the ID to the [activeProfileId] variable:

[!code-cs[sample](../../../Tests/Editor/DocExampleCode/BuildLauncher.cs#setProfile)]

<!--
```csharp
static void setProfile(string profile)
{
    AddressableAssetSettings settings 
        = AddressableAssetSettingsDefaultObject.Settings;
        
    string profileId = settings.profileSettings.GetProfileId(profile);

    if (String.IsNullOrEmpty(profileId))
        Debug.LogWarning($"Couldn't find a profile named, {profile}.");
    else
        settings.activeProfileId = profileId;
}
```
-->

#### Set the active build script

The BuildContent method launches the build based on the current [ActivePlayerDataBuilder] setting. To use a specific build script, assign the index of the IDataBuilder object in the [AddressableAssetSetting.DataBuilders] list to the [ActivePlayerDataBuilderIndex] property.

The build script must be a ScriptableObject that implements [IDataBuilder] and you must add it to the [DataBuilders] list in the [AddressableAssetSettings] instance. Once added to the list, use the standard [List.IndexOf] method to get the index of the object.

[!code-cs[sample](../../../Tests/Editor/DocExampleCode/BuildLauncher.cs#setBuilder)]

<!--
```csharp
static void setBuilder(IDataBuilder builder)
{ 
    AddressableAssetSettings settings 
        = AddressableAssetSettingsDefaultObject.Settings;
    
    int index = settings.DataBuilders.IndexOf((ScriptableObject) builder);

    if (index > 0)
        settings.ActivePlayerDataBuilderIndex = index;
    else
        Debug.LogWarning($"{builder} must be added to the " +
                         $"DataBuilders list before it can be made " +
                         $"active. Using last run builder instead.");
}
```
--> 

#### Launch a build

After setting the profile and builder to use (if desired), you can launch the build:

[!code-cs[sample](../../../Tests/Editor/DocExampleCode/BuildLauncher.cs#buildAddressableContent)]

<!--
```csharp
    static bool buildAddressableContent()
    {
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        bool success = string.IsNullOrEmpty(result.Error);

        if(!success)
        {
            Debug.LogError("Addressables build error encountered: " + result.Error);
        }
        return success;
    }
```
-->

To check for success, use BuildPlayerContent(out AddressablesPlayerBuildResult result). result.Error contains any error message returned if the Addressables build failed. If string.IsNullOrEmpty(result.Error) is true, the build was successful.

#### Example script to launch build

The following example adds a couple of menu commands to the Asset Management > Addressables  menu in the Editor. The first command builds the Addressable content using the preset profile and build script. The second command builds the Addressable content, and, if it succeeds, builds the Player, too.

Note that if your build script makes setting changes that require a domain reload, you should run the build script using Unity command line options, instead of running it interactively in the Editor. See [Domain reloads and Addressable builds] for more information.

[!code-cs[sample](../../../Tests/Editor/DocExampleCode/BuildLauncher.cs#doc_BuildLauncher)]

<!--
```csharp
#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using System;
using UnityEngine;

public class BuildLauncher
{
    public static string build_script = "Assets/AddressableAssetsData/DataBuilders/BuildScriptPackedMode.asset";
    public static string settings_asset = "Assets/AddressableAssetsData/AddressableAssetSettings.asset";
    public static string profile_name = "Default";
    private static AddressableAssetSettings settings;

    static void getSettingsObject(string settingsAsset)
    {
        // This step is optional, you can also use the default settings:
        //settings = AddressableAssetSettingsDefaultObject.Settings;

        settings 
            = AssetDatabase.LoadAssetAtPath<ScriptableObject>(settingsAsset) 
                as AddressableAssetSettings;

        if (settings == null)
            Debug.LogError($"{settingsAsset} couldn't be found or isn't a settings object.");
    }

    static void setProfile(string profile)
    {
        string profileId = settings.profileSettings.GetProfileId(profile);
        if (String.IsNullOrEmpty(profileId))
            Debug.LogWarning($"Couldn't find a profile named, {profile}, using current profile instead.");
        else
            settings.activeProfileId = profileId;
    }

    static void setBuilder(IDataBuilder builder)
    {
        int index = settings.DataBuilders.IndexOf((ScriptableObject) builder);

        if (index > 0)
            settings.ActivePlayerDataBuilderIndex = index;
        else
            Debug.LogWarning($"{builder} must be added to the " +
                             $"DataBuilders list before it can be made " +
                             $"active. Using last run builder instead.");
    }

    static bool buildAddressableContent()
    {
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        bool success = string.IsNullOrEmpty(result.Error);

        if(!success)
        {
            Debug.LogError("Addressables build error encountered: " + result.Error);
        }
        return success;
    }

    [MenuItem("Window/Asset Management/Addressables/Build Addressables only")]
    public static bool BuildAddressables()
    {
        getSettingsObject(settings_asset);
        setProfile(profile_name);
        IDataBuilder builderScript 
            = AssetDatabase.LoadAssetAtPath<ScriptableObject>(build_script) as IDataBuilder;

        if (builderScript == null)
        {
            Debug.LogError(build_script + " couldn't be found or isn't a build script.");
            return false;
        }
            
        setBuilder(builderScript);

        return buildAddressableContent();
    }

    [MenuItem("Window/Asset Management/Addressables/Build Addressables and Player")]
    public static void BuildAddressablesAndPlayer()
    {
        bool contentBuildSucceeded = BuildAddressables();

        if (contentBuildSucceeded)
        {
            BuildPlayerOptions playerSettings 
                = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(new BuildPlayerOptions());

            BuildPipeline.BuildPlayer(playerSettings);
        }
    }
}
#endif
```
-->

#### Domain reloads and Addressables builds

If your scripted build process involves changing settings that trigger a domain reload before it makes an Addressables build, then you should script such builds to use [Unity's command line arguments] rather than interactively running a script in the Editor. These types of settings include:

* Changing the defined compiler symbols
* Changing platform target or target group

When you run a script that triggers a domain reload interactively in the Editor (using a menu command, for example), your Editor script finishes executing before the domain reload occurs. Thus, if you immediately start an Addressables build, both your code and imported assets are still in their original state. You must wait for the domain reload to complete before you start the content build.

Waiting for the domain reload to finish is relatively straightforward when you run the build from the command line, but can be difficult or impossible to accomplish reliably in an interactive script (for a variety of reasons). 

The following example script defines two functions that can be invoked when running Unity on the command line. The `ChangeSettings` example sets the specified define symbols. The `BuildContentAndPlayer` function runs the Addressables build and the Player build.

[!code-cs[sample](../../../Tests/Editor/DocExampleCode/BatchBuild.cs#doc_BatchBuild)]

<!--
```csharp
#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BatchBuild
{
    public static string build_script = "Assets/AddressableAssetsData/DataBuilders/BuildScriptPackedMode.asset";
    public static string profile_name = "Default";
    public static void ChangeSettings()
    {
        string defines = "";
        string[] args = Environment.GetCommandLineArgs();

        foreach (var arg in args)
            if (arg.StartsWith("-defines=", System.StringComparison.CurrentCulture))
                defines = arg.Substring(("-defines=".Length));

        PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, defines);
    }

    public static void BuildContentAndPlayer()
    {
        AddressableAssetSettings settings 
            = AddressableAssetSettingsDefaultObject.Settings;

        settings.activeProfileId 
            = settings.profileSettings.GetProfileId(profile_name);

        IDataBuilder builder 
            = AssetDatabase.LoadAssetAtPath<ScriptableObject>(build_script) as IDataBuilder; 

        settings.ActivePlayerDataBuilderIndex 
            = settings.DataBuilders.IndexOf((ScriptableObject) builder);

        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);

        if(!string.IsNullOrEmpty(result.Error))
            throw new Exception(result.Error);

        BuildReport buildReport 
            = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, 
            "d:/build/winApp.exe",EditorUserBuildSettings.activeBuildTarget, 
            BuildOptions.None);

        if (buildReport.summary.result != BuildResult.Succeeded)
            throw new Exception(buildReport.summary.ToString());
    }
}
#endif
```
-->

To call these functions, use [Unity's command line arguments] in a terminal or command prompt or in a shell script:

```
D:\Unity\2020.3.0f1\Editor\Unity.exe -quit -batchMode -projectPath . -executeMethod BatchBuild.ChangeSettings -defines=FOO;BAR -buildTarget Android
D:\Unity\2020.3.0f1\Editor\Unity.exe -quit -batchMode -projectPath . -executeMethod BatchBuild.BuildContentAndPlayer -buildTarget Android
```

> [!NOTE]
> If you specify the platform target as a command line parameter, you can perform an Addressables build in the same command. However, if you wanted to change the platform in a script, you should do it in a separate command, such as the `ChangeSettings` function in this example.

<!-- Ideally we would have more examples for build scripting in the following sections -->

### Custom Build Scripting

To configure a new custom script, add it to the [Build and Play Mode Scripts] list.

Custom scripts extend the [BuildScriptBase] class or implement the [IDataBuilder] interface. There are several overridable methods, such as `ClearCachedData` and `CanBuildData<T>`. If extending the [BuildScriptBase] class, the most notable method to override is `BuildDataImplementation<TResult>`. This is the method that is used to setup or build content. 

A custom script is either a Build Script or a Play Mode Script. This is determined by how the `CanBuildData<T>` method is implemented. Build scripts can only build data of the type `AddressablesPlayerBuildResult`, so the method is implemented in this way:

[!code-cs[sample](../../../Tests/Editor/DocExampleCode/CustomBuildScript.cs#doc_CustomBuildScript)]
<!--
```csharp
public override bool CanBuildData<T>()
{
    return typeof(T).IsAssignableFrom(typeof(AddressablesPlayerBuildResult));
}
```
-->
This allows the script to be listed in the **Build/New Build/** menu.

Play Mode Scripts can only build data of the type `AddressablesPlayModeBuildResult`, so the method is implemented in this way:

[!code-cs[sample](../../../Tests/Editor/DocExampleCode/CustomBuildScript.cs#doc_CustomPlayModeScript)]
<!--
```csharp
public override bool CanBuildData<T>()
{
    return typeof(T).IsAssignableFrom(typeof(AddressablesPlayModeBuildResult));
}
```
-->
This allows the script to be listed in the **Play Mode Scripts** menu.

See the [Custom Build and Playmode Scripts Sample] for an example.

#### Extend the default build script

If you want to use the same basic build as the default build script [BuildScriptPackedMode], but want to treat specific groups or types of assets differently, you can extend the default build script and override the functions within it. If the group or asset the build script is processing is one that you want to treat differently, you can run your own code, otherwise you can call the base class version of the function to use the default algorithm. 

See the [Addressable variants project] for an example.

#### Save the content state

If you support [remote content distribution] and update your content between player releases, you must record the state of your Addressables groups at the time of the build. Recording the state allows you to perform a differential build using the [Update a Previous Build] script.

See the implementation of [BuildScriptPackedMode] and [ContentUpdateScript], for details.

<a name="faq-build-while-compiling"></a>
### Can I build Addressables when recompiling scripts?
If you have a pre-build step that triggers a domain reload, then you must take special care that the Addressables build itself does not start until after the domain reload is finished.

Using methods such as setting scripting define symbols ([PlayerSettings.SetScriptingDefineSymbolsForGroup](https://docs.unity3d.com/ScriptReference/PlayerSettings.SetScriptingDefineSymbolsForGroup.html)) or switching active build target ([EditorUserBuildSettings.SwitchActiveBuildTarget](https://docs.unity3d.com/ScriptReference/EditorUserBuildSettings.SwitchActiveBuildTarget.html)), triggers scripts to recompile and reload. The execution of the Editor code will continue with the currently loaded domain until the domain reloads and execution stops. Any [platform dependent compilation](https://docs.unity3d.com/Manual/PlatformDependentCompilation.html) or custom defines will not be set until after the domain reloads. This can lead to unexpected issues where code relies on these defines to build correctly, and can be easily missed.

#### Is there a safe way to change scripts before building?
To switch Platform, or modify Editor scripts in code and then continue with the defines set, a domain reload must be performed. Note in this case, -quit argument should not be used or the Editor will exit immediately after execution of the invoked method.

When the domain reloads, InitializeOnLoad is invoked. The code below demonstrates how to set scripting define symbols and react to those in the Editor code, building Addressables after the domain reload completes. The same process can be done for switching platforms and [platform dependent compilation](https://docs.unity3d.com/Manual/PlatformDependentCompilation.html).

```
[InitializeOnLoad]
public class BuildWithScriptingDefinesExample
{
    static BuildWithScriptingDefinesExample()
    {
        bool toBuild = SessionState.GetBool("BuildAddressables", false);
        SessionState.EraseBool("BuildAddressables");
        if (toBuild)
        {
            Debug.Log("Domain reload complete, building Addressables as requested");
            BuildAddressablesAndRevertDefines();
        }
    }

    [MenuItem("Build/Addressables with script define")]
    public static void BuildTest()
    {
#if !MYDEFINEHERE
        Debug.Log("Setting up SessionState to inform an Addressables build is requested on next Domain Reload");
        SessionState.SetBool("BuildAddressables", true);
        string originalDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        string newDefines = string.IsNullOrEmpty(originalDefines) ? "MYDEFINEHERE" : originalDefines + ";MYDEFINEHERE";
        Debug.Log("Setting Scripting Defines, this will then start compiling and begin a domain reload of the Editor Scripts.");
        PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, newDefines);
#endif
    }

    static void BuildAddressablesAndRevertDefines()
    {
#if MYDEFINEHERE
        Debug.Log("Correct scripting defines set for desired build");
        AddressableAssetSettings.BuildPlayerContent();
        string originalDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        if (originalDefines.Contains(";MYDEFINEHERE"))
            originalDefines = originalDefines.Replace(";MYDEFINEHERE", "");
        else
            originalDefines = originalDefines.Replace("MYDEFINEHERE", "");
        PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, originalDefines);
        AssetDatabase.SaveAssets();
#endif
        EditorApplication.Exit(0);
    }
}
``` 

[Addressable variants project]: https://github.com/Unity-Technologies/Addressables-Sample/tree/master/Advanced/Addressable%20Variants
[ActivePlayerDataBuilder]: xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ActivePlayerDataBuilder
[activeProfileId]: xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.activeProfileId
[AddressableAssetSetting.DataBuilders]: xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.DataBuilders
[AddressableAssetSettings.BuildPlayerContent]: xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent*
[AddressableAssetSettings]: xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings
[AddressableAssetSettingsDefaultObject]: xref:UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject
[AddressableAssetSettingsDefaultObject.Settings]: xref:UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings
[BuildScriptBase]: xref:UnityEditor.AddressableAssets.Build.DataBuilders.BuildScriptBase
[BuildScriptPackedMode]: xref:UnityEditor.AddressableAssets.Build.DataBuilders.BuildScriptPackedMode
[Build and Play Mode Scripts]: xref:addressables-asset-settings#build-and-play-mode-scripts
[ContentUpdateScript]: xref:UnityEditor.AddressableAssets.Build.ContentUpdateScript
[Custom Build and Playmode Scripts Sample]: xref:samples-custom-build-and-playmode-scripts
[DataBuilders]: xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.DataBuilders
[Domain reloads and Addressable builds]: #domain-reloads-and-addressables-builds
[IDataBuilder]: xref:UnityEditor.AddressableAssets.Build.IDataBuilder
[List.IndexOf]: xref:System.Collections.Generic.List`1.IndexOf*
[Profile]: xref:addressables-profiles
[remote content distribution]: xref:addressables-remote-content-distribution
[Unity's command line arguments]: xref:CommandLineArguments
[Update a Previous Build]: xref:addressables-content-update-builds#building-content-updates
