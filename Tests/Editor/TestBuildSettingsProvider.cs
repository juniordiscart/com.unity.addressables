using UnityEditor;
using UnityEditor.AddressableAssets.Build;

namespace UnityEditor.AddressableAssets.Tests
{
    /// <summary>
    /// Test implementation of IBuildSettingsProvider that allows setting values for testing.
    /// </summary>
    public class TestBuildSettingsProvider : IBuildSettingsProvider
    {
        /// <summary>
        /// Gets or sets the active build target.
        /// </summary>
        public BuildTarget activeBuildTarget { get; set; }

        /// <summary>
        /// Gets or sets whether development build is enabled.
        /// </summary>
        public bool development { get; set; }

        /// <summary>
        /// Gets or sets the active script compilation defines.
        /// </summary>
        public string[] extraScriptingDefines { get; set; }

        /// <summary>
        /// Creates a new TestBuildSettingsProvider with default values.
        /// </summary>
        public TestBuildSettingsProvider()
        {
            activeBuildTarget = BuildTarget.StandaloneWindows64;
            development = false;
            extraScriptingDefines = new string[0];
        }

        /// <summary>
        /// Creates a new TestBuildSettingsProvider with specified values.
        /// </summary>
        public TestBuildSettingsProvider(BuildTarget target, bool devBuild, string[] defines)
        {
            activeBuildTarget = target;
            development = devBuild;
            extraScriptingDefines = defines ?? new string[0];
        }
    }
}
