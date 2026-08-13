using NUnit.Framework;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AddressablesPlayerBuildProcessorTests
{
    protected static string TestFolder => $"Assets/AddressablesPlayerBuildProcessor_Tests";
    protected static string ConfigFolder => TestFolder + "/Config";

    AddressableAssetSettings m_Settings;

    [SetUp]
    public void Setup()
    {
        DirectoryUtility.DeleteDirectory(TestFolder, false);
        Directory.CreateDirectory(ConfigFolder);
        m_Settings = AddressableAssetSettings.Create(ConfigFolder, "AddressableAssetSettings.Tests", true, true);
        CreateAddressablePrefab("test");
    }

    [TearDown]
    public void Teardown()
    {
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(m_Settings));
        Resources.UnloadAsset(m_Settings);
        DirectoryUtility.DeleteDirectory(TestFolder, false);
        AssetDatabase.Refresh();
    }

    AddressableAssetEntry CreateAddressablePrefab(string name, AddressableAssetGroup group = null)
    {
        string guid = CreateAsset(name);
        return MakeAddressable(guid, null, group);
    }

    static string CreateAsset(string name)
    {
        string assetPath = $"{TestFolder}/{name}.prefab";
        return CreateAsset(assetPath, name);
    }

    static string CreateAsset(string assetPath, string objectName)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = objectName;
        //this is to ensure that bundles are different for every run.
        go.transform.localPosition = UnityEngine.Random.onUnitSphere;
        PrefabUtility.SaveAsPrefabAsset(go, assetPath);
        UnityEngine.Object.DestroyImmediate(go, false);
        return AssetDatabase.AssetPathToGUID(assetPath);
    }

    AddressableAssetEntry MakeAddressable(string guid, string address = null, AddressableAssetGroup group = null)
    {
        if (group == null)
        {
            if (m_Settings.DefaultGroup == null)
                throw new System.Exception("No DefaultGroup to assign Addressable to");
            group = m_Settings.DefaultGroup;
        }

        var entry = m_Settings.CreateOrMoveEntry(guid, group, false, false);
        entry.address = address == null ? entry.AssetPath : address;
        return entry;
    }


    [Test]
    [TestCase(AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer, false)]
    [TestCase(AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer, false)]
    [TestCase(AddressableAssetSettings.PlayerBuildOption.PreferencesValue, false)]
    [TestCase(AddressableAssetSettings.PlayerBuildOption.PreferencesValue, true)]
    public void PrepareAddressableBuildForPlayerBuild_ShouldBuildAddressables_CorrectForSettings(int settingValue, bool preferencesValue)
    {
        // Setup
        m_Settings.BuildAddressablesWithPlayerBuild = (AddressableAssetSettings.PlayerBuildOption)settingValue;
        bool deleteKey = !EditorPrefs.HasKey(AddressablesPreferences.kBuildAddressablesWithPlayerBuildKey);
        bool previousPrefValue = EditorPrefs.GetBool(AddressablesPreferences.kBuildAddressablesWithPlayerBuildKey, true);
        EditorPrefs.SetBool(AddressablesPreferences.kBuildAddressablesWithPlayerBuildKey, preferencesValue);

        try
        {
            bool result = AddressablesPlayerBuildProcessor.ShouldBuildAddressablesForPlayerBuild(m_Settings);
            if (m_Settings.BuildAddressablesWithPlayerBuild == AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer)
                Assert.IsTrue(result, "Addressables build was expected to set to build when preparing a Player Build");
            else if (m_Settings.BuildAddressablesWithPlayerBuild == AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer)
                Assert.IsFalse(result, "Addressables build is not expected to set be build when preparing a Player Build");
            else if (m_Settings.BuildAddressablesWithPlayerBuild == AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer)
            {
                if (preferencesValue)
                    Assert.IsTrue(result, "Addressables build was expected to set to build when preparing a Player Build");
                else
                    Assert.IsFalse(result, "Addressables build is not expected to set be build when preparing a Player Build");
            }
        }
        catch (Exception e)
        {
            Assert.Fail("Unhandled Exception in Preparing for Addressables - With Exception: " + e);
        }
        finally
        {
            if (deleteKey)
                EditorPrefs.DeleteKey(AddressablesPreferences.kBuildAddressablesWithPlayerBuildKey);
            else
                EditorPrefs.SetBool(AddressablesPreferences.kBuildAddressablesWithPlayerBuildKey, previousPrefValue);
        }
    }

    [Test]
    public void PrepareAddressableBuildForPlayerBuild_LinkXML_CopiedCorrectly()
    {
        string buildPath = Addressables.BuildPath + "/AddressablesLink/link.xml";
        Directory.CreateDirectory(Addressables.BuildPath + "/AddressablesLink");
        bool preexistingFile = File.Exists(buildPath);

        if (!preexistingFile)
        {
            var textStream = File.CreateText(buildPath);
            textStream.Write("link test file");
            textStream.Close();
        }

        // do the test
        string projectPath = Path.Combine(m_Settings.ConfigFolder, "link.xml");
        try
        {
            AddressablesPlayerBuildProcessor.PrepareForPlayerbuild(m_Settings, null, false);
            Assert.IsTrue(File.Exists(projectPath), "Link.xml file not found at project path when preparing for build");
        }
        // clean up
        finally
        {
            AddressablesPlayerBuildProcessor.RemovePlayerBuildLinkXML(m_Settings);
            if (!preexistingFile)
                File.Delete(buildPath);
            Assert.IsFalse(File.Exists(projectPath), "Link.xml file remains at the ProjectPath, link.xml expected to be deleted");
        }
    }

    [Test]
    public void PrepareForPlayerbuild_WhenBuildFails_ThrowsBuildFailedException()
    {
        // Save original override
        var originalOverride = AddressablesPlayerBuildProcessor.BuildAddressablesOverride;

        try
        {
            // Set up override to return a failed build result
            string expectedError = "Test build error message";
            AddressablesPlayerBuildProcessor.BuildAddressablesOverride = (settings) =>
            {
                var result = new AddressablesPlayerBuildResult();
                result.Error = expectedError;
                return result;
            };

            // Verify that BuildFailedException is thrown with the correct message
            var exception = Assert.Throws<BuildFailedException>(() =>
            {
                AddressablesPlayerBuildProcessor.PrepareForPlayerbuild(m_Settings, null, true);
            });

            Assert.IsNotNull(exception);
            StringAssert.Contains(expectedError, exception.Message,
                "Exception message should contain the build error");
            StringAssert.Contains("Failed to build Addressables content", exception.Message,
                "Exception message should indicate Addressables build failure");
        }
        finally
        {
            // Restore original override
            AddressablesPlayerBuildProcessor.BuildAddressablesOverride = originalOverride;
        }
    }

    [Test]
    public void PrepareForPlayerbuild_WhenBuildSucceeds_DoesNotThrow()
    {
        // Save original override
        var originalOverride = AddressablesPlayerBuildProcessor.BuildAddressablesOverride;

        try
        {
            // Set up override to return a successful build result
            AddressablesPlayerBuildProcessor.BuildAddressablesOverride = (settings) =>
            {
                var result = new AddressablesPlayerBuildResult();
                result.Error = null; // No error = success
                return result;
            };

            // Verify that no exception is thrown
            Assert.DoesNotThrow(() =>
            {
                AddressablesPlayerBuildProcessor.PrepareForPlayerbuild(m_Settings, null, true);
            });
        }
        finally
        {
            // Restore original override
            AddressablesPlayerBuildProcessor.BuildAddressablesOverride = originalOverride;
        }
    }

    [Test]
    public void PrepareForPlayerbuild_WhenBuildAddressablesIsFalse_DoesNotBuild()
    {
        // Save original override
        var originalOverride = AddressablesPlayerBuildProcessor.BuildAddressablesOverride;
        bool buildWasCalled = false;

        try
        {
            // Set up override to track if it was called
            AddressablesPlayerBuildProcessor.BuildAddressablesOverride = (settings) =>
            {
                buildWasCalled = true;
                return new AddressablesPlayerBuildResult();
            };

            // Call with buildAddressables = false
            AddressablesPlayerBuildProcessor.PrepareForPlayerbuild(m_Settings, null, false);

            // Verify that the build override was not called
            Assert.IsFalse(buildWasCalled, "Build should not be called when buildAddressables is false");
        }
        finally
        {
            // Restore original override
            AddressablesPlayerBuildProcessor.BuildAddressablesOverride = originalOverride;
        }
    }
}
