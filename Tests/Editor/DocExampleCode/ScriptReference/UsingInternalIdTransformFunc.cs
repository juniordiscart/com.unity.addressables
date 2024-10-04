namespace AddressableAssets.DocExampleCode
{
    using System;
    using UnityEngine;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.ResourceManagement.ResourceLocations;
    using UnityEngine.ResourceManagement.ResourceProviders;
    using UnityEngine.AddressableAssets;

    internal class UsingInternalIdTransformFunc
    {
        #region DECLARATION
        static public Func<IResourceLocation, string> InternalIdTransformFunc { get; set; }
        #endregion

        #region SAMPLE
        public AssetReferenceGameObject asset; // Identify the asset
        AsyncOperationHandle<GameObject> opHandle;

        void UsingInternalIdTransformFuncSample()
        {
            Addressables.InternalIdTransformFunc = MyCustomTransform;
            opHandle = Addressables.InstantiateAsync(asset);
            opHandle.Completed += OnInstantiateComplete;
        }

        //Implement a method to transform the internal ids of locations
        static string MyCustomTransform(IResourceLocation location)
        {
            if (location.ResourceType == typeof(IAssetBundleResource)
                && !location.InternalId.StartsWith("http"))
            {
                Debug.Log($"Replace local identifier with remote URL : {location.InternalId}");
                return "file:///" + location.InternalId;
            }

            return location.InternalId;
        }

        void OnInstantiateComplete(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"Successfully instantiated GameObject named '{handle.Result.name}'");
            }
        }

        void ReleaseResources()
        {
            Addressables.Release(opHandle);
        }

        // When ready to release the asset, call ReleaseResources().
        // For example during OnDestroy().
        //void OnDestroy()
        //{
        //    ReleaseResources();
        //}
        #endregion
    }
}
