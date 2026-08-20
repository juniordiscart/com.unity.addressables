using UnityEditor.AddressableAssets.Build.AnalyzeRules;

namespace UnityEditor.AddressableAssets.GUI
{
    /// <summary>
    /// One registered analyze rule; hosts the subtree built from cached analyze results for that rule.
    /// </summary>
    class AnalyzeRuleTreeViewItem : AnalyzeTreeViewItemBase
    {
        internal AnalyzeRule analyzeRule;

        public AnalyzeRuleTreeViewItem(int id, int depth, AnalyzeRule rule)
            : base(id, depth, rule.ruleName)
        {
            analyzeRule = rule;
            children = EmptyList();
        }
    }
}
