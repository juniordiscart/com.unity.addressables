using System;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Describes an action queued for execution.
    /// </summary>
    public struct Command
    {
        #region Fields
        /// <summary>
        /// Action to invoke when the command is executed.
        /// </summary>
        public Action Action;

        /// <summary>
        /// Human-readable description of the command.
        /// </summary>
        public string Info;
        #endregion
    }
}
