#if ENABLE_CONTENT_DIRECTORIES
using System.Collections.Generic;
using Unity.Loading;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace UnityEngine.AddressableAssets.ResourceProviders
{
    /// <summary>
    /// A ScriptableObject that serves as the single global root asset for all Content Directory groups.
    /// Stores LoadableObjectIds for assets and LoadableSceneIds for scenes with O(1) lookup by int index.
    /// </summary>
    public class AddressableRootAsset : ScriptableObject, IAddressableRootAsset
    {
        [SerializeField]
        private List<LoadableObjectId> m_loadableObjectIds = new List<LoadableObjectId>();

        [SerializeField]
        private List<LoadableSceneId> m_loadableSceneIds = new List<LoadableSceneId>();

        /// <summary>
        /// Key is not used for the global AddressableRootAsset but required by IAddressableRootAsset.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Adds a LoadableObjectId for an asset during build.
        /// </summary>
        /// <param name="loadableObjId">The LoadableObjectId for the asset.</param>
        /// <returns>The integer index used to look up this asset at runtime.</returns>
        public int AddAsset(LoadableObjectId loadableObjId)
        {
            m_loadableObjectIds.Add(loadableObjId);
            return m_loadableObjectIds.Count - 1;
        }

        /// <summary>
        /// Adds a LoadableSceneId for a scene during build.
        /// </summary>
        /// <param name="sceneId">The LoadableSceneId for the scene.</param>
        /// <returns>The integer index used to look up this scene at runtime.</returns>
        public int AddScene(LoadableSceneId sceneId)
        {
            m_loadableSceneIds.Add(sceneId);
            return m_loadableSceneIds.Count - 1;
        }

        /// <summary>
        /// Gets a LoadableObjectId by its integer index with O(1) lookup.
        /// </summary>
        /// <param name="id">The integer index returned by AddAsset().</param>
        /// <returns>The LoadableObjectId.</returns>
        public LoadableObjectId GetLoadableObjectId(int id)
        {
            if (id < 0 || id >= m_loadableObjectIds.Count)
                return default;

            return m_loadableObjectIds[id];
        }

        /// <summary>
        /// Gets a LoadableSceneId by its integer index with O(1) lookup.
        /// </summary>
        /// <param name="id">The integer index returned by AddScene().</param>
        /// <returns>The LoadableSceneId.</returns>
        public LoadableSceneId GetLoadableSceneId(int id)
        {
            if (id < 0 || id >= m_loadableSceneIds.Count)
                return default;

            return m_loadableSceneIds[id];
        }
    }
}
#endif
