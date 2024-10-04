namespace AddressableAssets.DocExampleCode
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.ResourceManagement.ResourceLocations;
    using UnityEngine.ResourceManagement.ResourceProviders;

    internal class UsingInstanceProvider
    {
        #region DECLARATION
        public static IInstanceProvider InstanceProvider { get; }
        #endregion

        #region SAMPLE
        public AssetReferenceGameObject asset; // Identify the asset
        AsyncOperationHandle<GameObject> instHandle;
        AsyncOperationHandle<IList<IResourceLocation>> locHandle;

        void UsingInstanceProviderSample()
        {
            locHandle = Addressables.LoadResourceLocationsAsync(asset, typeof(GameObject));
            locHandle.Completed += OnLoadComplete;
        }

        void OnLoadComplete(AsyncOperationHandle<IList<IResourceLocation>> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"Successfully loaded resource locations");
                foreach (IResourceLocation location in handle.Result)
                {
                    ResourceManager rm = Addressables.ResourceManager;
                    IInstanceProvider provider = Addressables.InstanceProvider;
                    instHandle = rm.ProvideInstance(provider, location, default(InstantiationParameters));
                    instHandle.Completed += OnProvideInstanceComplete;
                }
            }
        }

        void OnProvideInstanceComplete(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"Successfully instantiated GameObject named '{handle.Result.name}'");
            }
        }

        void ReleaseResources()
        {
            Addressables.Release(locHandle);
            Addressables.Release(instHandle);
        }

        // When ready to release the asset, call ReleaseResources().
        // For example during OnDestroy().
        // void OnDestroy()
        // {
        //     ReleaseResources();
        // }
        #endregion
    }
}
