namespace UnityEditor.AddressableAssets.GUI
{
    /// <summary>
    /// Grouping row only (for example "Analyze Rules", "Fixable Rules").
    /// Not registered in <see cref="UnityEditor.AddressableAssets.Build.AnalyzeSystem.Rules"/>.
    /// </summary>
    class AnalyzeGroupTreeViewItem : AnalyzeTreeViewItemBase
    {
        public AnalyzeGroupTreeViewItem(int id, int depth, string displayName)
            : base(id, depth, displayName)
        {
            children = EmptyList();
        }
    }
}
