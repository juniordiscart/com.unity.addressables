using System;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Serializable description of a generated group layout.
    /// </summary>
    [Serializable]
    public class GroupLayout : Subgraph
    {
        #region Fields
        /// <summary>
        /// Name of the Addressable group template applied to this layout.
        /// </summary>
        public string TemplateName;

        /// <summary>
        /// Display name for the group layout.
        /// </summary>
        public string Name;
        #endregion

        #region Properties
        /// <summary>
        /// Gets a value indicating whether the layout is shared by multiple sources.
        /// </summary>
        public bool IsShared => Sources.Count > 1;
        #endregion
    }
}
