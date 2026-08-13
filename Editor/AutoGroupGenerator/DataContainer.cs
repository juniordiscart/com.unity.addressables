using System.Collections.Generic;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Shared data used across AutoGroupGenerator editor workflows.
    /// </summary>
    /// <seealso cref="AutoGroupGeneratorWindow"/>
    /// <seealso cref="AutoGroupGeneratorSettings"/>
    /// <seealso cref="DependencyGraph"/>
    public class DataContainer
    {
        #region Fields
        /// <summary>
        /// Set of input asset paths gathered from input rules.
        /// </summary>
        public HashSet<string> InputAssets;

        /// <summary>
        /// Dependency graph generated for the project assets.
        /// </summary>
        public DependencyGraph DependencyGraph;

        /// <summary>
        /// File path for the active settings asset.
        /// </summary>
        public string SettingsFilePath;

        /// <summary>
        /// Loaded settings instance controlling the workflow.
        /// </summary>
        public AutoGroupGeneratorSettings Settings;

        /// <summary>
        /// Assets excluded by exclusion rules.
        /// </summary>
        public HashSet<AssetNode> ExcludedAssets;

        /// <summary>
        /// Subgraph data keyed by hash.
        /// </summary>
        public Dictionary<int, Subgraph> Subgraphs;

        /// <summary>
        /// Group layout data keyed by group name.
        /// </summary>
        public Dictionary<string, GroupLayout> GroupLayout;

        /// <summary>
        /// Indicates whether Addressable asset editing is currently in progress.
        /// </summary>
        public bool AssetEditingInProgress;

        /// <summary>
        /// Logger used for workflow messages.
        /// </summary>
        public Logger Logger;
        #endregion
    }
}
