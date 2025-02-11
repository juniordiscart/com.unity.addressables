using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement.ResourceLocations
{
    /// <summary>
    /// Contains enough information to load an asset (what/where/how/dependencies)
    /// </summary>
    public interface IResourceLocation
    {
        /// <summary>
        /// Internal name used by the provider to load this location
        /// </summary>
        /// <value>The identifier.</value>
        string InternalId { get; }

        /// <summary>
        /// Matches the provider used to provide/load this location
        /// </summary>
        /// <value>The provider id.</value>
        string ProviderId { get; }

        /// <summary>
        /// Gets the dependencies to other IResourceLocations
        /// </summary>
        /// <value>The dependencies.</value>
        IList<IResourceLocation> Dependencies { get; }

        /// <summary>
        /// The hash of this location combined with the specified type.
        /// </summary>
        /// <param name="resultType">The type of the result.</param>
        /// <returns>The combined hash of the location and the type.</returns>
        int Hash(Type resultType);

        /// <summary>
        /// The precomputed hash code of the dependencies.
        /// </summary>
        int DependencyHashCode { get; }

        /// <summary>
        /// Gets the dependencies to other IResourceLocations
        /// </summary>
        /// <value>The dependencies.</value>
        bool HasDependencies { get; }

        /// <summary>
        /// Gets any data object associated with this locations
        /// </summary>
        /// <value>The object.</value>
        object Data { get; }

        /// <summary>
        /// Primary address for this location.
        /// </summary>
        string PrimaryKey { get; }

        /// <summary>
        /// The type of the resource for th location.
        /// </summary>
        Type ResourceType { get; }
    }

    /// <summary>
    /// An IEqualityComparerer to check if two IResourceLocations are the same
    /// </summary>
    public class ResourceLocationComparer : IEqualityComparer<IResourceLocation>
    {
        /// <summary>
        /// Check if two Resource locations are equal
        /// </summary>
        /// <param name="x">First location to compare</param>
        /// <param name="y">Second location to compare</param>
        /// <returns>True if they're equal, false otherwise</returns>
        public bool Equals(IResourceLocation x, IResourceLocation y)
        {
            return GetHashCode(x) == GetHashCode(y);
        }

        /// <summary>
        /// Calculates the hash code for a Resource Location
        /// </summary>
        /// <param name="obj">The resource location to compute</param>
        /// <returns>A hash code of the data in a Resource Location</returns>
        public int GetHashCode(IResourceLocation obj)
        {
            return obj.InternalId.GetHashCode() * 31 + obj.ResourceType.GetHashCode();
        }
    }
}
