using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.IMGUI.Controls;

namespace UnityEditor.AddressableAssets.GUI
{
    /// <summary>
    /// Helpers for resolving which registered analyze rule owns selected result rows (unit-tested via internals visible).
    /// </summary>
    static class AnalyzeResultsSelection
    {
        /// <summary>
        /// Returns the closest ancestor <see cref="AnalyzeRuleTreeViewItem"/> whose rule is listed in <see cref="AnalyzeSystem.Rules"/>.
        /// </summary>
        internal static AnalyzeRuleTreeViewItem FindRegisteredRuleContainerParent(AnalyzeResultsTreeViewItem item)
        {
            if (item == null)
                return null;

#if UNITY_6000_2_OR_NEWER
            for (TreeViewItem<int> p = item.parent; p != null; p = p.parent)
#else
            for (TreeViewItem p = item.parent; p != null; p = p.parent)
#endif
            {
                if (p is AnalyzeRuleTreeViewItem ruleRow && AnalyzeSystem.Rules.Contains(ruleRow.analyzeRule))
                    return ruleRow;
            }

            return null;
        }

        /// <summary>
        /// Succeeds when every result row shares the same registered rule parent.
        /// </summary>
        internal static bool TryGetSingleRegisteredRuleContainer(IReadOnlyList<AnalyzeResultsTreeViewItem> items,
            out AnalyzeRule rule, out AnalyzeRuleTreeViewItem container)
        {
            rule = null;
            container = null;
            if (items == null || items.Count == 0)
                return false;

            AnalyzeRuleTreeViewItem found = null;
            foreach (var item in items)
            {
                var c = FindRegisteredRuleContainerParent(item);
                if (c == null)
                    return false;
                if (found == null)
                    found = c;
                else if (!ReferenceEquals(found.analyzeRule, c.analyzeRule))
                    return false;
            }

            container = found;
            rule = found.analyzeRule;
            return true;
        }
    }
}
