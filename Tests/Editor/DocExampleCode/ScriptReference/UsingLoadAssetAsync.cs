namespace AddressableAssets.DocExampleCode
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.ResourceManagement.ResourceLocations;

    internal class UsingLoadAssetAsync
    {
        #region DECLARATION_1
        public static AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(IResourceLocation location)
        #endregion
        {
            return default;
        }

        #region DECLARATION_2
        public static AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(object key)
        #endregion
        {
            return default;
        }

        #region SAMPLE_LOCATION
        public AssetReference materialLocation; // Identify the material
        public GameObject goLocation; // Identify the GameObject
        AsyncOperationHandle<Material> instHandleLocation;
        AsyncOperationHandle<IList<IResourceLocation>> locHandle;

        public void UsingLoadAssetAsyncSampleLocation()
        {
            locHandle = Addressables.LoadResourceLocationsAsync(materialLocation, typeof(Material));
            locHandle.Completed += OnLoadComplete;
        }

        void OnLoadComplete(AsyncOperationHandle<IList<IResourceLocation>> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"Successfully loaded resource locations");
                foreach (IResourceLocation location in handle.Result)
                {
                    instHandleLocation = Addressables.LoadAssetAsync<Material>(location);
                    instHandleLocation.Completed += OnLoadCompleteLocation;
                }
            }
        }

        void OnLoadCompleteLocation(AsyncOperationHandle<Material> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var loadedMaterial = handle.Result;
                Debug.Log($"Successfully loaded material '{loadedMaterial.name}'");

                var renderer = goLocation.GetComponent<MeshRenderer>();
                if (renderer == null)
                    renderer = goLocation.AddComponent<MeshRenderer>();
                renderer.material = loadedMaterial;
                Debug.Log($"Assigned loaded material to GameObject named '{goLocation.name}'");
            }
        }

        void ReleaseResourcesLocation()
        {
            Addressables.Release(locHandle);
            Addressables.Release(instHandleLocation);
        }

        // When ready to release the asset, call ReleaseResourcesLocation().
        // For example during OnDestroy().
        // void OnDestroy()
        // {
        //     ReleaseResourcesLocation();
        // }
        #endregion

        #region SAMPLE_KEY
        public AssetReference materialKey; // Identify the material
        public GameObject goKey; // Identify the GameObject
        AsyncOperationHandle<Material> handleKey;

        public void UsingLoadAssetAsyncSampleKey()
        {
            handleKey = Addressables.LoadAssetAsync<Material>(materialKey);
            handleKey.Completed += OnLoadCompleteKey;
        }

        void OnLoadCompleteKey(AsyncOperationHandle<Material> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var loadedMaterial = handle.Result;
                Debug.Log($"Successfully loaded material '{loadedMaterial.name}'");

                var renderer = goKey.GetComponent<MeshRenderer>();
                if (renderer == null)
                    renderer = goKey.AddComponent<MeshRenderer>();
                renderer.material = loadedMaterial;
                Debug.Log($"Assigned loaded material to GameObject named '{goKey.name}'");
            }
        }

        void ReleaseResourcesKey()
        {
            Addressables.Release(handleKey);
        }

        // When ready to release the asset, call ReleaseResourcesKey().
        // For example during OnDestroy().
        //void OnDestroy()
        //{
        //    ReleaseResourcesKey();
        //}
        #endregion
    }
}
