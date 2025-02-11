using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UnityEngine.AddressableAssets.ResourceLocators
{
    /// <summary>
    /// Interface used by the Addressables system to find the locations of a given key.  An implementation of this interface must transform a key into an <see cref="IResourceLocation"/>.
    /// The content catalog is the primary resource locator as it contains a mapping of keys to locations.  Custom locators could pull their location data from an online source.
    /// </summary>
    public interface IResourceLocator
    {
        /// <summary>
        /// The id for this locator.
        /// </summary>
        string LocatorId { get; }

        /// <summary>
        /// The keys defined by this locator.
        /// </summary>
        IEnumerable<object> Keys { get; }

#if !ENABLE_JSON_CATALOG
        /// <summary>
        /// All locations that are available by this locator.
        /// </summary>
        IEnumerable<IResourceLocation> AllLocations { get; }
#endif

        /// <summary>
        /// Retrieve the locations from a specified key.
        /// </summary>
        /// <param name="key">The key to use.</param>
        /// <param name="type">The resource type.</param>
        /// <param name="locations">The resulting set of locations for the key.</param>
        /// <returns>True if any locations were found with the specified key.</returns>
        bool Locate(object key, Type type, out IList<IResourceLocation> locations);
    }
}
