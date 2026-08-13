using System.Collections.Generic;
using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Base class for rules that provide input asset paths.
    /// </summary>
    public abstract class InputRule : ScriptableObject
    {
        #region Methods
        /// <summary>
        /// Returns the set of asset paths that this rule includes.
        /// </summary>
        /// <returns>A set of asset paths to include.</returns>
        public abstract HashSet<string> GetIncludedAssets();
        #endregion
    }
}
