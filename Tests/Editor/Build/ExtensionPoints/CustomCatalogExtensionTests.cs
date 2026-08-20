using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets.Tests;
using UnityEngine;

namespace ThirdParty.AddressablesExtensions.Tests
{
    /// <summary>
    /// Verifies that a custom ICatalogBuilder defined outside the Addressables namespace can be
    /// wired into a BuildScriptSchemaDriven subclass via the CreateCatalogBuilder extension point,
    /// and that GenerateCatalog is called during a real Addressables build.
    /// </summary>
    [TestFixture]
    public class CustomCatalogExtensionTests : AddressableAssetTestBase
    {
        protected override bool PersistSettings => false;

        AddressableAssetSettings m_PersistedSettings;
        AddressablesDataBuilderInput m_BuilderInput;

        protected new AddressableAssetSettings Settings =>
            m_PersistedSettings != null ? m_PersistedSettings : base.Settings;

        [SetUp]
        public void PerTestSetup()
        {
            MyTestCatalogBuilder.ClearCallRecord();
            CustomCatalogBuildScript.ClearCallRecord();
            CustomCatalogBundledAssetSchemaBuilder.ClearCallRecord();
            using (new IgnoreFailingLogMessage())
            {
                m_PersistedSettings = AddressableAssetSettings.Create(ConfigFolder, k_TestConfigName, true, true);
                m_BuilderInput = new AddressablesDataBuilderInput(Settings);
            }
        }

        [TearDown]
        public void PerTestTearDown()
        {
            m_BuilderInput = null;
            if (m_PersistedSettings != null)
            {
                Object.DestroyImmediate(m_PersistedSettings, true);
                m_PersistedSettings = null;
            }
        }

        [Test]
        public void Build_WithCustomCatalogBuilder_CallsCatalogBuilderMethods()
        {
            AddressableAssetGroup group = Settings.CreateGroup("ThirdPartyCatalogGroup", false, false, false,
                new List<AddressableAssetGroupSchema>(), typeof(BundledAssetGroupSchema));
            Settings.CreateOrMoveEntry(
                AssetDatabase.AssetPathToGUID(Path.Combine(TestFolder, "test 1.prefab")),
                group, false, false);

            var buildScript = ScriptableObject.CreateInstance<CustomCatalogBuildScript>();
            try
            {
                var result = buildScript.BuildData<AddressableAssetBuildResult>(m_BuilderInput);
                Assert.IsTrue(string.IsNullOrEmpty(result.Error), $"Build failed: {result.Error}");

                Assert.GreaterOrEqual(CustomCatalogBuildScript.CreateCatalogBuilderCallCount, 1,
                    "CreateCatalogBuilder was not called on the build script — the virtual seam was not exercised");
                Assert.GreaterOrEqual(MyTestCatalogBuilder.GenerateCatalogCallCount, 1,
                    "GenerateCatalog was not called on the custom ICatalogBuilder");
            }
            finally
            {
                Settings.RemoveGroup(group);
                Object.DestroyImmediate(buildScript);
            }
        }
    }
}
