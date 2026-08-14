#if ENABLE_CONTENT_DIRECTORIES
using System;
using System.Collections.Generic;
using Unity.Loading;
using UnityEngine.ResourceManagement.Exceptions;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    internal class GroupRootAssetResource
    {
        Object m_GroupRootAsset;
        ContentDirectoryHandle m_ContentDirectoryHandle;
        ProvideHandle m_ProvideHandle;

        IContentDirectoryResource GetContentDirectoryResourceFromDependencies(IList<object> deps)
        {
            //We go through this way because the Loadable should have its ContentDirectory as a dependency.
            //We'll build that into the location during the post-build step.
            if (deps != null && deps.Count > 0)
            {
                foreach (var d in deps)
                {
                    if (d is IContentDirectoryResource)
                        return d as IContentDirectoryResource;
                }
            }
            return null;
        }

        public void Release()
        {
            m_GroupRootAsset = null;
        }

        public void Start(ProvideHandle provideHandle)
        {
            m_ProvideHandle = provideHandle;
            List<object> deps = new List<object>();
            m_ProvideHandle.GetDependencies(deps);
            IContentDirectoryResource cdr = GetContentDirectoryResourceFromDependencies(deps);
            if (cdr != null)
                m_ContentDirectoryHandle = cdr.GetContentDirectoryHandle();
            else
            {
                m_ProvideHandle.Complete<IContentDirectoryResource>(null, false,
                    new ProviderException("No valid ContentDirectoryResource found in dependencies."));
                return;
            }

            if (!m_ContentDirectoryHandle.IsValid)
            {
                m_ProvideHandle.Complete<IContentDirectoryResource>(null, false,
                    new ProviderException("Invalid ContentDirectoryHandle found in ContentDirectoryResource."));
                return;
            }

            string key = m_ProvideHandle.Location.InternalId;
            Type resourceType = m_ProvideHandle.Location.ResourceType;

            var rootAssets = ContentLoadManager.GetRootAssets(m_ContentDirectoryHandle);
            if (rootAssets.Length == 0)
            {
                m_ProvideHandle.Complete<Object>(null, false,
                    new ProviderException("No root assets found in ContentDirectory."));
                return;
            }
            foreach(var rootAsset in rootAssets)
            {
                if (rootAsset.name == key)
                {
                    m_GroupRootAsset = rootAsset;
                    break;
                }
            }

            if (m_GroupRootAsset == null)
            {
                m_ProvideHandle.Complete<Object>(null, false,
                    new ProviderException($"No root object found in ContentDirectory with name {key}."));
                return;
            }

            var loadedObject = Convert.ChangeType(m_GroupRootAsset, resourceType);
            m_ProvideHandle.Complete(loadedObject, loadedObject != null, loadedObject == null ? new ProviderException($"Failed to load object of type {resourceType} with key {key}") : null);
        }
    }

    /// <summary>
    /// The provider responsible for loading Root Asset objects from a ContentDirectory.
    /// </summary>
    public class GroupRootAssetProvider : ResourceProviderBase
    {
        GroupRootAssetResource m_GroupRootAssetResource;

        /// <summary>
        /// Provide the Root Object object specified in the provideHandle.
        /// </summary>
        /// <param name="provideHandle">The provide handle with the location information for the requested asset.</param>
        public override void Provide(ProvideHandle provideHandle)
        {
            m_GroupRootAssetResource = new GroupRootAssetResource();
            m_GroupRootAssetResource.Start(provideHandle);
        }

        /// <summary>
        /// Release the Root Object object
        /// </summary>
        /// <param name="location"></param>
        /// <param name="obj"></param>
        public override void Release(IResourceLocation location, object obj)
        {
            if (m_GroupRootAssetResource != null)
            {
                m_GroupRootAssetResource.Release();
            }
        }
    }
}
#endif
