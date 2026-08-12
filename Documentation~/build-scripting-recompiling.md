# Handle domain reloads

If your scripted build process changes settings that trigger a domain reload before making a build, use [Unity's command line arguments](xref:um-command-line-arguments) rather than running a script in the Unity Editor. These types of settings include:

* Changing the [scripting define symbols](xref:um-custom-scripting-symbols).
* Changing platform target or target group.

Using methods such as setting scripting define symbols with [`PlayerSettings.SetScriptingDefineSymbolsForGroup`](https://docs.unity3d.com/ScriptReference/PlayerSettings.SetScriptingDefineSymbolsForGroup.html), or switching the active build target with [`EditorUserBuildSettings.SwitchActiveBuildTarget`](https://docs.unity3d.com/ScriptReference/EditorUserBuildSettings.SwitchActiveBuildTarget.html), triggers scripts to recompile and reload. The execution of the Unity Editor code continues with the currently loaded domain until the domain reloads and execution stops. Any [platform dependent compilation](https://docs.unity3d.com/Manual/PlatformDependentCompilation.html) or custom defines isn't set until after the domain reloads. This can lead to unexpected issues where code relies on these defines to build correctly, and can be easily missed.

When you run a script that triggers a domain reload interactively in the Editor, such as using a menu command, your Editor script finishes executing before the domain reload happens. Therefore, if you immediately start an Addressables build, both your code and imported assets are still in their original state. You must wait for the domain reload to complete before you start the content build.

It's best practice to wait for the domain reload to finish when you run the build from the command line, because it can be difficult or impossible to carry out reliably in an interactive script.

The following example script defines two functions that can be invoked when running Unity on the command line. The `ChangeSettings` example sets the specified define symbols. The `BuildContentAndPlayer` function runs the Addressables build and the Player build.

[!code-cs[sample](../Tests/Editor/DocExampleCode/BatchBuild.cs#doc_BatchBuild)]

To call these functions, use [Unity's command line arguments](xref:um-command-line-arguments) in a terminal or command prompt or in a shell script:

```
D:\Unity\2020.3.0f1\Editor\Unity.exe -quit -batchMode -projectPath . -executeMethod BatchBuild.ChangeSettings -defines=FOO;BAR -buildTarget Android
D:\Unity\2020.3.0f1\Editor\Unity.exe -quit -batchMode -projectPath . -executeMethod BatchBuild.BuildContentAndPlayer -buildTarget Android
```

> [!NOTE]
> If you specify the platform target as a command line parameter, you can perform an Addressables build in the same command. However, if you wanted to change the platform in a script, you should do it in a separate command, such as the `ChangeSettings` function in this example.

## Change scripts before building

To change platform, or modify Editor scripts in code and then continue with the defines set, a domain reload must be performed. In this case, the `-quit` argument should not be used or the Editor will exit immediately after execution of the invoked method.

When the domain reloads, `InitializeOnLoad` is invoked. The code below demonstrates how to set scripting define symbols and react to those in the Editor code, building Addressables after the domain reload completes. The same process can be done for switching platforms and [platform dependent compilation](https://docs.unity3d.com/Manual/PlatformDependentCompilation.html).

```c#
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

## Additional resources

* [Create a custom build script](build-scripting-custom.md)
* [Start a build from a script](build-scripting-start-build.md)
* [Command line arguments reference](xref:um-command-line-arguments)