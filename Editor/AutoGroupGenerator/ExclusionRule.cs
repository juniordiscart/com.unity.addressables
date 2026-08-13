using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Base class for rules that exclude assets from the input pipeline.
    /// </summary>
    /// <seealso cref="DataContainer"/>
    /// <seealso cref="AssetNode"/>
    public abstract class ExclusionRule : ScriptableObject
    {
        #region Fields
        /// <summary>
        /// Data container used to evaluate exclusion rules.
        /// </summary>
        protected DataContainer m_DataContainer;
        #endregion

        #region Methods
        /// <summary>
        /// Initializes the rule with shared data.
        /// </summary>
        /// <param name="dataContainer">The shared data container that provides context for the rule.</param>
        public virtual void Initialize(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
        }

        /// <summary>
        /// Determines whether a node should be ignored.
        /// </summary>
        /// <param name="node">The asset node to evaluate for exclusion during processing.</param>
        /// <returns>True if the node should be excluded.</returns>
        public abstract bool ShouldIgnoreNode(AssetNode node);

        /// <summary>
        /// Clears cached data when the rule is no longer needed.
        /// </summary>
        public virtual void UnInitialize()
        {
            m_DataContainer = null;
        }
        #endregion
    }
}
