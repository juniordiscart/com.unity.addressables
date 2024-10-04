using System.Collections.Generic;
using System.IO;
using UnityEditor.AddressableAssets.Settings;

namespace UnityEditor.AddressableAssets.Build.AnalyzeRules
{
    /// <summary>
    /// Rule class to check resource dependencies for duplicates
    /// </summary>
    public class CheckResourcesDupeDependencies : BundleRuleBase
    {
        /// <inheritdoc />
        public override string ruleName
        {
            get { return "Check Resources to Addressable Duplicate Dependencies"; }
        }

        /// <inheritdoc />
        public override bool CanFix
        {
            get { return false; }
        }

        /// <summary>
        /// Clear analysis and calculate built in resources and corresponding bundle dependencies
        /// </summary>
        /// <param name="settings">The current Addressables settings object</param>
        /// <returns>List of results from analysis</returns>
        public override List<AnalyzeResult> RefreshAnalysis(AddressableAssetSettings settings)
        {
            ClearAnalysis();
            AddressableAnalytics.ReportUsageEvent(AddressableAnalytics.UsageEventType.RunCheckResourcesDupeDependenciesRule);
            return CalculateBuiltInResourceDependenciesToBundleDependecies(settings, GetResourcePaths());
        }

        /// <inheritdoc />
        public override void FixIssues(AddressableAssetSettings settings)
        {
            //Do nothing.  There's nothing to fix.
        }

        /// <inheritdoc />
        internal protected override string[] GetResourcePaths()
        {
            string[] resourceDirectory = Directory.GetDirectories("Assets", "Resources", SearchOption.AllDirectories);
            List<string> resourcePaths = new List<string>();
            foreach (string directory in resourceDirectory)
                resourcePaths.AddRange(Directory.GetFiles(directory, "*", SearchOption.AllDirectories));
            return resourcePaths.ToArray();
        }
    }


    [InitializeOnLoad]
    class RegisterCheckResourcesDupeDependencies
    {
        static RegisterCheckResourcesDupeDependencies()
        {
            AnalyzeSystem.RegisterNewRule<CheckResourcesDupeDependencies>();
        }
    }
}
