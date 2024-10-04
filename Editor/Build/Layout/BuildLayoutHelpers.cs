using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Build.Layout
{
    /// <summary>
    /// Helper methods for gathering data about a build layout.
    /// </summary>
    public class BuildLayoutHelpers
    {
        /// <summary>
        /// Gather a list of Explicit Assets defined in a BuildLayout
        /// </summary>
        /// <param name="layout">The BuildLayout generated during a build</param>
        /// <returns>A list of ExplicitAsset data.</returns>
        public static IEnumerable<BuildLayout.ExplicitAsset> EnumerateAssets(BuildLayout layout)
        {
            return EnumerateBundles(layout).SelectMany(b => b.Files).SelectMany(f => f.Assets);
        }

        internal static IEnumerable<BuildLayout.DataFromOtherAsset> EnumerateImplicitAssets(BuildLayout layout)
        {
            return EnumerateBundles(layout).SelectMany(b => b.Files).SelectMany(f => f.Assets).SelectMany(a => a.InternalReferencedOtherAssets);
        }

        internal static IEnumerable<BuildLayout.DataFromOtherAsset> EnumerateImplicitAssets(BuildLayout.Bundle bundle)
        {
            return bundle.Files.SelectMany(f => f.OtherAssets);
        }

        /// <summary>
        /// Gather a list of Explicit Assets defined in a Bundle
        /// </summary>
        /// <param name="bundle">The Bundle data generated during a build</param>
        /// <returns>A list of ExplicitAssets defined in the Bundle</returns>
        public static IEnumerable<BuildLayout.ExplicitAsset> EnumerateAssets(BuildLayout.Bundle bundle)
        {
            return bundle.Files.SelectMany(f => f.Assets);
        }

        /// <summary>
        /// Gather a list of Bundle data defined in a BuildLayout
        /// </summary>
        /// <param name="layout">The BuildLayout generated during a build</param>
        /// <returns>A list of the Bundle data defined in a BuildLayout</returns>
        public static IEnumerable<BuildLayout.Bundle> EnumerateBundles(BuildLayout layout)
        {
            foreach (BuildLayout.Bundle b in layout.BuiltInBundles)
                yield return b;

            foreach (BuildLayout.Bundle b in layout.Groups.SelectMany(g => g.Bundles))
                yield return b;
        }

        /// <summary>
        /// Gather a list of File data defined in a BuildLayout
        /// </summary>
        /// <param name="layout">The BuildLayout generated during a build</param>
        /// <returns>A list of File data</returns>
        public static IEnumerable<BuildLayout.File> EnumerateFiles(BuildLayout layout)
        {
            return EnumerateBundles(layout).SelectMany(b => b.Files);
        }

        private static Dictionary<System.Type, AssetType> m_SystemTypeToAssetType = null;
        private static Dictionary<System.Type, AssetType> SystemTypeToAssetType
        {
            get
            {
                if (m_SystemTypeToAssetType == null)
                {
                    m_SystemTypeToAssetType = new Dictionary<Type, AssetType>()
                    {
                        { typeof(SceneAsset), AssetType.Scene }
                    };
                }
                return m_SystemTypeToAssetType;
            }
        }

        private static List<(System.Type, AssetType)> m_AssignableSystemTypeToAssetType = null;
        private static List<(System.Type, AssetType)> AssignableSystemTypeToAssetType
        {
            get
            {
                if (m_AssignableSystemTypeToAssetType == null)
                {
                    m_AssignableSystemTypeToAssetType = new List<(Type, AssetType)>()
                    {
                        (typeof(ScriptableObject), AssetType.ScriptableObject),
                        (typeof(MonoBehaviour), AssetType.MonoBehaviour),
                        (typeof(Component), AssetType.Component)
                    };
                }
                return m_AssignableSystemTypeToAssetType;
            }
        }

        private static Dictionary<System.Type, AssetType> m_RuntimeSystemTypeToAssetType = null;
        private static Dictionary<System.Type, AssetType> RuntimeSystemTypeToAssetType
        {
            get
            {
                if (m_RuntimeSystemTypeToAssetType == null)
                {
                    m_RuntimeSystemTypeToAssetType = new Dictionary<Type, AssetType>()
                    {
                        { typeof(RuntimeAnimatorController), AssetType.AnimationController }
                    };
                }
                return m_RuntimeSystemTypeToAssetType;
            }
        }

        /// <summary>
        /// Gets the enum AssetType associated with the param systemType ofType
        /// </summary>
        /// <param name="ofType">The Type of the asset</param>
        /// <returns>An AssetType or <see cref="AssetType.Other" /> if null or unknown.</returns>
        public static AssetType GetAssetType(Type ofType)
        {
            if (ofType == null)
                return AssetType.Other;

            if (AssetType.TryParse(ofType.Name, out AssetType assetType))
                return assetType;

            // types where the class name doesn't equal the AssetType (legacy enum values)
            if (SystemTypeToAssetType.TryGetValue(ofType, out assetType))
                return assetType;

            foreach ((Type, AssetType) typeAssignment in AssignableSystemTypeToAssetType)
            {
                if (typeAssignment.Item1.IsAssignableFrom(ofType))
                    return typeAssignment.Item2;
            }

            ofType = AddressableAssetUtility.MapEditorTypeToRuntimeType(ofType, false);
            if (ofType == null)
                return AssetType.Other;
            if (SystemTypeToAssetType.TryGetValue(ofType, out assetType))
                return assetType;
            if (RuntimeSystemTypeToAssetType.TryGetValue(ofType, out assetType))
                return assetType;

            return AssetType.Other;
        }
    }
}
