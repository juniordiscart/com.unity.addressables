# Build while recompiling

If you have a pre-build step that triggers a domain reload, then you must take special care that the Addressables build itself does not start until after the domain reload is finished.

Using methods such as setting scripting define symbols with [`PlayerSettings.SetScriptingDefineSymbolsForGroup`](https://docs.unity3d.com/ScriptReference/PlayerSettings.SetScriptingDefineSymbolsForGroup.html), or switching the active build target with [`EditorUserBuildSettings.SwitchActiveBuildTarget`](https://docs.unity3d.com/ScriptReference/EditorUserBuildSettings.SwitchActiveBuildTarget.html), triggers scripts to recompile and reload. The execution of the Unity Editor code continues with the currently loaded domain until the domain reloads and execution stops. Any [platform dependent compilation](https://docs.unity3d.com/Manual/PlatformDependentCompilation.html) or custom defines isn't set until after the domain reloads. This can lead to unexpected issues where code relies on these defines to build correctly, and can be easily missed.

## Change scripts before building

To switch platform, or modify Editor scripts in code and then continue with the defines set, a domain reload must be performed. In this case, the `-quit` argument should not be used or the Editor will exit immediately after execution of the invoked method.

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
