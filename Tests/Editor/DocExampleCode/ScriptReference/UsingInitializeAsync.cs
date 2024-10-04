namespace AddressableAssets.DocExampleCode
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.AddressableAssets.ResourceLocators;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.ResourceManagement.ResourceLocations;

    internal class UsingInitializeAsync
    {
        #region DECLARATION_1
        public static AsyncOperationHandle<IResourceLocator> InitializeAsync()
        #endregion
        {
            return default;
        }

        #region DECLARATION_2
        public static AsyncOperationHandle<IResourceLocator> InitializeAsync(bool autoReleaseHandle)
        #endregion
        {
            return default;
        }

        #region SAMPLE_LOADALL
        void UsingInitializeAsyncSampleCallback()
        {
            Addressables.InitializeAsync().Completed += OnInitializeComplete;
        }

        void OnInitializeComplete(AsyncOperationHandle<IResourceLocator> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("Addressables initialization succeeded");
                IResourceLocator locator = handle.Result;

                // locator.Keys includes bundle names and other identifiers from the local catalog.
                foreach (object locatorKey in locator.Keys)
                {
                    locator.Locate(locatorKey, typeof(UnityEngine.Object), out IList<IResourceLocation> locations);
                    if (locations == null)
                    {
                        continue;
                    }

                    foreach (IResourceLocation location in locations)
                    {
                        // The key representing the location of an Addressable asset.
                        string locationKey = location.InternalId;
                        Addressables.LoadAssetAsync<UnityEngine.Object>(locationKey).Completed += OnLoadAssetComplete;
                    }
                }
            }
        }

        void OnLoadAssetComplete(AsyncOperationHandle<UnityEngine.Object> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"Successfully loaded {handle.Result.name} ({handle.Result.GetType()})");
            }
        }

        #endregion

        #region SAMPLE_TASK
        async Task UsingInitializeAsyncSampleTask()
        {
            AsyncOperationHandle<IResourceLocator> handle = Addressables.InitializeAsync(false);
            await handle.Task;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("Addressables initialization succeeded");
                IResourceLocator locator = handle.Result;
                Debug.Log($"The resource locator returned has an id of {locator.LocatorId}");
            }
            Addressables.Release(handle);
        }

        #endregion
    }
}
