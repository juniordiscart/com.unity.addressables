using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Settings.GroupSchemas
{
    /// <summary>
    /// Interface for buildable schemas that can be included in builds.
    /// Inherits IsEnabled from ICanBeEnabled.
    /// </summary>
    internal interface ICanIncludeInBuild
    {
        bool IncludeInBuild { get; set; }
    }
}
