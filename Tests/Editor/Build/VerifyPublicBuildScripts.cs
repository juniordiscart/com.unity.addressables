using System;
using System.Collections;
using System.IO;
using System.Numerics;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.TestTools;
using File = System.IO.File;
using Path = System.IO.Path;

/// <summary>
/// This test exists because we frequently suggest that our users extend or copy and paste the builder script
/// files to customize or make their own. We didn't check this on every release and so internal API usage
/// had crept in and made it impossible to do this without copying the entire package. This test verifies
/// that you can copy the script into your own namespace and it will compile.
/// </summary>
public class VerifyPublicBuildScripts
{
    private string m_PackagePath;
    private string m_SamplePath;
    private string m_FolderPath = $"Assets{Path.DirectorySeparatorChar}ScriptFolder/Editor";

    /// <summary>
    /// Test setup for validating build scripts
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        if(AssetDatabase.IsValidFolder(m_FolderPath))
        {
            AssetDatabase.DeleteAsset(m_FolderPath);
        }
        AssetDatabase.CreateFolder("Assets", "ScriptFolder");
        AssetDatabase.CreateFolder("Assets/ScriptFolder", "Editor");

        m_PackagePath = "Packages/com.unity.addressables";
        m_SamplePath = "Samples";
        if (Directory.Exists(String.Join($"{Path.DirectorySeparatorChar}", new [] {m_PackagePath, "Samples~"})))
        {
            // when packaging the samples are moved into a hidden directory
            m_SamplePath = "Samples~";
        }

        // this is a dependant class and copying is easier than an asmdef
        var loadScenePath = "Samples/CustomBuildAndPlaymodeScripts/LoadSceneForCustomBuild.cs";
        var fullPath = String.Join($"{Path.DirectorySeparatorChar}", new[] { m_PackagePath, loadScenePath });
        fullPath = fullPath.Replace("Samples", m_SamplePath);
        var testFilePath = String.Join($"{Path.DirectorySeparatorChar}", new[] { m_FolderPath, Path.GetFileName(loadScenePath) });
        File.Copy(fullPath, testFilePath);
    }

    /// <summary>
    /// Test tear down
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if(AssetDatabase.IsValidFolder(m_FolderPath))
        {
            AssetDatabase.DeleteAsset(m_FolderPath);
        }
    }

    private static string[] BuildScripts =
    {
        "Editor/Build/DataBuilders/BuildScriptFastMode.cs",
        "Editor/Build/DataBuilders/BuildScriptPackedMode.cs",
        "Editor/Build/DataBuilders/BuildScriptPackedPlayMode.cs",
        "Samples/CustomBuildAndPlaymodeScripts/Editor/CustomBuildScript.cs",
        "Samples/CustomBuildAndPlaymodeScripts/Editor/CustomPlayModeScript.cs",
    };

    /// <summary>
    /// Verify that the public build scripts aren't using internal APIs directly
    /// </summary>
    /// <param name="buildScriptPath">The filepath of the build script</param>
    /// <returns>IEnumerator for async test</returns>
    [UnityTest]
    public IEnumerator Verify_BuildScript_HasNoInternalApis([ValueSource(nameof(BuildScripts))] string buildScriptPath)
    {
        var fullPath = String.Join($"{Path.DirectorySeparatorChar}", new[] { m_PackagePath, buildScriptPath });
        fullPath = fullPath.Replace("Samples", m_SamplePath);
        var content = File.ReadAllText(fullPath);
        content = content.Replace("namespace UnityEditor.AddressableAssets.Build.DataBuilders", "namespace TestBuildScriptNamespace");
        // this is the using statement for the package the scripts are being copied from
        content = "using UnityEditor; // added by unit test\n" + content;
        content = "using UnityEditor.AddressableAssets.Build; // added by unit test\n" + content;
        content = "using UnityEditor.AddressableAssets.Build.DataBuilders; // added by unit test\n" + content;
        content = "using UnityEditor.AddressableAssets; // added by unit test\n" + content;
        // content = "compile error;" + content;

        var testFilePath = String.Join($"{Path.DirectorySeparatorChar}", new[] { m_FolderPath, Path.GetFileName(buildScriptPath) });
        Debug.Log(testFilePath);

        File.WriteAllText(testFilePath, content);
        AssetDatabase.Refresh();
        yield return new WaitForDomainReload();

        // assert we didn't get any log messages when compiling the test file
        LogAssert.NoUnexpectedReceived();
    }
}
