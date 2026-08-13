using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class BuildScriptPackedIntegrationTests
{
    string CreateTexture(string path)
    {
        var data = ImageConversion.EncodeToPNG(new Texture2D(32, 32));
        File.WriteAllBytes(path, data);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        return AssetDatabase.AssetPathToGUID(path);
    }

    static IDataBuilder GetBuilderOfType(AddressableAssetSettings settings, Type modeType)
    {
        foreach (var db in settings.DataBuilders)
        {
            var b = db;
            if (b.GetType() == modeType)
                return b as IDataBuilder;
        }

        throw new Exception("DataBuilder not found");
    }

    protected string m_SingleTestBuildFolder;
    protected string m_SingleTestAssetFolder;

    void DeleteSingleTestDirectories()
    {
        if (Directory.Exists(m_SingleTestBuildFolder))
            Directory.Delete(m_SingleTestBuildFolder, true);

        if (AssetDatabase.IsValidFolder(m_SingleTestAssetFolder))
            AssetDatabase.DeleteAsset(m_SingleTestAssetFolder);

        // Clear the build cache to prevent stale asset references
        BuildCache.PurgeCache(false);
    }

    string m_SettingsPath;
    AddressableAssetSettings m_Settings;

    AddressableAssetSettings Settings
    {
        get
        {
            if (m_Settings == null)
                m_Settings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(m_SettingsPath);
            return m_Settings;
        }
    }

    [SetUp]
    public void Setup()
    {
        m_SingleTestBuildFolder = "Temp/TestBuild";
        m_SingleTestAssetFolder = "Assets/SingleTestFolder";
        m_Settings = null; // Reset cached settings to ensure we load fresh settings
        DeleteSingleTestDirectories();
        Directory.CreateDirectory(m_SingleTestBuildFolder);
        Directory.CreateDirectory(m_SingleTestAssetFolder);
        AddressableAssetSettings settings = AddressableAssetSettings.Create(Path.Combine(m_SingleTestAssetFolder, "Settings"), "AddressableAssetSettings.Tests", false, true);
        m_SettingsPath = settings.AssetPath;
    }

    [TearDown]
    public void TearDown()
    {
        DeleteSingleTestDirectories();
    }

    [Test]
    public void IncrementalBuild_WhenBundleTimestampUnchanged_DoesNotCopy()
    {
        AddressableAssetBuildResult result;
        var group = Settings.CreateGroup("MyTestGroup", true, false, false, null, typeof(BundledAssetGroupSchema));
        string localBuildPath = Settings.profileSettings.GetValueByName(Settings.activeProfileId, AddressableAssetSettings.kLocalBuildPath);
        string localLoadPath = Settings.profileSettings.GetValueByName(Settings.activeProfileId, AddressableAssetSettings.kLocalLoadPath);
        try
        {
            var spriteEntry = Settings.CreateOrMoveEntry(CreateTexture($"{m_SingleTestAssetFolder}/testTexture.png"), group, false, false);
            Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kLocalBuildPath, m_SingleTestBuildFolder);
            Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kLocalLoadPath, "Library/LocalLoadPath");

            IDataBuilder b = GetBuilderOfType(Settings, typeof(BuildScriptPackedMode));
            b.BuildData<AddressableAssetBuildResult>(new AddressablesDataBuilderInput(Settings));

            string[] buildFiles = Directory.GetFiles(m_SingleTestBuildFolder);

            // Build again with a lock on the output bundle. This is how we ensure that the bundle is not written again
            using (File.Open(buildFiles[0], FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                result = b.BuildData<AddressableAssetBuildResult>(new AddressablesDataBuilderInput(Settings));

            Assert.AreEqual(1, buildFiles.Length, "There should only be one bundle file in the build output folder");
            Assert.IsTrue(string.IsNullOrEmpty(result.Error));
        }
        finally
        {
            Settings.RemoveGroup(group);
            Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kLocalBuildPath, localBuildPath);
            Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kLocalLoadPath, localLoadPath);
        }
    }

    [Test]
    public void IncrementalBuild_WhenBundleTimestampChanges_CopiesNewFile()
    {
        var group = Settings.CreateGroup("MyTestGroup", true, false, false, null, typeof(BundledAssetGroupSchema));
        string localBuildPath = Settings.profileSettings.GetValueByName(Settings.activeProfileId, AddressableAssetSettings.kLocalBuildPath);
        string localLoadPath = Settings.profileSettings.GetValueByName(Settings.activeProfileId, AddressableAssetSettings.kLocalLoadPath);
        try
        {
            var spriteEntry = Settings.CreateOrMoveEntry(CreateTexture($"{m_SingleTestAssetFolder}/testTexture.png"), group, false, false);
            Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kLocalBuildPath, m_SingleTestBuildFolder);
            Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kLocalLoadPath, "Library/LocalLoadPath");

            IDataBuilder b = GetBuilderOfType(Settings, typeof(BuildScriptPackedMode));
            b.BuildData<AddressableAssetBuildResult>(new AddressablesDataBuilderInput(Settings));

            string[] buildFiles = Directory.GetFiles(m_SingleTestBuildFolder);

            byte[] initialBundleBytes = File.ReadAllBytes(buildFiles[0]);
            File.Delete(buildFiles[0]);
            File.WriteAllText(buildFiles[0], "content");
            File.SetLastWriteTime(buildFiles[0], new DateTime(2019, 1, 1));

            b.BuildData<AddressableAssetBuildResult>(new AddressablesDataBuilderInput(Settings));

            Assert.AreEqual(1, buildFiles.Length, "There should only be one bundle file in the build output folder");
            CollectionAssert.AreEqual(initialBundleBytes, File.ReadAllBytes(buildFiles[0]));
        }
        finally
        {
            Settings.RemoveGroup(group);
            Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kLocalBuildPath, localBuildPath);
            Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kLocalLoadPath, localLoadPath);
        }
    }

    [Test]
    [TestCase(BundledAssetGroupSchema.BundleNamingStyle.AppendHash)]
    [TestCase(BundledAssetGroupSchema.BundleNamingStyle.NoHash)]
    [TestCase(BundledAssetGroupSchema.BundleNamingStyle.OnlyHash)]
    [TestCase(BundledAssetGroupSchema.BundleNamingStyle.FileNameHash)]
    public void UniqueBundleIds_PackedTogether_CanBuildSceneAndAssetsBundle(BundledAssetGroupSchema.BundleNamingStyle bundleNaming)
    {
        // Create group
        string groupName = "UniqueBundleIdsTests";
        var group = Settings.CreateGroup(groupName, true, false, false, null, typeof(BundledAssetGroupSchema));
        var schema = group.GetSchema<BundledAssetGroupSchema>();
        schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
        schema.BundleNaming = bundleNaming;
        schema.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalBuildPath);
        schema.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);

        string localBuildPath = Settings.profileSettings.GetValueByName(Settings.activeProfileId, AddressableAssetSettings.kLocalBuildPath);
        string localLoadPath = Settings.profileSettings.GetValueByName(Settings.activeProfileId, AddressableAssetSettings.kLocalLoadPath);

        Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kLocalBuildPath, m_SingleTestBuildFolder);
        Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kLocalLoadPath, "Library/LocalLoadPath");

        // Create prefab
        string prefabPath = $"{m_SingleTestAssetFolder}/TestPrefab.prefab";
        GameObject prefabObj = new GameObject("TestPrefab");
        PrefabUtility.SaveAsPrefabAsset(prefabObj, prefabPath);
        UnityEngine.Object.DestroyImmediate(prefabObj);
        AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        string prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath);

        // Create scene
        string scenePath = $"{m_SingleTestAssetFolder}/TestScene.unity";
        var testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        EditorSceneManager.SaveScene(testScene, scenePath);
        AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);

        // Add both to the same group
        var prefabEntry = Settings.CreateOrMoveEntry(prefabGuid, group, false, false);
        prefabEntry.address = "test_prefab";

        var sceneEntry = Settings.CreateOrMoveEntry(sceneGuid, group, false, false);
        sceneEntry.address = "test_scene";

        var uniqueBundleIds = Settings.UniqueBundleIds;
        Settings.UniqueBundleIds = true;
        try
        {
            IDataBuilder builder = GetBuilderOfType(Settings, typeof(BuildScriptPackedMode));
            AddressableAssetBuildResult result = builder.BuildData<AddressableAssetBuildResult>(
                new AddressablesDataBuilderInput(Settings));

            bool hasAssetsBundle = false;
            bool hasSceneBundle = false;
            int numBundles = 0;
            foreach (var path in result.FileRegistry.GetFilePaths())
            {
                if (path.Contains(groupName.ToLower()))
                {
                    if (path.Contains("_assets_"))
                        hasAssetsBundle = true;
                    else if (path.Contains("_scenes_"))
                        hasSceneBundle = true;
                }

                if (path.EndsWith(".bundle"))
                    ++numBundles;
            }

            Assert.AreEqual(3, numBundles); // built in bundle, assets, scenes

            if (bundleNaming == BundledAssetGroupSchema.BundleNamingStyle.AppendHash ||
                bundleNaming == BundledAssetGroupSchema.BundleNamingStyle.NoHash)
            {
                Assert.IsTrue(hasAssetsBundle, "The bundle containing the prefab asset is misnamed.");
                Assert.IsTrue(hasSceneBundle, "The bundle containing the scene asset is misnamed.");
            }
        }
        finally
        {
            // Create a new empty scene before closing the test scene
            // Unity doesn't allow closing the last loaded scene, so we need another scene first
            // This prevents "scene changed on disk" dialogs when running multiple test cases
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Settings.UniqueBundleIds = uniqueBundleIds;
            Settings.RemoveGroup(group);
            Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kLocalBuildPath, localBuildPath);
            Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kLocalLoadPath, localLoadPath);
        }
    }

    [Test]
    public void IncrementalBuild_WithDevelopmentBuildAndExtraDefines_CompilesWithCorrectSettings()
    {
        var group = Settings.CreateGroup("MyTestGroup", true, false, false, null, typeof(BundledAssetGroupSchema));

        var spriteEntry = Settings.CreateOrMoveEntry(CreateTexture($"{m_SingleTestAssetFolder}/testTexture.png"), group, false, false);
        Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kLocalBuildPath, m_SingleTestBuildFolder);
        Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kLocalLoadPath, "Library/LocalLoadPath");

        IDataBuilder b = GetBuilderOfType(Settings, typeof(BuildScriptPackedMode));

        // Create builder input with development build and extra scripting defines
        var builderInput = new AddressablesDataBuilderInput(Settings);
        builderInput.DevelopmentBuild = true;
        builderInput.ExtraScriptingDefines = new[] { "TEST_BUILD_DEFINE" };

        var result = b.BuildData<AddressableAssetBuildResult>(builderInput);

        Assert.IsTrue(string.IsNullOrEmpty(result.Error), $"Build should succeed but got error: {result.Error}");
        Assert.IsNotNull(result, "Build result should not be null");

        string[] buildFiles = Directory.GetFiles(m_SingleTestBuildFolder);
        Assert.Greater(buildFiles.Length, 0, "Build should produce at least one output file");
    }

    [Test]
    public void IncrementalBuild_WithBuildPlayerOptions_UsesBuildPlayerOptions()
    {
        var group = Settings.CreateGroup("MyTestGroup", true, false, false, null, typeof(BundledAssetGroupSchema));

        var spriteEntry = Settings.CreateOrMoveEntry(CreateTexture($"{m_SingleTestAssetFolder}/testTexture.png"), group, false, false);
        Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kLocalBuildPath, m_SingleTestBuildFolder);
        Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kLocalLoadPath, "Library/LocalLoadPath");

        IDataBuilder b = GetBuilderOfType(Settings, typeof(BuildScriptPackedMode));

        // Create BuildPlayerOptions with development build and extra defines
        var buildPlayerOptions = new BuildPlayerOptions
        {
            target = EditorUserBuildSettings.activeBuildTarget,
            options = BuildOptions.Development,
            extraScriptingDefines = new[] { "INTEGRATION_TEST_DEFINE" }
        };

        // Create builder input using BuildPlayerOptions constructor
        var builderInput = new AddressablesDataBuilderInput(Settings, buildPlayerOptions);

        var result = b.BuildData<AddressableAssetBuildResult>(builderInput);

        Assert.IsTrue(string.IsNullOrEmpty(result.Error), $"Build should succeed but got error: {result.Error}");
        Assert.IsNotNull(result, "Build result should not be null");
        Assert.IsTrue(builderInput.DevelopmentBuild, "DevelopmentBuild should be true from BuildPlayerOptions");
        Assert.IsNotNull(builderInput.ExtraScriptingDefines, "ExtraScriptingDefines should not be null");
        CollectionAssert.Contains(builderInput.ExtraScriptingDefines, "INTEGRATION_TEST_DEFINE",
            "ExtraScriptingDefines should contain the define from BuildPlayerOptions");

        string[] buildFiles = Directory.GetFiles(m_SingleTestBuildFolder);
        Assert.Greater(buildFiles.Length, 0, "Build should produce at least one output file");
    }
}
