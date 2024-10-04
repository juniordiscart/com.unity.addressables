using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Profiling;
using UnityEngine.Profiling;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.ResourceManagement.Profiling
{
    internal static class ProfilerRuntime
    {
        public static readonly Guid kResourceManagerProfilerGuid = new Guid("4f8a8c93-7634-4ef7-bbbc-6c9928567fa4");
        public const int kCatalogTag = 0;
        public const int kBundleDataTag = 1;
        public const int kAssetDataTag = 2;
        public const int kSceneDataTag = 3;

        private static ProfilerCounterValue<int> CatalogLoadCounter = new ProfilerCounterValue<int>(ProfilerCategory.Loading, "Catalogs", ProfilerMarkerDataUnit.Count);
        private static ProfilerCounterValue<int> AssetBundleLoadCounter = new ProfilerCounterValue<int>(ProfilerCategory.Loading, "Asset Bundles", ProfilerMarkerDataUnit.Count);
        private static ProfilerCounterValue<int> AssetLoadCounter = new ProfilerCounterValue<int>(ProfilerCategory.Loading, "Assets", ProfilerMarkerDataUnit.Count);
        private static ProfilerCounterValue<int> SceneLoadCounter = new ProfilerCounterValue<int>(ProfilerCategory.Loading, "Scenes", ProfilerMarkerDataUnit.Count);

        private static ProfilerFrameData<Hash128, CatalogFrameData> m_CatalogData = new ProfilerFrameData<Hash128, CatalogFrameData>(4);
        private static ProfilerFrameData<IAsyncOperation, BundleFrameData> m_BundleData = new ProfilerFrameData<IAsyncOperation, BundleFrameData>(64);
        private static ProfilerFrameData<IAsyncOperation, AssetFrameData> m_AssetData = new ProfilerFrameData<IAsyncOperation, AssetFrameData>(512);
        private static ProfilerFrameData<IAsyncOperation, AssetFrameData> m_SceneData = new ProfilerFrameData<IAsyncOperation, AssetFrameData>(16);

        private static Dictionary<string, IAsyncOperation> m_BundleNameToOperation = new Dictionary<string, IAsyncOperation>(64);
        private static Dictionary<string, List<IAsyncOperation>> m_BundleNameToAssetOperations = new Dictionary<string, List<IAsyncOperation>>(512);
        private static Dictionary<IAsyncOperation, (int, float)> m_DataChange = new Dictionary<IAsyncOperation, (int, float)>(64);

        public static void Initialise()
        {
            CatalogLoadCounter.Value = 0;
            AssetBundleLoadCounter.Value = 0;
            AssetLoadCounter.Value = 0;
            SceneLoadCounter.Value = 0;

            MonoBehaviourCallbackHooks.Instance.OnLateUpdateDelegate += InstanceOnOnLateUpdateDelegate;
        }

        private static void InstanceOnOnLateUpdateDelegate(float deltaTime)
        {
            PushToProfilerStream();
        }

        public static void AddCatalog(Hash128 buildHash)
        {
            if (!buildHash.isValid)
                return;

            m_CatalogData.Add(buildHash, new CatalogFrameData(){BuildResultHash = buildHash});
            CatalogLoadCounter.Value++;
        }

        public static void AddBundleOperation(ProvideHandle handle, [NotNull] AssetBundleRequestOptions requestOptions, ContentStatus status, BundleSource source)
        {
            IAsyncOperation op = handle.InternalOp as IAsyncOperation;
            if (op == null)
            {
                string msg = "Could not get Bundle operation for handle loaded for Key " + handle.Location.PrimaryKey;
                throw new System.NullReferenceException(msg);
            }

            string bundleName = requestOptions.BundleName;
            BundleOptions loadingOptions = BundleOptions.None;

            bool doCRC = requestOptions.Crc != 0;
            if (doCRC && source == BundleSource.Cache)
                doCRC = requestOptions.UseCrcForCachedBundle;
            if (doCRC)
                loadingOptions |= BundleOptions.CheckSumEnabled;
            if (!string.IsNullOrEmpty(requestOptions.Hash))
                loadingOptions |= BundleOptions.CachingEnabled;

            BundleFrameData data = new BundleFrameData()
            {
                ReferenceCount = op.ReferenceCount,
                BundleCode = bundleName.GetHashCode(),
                Status = status,
                LoadingOptions = loadingOptions,
                Source = source
            };

            m_BundleData.Add(op, data);
            if (!m_BundleNameToOperation.ContainsKey(bundleName))
                AssetBundleLoadCounter.Value += 1;
            m_BundleNameToOperation[bundleName] = op;
        }

        public static void BundleReleased(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName) || !m_BundleNameToOperation.TryGetValue(bundleName, out var op))
                return;

            m_BundleData.Remove(op);
            m_BundleNameToOperation.Remove(bundleName);
            AssetBundleLoadCounter.Value -= 1;

            // remove all the assets from the bundle
            if (m_BundleNameToAssetOperations.TryGetValue(bundleName, out var assetOps))
            {
                m_BundleNameToAssetOperations.Remove(bundleName);
                foreach (IAsyncOperation assetOp in assetOps)
                {
                    AssetLoadCounter.Value -= 1;
                    m_AssetData.Remove(assetOp);
                }
            }
        }

        public static void AddAssetOperation(ProvideHandle handle, ContentStatus status)
        {
            if (!handle.IsValid)
                throw new ArgumentException("Attempting to add a Asset handle to profiler that is not valid");
            IAsyncOperation assetLoadOperation = handle.InternalOp as IAsyncOperation;
            if (assetLoadOperation == null)
                throw new NullReferenceException("Could not get operation for InternalOp of handle loaded with primary key: " + handle.Location.PrimaryKey);

            string containingBundleName = GetContainingBundleNameForLocation(handle.Location);

            string assetId;
            if (handle.Location.InternalId.EndsWith(']'))
            {
                int start = handle.Location.InternalId.IndexOf('[');
                assetId = handle.Location.InternalId.Remove(start);
            }
            else
                assetId = handle.Location.InternalId;

            AssetFrameData profileObject = new AssetFrameData();
            profileObject.AssetCode = assetId.GetHashCode();
            profileObject.ReferenceCount = assetLoadOperation.ReferenceCount;
            profileObject.BundleCode = containingBundleName.GetHashCode();
            profileObject.Status = status;

            if (m_BundleNameToAssetOperations.TryGetValue(containingBundleName, out List<IAsyncOperation> assetOperations))
            {
                if (!assetOperations.Contains(assetLoadOperation))
                    assetOperations.Add(assetLoadOperation);
            }
            else
                m_BundleNameToAssetOperations.Add(containingBundleName, new List<IAsyncOperation>(){assetLoadOperation});

            if (m_AssetData.Add(assetLoadOperation, profileObject))
                AssetLoadCounter.Value += 1;
        }

        private static string GetContainingBundleNameForLocation(IResourceLocation location)
        {
            if (location == null || location.Dependencies == null || location.Dependencies.Count == 0)
            {
                // AssetDatabase mode has no dependencies
                return "";
            }

            AssetBundleRequestOptions options = location.Dependencies[0].Data as AssetBundleRequestOptions;
            if (options == null)
            {
                Debug.LogError($"Dependency bundle location does not have AssetBundleRequestOptions");
                return "";
            }

            return options.BundleName;
        }

        public static void AddSceneOperation(AsyncOperationHandle<SceneInstance> handle, IResourceLocation location, ContentStatus status)
        {
            IAsyncOperation sceneLoadOperation = handle.InternalOp as IAsyncOperation;
            Debug.Assert(sceneLoadOperation != null, "Could not get operation for " + location.PrimaryKey);

            string containingBundleName = GetContainingBundleNameForLocation(location);

            AssetFrameData profileObject = new AssetFrameData();
            profileObject.AssetCode = location.InternalId.GetHashCode();
            profileObject.ReferenceCount = sceneLoadOperation.ReferenceCount;
            profileObject.BundleCode = containingBundleName.GetHashCode();
            profileObject.Status = status;

            if (m_SceneData.Add(sceneLoadOperation, profileObject))
                SceneLoadCounter.Value += 1;
        }

        public static void SceneReleased(AsyncOperationHandle<SceneInstance> handle)
        {
            if (handle.InternalOp is ChainOperationTypelessDepedency<SceneInstance> chainOp)
            {
                if (m_SceneData.Remove(chainOp.WrappedOp.InternalOp))
                    SceneLoadCounter.Value -= 1;
                else
                    Debug.LogWarning($"Failed to remove scene from Addressables profiler for " + chainOp.WrappedOp.DebugName);
            }
            else
            {
                if (m_SceneData.Remove(handle.InternalOp))
                    SceneLoadCounter.Value -= 1;
                else
                    Debug.LogWarning($"Failed to remove scene from Addressables profiler for " + handle.DebugName);
            }
        }

        private static void PushToProfilerStream()
        {
            if (!Profiler.enabled)
                return;
            RefreshChangedReferenceCounts();
            Profiler.EmitFrameMetaData(kResourceManagerProfilerGuid, kCatalogTag, m_CatalogData.Values);
            Profiler.EmitFrameMetaData(kResourceManagerProfilerGuid, kBundleDataTag, m_BundleData.Values);
            Profiler.EmitFrameMetaData(kResourceManagerProfilerGuid, kAssetDataTag, m_AssetData.Values);
            Profiler.EmitFrameMetaData(kResourceManagerProfilerGuid, kSceneDataTag, m_SceneData.Values);
        }

        private static void RefreshChangedReferenceCounts()
        {
            m_DataChange.Clear();

            foreach (KeyValuePair<IAsyncOperation, BundleFrameData> pair in m_BundleData.Data)
            {
                if (ShouldUpdateFrameDataWithOperationData(pair.Key, pair.Value.ReferenceCount, pair.Value.PercentComplete, out (int, float) newValues))
                    m_DataChange.Add(pair.Key, newValues);
            }
            foreach (KeyValuePair<IAsyncOperation, (int, float)> pair in m_DataChange)
            {
                var temp = m_BundleData[pair.Key];
                temp.ReferenceCount = pair.Value.Item1;
                temp.PercentComplete = pair.Value.Item2;
                m_BundleData[pair.Key] = temp;
            }
            m_DataChange.Clear();

            foreach (KeyValuePair<IAsyncOperation,AssetFrameData> pair in m_AssetData.Data)
            {
                if (ShouldUpdateFrameDataWithOperationData(pair.Key, pair.Value.ReferenceCount, pair.Value.PercentComplete, out (int, float) newValues))
                    m_DataChange.Add(pair.Key, newValues);
            }
            foreach (KeyValuePair<IAsyncOperation, (int, float)> pair in m_DataChange)
            {
                var temp = m_AssetData[pair.Key];
                temp.ReferenceCount = pair.Value.Item1;
                temp.PercentComplete = pair.Value.Item2;
                m_AssetData[pair.Key] = temp;
            }
            m_DataChange.Clear();

            foreach (KeyValuePair<IAsyncOperation,AssetFrameData> pair in m_SceneData.Data)
            {
                if (ShouldUpdateFrameDataWithOperationData(pair.Key, pair.Value.ReferenceCount, pair.Value.PercentComplete, out (int, float) newValues))
                    m_DataChange.Add(pair.Key, newValues);
            }
            foreach (KeyValuePair<IAsyncOperation, (int, float)> pair in m_DataChange)
            {
                var temp = m_SceneData[pair.Key];
                temp.ReferenceCount = pair.Value.Item1;
                temp.PercentComplete = pair.Value.Item2;
                m_SceneData[pair.Key] = temp;
            }
        }

        // Because the ProfilerFrameData keeps track of both a dictionary and array, and not updated often,
        // check if done on if to update the collection
        private static bool ShouldUpdateFrameDataWithOperationData(IAsyncOperation activeOperation, int frameReferenceCount, float framePercentComplete, out (int, float) newDataOut)
        {
            int currentReferenceCount = activeOperation.ReferenceCount;
            switch (activeOperation.Status)
            {
                case AsyncOperationStatus.Succeeded:
                    break;
                case AsyncOperationStatus.Failed:
                    currentReferenceCount = 0;
                    break;
                case AsyncOperationStatus.None:
                    bool inProgress = !activeOperation.IsDone && activeOperation.IsRunning;
                    if (!inProgress)
                        currentReferenceCount = 0;
                    break;
            }

            float currentPercentComplete = activeOperation.PercentComplete;

            newDataOut = (currentReferenceCount, currentPercentComplete);

            return currentReferenceCount != frameReferenceCount
                    || !Mathf.Approximately(currentPercentComplete, framePercentComplete);
        }
    }
}
