#if ENABLE_CONTENT_DIRECTORIES
using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.AddressableAssets.ResourceProviders
{
    /// <summary>
    /// The provider used to load objects from an GroupRootAsset.
    /// </summary>
    public class GroupRootAssetEntryProvider : ResourceProviderBase
    {
        /// <inheritdoc/>
        public override Type SceneDependencyResourceType => typeof(Object);

        GroupRootAsset GetContentDirectoryResourceFromDependencies(IList<object> deps)
        {
            //We go through this way because the Entry should have its GroupRootAsset as a dependency.
            //We'll build that into the location during the post-build step.
            if (deps != null && deps.Count > 0)
            {
                foreach (var d in deps)
                {
                    if (d is GroupRootAsset)
                        return d as GroupRootAsset;
                }
            }
            return null;
        }

        /// <inheritdoc />
        public override void Provide(ProvideHandle provideHandle)
        {
            List<object> deps = new List<object>();
            provideHandle.GetDependencies(deps);
            GroupRootAsset cdr = GetContentDirectoryResourceFromDependencies(deps);

            if(cdr == null)
            {
                provideHandle.Complete<GroupRootAsset>(null, false, new System.Exception("GroupRootAssetEntryProvider failed to find GroupRootAsset in dependencies."));
                return;
            }

            // Check if requesting IList<T> of subassets
            if (provideHandle.Type.IsGenericType && typeof(IList<>) == provideHandle.Type.GetGenericTypeDefinition())
            {
                Type elementType = provideHandle.Type.GetGenericArguments()[0];
                var subassets = cdr.GetAllSubAssets(provideHandle.Location.PrimaryKey, elementType);

                if (subassets == null || subassets.Count == 0)
                {
                    provideHandle.Complete<object>(null, false, new System.Exception($"GroupRootAssetEntryProvider failed to find subassets for {provideHandle.Location.PrimaryKey} of type {elementType}."));
                    return;
                }

                // Load all subassets
                var loadedAssets = new List<Object>();
                foreach (var subasset in subassets)
                {
                    var loaded = subasset.loadable.Load();
                    if (loaded != null && elementType.IsAssignableFrom(loaded.GetType()))
                    {
                        loadedAssets.Add(loaded);
                    }
                }

                // Convert to IList<T> using ResourceManagerConfig helper
                var result = ResourceManagerConfig.CreateListResult(provideHandle.Type, loadedAssets.ToArray());
                provideHandle.Complete(result, result != null,
                    result == null ? new System.Exception($"GroupRootAssetEntryProvider failed to create list result for {provideHandle.Location.PrimaryKey}.") : null);
                return;
            }

            // Handle single asset
            LoadableInfo info = cdr.GetLoadableInfo(provideHandle.Location.PrimaryKey, provideHandle.Location.ResourceType);

            if (info == null)
            {
                provideHandle.Complete<GroupRootAsset>(null, false, new System.Exception($"GroupRootAssetEntryProvider failed to find LoadableInfo for {provideHandle.Location.PrimaryKey} in GroupRootAsset."));
                return;
            }

            Object loadedObject = null;
            if (info.loadable.Status == Unity.Loading.LoadableStatus.Loaded)
                loadedObject = info.loadable.Target;
            else
                loadedObject = info.loadable.Load();

            provideHandle.Complete(loadedObject, loadedObject != null,
                loadedObject == null ? new System.Exception($"GroupRootAssetEntryProvider failed to load {provideHandle.Location.PrimaryKey} from GroupRootAsset.") : null);
        }
    }
}
#endif
