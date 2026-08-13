using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.AddressableAssets
{
    /// <summary>
    /// Interface for providing a runtime key to addressable assets.
    /// </summary>
    /// <remarks>
    /// Use the runtime key as an alternative to directly referencing an object. Implementations expose a RuntimeKey and report whether it's valid.
    /// </remarks>
    public interface IKeyEvaluator
    {
        /// <summary>
        /// The runtime key to use.
        /// </summary>
        object RuntimeKey { get; }

        /// <summary>
        /// Checks if the current RuntimeKey is valid.
        /// </summary>
        /// <returns>Whether the RuntimeKey is valid.</returns>
        bool RuntimeKeyIsValid();
    }
}
