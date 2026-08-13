using UnityEditor;

namespace UnityEditor.AddressableAssets.Build
{
    /// <summary>
    /// Interface for providing build settings. Allows dependency injection for testability.
    /// </summary>
    public interface IBuildSettingsProvider
    {
        /// <summary>
        /// Gets the active build target.
        /// </summary>
        BuildTarget activeBuildTarget { get; }

        /// <summary>
        /// Gets whether development build is enabled.
        /// </summary>
        bool development { get; }

        /// <summary>
        /// Gets the script compilation defines.
        /// </summary>
        string[] extraScriptingDefines { get; }
    }
}
