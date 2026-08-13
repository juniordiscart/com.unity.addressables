using System.Linq;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Extension helpers for classifying subgraph topology.
    /// </summary>
    /// <seealso cref="Subgraph"/>
    /// <seealso cref="DependencyGraph"/>
    public static class SubgraphTopologyUtil
    {
        #region Static Methods

        /// <summary>
        /// Determines whether a subgraph is shared by multiple sources.
        /// </summary>
        /// <param name="subgraph">The subgraph instance to evaluate for shared source ownership.</param>
        /// <returns>True when the subgraph has more than one source.</returns>
        public static bool IsShared(this Subgraph subgraph)
        {
            return subgraph.Sources.Count > 1;
        }

        /// <summary>
        /// Determines whether a subgraph forms a hierarchy rooted at its sources.
        /// </summary>
        /// <param name="subgraph">The subgraph instance to evaluate for hierarchical structure.</param>
        /// <param name="dependencyGraph">The dependency graph that contains the subgraph nodes.</param>
        /// <returns>True when the subgraph is hierarchical.</returns>
        public static bool IsHierarchy(Subgraph subgraph, DependencyGraph dependencyGraph)
        {
            return subgraph.Nodes.Count > 1 && subgraph.Sources.IsSubsetOf(subgraph.Nodes);
        }

        #endregion
    }
}
