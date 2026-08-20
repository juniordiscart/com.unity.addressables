using System;
using System.Collections;
using System.IO;
using System.Numerics;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;
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
#if ENABLE_CONTENT_DIRECTORIES
        "Editor/Build/DataBuilders/BuildScriptSchemaDriven.cs",
#endif
        "Editor/Build/CatalogBuilders/JsonCatalogBuilder.cs",
        "Editor/Build/CatalogBuilders/BinaryCatalogBuilder.cs",
        "Runtime/ResourceProviders/JsonCatalogProvider.cs",
        "Runtime/ResourceProviders/BinaryCatalogProvider.cs",
        "Runtime/ResourceManager/ResourceProviders/InstanceProvider.cs",
        "Editor/Build/DataBuilders/SchemaBuilders/BundledAssetSchemaBuilder.cs",
        "Samples/CustomBuildAndPlaymodeScripts/Editor/CustomBuildScript.cs",
        "Samples/CustomBuildAndPlaymodeScripts/Editor/CustomPlayModeScript.cs",
    };

    /// <summary>
    /// A named group of build script files that must compile together.
    /// </summary>
    public class BuildScriptGroupData
    {
        /// <summary>
        /// The name of the build script group, used as the test case name.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The package-relative file paths of the scripts in the group.
        /// </summary>
        public readonly string[] Files;

        /// <summary>
        /// Creates a new group of build script files.
        /// </summary>
        /// <param name="name">The name of the group.</param>
        /// <param name="files">The package-relative file paths of the scripts in the group.</param>
        public BuildScriptGroupData(string name, string[] files)
        {
            Name = name;
            Files = files;
        }

        /// <summary>
        /// Returns the group name.
        /// </summary>
        /// <returns>The name of the group.</returns>
        public override string ToString() => Name;
    }


#if ENABLE_CONTENT_DIRECTORIES
    private static BuildScriptGroupData[] BuildScriptGroups =
    {
        new BuildScriptGroupData("ContentDirectorySchemaBuilder", new[]
        {
            "Editor/Build/DataBuilders/SchemaBuilders/ContentDirectorySchemaBuilder.cs",
            "Editor/Build/DataBuilders/SchemaBuilders/ContentDirectoryArchiver.cs",
            "Editor/Build/DataBuilders/SchemaBuilders/WebGLContentDirectoryManifest.cs",
        })
    };

#endif

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
        content = StripContentDirectoriesGuard(content);
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

#if ENABLE_CONTENT_DIRECTORIES
    /// <summary>
    /// Verify that a group of related build scripts compile together without using internal APIs
    /// </summary>
    /// <param name="buildScriptPaths">File paths of the build scripts to compile as a group</param>
    /// <returns>IEnumerator for async test</returns>
    [UnityTest]
    public IEnumerator Verify_BuildScriptGroup_HasNoInternalApis([ValueSource(nameof(BuildScriptGroups))] BuildScriptGroupData group)
    {
        foreach (var buildScriptPath in group.Files)
        {
            var fullPath = String.Join($"{Path.DirectorySeparatorChar}", new[] { m_PackagePath, buildScriptPath });
            fullPath = fullPath.Replace("Samples", m_SamplePath);
            var content = File.ReadAllText(fullPath);
            content = StripContentDirectoriesGuard(content);
            content = content.Replace("namespace UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders", "namespace TestBuildScriptNamespace");
            content = content.Replace("namespace UnityEditor.AddressableAssets.Build.DataBuilders", "namespace TestBuildScriptNamespace");
            content = "using UnityEditor; // added by unit test\n" + content;
            content = "using UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders; // added by unit test\n" + content;
            content = "using UnityEditor.AddressableAssets.Build.DataBuilders; // added by unit test\n" + content;
            content = "using UnityEditor.AddressableAssets.Build; // added by unit test\n" + content;
            content = "using UnityEditor.AddressableAssets; // added by unit test\n" + content;

            var testFilePath = String.Join($"{Path.DirectorySeparatorChar}", new[] { m_FolderPath, Path.GetFileName(buildScriptPath) });
            Debug.Log(testFilePath);

            File.WriteAllText(testFilePath, content);
        }

        AssetDatabase.Refresh();
        yield return new WaitForDomainReload();

        LogAssert.NoUnexpectedReceived();
    }
#endif

    // Content Directory source files are wrapped in #if ENABLE_CONTENT_DIRECTORIES.
    // That symbol is not defined for the predefined Assembly-CSharp-Editor assembly
    // the copied test files land in, so without stripping the guard the body would
    // be preprocessed out and the compile check would pass vacuously.
    private static string StripContentDirectoriesGuard(string content)
    {
        string trimmed = content.TrimStart();
        if (!trimmed.StartsWith("#if ENABLE_CONTENT_DIRECTORIES"))
            return content;

        int ifIndex = content.IndexOf("#if ENABLE_CONTENT_DIRECTORIES", StringComparison.Ordinal);
        int lineEnd = content.IndexOf('\n', ifIndex);
        content = content.Substring(lineEnd + 1);

        int endifIndex = content.LastIndexOf("#endif", StringComparison.Ordinal);
        if (endifIndex >= 0)
            content = content.Substring(0, endifIndex);

        return content;
    }
}
