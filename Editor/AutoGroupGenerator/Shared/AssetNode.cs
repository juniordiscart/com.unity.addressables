using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Represents an asset in the dependency graph via its GUID.
    /// </summary>
    [Serializable]
    public class AssetNode : IEquatable<AssetNode>
    {
        #region Static Methods
        /// <summary>
        /// Creates an <see cref="AssetNode"/> from an asset path.
        /// </summary>
        /// <param name="assetPath">The project-relative asset path used to resolve a GUID.</param>
        /// <returns>The created node, or null if the path is invalid.</returns>
        public static AssetNode FromAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }


            var guidString = AssetDatabase.AssetPathToGUID(assetPath, AssetPathToGUIDOptions.OnlyExistingAssets);

            return FromGuidString(guidString);
        }

        /// <summary>
        /// Creates an <see cref="AssetNode"/> from a serialized string representation.
        /// </summary>
        /// <param name="value">The serialized value containing a GUID string to parse.</param>
        /// <returns>The created node, or null if parsing fails.</returns>
        public static AssetNode FromString(string value)
        {
            string[] parts = value.Split('|');

            var guidString = parts[0];

            return FromGuidString(guidString);
        }

        /// <summary>
        /// Creates an <see cref="AssetNode"/> from a GUID string.
        /// </summary>
        /// <param name="guidString">The GUID string representation used to create the node.</param>
        /// <returns>The created node, or null if parsing fails.</returns>
        public static AssetNode FromGuidString(string guidString)
        {
            return GUID.TryParse(guidString, out var guid) ? new AssetNode(guid) : null;
        }
        #endregion

        #region Fields
        /// <summary>
        /// The GUID backing this node.
        /// </summary>
        public readonly GUID Guid;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the asset path represented by this node.
        /// </summary>
        public string AssetPath => AssetDatabase.GUIDToAssetPath(Guid);

        /// <summary>
        /// Gets the asset file name without the directory path.
        /// </summary>
        public string FileName => Path.GetFileName(AssetPath);
        #endregion

        #region Methods
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetNode"/> class.
        /// </summary>
        /// <param name="guid">The GUID that uniquely identifies the asset in the project.</param>
        public AssetNode(GUID guid)
        {
            Guid = guid;
        }

        /// <summary>
        /// Returns the GUID string for this node.
        /// </summary>
        /// <returns>The GUID string.</returns>
        public override string ToString()
        {
            return Guid.ToString();
        }

        /// <summary>
        /// Determines whether another node is equal to this node.
        /// </summary>
        /// <param name="other">The other node.</param>
        /// <returns>True when the GUIDs are equal.</returns>
        public bool Equals(AssetNode other)
        {
            return other != null && Guid.Equals(other.Guid);
        }

        /// <summary>
        /// Determines whether another object is equal to this node.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True when the object is an equal <see cref="AssetNode"/>.</returns>
        public override bool Equals(object obj)
        {
            return obj is AssetNode other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code for this node.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }
        #endregion
    }
}
