using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Tests
{
    public class AddressablesDataBuilderInputTests : AddressableAssetTestBase
    {
        protected override bool PersistSettings
        {
            get { return false; }
        }

        [Test]
        public void Constructor_WithSettings_UsesBuildSettingsProvider()
        {
            // Create a test provider with known values
            var testProvider = new TestBuildSettingsProvider
            {
                activeBuildTarget = BuildTarget.StandaloneWindows64,
                development = true,
                extraScriptingDefines = new[] { "TEST_DEFINE_1", "TEST_DEFINE_2" }
            };

            // Create input with settings and test provider
            var input = new AddressablesDataBuilderInput(Settings, testProvider);

            // Verify it uses the test provider values
            Assert.AreEqual(testProvider.development, input.DevelopmentBuild);
            Assert.AreEqual(testProvider.activeBuildTarget, input.Target);
            CollectionAssert.AreEquivalent(
                testProvider.extraScriptingDefines,
                input.ExtraScriptingDefines);
        }

        [Test]
        public void Constructor_WithBuildPlayerOptions_UsesBuildPlayerOptions()
        {
            var buildPlayerOptions = new BuildPlayerOptions
            {
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
                extraScriptingDefines = new[] { "TEST_DEFINE_FROM_OPTIONS" }
            };

            var input = new AddressablesDataBuilderInput(Settings, buildPlayerOptions);

            Assert.IsTrue(input.DevelopmentBuild, "DevelopmentBuild should be true when BuildOptions.Development is set");
            Assert.IsNotNull(input.ExtraScriptingDefines, "ExtraScriptingDefines should not be null");
            CollectionAssert.Contains(input.ExtraScriptingDefines, "TEST_DEFINE_FROM_OPTIONS",
                "ExtraScriptingDefines should contain the define from BuildPlayerOptions");
        }

        [Test]
        public void Constructor_WithBuildPlayerOptions_NonDevelopment_UsesBuildPlayerOptions()
        {
            var buildPlayerOptions = new BuildPlayerOptions
            {
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
                extraScriptingDefines = new[] { "NON_DEV_DEFINE" }
            };

            var input = new AddressablesDataBuilderInput(Settings, buildPlayerOptions);

            Assert.IsFalse(input.DevelopmentBuild, "DevelopmentBuild should be false when BuildOptions.Development is not set");
            Assert.IsNotNull(input.ExtraScriptingDefines, "ExtraScriptingDefines should not be null");
            CollectionAssert.Contains(input.ExtraScriptingDefines, "NON_DEV_DEFINE",
                "ExtraScriptingDefines should contain the define from BuildPlayerOptions");
        }

        [Test]
        public void Constructor_WithBuildPlayerOptions_EmptyExtraDefines_HandlesNull()
        {
            var buildPlayerOptions = new BuildPlayerOptions
            {
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
                extraScriptingDefines = null
            };

            var input = new AddressablesDataBuilderInput(Settings, buildPlayerOptions);

            Assert.IsFalse(input.DevelopmentBuild);
            Assert.IsNull(input.ExtraScriptingDefines, "ExtraScriptingDefines should be null when BuildPlayerOptions has null extraScriptingDefines");
        }

        [Test]
        public void Constructor_WithPlayerBuildVersion_UsesBuildSettingsProvider()
        {
            // Create a test provider with known values
            var testProvider = new TestBuildSettingsProvider
            {
                activeBuildTarget = BuildTarget.StandaloneWindows64,
                development = false,
                extraScriptingDefines = new[] { "VERSION_TEST_DEFINE" }
            };

            // Create input with playerBuildVersion and test provider
            var input = new AddressablesDataBuilderInput(Settings, "1.0.0", testProvider);

            // Verify it uses the test provider values
            Assert.AreEqual(testProvider.development, input.DevelopmentBuild);
            Assert.AreEqual(testProvider.activeBuildTarget, input.Target);
            CollectionAssert.AreEquivalent(
                testProvider.extraScriptingDefines,
                input.ExtraScriptingDefines);
            Assert.AreEqual("1.0.0", input.PlayerVersion);
        }

        [Test]
        public void Constructor_WithSettings_DefaultsToEditorBuildSettingsProvider()
        {
            // This test verifies that when no provider is specified, it uses EditorBuildSettingsProvider.Default
            // We can't fully test this without manipulating globals, but we can verify the constructor accepts null
            var input = new AddressablesDataBuilderInput(Settings);

            // Verify it was created successfully and has reasonable values
            Assert.IsNotNull(input);
            Assert.IsNotNull(input.AddressableSettings);
        }
    }
}
