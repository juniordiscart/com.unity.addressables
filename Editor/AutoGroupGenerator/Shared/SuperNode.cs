using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Represents a strongly connected component (SCC) in the AutoGroupGenerator asset dependency graph.
    ///
    /// A SuperNode is an abstraction that groups together asset nodes which are
    /// mutually dependent on each other, forming a dependency cycle. In graph terms,
    /// a set of nodes is considered <em>strongly connected</em> if every node in the set
    /// can be reached from every other node by following dependency edges.
    ///
    /// SuperNodes are used internally by AutoGroupGenerator to resolve cyclic dependencies by
    /// collapsing such cycles into a single logical node during graph analysis.
    /// This allows the system to reason about dependencies, ordering, and grouping
    /// in a deterministic way without being blocked by dependency loops.
    ///
    /// SuperNodes are not user-facing and are not intended to be created or manipulated
    /// directly. They exist solely as an implementation detail of AutoGroupGenerator’s graph processing
    /// pipeline and are handled automatically by the system.
    /// </summary>
    [Serializable]
    public class SuperNode : IEquatable<SuperNode>
    {
        #region Static Methods
        /// <summary>
        /// Creates a <see cref="SuperNode"/> containing a single asset node.
        /// </summary>
        /// <param name="node">The asset node that will be the only member of the super node.</param>
        /// <returns>A super node containing the provided node as its sole entry.</returns>
        public static SuperNode FromSingle(AssetNode node) => new SuperNode(new[] { node });
        #endregion

        #region Fields
        private readonly HashSet<AssetNode> _nodes;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the asset nodes contained in this super node.
        /// </summary>
        public IReadOnlyCollection<AssetNode> Nodes => _nodes;
        #endregion

        #region Methods
        /// <summary>
        /// Initializes a new instance of the <see cref="SuperNode"/> class.
        /// </summary>
        /// <param name="nodes">The asset nodes to include in the strongly connected component.</param>
        public SuperNode(IEnumerable<AssetNode> nodes)
        {
            _nodes = new HashSet<AssetNode>(nodes ?? Enumerable.Empty<AssetNode>());
        }

        /// <summary>
        /// Determines whether another super node has the same member nodes.
        /// </summary>
        /// <param name="other">The other super node.</param>
        /// <returns>True when both super nodes contain the same nodes.</returns>
        public bool Equals(SuperNode other)
        {
            if (other == null)
            {
                return false;
            }


            return _nodes.SetEquals(other._nodes);
        }

        /// <summary>
        /// Determines whether another object is equal to this super node.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True when the object is an equal <see cref="SuperNode"/>.</returns>
        public override bool Equals(object obj)
        {
            return obj is SuperNode other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code for this super node.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                foreach (var node in _nodes.OrderBy(n => n.Guid.ToString()))
                {
                    hash = hash* 31 + node.GetHashCode();
                }

                return hash;
            }
        }

        /// <summary>
        /// Returns a string representation of the super node.
        /// </summary>
        /// <returns>A human-readable string of contained nodes.</returns>
        public override string ToString()
        {
            return $"SuperNode[{string.Join(", ", _nodes)}]";
        }

        /// <summary>
        /// Determines whether the super node contains the specified asset node.
        /// </summary>
        /// <param name="node">The asset node to look for within the super node membership.</param>
        /// <returns>True when the node is present in the super node collection.</returns>
        public bool Contains(AssetNode node) => _nodes.Contains(node);
        #endregion
    }
}
