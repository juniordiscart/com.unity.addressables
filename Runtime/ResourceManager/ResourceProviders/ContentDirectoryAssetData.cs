#if ENABLE_CONTENT_DIRECTORIES
using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    /// <summary>
    /// Data stored in ContentCatalogDataEntry.Data for asset entries in Content Directories.
    /// </summary>
    [Serializable]
    public class ContentDirectoryAssetData
    {
        /// <summary>
        /// Default value used for <see cref="AssetId"/> and <see cref="SceneId"/>.
        /// </summary>
        public const int kInvalidId = -1;

        /// <summary>
        /// Integer ID for regular assets. Maps to LoadableObjectId via AddressableRootAsset.GetLoadableObjectId().
        /// Not applicable for scene entries; check <see cref="IsAssetIdValid"/>.
        /// </summary>
        [SerializeField]
        public int AssetId = kInvalidId;

        /// <summary>
        /// Integer ID for scene entries. Maps to LoadableSceneId via AddressableRootAsset.GetLoadableSceneId().
        /// Not applicable for regular asset entries; check <see cref="IsSceneIdValid"/>.
        /// </summary>
        [SerializeField]
        public int SceneId = kInvalidId;

        /// <summary>
        /// Whether <see cref="AssetId"/> refers to an asset in the AddressableRootAsset.
        /// False for scene entries, where it is <see cref="kInvalidId"/>.
        /// </summary>
        public bool IsAssetIdValid => AssetId != kInvalidId;

        /// <summary>
        /// Whether <see cref="SceneId"/> refers to a scene in the AddressableRootAsset.
        /// False for regular asset entries, where it is <see cref="kInvalidId"/>.
        /// </summary>
        public bool IsSceneIdValid => SceneId != kInvalidId;

        /// <summary>
        /// Precomputed list of subasset LoadableObjectId integers for this entry.
        /// Used by HandleListRequest to load all subassets in batch.
        /// </summary>
        [SerializeField]
        public int[] SubAssetIds;

        /// <summary>
        /// The (symbolic) load path of the Content Directory this entry belongs to. Resolved at
        /// runtime and used by the asset/scene providers to mount the Content Directory via
        /// <see cref="ContentDirectoryMountManager"/>. Stored unresolved so runtime placeholders
        /// such as {UnityEngine.AddressableAssets.Addressables.RuntimePath} expand per-platform.
        /// </summary>
        [SerializeField]
        public string LoadPath;

        /// <summary>
        /// Serialization adapter for binary catalog support.
        /// </summary>
        internal class SerializationAdapter : BinaryStorageBuffer.ISerializationAdapter<ContentDirectoryAssetData>
        {
            public IEnumerable<BinaryStorageBuffer.ISerializationAdapter> Dependencies => null;

            struct Data
            {
                public int assetId;
                public int sceneId;
                public uint subAssetIdsOffset;
                public uint loadPathOffset;
            }

            public object Deserialize(BinaryStorageBuffer.Reader reader, Type type, uint offset, out uint size)
            {
                size = 0;
                if (type != typeof(ContentDirectoryAssetData))
                    return null;

                var data = reader.ReadValue<Data>(offset, out var dataSize);
                int[] subAssetIds = null;
                uint subAssetIdsSize = 0;
                if (data.subAssetIdsOffset != 0)
                    subAssetIds = reader.ReadValueArray<int>(data.subAssetIdsOffset, out subAssetIdsSize, true);

                string loadPath = null;
                uint loadPathSize = 0;
                if (data.loadPathOffset != 0)
                    loadPath = reader.ReadString(data.loadPathOffset, out loadPathSize, '/', true);

                size = dataSize + subAssetIdsSize + loadPathSize;
                return new ContentDirectoryAssetData
                {
                    AssetId = data.assetId,
                    SceneId = data.sceneId,
                    SubAssetIds = subAssetIds,
                    LoadPath = loadPath
                };
            }

            public uint Serialize(BinaryStorageBuffer.Writer writer, object val)
            {
                var assetData = val as ContentDirectoryAssetData;
                var data = new Data
                {
                    assetId = assetData?.AssetId ?? kInvalidId,
                    sceneId = assetData?.SceneId ?? kInvalidId,
                    subAssetIdsOffset = assetData?.SubAssetIds != null && assetData.SubAssetIds.Length > 0
                        ? writer.Write(assetData.SubAssetIds)
                        : 0,
                    loadPathOffset = string.IsNullOrEmpty(assetData?.LoadPath)
                        ? 0
                        : writer.WriteString(assetData.LoadPath, '/')
                };
                return writer.Write(data);
            }
        }
    }
}
#endif
