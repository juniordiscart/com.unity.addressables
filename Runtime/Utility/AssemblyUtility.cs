using System;
using System.Collections.Generic;
using System.Reflection;
#if UNITY_6000_5_OR_NEWER
using UnityEngine.Assemblies;
#endif

namespace UnityEngine.AddressableAssets.Utility
{
    /// <summary>
    /// Utility class for working with assemblies in a CoreCLR-compatible way.
    /// </summary>
    public static class AssemblyUtility
    {
        /// <summary>
        /// Gets all loaded assemblies. Uses CoreCLR-compatible API on Unity 6000.5 or newer.
        /// </summary>
        /// <returns>Array of all loaded assemblies.</returns>
        public static IEnumerable<Assembly> GetAssemblies()
        {
#if UNITY_6000_5_OR_NEWER
            return CurrentAssemblies.GetLoadedAssemblies();
#else
            return AppDomain.CurrentDomain.GetAssemblies();
#endif
        }
    }
}
