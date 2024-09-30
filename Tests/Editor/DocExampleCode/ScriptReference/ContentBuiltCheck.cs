#region CONTENT_BUILT_CHECK
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ContentBuiltCheck : IPreprocessBuildWithReport
{
    public int callbackOrder => 1;

    public void OnPreprocessBuild(BuildReport report)
    {
        // we don't want to throw the exception in our continuous integration environment
        if (Application.isBatchMode)
        {
            return;
        }
        var settingsPath = Addressables.BuildPath + "/settings.json";
        if (!File.Exists(settingsPath))
        {
            throw new System.Exception("Player content has not been built. Aborting build until content is built. This can be done from the Addressables window in the Build->Build Player Content menu command.");
        }
    }
}
#endif
#endregion
