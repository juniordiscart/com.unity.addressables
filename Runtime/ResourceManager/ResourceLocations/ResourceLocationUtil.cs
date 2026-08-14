using System;
using System.IO;
using UnityEngine;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using static UnityEngine.ResourceManagement.ResourceProviders.AssetBundleResource;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    /// <summary>
    /// Utility class for working with resource locations in the Resource Manager.
    /// </summary>
    public static class ResourceLocationUtil
    {
        /// <summary>
        /// Determines the load type and path for a given resource location.
        /// Evaluates whether the resource should be loaded locally or via web request based on the path and options.
        /// </summary>
        /// <param name="location">The resource location to evaluate.</param>
        /// <param name="resourceManager">The resource manager used to transform the internal ID.</param>
        /// <param name="loadType">Output parameter indicating whether to load locally or via web request.</param>
        /// <param name="path">Output parameter containing the resolved path to load from.</param>
        public static void GetLoadInfo(IResourceLocation location, ResourceManager resourceManager, out LoadType loadType, out string path)
        {
            var options = location?.Data as AssetBundleRequestOptions;
            if (options == null)
            {
                loadType = LoadType.Local;
                path = resourceManager.TransformInternalId(location);
                if (ResourceManagerConfig.ShouldPathUseWebRequest(path))
                    Debug.LogWarning($"Location {location} appears to be remote but the download option have been stripped.  Ensure that the group that contains this bundle does not have StripDownloadOptions enabled.");
                return;
            }

            path = resourceManager.TransformInternalId(location);
            if (Application.platform == RuntimePlatform.Android && path.StartsWith("jar:", StringComparison.Ordinal))
            {
                loadType = options.UseUnityWebRequestForLocalBundles ? LoadType.Web : LoadType.Local;
            }
            else if (ResourceManagerConfig.ShouldPathUseWebRequest(path))
            {
                loadType = LoadType.Web;
            }
            else if (options.UseUnityWebRequestForLocalBundles)
            {
                path = "file:///" + Path.GetFullPath(path);
                loadType = LoadType.Web;
            }
            else
            {
                loadType = LoadType.Local;
            }

            if (loadType == LoadType.Web)
                path = path.Replace('\\', '/');
        }
    }
}
