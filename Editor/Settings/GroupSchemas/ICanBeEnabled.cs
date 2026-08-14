using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Settings.GroupSchemas
{
    /// <summary>
    /// Interface for schemas that can be enabled/disabled in the UI and build process.
    /// </summary>
    internal interface ICanBeEnabled
    {
        /// <summary>
        /// Determines whether this schema is enabled and will participate in builds.
        /// </summary>
        bool IsEnabled { get; set; }
    }
}
