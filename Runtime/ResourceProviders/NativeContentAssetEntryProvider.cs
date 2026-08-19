#if ENABLE_CONTENT_DIRECTORIES
using System;
using System.Collections.Generic;
using Unity.Loading;
using Unity.Loading.LowLevel;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.AddressableAssets.ResourceProviders
{
    /// <summary>
    /// Loads objects from a GroupRootAsset by issuing batched requests through the
    /// L0 NativeLoadingSystem API. Requests accumulate into a fixed-size staging
    /// buffer and are submitted in one LoadAsync call per frame (or sooner if the
    /// buffer fills).
    /// </summary>
    public class NativeContentAssetEntryProvider : ResourceProviderBase, IUpdateReceiver
    {
        const int kBatchCapacity = 32;
        const int kDrainChunkSize = 32;

        // Single-asset Provide. Scalar fields - no per-Provide array allocs.
        sealed class PendingSingle
        {
            public NativeContentAssetEntryProvider m_Provider;
            public readonly Func<bool> waitCallback;

            public ProvideHandle provideHandle;
            public ResourceHandle handle;
            public EntityId entityId;
            public bool succeeded;

            public PendingSingle()
            {
                waitCallback = WaitForCompletion;
            }

            bool WaitForCompletion() => m_Provider.WaitForSingle(this);
        }

        // IList<T> Provide. Parallel arrays sized to subasset count. Allocated fresh per
        // Provide call - the list path is rare, so we don't bother pooling.
        sealed class PendingList
        {
            public ProvideHandle provideHandle;
            public ResourceHandle[] handles;
            public EntityId[] entityIds;
            public bool[] succeeded;
            public int remaining;
            public Func<bool> waitCallback;
        }

        struct InFlightListSlot
        {
            public PendingList owner;
            public int slot;
        }

        readonly LoadableObjectId[] m_StagingIds = new LoadableObjectId[kBatchCapacity];
        readonly ResourceHandle[] m_StagingHandles = new ResourceHandle[kBatchCapacity];
        readonly object[] m_StagingOwners = new object[kBatchCapacity];
        readonly int[] m_StagingSlots = new int[kBatchCapacity];
        int m_StagingCount;

        // Pinned each Drain call to receive results from the L0 response queue.
        readonly AsyncResult[] m_DrainBuffer = new AsyncResult[kDrainChunkSize];

        // L0 handle value -> in-flight owner. Two dicts so the single-asset path stays scalar.
        readonly Dictionary<ulong, PendingSingle> m_InFlightSingles = new Dictionary<ulong, PendingSingle>();
        readonly Dictionary<ulong, InFlightListSlot> m_InFlightListSlots = new Dictionary<ulong, InFlightListSlot>();

        // Populated when an owner completes; used by Release() to issue ReleaseAsync.
        readonly Dictionary<IResourceLocation, ResourceHandle> m_SingleHandlesByLocation = new Dictionary<IResourceLocation, ResourceHandle>();
        readonly Dictionary<IResourceLocation, ResourceHandle[]> m_ListHandlesByLocation = new Dictionary<IResourceLocation, ResourceHandle[]>();

        // Reused across calls to keep the single-asset path allocation-free.
        readonly List<object> m_DepsBuffer = new List<object>();

        readonly LoadingResponseQueue m_Queue;
        readonly int m_MainThreadId;

        public NativeContentAssetEntryProvider()
        {
            m_MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            m_Queue = new LoadingResponseQueue();
        }

#if ADDR_NATIVECONTENT_STATS
        // Batching stats. Cumulative across the lifetime of this provider instance.
        const float kStatsReportInterval = 5f;
        int m_StatsFlushCount;
        int m_StatsRequestCount;
        int m_StatsMaxBatchSize;
        int m_StatsBufferFullFlushes;
        int m_StatsSingleProvides;
        int m_StatsListProvides;
        float m_StatsTotalTime;
        float m_StatsTimeAtLastReport;
#endif

        /// <inheritdoc/>
        public override Type SceneDependencyResourceType => typeof(Object);

        GroupRootAsset GetContentDirectoryResourceFromDependencies(ref ProvideHandle provideHandle)
        {
            provideHandle.GetDependencies(m_DepsBuffer);
            foreach (var d in m_DepsBuffer)
            {
                if (d is GroupRootAsset cdr)
                {
                    m_DepsBuffer.Clear();
                    return cdr;
                }
            }
            m_DepsBuffer.Clear();
            throw new Exception("NativeContentAssetEntryProvider failed to find GroupRootAsset in dependencies.");
        }

        /// <inheritdoc />
        public override void Provide(ProvideHandle provideHandle)
        {
            try
            {
                GroupRootAsset cdr = GetContentDirectoryResourceFromDependencies(ref provideHandle);

                if (HandleListRequest(ref provideHandle, cdr))
                    return;

                string primaryKey = provideHandle.Location.PrimaryKey;
                LoadableInfo info = cdr.GetLoadableInfo(primaryKey, provideHandle.Location.ResourceType);
                if (info == null)
                    throw new Exception($"NativeContentAssetEntryProvider failed to find LoadableInfo for {primaryKey} in GroupRootAsset.");

                var single = GetPendingSingle(provideHandle);
                Enqueue(info.loadable.LoadableObjectId, single, 0);
                provideHandle.SetWaitForCompletionCallback(single.waitCallback);
#if ADDR_NATIVECONTENT_STATS
                m_StatsSingleProvides++;
#endif
            }
            catch (Exception ex)
            {
                provideHandle.Complete<object>(null, false, ex);
            }
        }

        bool HandleListRequest(ref ProvideHandle provideHandle, GroupRootAsset cdr)
        {
            Type requestedType = provideHandle.Type;
            if (!requestedType.IsGenericType || typeof(IList<>) != requestedType.GetGenericTypeDefinition())
                return false;

            // IList<T> path: load all subassets in one batch.
            string primaryKey = provideHandle.Location.PrimaryKey;
            Type elementType = requestedType.GetGenericArguments()[0];
            var subassets = cdr.GetAllSubAssets(primaryKey, elementType);

            if (subassets == null || subassets.Count == 0)
                throw new Exception($"NativeContentAssetEntryProvider failed to find subassets for {primaryKey} of type {elementType}.");

            var list = new PendingList
            {
                provideHandle = provideHandle,
                handles = new ResourceHandle[subassets.Count],
                entityIds = new EntityId[subassets.Count],
                succeeded = new bool[subassets.Count],
                remaining = subassets.Count
            };
            list.waitCallback = () => WaitForList(list);

            for (int i = 0; i < subassets.Count; i++)
                Enqueue(subassets[i].loadable.LoadableObjectId, list, i);
            provideHandle.SetWaitForCompletionCallback(list.waitCallback);
#if ADDR_NATIVECONTENT_STATS
            m_StatsListProvides++;
#endif
            return true;
        }

        PendingSingle GetPendingSingle(ProvideHandle provideHandle)
        {
            PendingSingle s = Pool.GenericPool<PendingSingle>.Get();
            s.provideHandle = provideHandle;
            s.m_Provider = this;
            return s;
        }

        void ReleasePendingSingle(PendingSingle s)
        {
            s.m_Provider = null;
            s.provideHandle = default;
            s.handle = default;
            s.entityId = default;
            s.succeeded = false;
            Pool.GenericPool<PendingSingle>.Release(s);
        }

        void Enqueue(LoadableObjectId id, object owner, int slot)
        {
            if (m_StagingCount == kBatchCapacity)
            {
#if ADDR_NATIVECONTENT_STATS
                m_StatsBufferFullFlushes++;
#endif
                Flush();
            }

            m_StagingIds[m_StagingCount] = id;
            m_StagingOwners[m_StagingCount] = owner;
            m_StagingSlots[m_StagingCount] = slot;
            m_StagingCount++;
        }

        unsafe void Flush()
        {
            if (m_StagingCount == 0)
                return;

            int count = m_StagingCount;
#if ADDR_NATIVECONTENT_STATS
            m_StatsFlushCount++;
            m_StatsRequestCount += count;
            if (count > m_StatsMaxBatchSize)
                m_StatsMaxBatchSize = count;
#endif

            fixed (LoadableObjectId* idPtr = m_StagingIds)
            fixed (ResourceHandle* handlePtr = m_StagingHandles)
            {
                NativeLoadingSystem.LoadAsync(idPtr, handlePtr, count, m_Queue);
            }

            for (int i = 0; i < count; i++)
            {
                ResourceHandle h = m_StagingHandles[i];
                object owner = m_StagingOwners[i];

                if (owner is PendingSingle s)
                {
                    s.handle = h;
                    m_InFlightSingles[h.value] = s;
                }
                else
                {
                    var l = (PendingList)owner;
                    int slot = m_StagingSlots[i];
                    l.handles[slot] = h;
                    m_InFlightListSlots[h.value] = new InFlightListSlot { owner = l, slot = slot };
                }

                m_StagingOwners[i] = null;
            }

            m_StagingCount = 0;
        }

        unsafe void Drain()
        {
            List<PendingSingle> completedSingles = null;
            List<PendingList> completedLists = null;

            fixed (AsyncResult* results = m_DrainBuffer)
            {
                int n;
                while ((n = m_Queue.ConsumeResults(results, kDrainChunkSize)) > 0)
                {
                    for (int i = 0; i < n; i++)
                    {
                        AsyncResult r = results[i];
                        if (r.type != AsyncResultType.Load)
                            continue;

                        ulong handleVal = r.handle.value;

                        if (m_InFlightSingles.Remove(handleVal, out PendingSingle s))
                        {
                            s.entityId = r.objectId;
                            s.succeeded = r.resultCode == ReturnCode.Completed;
                            if (completedSingles == null)
                                completedSingles = Pool.ListPool<PendingSingle>.Get();
                            completedSingles.Add(s);
                            continue;
                        }

                        if (m_InFlightListSlots.Remove(handleVal, out InFlightListSlot ls))
                        {
                            ls.owner.entityIds[ls.slot] = r.objectId;
                            ls.owner.succeeded[ls.slot] = r.resultCode == ReturnCode.Completed;
                            if (--ls.owner.remaining == 0)
                            {
                                if (completedLists == null)
                                    completedLists = Pool.ListPool<PendingList>.Get();
                                completedLists.Add(ls.owner);
                            }
                        }
                    }
                }
            }

            if (completedSingles != null)
            {
                foreach (var s in completedSingles)
                {
                    try { CompleteSingle(s); }
                    catch (Exception ex) { Debug.LogException(ex); }
                }
                Pool.ListPool<PendingSingle>.Release(completedSingles);
            }

            if (completedLists != null)
            {
                foreach (var l in completedLists)
                {
                    try { CompleteList(l); }
                    catch (Exception ex) { Debug.LogException(ex); }
                }
                Pool.ListPool<PendingList>.Release(completedLists);
            }
        }

        void CompleteSingle(PendingSingle s)
        {
            ProvideHandle ph = s.provideHandle;
            IResourceLocation location = ph.Location;
            ResourceHandle handle = s.handle;

            Object loadedObject = null;
            if (s.succeeded && s.entityId.IsValid())
                loadedObject = Resources.EntityIdToObject(s.entityId);

            if (loadedObject == null)
            {
                ReleasePendingSingle(s);
                IssueReleaseSingle(handle);
                ph.Complete<Object>(null, false, new Exception($"NativeContentAssetEntryProvider failed to load {location.PrimaryKey} from GroupRootAsset."));
                return;
            }

            m_SingleHandlesByLocation[location] = handle;
            ReleasePendingSingle(s);
            ph.Complete(loadedObject, true, null);
        }

        void CompleteList(PendingList l)
        {
            ProvideHandle ph = l.provideHandle;
            IResourceLocation location = ph.Location;
            Type requestedType = ph.Type;
            Type elementType = requestedType.GetGenericArguments()[0];

            var loaded = new List<Object>(l.entityIds.Length);
            for (int i = 0; i < l.entityIds.Length; i++)
            {
                if (!l.succeeded[i])
                    continue;

                Object obj = l.entityIds[i].IsValid() ? Resources.EntityIdToObject(l.entityIds[i]) : null;
                if (obj != null && elementType.IsAssignableFrom(obj.GetType()))
                    loaded.Add(obj);
            }

            var result = ResourceManagerConfig.CreateListResult(requestedType, loaded.ToArray());
            if (result == null)
            {
                IssueRelease(l.handles);
                ph.Complete<object>(null, false, new Exception($"NativeContentAssetEntryProvider failed to create list result for {location.PrimaryKey}."));
                return;
            }

            m_ListHandlesByLocation[location] = l.handles;
            ph.Complete(result, true, null);
        }

        unsafe bool WaitForSingle(PendingSingle s)
        {
            Flush();
            ResourceHandle h = s.handle;
            NativeLoadingSystem.WaitForLoadCompletion(&h, 1);
            Drain();

            return true;
        }

        unsafe bool WaitForList(PendingList l)
        {
            Flush();

            if (l.remaining > 0)
            {
                fixed (ResourceHandle* handlePtr = l.handles)
                {
                    NativeLoadingSystem.WaitForLoadCompletion(handlePtr, l.handles.Length);
                }
                Drain();
            }

            return true;
        }

        unsafe void IssueReleaseSingle(ResourceHandle handle)
        {
            if (!handle.IsValid)
                return;

            ResourceHandle h = handle;
            NativeLoadingSystem.ReleaseAsync(&h, 1, m_Queue);
        }

        unsafe void IssueRelease(ResourceHandle[] handles)
        {
            if (handles == null || handles.Length == 0)
                return;

            fixed (ResourceHandle* handlePtr = handles)
            {
                NativeLoadingSystem.ReleaseAsync(handlePtr, handles.Length, m_Queue);
            }
        }

        /// <inheritdoc />
        public override void Release(IResourceLocation location, object obj)
        {
            if (location == null)
                return;

            // Release can be triggered from a GC finalizer.
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != m_MainThreadId)
                return;

            if (m_SingleHandlesByLocation.Remove(location, out ResourceHandle handle))
                IssueReleaseSingle(handle);
            else if (m_ListHandlesByLocation.Remove(location, out ResourceHandle[] handles))
                IssueRelease(handles);
        }

        void IUpdateReceiver.Update(float unscaledDeltaTime)
        {
            Flush();
            Drain();

#if ADDR_NATIVECONTENT_STATS
            m_StatsTotalTime += unscaledDeltaTime;
            if (m_StatsTotalTime - m_StatsTimeAtLastReport >= kStatsReportInterval)
            {
                ReportStats();
                m_StatsTimeAtLastReport = m_StatsTotalTime;
            }
#endif
        }

#if ADDR_NATIVECONTENT_STATS
        void ReportStats()
        {
            if (m_StatsFlushCount == 0 && m_StatsSingleProvides == 0 && m_StatsListProvides == 0)
                return;

            float avgBatch = m_StatsFlushCount > 0 ? (float)m_StatsRequestCount / m_StatsFlushCount : 0f;
            int endOfFrameFlushes = m_StatsFlushCount - m_StatsBufferFullFlushes;
            Debug.Log(
                $"[NativeContentAssetEntryProvider] cumulative over {m_StatsTotalTime:F1}s: " +
                $"{m_StatsSingleProvides} single + {m_StatsListProvides} list provides, " +
                $"{m_StatsRequestCount} requests over {m_StatsFlushCount} flushes " +
                $"(avg {avgBatch:F1}, max {m_StatsMaxBatchSize}/{kBatchCapacity}), " +
                $"{endOfFrameFlushes} end-of-frame + {m_StatsBufferFullFlushes} buffer-full");
        }
#endif
    }
}
#endif
