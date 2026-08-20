namespace UnityEditor.AddressableAssets.GUI
{
    /// <summary>
    /// Shared display and issue-count behavior for Addressables Analyze tree rows (grouping folders,
    /// registered rule rows, and result/issue rows).
    /// </summary>
    class AnalyzeTreeViewItemBase : UnityEditor.AddressableAssets.GUI.Adapters.TreeViewItemAdapter
    {
        string m_BaseDisplayName;
        string m_CurrentDisplayName;

        public override string displayName
        {
            get { return m_CurrentDisplayName; }
            set { m_BaseDisplayName = value; }
        }

        public AnalyzeTreeViewItemBase(int id, int depth, string displayName)
            : base(id, depth, displayName)
        {
            m_CurrentDisplayName = m_BaseDisplayName = displayName;
        }

        /// <summary>
        /// Shows aggregated issue counts on rows whose direct children include analyze result nodes.
        /// </summary>
        public int AddIssueCountToName()
        {
            int issueCount = 0;
            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child is AnalyzeResultsTreeViewItem analyzeNode)
                        issueCount += analyzeNode.AddIssueCountToName();
                }
            }

            if (issueCount == 0)
                return 1;

            m_CurrentDisplayName = m_BaseDisplayName + " (" + issueCount + ")";
            return issueCount;
        }
    }
}
