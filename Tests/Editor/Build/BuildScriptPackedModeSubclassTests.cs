using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Tests
{
    /// <summary>
    /// Verifies <see cref="AllHooksLoggingPackedMode"/> matches stock <see cref="BuildScriptPackedMode"/> for editor builds.
    /// </summary>
    /// <remarks>
    /// Uses persisted settings and a fresh <see cref="AddressablesDataBuilderInput"/> per test, matching
    /// <see cref="BuildScriptPackedTests.CatalogBuiltWithDifferentGroupOrder_AreEqualWhenOrderEnabled"/>.
    /// This class does not inherit <see cref="BuildScriptPackedTests"/> so NUnit does not discover every packed-mode test
    /// twice under this fixture (which would run them against this type's asset folder and break log expectations).
    /// </remarks>
    public sealed class BuildScriptPackedModeSubclassTests : AddressableAssetTestBase
    {
        protected override bool PersistSettings => false;

        AddressableAssetSettings m_PersistedSettings;
        AddressablesDataBuilderInput m_BuilderInput;

        protected new AddressableAssetSettings Settings =>
            m_PersistedSettings != null ? m_PersistedSettings : base.Settings;

        [SetUp]
        public void PerTestSetup()
        {
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
                UnityEngine.Object.DestroyImmediate(m_PersistedSettings, true);
                m_PersistedSettings = null;
            }
        }

        static IEnumerable<object[]> BuildScriptFactoryCases()
        {
            yield return new object[]
            {
                "Stock",
                (Func<BuildScriptPackedMode>)(() => ScriptableObject.CreateInstance<BuildScriptPackedMode>()),
                false
            };
            yield return new object[]
            {
                "AllHooks",
                (Func<BuildScriptPackedMode>)(() => ScriptableObject.CreateInstance<AllHooksLoggingPackedMode>()),
                true
            };
        }

        [Test, TestCaseSource(nameof(BuildScriptFactoryCases))]
        public void BuildData_ProducesCatalog_AndSucceeds(string label, Func<BuildScriptPackedMode> factory, bool expectHookInvocations)
        {
            if (expectHookInvocations)
                AllHooksLoggingPackedMode.ClearInvocationRecord();

            AddressableAssetGroup group1 = Settings.CreateGroup("SubclassPackedGroup1", false, false, false,
                new List<AddressableAssetGroupSchema>(), typeof(BundledAssetGroupSchema));
            Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(Path.Combine(TestFolder, "test 1.prefab")),
                group1, false, false);
            AddressableAssetGroup group2 = Settings.CreateGroup("SubclassPackedGroup2", false, false, false,
                new List<AddressableAssetGroupSchema>(), typeof(BundledAssetGroupSchema));
            Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(Path.Combine(TestFolder, "test 2.prefab")),
                group2, false, false);

            var buildScript = factory();
            try
            {
                var result = buildScript.BuildData<AddressableAssetBuildResult>(m_BuilderInput);
                Assert.IsTrue(string.IsNullOrEmpty(result.Error), $"Build failed ({label}): {result.Error}");

                string catalogPath = result.FileRegistry.GetFilePathForBundle("catalog");
                Assert.IsFalse(string.IsNullOrEmpty(catalogPath), $"Catalog path missing ({label})");
                string catalogJson = File.ReadAllText(catalogPath);
                Assert.IsFalse(string.IsNullOrEmpty(catalogJson), $"Catalog empty ({label})");

                if (expectHookInvocations)
                {
                    Assert.That(AllHooksLoggingPackedMode.InvokedHooks, Does.Contain("CreateSchemaDrivenBuildScript"));
                    Assert.That(AllHooksLoggingPackedMode.InvokedHooks, Does.Contain("BuildDataImplementation"));
                    Assert.That(AllHooksLoggingPackedMode.InvokedHooks, Does.Contain("DoBuild"));
                    Assert.That(AllHooksLoggingPackedMode.InvokedHooks, Does.Contain("ProcessAllGroups"));
                }
            }
            finally
            {
                Settings.RemoveGroup(group1);
                Settings.RemoveGroup(group2);
                UnityEngine.Object.DestroyImmediate(buildScript);
            }
        }

        [Test]
        public void AllHooksSubclassBuild_CatalogMatchesStockPackedMode()
        {
            AddressableAssetGroup group1 = Settings.CreateGroup("ParityPackedGroup1", false, false, false,
                new List<AddressableAssetGroupSchema>(), typeof(BundledAssetGroupSchema));
            Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(Path.Combine(TestFolder, "test 1.prefab")),
                group1, false, false);
            AddressableAssetGroup group2 = Settings.CreateGroup("ParityPackedGroup2", false, false, false,
                new List<AddressableAssetGroupSchema>(), typeof(BundledAssetGroupSchema));
            Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(Path.Combine(TestFolder, "test 2.prefab")),
                group2, false, false);

            string catalogStock;
            var stock = ScriptableObject.CreateInstance<BuildScriptPackedMode>();
            try
            {
                var r1 = stock.BuildData<AddressableAssetBuildResult>(m_BuilderInput);
                Assert.IsTrue(string.IsNullOrEmpty(r1.Error), r1.Error);
                string p1 = r1.FileRegistry.GetFilePathForBundle("catalog");
                catalogStock = File.ReadAllText(p1);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stock);
            }

            string catalogAllHooks;
            var allHooks = ScriptableObject.CreateInstance<AllHooksLoggingPackedMode>();
            try
            {
                var r2 = allHooks.BuildData<AddressableAssetBuildResult>(m_BuilderInput);
                Assert.IsTrue(string.IsNullOrEmpty(r2.Error), r2.Error);
                string p2 = r2.FileRegistry.GetFilePathForBundle("catalog");
                catalogAllHooks = File.ReadAllText(p2);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(allHooks);
            }

            Settings.RemoveGroup(group1);
            Settings.RemoveGroup(group2);

            Assert.AreEqual(catalogStock.GetHashCode(), catalogAllHooks.GetHashCode(),
                "Catalog hash should match stock BuildScriptPackedMode vs AllHooksLoggingPackedMode for identical settings.");
        }
    }
}
