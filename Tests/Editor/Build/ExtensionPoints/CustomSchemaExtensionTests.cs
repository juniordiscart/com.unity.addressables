using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets.Tests;
using UnityEngine;

namespace ThirdParty.AddressablesExtensions.Tests
{
    /// <summary>
    /// Verifies that a custom Schema + ISchemaBuilder pair defined outside the Addressables namespace
    /// can be wired into a BuildScriptSchemaDriven subclass via CreateSchemaBuilders and that all
    /// ISchemaBuilder methods are called during a real Addressables build.
    /// </summary>
    [TestFixture]
    public class CustomSchemaExtensionTests : AddressableAssetTestBase
    {
        protected override bool PersistSettings => false;

        AddressableAssetSettings m_PersistedSettings;
        AddressablesDataBuilderInput m_BuilderInput;

        protected new AddressableAssetSettings Settings =>
            m_PersistedSettings != null ? m_PersistedSettings : base.Settings;

        [SetUp]
        public void PerTestSetup()
        {
            MyTestSchemaBuilder.ClearInvocationRecord();
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
        public void Build_WithCustomSchemaBuilder_CallsAllSchemaBuilderMethods()
        {
            AddressableAssetGroup group = Settings.CreateGroup("ThirdPartySchemaGroup", false, false, false,
                new List<AddressableAssetGroupSchema>(), typeof(BundledAssetGroupSchema), typeof(MyTestSchema));
            Settings.CreateOrMoveEntry(
                AssetDatabase.AssetPathToGUID(Path.Combine(TestFolder, "test 1.prefab")),
                group, false, false);

            var buildScript = ScriptableObject.CreateInstance<CustomSchemaBuildScript>();
            try
            {
                var result = buildScript.BuildData<AddressableAssetBuildResult>(m_BuilderInput);
                Assert.IsTrue(string.IsNullOrEmpty(result.Error), $"Build failed: {result.Error}");

                // ISchemaBuilder extension methods — all must fire during the build
                Assert.That(MyTestSchemaBuilder.InvokedHooks, Does.Contain("Init"),
                    "ISchemaBuilder.Init was not called");
                Assert.That(MyTestSchemaBuilder.InvokedHooks, Does.Contain("ProcessGroupSchema"),
                    "ISchemaBuilder.ProcessGroupSchema was not called");
                Assert.That(MyTestSchemaBuilder.InvokedHooks, Does.Contain("Build"),
                    "ISchemaBuilder.Build was not called");
                Assert.That(MyTestSchemaBuilder.InvokedHooks, Does.Contain("GenerateCatalogLocations"),
                    "ISchemaBuilder.GenerateCatalogLocations was not called");
                Assert.That(MyTestSchemaBuilder.InvokedHooks, Does.Contain("GenerateTypeStrippingInfo"),
                    "ISchemaBuilder.GenerateTypeStrippingInfo was not called");
                Assert.That(MyTestSchemaBuilder.InvokedHooks, Does.Contain("GenerateContentUpdate"),
                    "ISchemaBuilder.GenerateContentUpdate was not called");
            }
            finally
            {
                Settings.RemoveGroup(group);
                Object.DestroyImmediate(buildScript);
            }
        }
    }
}
