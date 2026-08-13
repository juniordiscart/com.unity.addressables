using UnityEditor;

namespace UnityEditor.AddressableAssets.Build
{
    /// <summary>
    /// Default implementation of IBuildSettingsProvider that reads from EditorUserBuildSettings.
    /// </summary>
    public class EditorBuildSettingsProvider : IBuildSettingsProvider
    {
        /// <summary>
        /// Default instance that reads from EditorUserBuildSettings.
        /// </summary>
        public static readonly EditorBuildSettingsProvider Default = new EditorBuildSettingsProvider();

        /// <inheritdoc />
        public BuildTarget activeBuildTarget => EditorUserBuildSettings.activeBuildTarget;

        /// <inheritdoc />
        public bool development => EditorUserBuildSettings.development;

        /// <inheritdoc />
        public string[] extraScriptingDefines => EditorUserBuildSettings.activeScriptCompilationDefines;
    }
}
