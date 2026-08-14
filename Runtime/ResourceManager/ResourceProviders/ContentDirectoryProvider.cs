#if ENABLE_CONTENT_DIRECTORIES
using System;
using System.Collections.Generic;
using Unity.Loading;
using UnityEngine.ResourceManagement.Exceptions;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    public interface IAddressableRootAsset
    {
        string Key { get; set; }
        LoadableInfo GetLoadableInfo(string key, Type assetType);
    }

    /// <summary>
    /// Interface used for ContentDirectory resources.
    /// </summary>
    public interface IContentDirectoryResource
    {
        /// <summary>
        /// Fetches the loaded ContentDirectoryHandle. Will be null if the load is not yet complete.
        /// </summary>
        /// <returns>The handle that points to the ContentDirectory.</returns>
        ContentDirectoryHandle GetContentDirectoryHandle();
    }

    /// <summary>
    /// The provider for ContentDirectory resources. Given a valid ContentDirectory location, this provider
    /// can load the ContentDirectory and provide a handle to it.
    /// </summary>
    public class ContentDirectoryProvider : ResourceProviderBase
    {
        ContentDirectoryResource m_ContentDirectoryResource;

        /// <inheritdoc/>
        public override void Provide(ProvideHandle provideHandle)
        {
            m_ContentDirectoryResource = new ContentDirectoryResource();
            m_ContentDirectoryResource.Start(provideHandle);
        }

        public override void Release(IResourceLocation location, object obj)
        {
            //TODO: Some validation based on the location? The AssetBundleProvider does validation.
            m_ContentDirectoryResource.Release();
        }

        /// <inheritdoc/>
        public override IOperationCacheKey CreateCacheKeyForLocation(ResourceManager rm, IResourceLocation location, Type desiredType)
        {
            //We need to transform the ID first
            //so we don't try and load the same bundle twice if the user is manipulating the path at runtime.
            return new IdCacheKey(location.GetType(), rm.TransformInternalId(location));
        }
    }

    /// <summary>
    /// A container for a ContentDirectory that is being loaded.
    /// </summary>
    public class ContentDirectoryResource : IContentDirectoryResource, IUpdateReceiver
    {
        ContentDirectoryHandle m_ContentDirectoryHandle;
        ProvideHandle m_ProvideHandle;

        /// <inheritdoc/>
        public ContentDirectoryHandle GetContentDirectoryHandle()
        {
            return m_ContentDirectoryHandle;
        }

        /// <summary>
        /// Starts the load of the ContentDirectory.
        /// </summary>
        /// <param name="provideHandle"></param>
        public void Start(ProvideHandle provideHandle)
        {
            m_ProvideHandle = provideHandle;
            BeginOperation();
        }

        /// <inheritdoc/>
        public void Update(float unscaledDeltaTime)
        {
            //Do nothing for now. This is only included now so that when we start
            //doing remote content, we don't have to break the API in order to add
            //download metrics like we have in the AssetBundleResource.
        }

        public void Release()
        {
            if (m_ContentDirectoryHandle.IsValid)
            {
                ContentLoadManager.UnregisterContentDirectory(m_ContentDirectoryHandle);
                m_ContentDirectoryHandle = default;
                m_ProvideHandle = default;
            }
        }

        private void BeginOperation()
        {
            string path;

            LoadType loadType;

            ResourceLocationUtil.GetLoadInfo(m_ProvideHandle.Location, m_ProvideHandle.ResourceManager, out loadType, out path);
            Debug.Log($"registering content directory {path}");

            //So, when entering and exiting playmode, the CDs don't get unloaded like asset bundles
            //Because CDs aren't part of the "cleanup the world" processes that happen on playmode exit.
            //Either, we need this mirror that functionality for CDs, or we need to handle it ourselves somehow.
            m_ContentDirectoryHandle = ContentLoadManager.RegisterContentDirectory(path);
            if (!m_ContentDirectoryHandle.IsValid)
            {
                m_ProvideHandle.Complete<ContentDirectoryResource>(null, false,
                    new ProviderException($"Failed to load ContentDirectory at {path}"));
                return;
            }

            m_ProvideHandle.Complete(this, true, null);
        }
    }
}
#endif
