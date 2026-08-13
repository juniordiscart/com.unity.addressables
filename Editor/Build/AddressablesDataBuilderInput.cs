using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Build
{
    /// <summary>
    /// Data builder context object for Addressables.  This contains all of the necessary input data to a build.
    /// </summary>
    public class AddressablesDataBuilderInput
    {
        /// <summary>
        /// The main addressables settings object.
        /// </summary>
        public AddressableAssetSettings AddressableSettings { get; private set; }

        /// <summary>
        /// Toggle Development Build and DEVELOPMENT_BUILD define.
        /// </summary>
        public bool DevelopmentBuild { get; set; }

        /// <summary>
        /// Add extra scripting defines to the build.
        /// </summary>
        public string[] ExtraScriptingDefines { get; set; }

        /// <summary>
        /// Build target group.
        /// </summary>
        public BuildTargetGroup TargetGroup { get; private set; }

        /// <summary>
        /// Build target.
        /// </summary>
        public BuildTarget Target { get; private set; }

        /// <summary>
        /// Player build version.
        /// </summary>
        public string PlayerVersion { get; set; }

        /// <summary>
        /// Registry of files created during the build
        /// </summary>
        public FileRegistry Registry { get; private set; }

        /// <summary>
        /// can be used in testing to append a suffix to file paths
        /// </summary>
        public string PathSuffix = string.Empty;

        /// <summary>
        /// The name of the default Runtime Settings file.
        /// </summary>
        public string RuntimeSettingsFilename = "settings.json";

        /// <summary>
        /// The name of the default Runtime Catalog file.
        /// </summary>
        public string RuntimeCatalogFilename =
#if ENABLE_JSON_CATALOG
           "catalog.json";
#else
            "catalog.bin";
#endif
        /// <summary>
        /// The asset content state of a previous build.  This allows detection of deltas with the current build content state.  This will be
        /// null in standard builds.  This is only set during content update builds.
        /// </summary>
        public AddressablesContentState PreviousContentState { get; set; }

        private string GetDefaultPlayerVersion(AddressableAssetSettings settings)
        {
            string version = string.Empty;
            if (settings == null)
            {
                Debug.LogError("Attempting to set up AddressablesDataBuilderInput with null settings.");
            }
            else
                version = settings.PlayerBuildVersion;

            return version;
        }


        /// <summary>
        /// Creates a default context object with values taken from the AddressableAssetSettings parameter.
        /// </summary>
        /// <param name="settings">The settings object to pull values from.</param>
        public AddressablesDataBuilderInput(AddressableAssetSettings settings) : this(settings, EditorBuildSettingsProvider.Default)
        {
        }

        /// <summary>
        /// Creates a default context object with values taken from the AddressableAssetSettings parameter.
        /// </summary>
        /// <param name="settings">The settings object to pull values from.</param>
        /// <param name="buildSettingsProvider">Build settings provider.</param>
        public AddressablesDataBuilderInput(AddressableAssetSettings settings, IBuildSettingsProvider buildSettingsProvider)
        {
            SetAllValues(settings,
                BuildPipeline.GetBuildTargetGroup(buildSettingsProvider.activeBuildTarget),
                buildSettingsProvider.activeBuildTarget,
                GetDefaultPlayerVersion(settings),
                buildSettingsProvider.development,
                buildSettingsProvider.extraScriptingDefines);
        }

        /// <summary>
        /// Creates a default context object with values taken from the AddressableAssetSettings parameter and BuildPlayerOptions.
        /// </summary>
        /// <param name="settings">The settings object to pull values from.</param>
        /// <param name="buildPlayerOptions">The build player options containing development build status and extra scripting defines.</param>
        public AddressablesDataBuilderInput(AddressableAssetSettings settings, BuildPlayerOptions buildPlayerOptions)
        {
            if (settings == null)
            {
                Debug.LogError("Attempting to set up AddressablesDataBuilderInput with null settings.");
            }

            SetAllValues(settings,
                BuildPipeline.GetBuildTargetGroup(buildPlayerOptions.target),
                buildPlayerOptions.target,
                GetDefaultPlayerVersion(settings),
                (buildPlayerOptions.options & BuildOptions.Development) != 0,
                buildPlayerOptions.extraScriptingDefines);
        }




        /// <summary>
        /// Creates a default context object with values taken from the AddressableAssetSettings parameter.
        /// </summary>
        /// <param name="settings">The settings object to pull values from.</param>
        /// <param name="playerBuildVersion">The player build version.</param>
        public AddressablesDataBuilderInput(AddressableAssetSettings settings, string playerBuildVersion)
        : this(settings, playerBuildVersion, EditorBuildSettingsProvider.Default)
        {
        }

        /// <summary>
        /// Creates a default context object with values taken from the AddressableAssetSettings parameter.
        /// </summary>
        /// <param name="settings">The settings object to pull values from.</param>
        /// <param name="playerBuildVersion">The player build version.</param>
        /// <param name="buildSettingsProvider">Build settings provider.</param>
        public AddressablesDataBuilderInput(AddressableAssetSettings settings, string playerBuildVersion, IBuildSettingsProvider buildSettingsProvider)
        {
            if (settings == null)
            {
                Debug.LogError("Attempting to set up AddressablesDataBuilderInput with null settings.");
            }

            SetAllValues(settings,
                BuildPipeline.GetBuildTargetGroup(buildSettingsProvider.activeBuildTarget),
                buildSettingsProvider.activeBuildTarget,
                playerBuildVersion,
                buildSettingsProvider.development,
                buildSettingsProvider.extraScriptingDefines);
        }

        internal void SetAllValues(AddressableAssetSettings settings,
            BuildTargetGroup buildTargetGroup,
            BuildTarget buildTarget,
            string playerBuildVersion,
            bool developmentBuild,
            string[] extraScriptingDefines)
        {
            AddressableSettings = settings;

            TargetGroup = buildTargetGroup;
            Target = buildTarget;
            PlayerVersion = playerBuildVersion;
            Registry = new FileRegistry();
            PreviousContentState = null;
            DevelopmentBuild = developmentBuild;
            ExtraScriptingDefines = extraScriptingDefines;
        }

        internal bool IsBuildAndRelease = false;
        internal bool IsContentUpdateBuild = false;

        internal IBuildLogger Logger { get; set; }
    }
}
