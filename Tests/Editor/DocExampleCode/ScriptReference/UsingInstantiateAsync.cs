namespace AddressableAssets.DocExampleCode
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.ResourceManagement.ResourceLocations;
    using UnityEngine.ResourceManagement.ResourceProviders;

    internal class UsingInstantiateAsync
    {
        #region DECLARATION_1
        public static AsyncOperationHandle<GameObject> InstantiateAsync(IResourceLocation location, Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true)
        #endregion
        {
            return default;
        }

        #region DECLARATION_2
        public static AsyncOperationHandle<GameObject> InstantiateAsync(IResourceLocation location, Vector3 position, Quaternion rotation, Transform parent = null, bool trackHandle = true)
        #endregion
        {
            return Addressables.InstantiateAsync(location, position, rotation, parent, trackHandle);
        }

        #region DECLARATION_3
        public static AsyncOperationHandle<GameObject> InstantiateAsync(IResourceLocation location, InstantiationParameters instantiateParameters, bool trackHandle = true)
        #endregion
        {
            return default;
        }

        #region DECLARATION_4
        public static AsyncOperationHandle<GameObject> InstantiateAsync(object key, Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true)
        #endregion
        {
            return default;
        }

        #region DECLARATION_5
        public static AsyncOperationHandle<GameObject> InstantiateAsync(object key, Vector3 position, Quaternion rotation, Transform parent = null, bool trackHandle = true)
        #endregion
        {
            return default;
        }

        #region DECLARATION_6
        public static AsyncOperationHandle<GameObject> InstantiateAsync(object key, InstantiationParameters instantiateParameters, bool trackHandle = true)
        #endregion
        {
            return default;
        }

        #region SAMPLE_LOCATION
        public AssetReferenceGameObject assetRefLocation; // Identify the asset
        public Transform parentTransformLocation; // Identify the transform of the parent GameObject
        AsyncOperationHandle<IList<IResourceLocation>> locHandle;
        GameObject instanceLocation;

        void UsingInstantiateAsyncSampleLocation()
        {
            locHandle = Addressables.LoadResourceLocationsAsync(assetRefLocation, typeof(GameObject));
            locHandle.Completed += OnLoadComplete;
        }

        void OnLoadComplete(AsyncOperationHandle<IList<IResourceLocation>> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"Successfully loaded resource locations");
                foreach (IResourceLocation location in handle.Result)
                {
                    string locationKey = location.InternalId;
                    var instParams = new InstantiationParameters(parentTransformLocation, false);
                    Addressables.InstantiateAsync(location, instParams).Completed += OnInstantiateCompleteLocation;
                }
            }
        }

        void OnInstantiateCompleteLocation(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                instanceLocation = handle.Result;
                Debug.Log($"Successfully instantiated GameObject with name '{instanceLocation.name}'.");
            }
        }

        void ReleaseResourcesLocation()
        {
            Addressables.Release(locHandle);
            Addressables.ReleaseInstance(instanceLocation);
        }

        // When ready to release the asset, call ReleaseTrackedResources().
        // For example during OnDestroy().
        //void OnDestroy()
        //{
        //    ReleaseResourcesLocation();
        //}
        #endregion

        #region SAMPLE_OBJECT_TRACKED
        public AssetReferenceGameObject assetRefTracked; // Identify the asset
        public Transform parentTransformTracked; // Identify the transform of the parent GameObject
        GameObject instanceTracked;

        void UsingInstantiateAsyncSampleKeyTracked()
        {
            var instParams = new InstantiationParameters(parentTransformTracked, false);
            Addressables.InstantiateAsync(assetRefTracked, instParams).Completed += OnInstantiateCompleteObjectTracked;
        }

        void OnInstantiateCompleteObjectTracked(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                instanceTracked = handle.Result;
                Debug.Log($"Successfully instantiated GameObject with name '{instanceTracked.name}'.");
            }
        }

        void ReleaseTrackedResources()
        {
            Addressables.ReleaseInstance(instanceTracked);
        }

        // When ready to release the asset, call ReleaseTrackedResources().
        // For example during OnDestroy().
        //void OnDestroy()
        //{
        //    ReleaseTrackedResources();
        //}
        #endregion

        #region SAMPLE_OBJECT_UNTRACKED
        public AssetReferenceGameObject assetRefUntracked; // Identify the asset
        public Transform parentTransformUntracked; // Identify the transform component of the parent GameObject
        AsyncOperationHandle<GameObject> handleUntracked;

        void UsingInstantiateAsyncSampleKeyUntracked()
        {
            var instParams = new InstantiationParameters(parentTransformUntracked, false);
            handleUntracked = Addressables.InstantiateAsync(assetRefUntracked, instParams, false);
            handleUntracked.Completed += OnInstantiateCompleteObjectUntracked;
        }

        void OnInstantiateCompleteObjectUntracked(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"Successfully instantiated GameObject with name '{handle.Result.name}'.");
            }
        }

        void ReleaseUntrackedResources()
        {
            Addressables.Release(handleUntracked);
        }

        // When ready to release the asset, call ReleaseUntrackedResources().
        // For example during OnDestroy().
        //void OnDestroy()
        //{
        //    ReleaseUntrackedResources();
        //}
        #endregion
    }
}
