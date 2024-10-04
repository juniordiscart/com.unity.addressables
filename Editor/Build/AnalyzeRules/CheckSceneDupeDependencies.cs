using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Settings;

namespace UnityEditor.AddressableAssets.Build.AnalyzeRules
{
    /// <summary>
    /// Rule class to check scene dependencies for duplicates
    /// </summary>
    public class CheckSceneDupeDependencies : BundleRuleBase
    {
        /// <inheritdoc />
        public override string ruleName
        {
            get { return "Check Scene to Addressable Duplicate Dependencies"; }
        }

        /// <inheritdoc />
        public override bool CanFix
        {
            get { return false; }
        }

        /// <inheritdoc />
        public override void FixIssues(AddressableAssetSettings settings)
        {
            //Do nothing, there's nothing to fix.
        }

        /// <summary>
        /// Clear analysis and calculate built in resources and corresponding bundle dependencies for scenes
        /// </summary>
        /// <param name="settings">The current Addressables settings object</param>
        /// <returns>List of results from analysis</returns>
        public override List<AnalyzeResult> RefreshAnalysis(AddressableAssetSettings settings)
        {
            ClearAnalysis();

            string[] scenePaths = (from editorScene in EditorBuildSettings.scenes
                where editorScene.enabled
                select editorScene.path).ToArray();
            AddressableAnalytics.ReportUsageEvent(AddressableAnalytics.UsageEventType.RunCheckSceneDupeDependenciesRule);
            return CalculateBuiltInResourceDependenciesToBundleDependecies(settings, scenePaths);
        }

        /// <inheritdoc />
        internal protected override string[] GetResourcePaths()
        {
            List<string> scenes = new List<string>(EditorBuildSettings.scenes.Length);
            foreach (EditorBuildSettingsScene settingsScene in EditorBuildSettings.scenes)
            {
                if (settingsScene.enabled)
                    scenes.Add(settingsScene.path);
            }

            return scenes.ToArray();
        }
    }

    [InitializeOnLoad]
    class RegisterCheckSceneDupeDependencies
    {
        static RegisterCheckSceneDupeDependencies()
        {
            AnalyzeSystem.RegisterNewRule<CheckSceneDupeDependencies>();
        }
    }
}
