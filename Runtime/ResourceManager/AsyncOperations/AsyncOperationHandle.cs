using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.ResourceManagement.Exceptions;

namespace UnityEngine.ResourceManagement.AsyncOperations
{
    /// <summary>
    /// Handle for internal operations.  This allows for reference counting and checking for valid references.
    /// </summary>
    /// <typeparam name="TObject">The object type of the underlying operation.</typeparam>
    public struct AsyncOperationHandle<TObject> : IEnumerator, IEquatable<AsyncOperationHandle<TObject>>
    {
        internal AsyncOperationBase<TObject> m_InternalOp;
        int m_Version;
        string m_LocationName;

        internal int Version { get => m_Version; }
        internal string LocationName
        {
            get { return m_LocationName; }
            set { m_LocationName = value; }
        }

        /// <summary>
        /// Conversion from typed to non typed handles.  This does not increment the reference count.
        /// To convert from non-typed back, use AsyncOperationHandle.Convert&lt;T&gt;()
        /// </summary>
        /// <param name="obj">The typed handle to convert.</param>
        /// <returns>Returns the converted operation handle.</returns>
        static public implicit operator AsyncOperationHandle(AsyncOperationHandle<TObject> obj)
        {
            return new AsyncOperationHandle(obj.m_InternalOp, obj.m_Version, obj.m_LocationName);
        }

        internal AsyncOperationHandle(AsyncOperationBase<TObject> op)
        {
            m_InternalOp = op;
            m_Version = op?.Version ?? 0;
            m_LocationName = null;
        }

        /// <summary>
        /// Return the current download status for this operation and its dependencies.
        /// </summary>
        /// <returns>The download status.</returns>
        public DownloadStatus GetDownloadStatus()
        {
            return InternalGetDownloadStatus(new HashSet<object>());
        }

        internal DownloadStatus InternalGetDownloadStatus(HashSet<object> visited)
        {
            if (visited == null)
                visited = new HashSet<object>();
            return visited.Add(InternalOp) ? InternalOp.GetDownloadStatus(visited) : new DownloadStatus() {IsDone = IsDone};
        }

        internal AsyncOperationHandle(IAsyncOperation op)
        {
            m_InternalOp = (AsyncOperationBase<TObject>)op;
            m_Version = op?.Version ?? 0;
            m_LocationName = null;
        }

        internal AsyncOperationHandle(IAsyncOperation op, int version)
        {
            m_InternalOp = (AsyncOperationBase<TObject>)op;
            m_Version = version;
            m_LocationName = null;
        }

        internal AsyncOperationHandle(IAsyncOperation op, string locationName)
        {
            m_InternalOp = (AsyncOperationBase<TObject>)op;
            m_Version = op?.Version ?? 0;
            m_LocationName = locationName;
        }

        internal AsyncOperationHandle(IAsyncOperation op, int version, string locationName)
        {
            m_InternalOp = (AsyncOperationBase<TObject>)op;
            m_Version = version;
            m_LocationName = locationName;
        }

        /// <summary>
        /// Acquire a new handle to the internal operation.  This will increment the reference count, therefore the returned handle must also be released.
        /// </summary>
        /// <typeparam name="TObject">The object type of the underlying operation.</typeparam>
        /// <returns>A new handle to the operation.  This handle must also be released.</returns>
        internal AsyncOperationHandle<TObject> Acquire()
        {
            InternalOp.IncrementReferenceCount();
            return this;
        }

        /// <summary>
        /// Completion event for the internal operation.  If this is assigned on a completed operation, the callback is deferred until the LateUpdate of the current frame.
        /// </summary>
        public event Action<AsyncOperationHandle<TObject>> Completed
        {
            add { InternalOp.Completed += value; }
            remove { InternalOp.Completed -= value; }
        }

        /// <summary>
        /// Automatically release this handle upon Completed callback
        /// </summary>
        public void ReleaseHandleOnCompletion()
        {
            InternalOp.MarkReleaseOnCompletionRegistered();
            Completed += op => op.Release();
        }

        /// <summary>
        /// Completion event for non-typed callback handlers.  If this is assigned on a completed operation, the callback is deferred until the LateUpdate of the current frame.
        /// </summary>
        public event Action<AsyncOperationHandle> CompletedTypeless
        {
            add { InternalOp.CompletedTypeless += value; }
            remove { InternalOp.CompletedTypeless -= value; }
        }

        /// <summary>
        /// Debug name of the operation.
        /// </summary>
        public string DebugName
        {
            get
            {
                if (!IsValid())
                    return "InvalidHandle";
                return ((IAsyncOperation)InternalOp).DebugName;
            }
        }

        /// <summary>
        /// Get dependency operations.
        /// </summary>
        /// <param name="deps">The list of AsyncOperationHandles that are dependencies of a given AsyncOperationHandle</param>
        public void GetDependencies(List<AsyncOperationHandle> deps)
        {
            InternalOp.GetDependencies(deps);
        }

        /// <summary>
        /// Event for handling the destruction of the operation.
        /// </summary>
        public event Action<AsyncOperationHandle> Destroyed
        {
            add { InternalOp.Destroyed += value; }
            remove { InternalOp.Destroyed -= value; }
        }

        /// <summary>
        /// Provide equality for this struct.
        /// </summary>
        /// <param name="other">The operation to compare to.</param>
        /// <returns>True if the the operation handles reference the same AsyncOperation and the version is the same.</returns>
        public bool Equals(AsyncOperationHandle<TObject> other)
        {
            return m_Version == other.m_Version && m_InternalOp == other.m_InternalOp;
        }

        /// <summary>
        /// Get hash code of this struct.
        /// </summary>
        /// <returns>The hash code of this struct.</returns>
        public override int GetHashCode()
        {
            return m_InternalOp == null ? 0 : m_InternalOp.GetHashCode() * 17 + m_Version;
        }

        /// <summary>
        /// Synchronously complete the async operation.
        /// </summary>
        /// <returns>The result of the operation or null.</returns>
        public TObject WaitForCompletion()
        {
            if (IsValid() && !InternalOp.IsDone)
                InternalOp.WaitForCompletion();

            m_InternalOp?.m_RM?.Update(Time.unscaledDeltaTime);
            if (IsValid())
                return Result;
            return default(TObject);
        }

        internal AsyncOperationBase<TObject> InternalOp
        {
            get
            {
                if (m_InternalOp == null || m_InternalOp.Version != m_Version)
                    throw new Exception("Attempting to use an invalid operation handle");
                return m_InternalOp;
            }
        }

        /// <summary>
        /// True if the operation is complete.
        /// </summary>
        public bool IsDone
        {
            get { return !IsValid() || InternalOp.IsDone; }
        }

        /// <summary>
        /// Check if the handle references an internal operation.
        /// </summary>
        /// <returns>True if valid.</returns>
        public bool IsValid()
        {
            return m_InternalOp != null && m_InternalOp.Version == m_Version;
        }

        /// <summary>
        /// The exception for a failed operation.  This will be null unless Status is failed.
        /// </summary>
        public Exception OperationException
        {
            get { return InternalOp.OperationException; }
        }

        /// <summary>
        /// The progress of the internal operation.
        /// This is evenly weighted between all sub-operations. For example, a LoadAssetAsync call could potentially
        /// be chained with InitializeAsync and have multiple dependent operations that download and load content.
        /// In that scenario, PercentComplete would reflect how far the overal operation was, and would not accurately
        /// represent just percent downloaded or percent loaded into memory.
        /// For accurate download percentages, use GetDownloadStatus().
        /// </summary>
        public float PercentComplete
        {
            get { return InternalOp.PercentComplete; }
        }

        /// <summary>
        /// The current reference count of the internal operation.
        /// Returns 0 if the handle is not valid (for example, after it has been released).
        /// </summary>
        public int ReferenceCount
        {
            get { return IsValid() ? m_InternalOp.ReferenceCount : 0; }
        }

        /// <summary>
        /// Release the handle.  If the internal operation reference count reaches 0, the resource will be released.
        /// </summary>
        public void Release()
        {
            InternalOp.DecrementReferenceCount();
            m_InternalOp = null;
        }

        /// <summary>
        /// The result object of the operations.
        /// </summary>
        public TObject Result
        {
            get { return InternalOp.Result; }
        }

        /// <summary>
        /// The status of the internal operation.
        /// </summary>
        public AsyncOperationStatus Status
        {
            get { return InternalOp.Status; }
        }

        /// <summary>
        /// Return a Task object to wait on when using async await.
        /// </summary>
        public System.Threading.Tasks.Task<TObject> Task
        {
            get { return InternalOp.Task; }
        }

        /// <summary>
        /// Creates an <see cref="Awaitable{TObject}"/> that completes when the operation completes.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="Task"/>, failure throws an <see cref="AsyncOperationHandleException{TObject}"/> instead of resolving
        /// with a default or partial <see cref="Result"/>. Its <see cref="AsyncOperationHandleException{TObject}.Handle"/> carries a
        /// reference you must release (via <c>e.Handle.Release()</c>) if you still need to inspect <see cref="Status"/> or a partial result.
        /// The returned <see cref="Awaitable{TObject}"/> can only be awaited once. Don't await the same handle from multiple call sites
        /// unless nothing else releases it - whichever awaiter hits a failure or cancellation first releases the handle out from
        /// under the others. For multiple independent consumers, hold the handle and use <see cref="Completed"/> instead.
        /// </remarks>
        /// <returns>An awaitable that completes with the operation's result, or throws on failure.</returns>
        public Awaitable<TObject> ToAwaitable()
        {
            return ToAwaitable(CancellationToken.None);
        }

        /// <summary>
        /// Creates an <see cref="Awaitable{TObject}"/> that completes when the operation completes, or is
        /// canceled when <paramref name="cancellationToken"/> is canceled.
        /// </summary>
        /// <remarks>
        /// See the parameterless <see cref="ToAwaitable()"/> for failure and single-use semantics. This overload also
        /// releases the handle on cancellation, so it assumes sole ownership of the reference - a lifetime-scoped token
        /// (e.g. <see cref="MonoBehaviour.destroyCancellationToken"/> via <see cref="ToAwaitable(MonoBehaviour)"/>) can
        /// therefore replace a manual release call. Expected to be canceled on the main thread.
        /// </remarks>
        /// <param name="cancellationToken">Token that cancels the awaitable and releases this handle.</param>
        /// <returns>An awaitable that completes with the operation's result, or throws on failure or cancellation.</returns>
        public Awaitable<TObject> ToAwaitable(CancellationToken cancellationToken)
        {
            // See AwaitableOperationDriver for the shared reference-counting/cancellation rationale;
            // `this` implicitly converts to the non-generic handle it drives against.
            var source = new AwaitableCompletionSource<TObject>();
            AwaitableOperationDriver.Drive(this, cancellationToken,
                completeSource: handle => CompleteSource(source, handle.Convert<TObject>()),
                setCanceled: () => source.TrySetCanceled());
            return source.Awaitable;
        }

        /// <summary>
        /// Creates an <see cref="Awaitable{TObject}"/> tied to <paramref name="owner"/>'s destruction - shorthand
        /// for <c>ToAwaitable(owner.destroyCancellationToken)</c>.
        /// </summary>
        /// <remarks>
        /// Destroy-scoped, not Disable-scoped: <see cref="MonoBehaviour.destroyCancellationToken"/> only cancels on
        /// destruction. For a load that repeats across <c>OnEnable</c>/<c>OnDisable</c>, use a
        /// <see cref="CancellationTokenSource"/> created and canceled per cycle instead - see <see cref="ToAwaitable(CancellationToken)"/>.
        /// </remarks>
        /// <param name="owner">The component whose destruction cancels the awaitable and releases this handle.</param>
        /// <returns>An awaitable that completes with the operation's result, or throws on failure or cancellation.</returns>
        public Awaitable<TObject> ToAwaitable(MonoBehaviour owner)
        {
            return ToAwaitable(owner.destroyCancellationToken);
        }

        static void CompleteSource(AwaitableCompletionSource<TObject> source, AsyncOperationHandle<TObject> handle)
        {
            if (handle.Status == AsyncOperationStatus.Failed)
            {
                var opEx = handle.OperationException;
                var message = opEx?.Message ?? $"Addressables operation '{handle.DebugName}' failed.";
                // Acquire a reference dedicated to the exception, distinct from `handle` (which the caller manages
                // separately) - otherwise releasing both would double-release the same reference.
                source.SetException(new AsyncOperationHandleException<TObject>(handle.Acquire(), message, opEx));
            }
            else
                source.SetResult(handle.Result);
        }

        /// <summary>
        /// Enables directly awaiting this handle, e.g. <c>TObject result = await handle;</c>.
        /// Declared as an instance member (rather than only an extension method) so that
        /// <c>await</c> works without requiring a <c>using UnityEngine.ResourceManagement.AsyncOperations;</c>
        /// directive. See <see cref="ToAwaitable"/> for failure and single-use semantics.
        /// </summary>
        /// <returns>The awaiter used by the compiler-generated await code.</returns>
        public Awaitable<TObject>.Awaiter GetAwaiter()
        {
            return ToAwaitable().GetAwaiter();
        }

        object IEnumerator.Current
        {
            get { return Result; }
        }

        /// <summary>
        /// Overload for <see cref="IEnumerator.MoveNext"/>.
        /// </summary>
        /// <returns>Returns true if the enumerator can advance to the next element in the collectin. Returns false otherwise.</returns>
        bool IEnumerator.MoveNext()
        {
            return !IsDone;
        }

        /// <summary>
        /// Overload for <see cref="IEnumerator.Reset"/>.
        /// </summary>
        void IEnumerator.Reset()
        {
        }
    }

    /// <summary>
    /// Non typed operation handle.  This allows for reference counting and checking for valid references.
    /// </summary>
    public struct AsyncOperationHandle : IEnumerator
    {

        internal IAsyncOperation m_InternalOp;
        int m_Version;
        string m_LocationName;

        internal int Version{ get => m_Version; }
        internal string LocationName
        {
            get { return m_LocationName; }
            set { m_LocationName = value; }
        }

        internal AsyncOperationHandle(IAsyncOperation op)
        {
            m_InternalOp = op;
            m_Version = op?.Version ?? 0;
            m_LocationName = null;
        }

        internal AsyncOperationHandle(IAsyncOperation op, int version)
        {
            m_InternalOp = op;
            m_Version = version;
            m_LocationName = null;
        }

        internal AsyncOperationHandle(IAsyncOperation op, string locationName)
        {
            m_InternalOp = op;
            m_Version = op?.Version ?? 0;
            m_LocationName = locationName;
        }

        internal AsyncOperationHandle(IAsyncOperation op, int version, string locationName)
        {
            m_InternalOp = op;
            m_Version = version;
            m_LocationName = locationName;
        }

        /// <summary>
        /// Acquire a new handle to the internal operation.  This will increment the reference count, therefore the returned handle must also be released.
        /// </summary>
        /// <returns>A new handle to the operation. This handle must also be released.</returns>
        internal AsyncOperationHandle Acquire()
        {
            InternalOp.IncrementReferenceCount();
            return this;
        }

        /// <summary>
        /// Completion event for the internal operation.  If this is assigned on a completed operation, the callback is deferred until the LateUpdate of the current frame.
        /// </summary>
        public event Action<AsyncOperationHandle> Completed
        {
            add { InternalOp.CompletedTypeless += value; }
            remove { InternalOp.CompletedTypeless -= value; }
        }

        /// <summary>
        /// Automatically release this handle upon Completed callback
        /// </summary>
        public void ReleaseHandleOnCompletion()
        {
            InternalOp.MarkReleaseOnCompletionRegistered();
            Completed += op => op.Release();
        }

        /// <summary>
        /// Converts handle to be typed.  This does not increment the reference count.
        /// To convert back to non-typed, implicit conversion is available.
        /// </summary>
        /// <typeparam name="T">The type of the handle.</typeparam>
        /// <returns>A new handle that is typed.</returns>
        public AsyncOperationHandle<T> Convert<T>()
        {
            return new AsyncOperationHandle<T>(InternalOp, m_Version, m_LocationName);
        }

        /// <summary>
        /// Provide equality for this struct.
        /// </summary>
        /// <param name="other">The operation to compare to.</param>
        /// <returns>True if the the operation handles reference the same AsyncOperation and the version is the same.</returns>
        public bool Equals(AsyncOperationHandle other)
        {
            return m_Version == other.m_Version && m_InternalOp == other.m_InternalOp;
        }

        /// <summary>
        /// Debug name of the operation.
        /// </summary>
        public string DebugName
        {
            get
            {
                if (!IsValid())
                    return "InvalidHandle";
                return InternalOp.DebugName;
            }
        }

        /// <summary>
        /// Event for handling the destruction of the operation.
        /// </summary>
        public event Action<AsyncOperationHandle> Destroyed
        {
            add { InternalOp.Destroyed += value; }
            remove { InternalOp.Destroyed -= value; }
        }

        /// <summary>
        /// Get dependency operations.
        /// </summary>
        /// <param name="deps">The list to add dependencies to</param>
        public void GetDependencies(List<AsyncOperationHandle> deps)
        {
            InternalOp.GetDependencies(deps);
        }

        /// <summary>
        /// Get hash code of this struct.
        /// </summary>
        /// <returns>The calculated hash code</returns>
        public override int GetHashCode()
        {
            return m_InternalOp == null ? 0 : m_InternalOp.GetHashCode() * 17 + m_Version;
        }

        IAsyncOperation InternalOp
        {
            get
            {
                if (m_InternalOp == null || m_InternalOp.Version != m_Version)
                    throw new Exception("Attempting to use an invalid operation handle");
                return m_InternalOp;
            }
        }


        /// <summary>
        /// Whether something has already registered a release-on-completion listener for this operation
        /// (an <c>autoReleaseHandle: true</c> API, or a direct <see cref="ReleaseHandleOnCompletion"/> call).
        /// Used by <see cref="AwaitableOperationDriver"/> to avoid releasing a reference that listener already owns.
        /// </summary>
        internal bool HasReleaseOnCompletionRegistered => InternalOp.HasReleaseOnCompletionRegistered;

        /// <summary>
        /// Whether this operation's <see cref="Completed"/> event already has listeners. Used by
        /// <see cref="AwaitableOperationDriver"/> to gate its fast path for an already-done operation.
        /// </summary>
        internal bool CompletedEventHasListeners => InternalOp.CompletedEventHasListeners;

        /// <summary>
        /// True if the operation is complete.
        /// </summary>
        public bool IsDone
        {
            get { return !IsValid() || InternalOp.IsDone; }
        }

        /// <summary>
        /// Check if the internal operation is not null and has the same version of this handle.
        /// </summary>
        /// <returns>True if valid.</returns>
        public bool IsValid()
        {
            return m_InternalOp != null && m_InternalOp.Version == m_Version;
        }

        /// <summary>
        /// The exception for a failed operation.  This will be null unless Status is failed.
        /// </summary>
        public Exception OperationException
        {
            get { return InternalOp.OperationException; }
        }

        /// <summary>
        /// The progress of the internal operation.
        /// This is evenly weighted between all sub-operations. For example, a LoadAssetAsync call could potentially
        /// be chained with InitializeAsync and have multiple dependent operations that download and load content.
        /// In that scenario, PercentComplete would reflect how far the overal operation was, and would not accurately
        /// represent just percent downloaded or percent loaded into memory.
        /// For accurate download percentages, use GetDownloadStatus().
        /// </summary>
        public float PercentComplete
        {
            get { return InternalOp.PercentComplete; }
        }

        /// <summary>
        /// Return the current download status for this operation and its dependencies.  In some instances, the information will not be available.  This can happen if the operation
        /// is dependent on the initialization operation for addressables.  Once the initialization operation completes, the information returned will be accurate.
        /// </summary>
        /// <returns>The download status.</returns>
        public DownloadStatus GetDownloadStatus()
        {
            return InternalGetDownloadStatus(new HashSet<object>());
        }

        internal DownloadStatus InternalGetDownloadStatus(HashSet<object> visited)
        {
            if (visited == null)
                visited = new HashSet<object>();
            return visited.Add(InternalOp) ? InternalOp.GetDownloadStatus(visited) : new DownloadStatus() {IsDone = IsDone};
        }

        /// <summary>
        /// The current reference count of the internal operation.
        /// Returns 0 if the handle is not valid (for example, after it has been released).
        /// </summary>
        public int ReferenceCount
        {
            get { return IsValid() ? m_InternalOp.ReferenceCount : 0; }
        }

        /// <summary>
        /// Release the handle.  If the internal operation reference count reaches 0, the resource will be released.
        /// </summary>
        public void Release()
        {
            InternalOp.DecrementReferenceCount();
            m_InternalOp = null;
        }

        /// <summary>
        /// The result object of the operations.
        /// </summary>
        public object Result
        {
            get { return InternalOp.GetResultAsObject(); }
        }

        /// <summary>
        /// The status of the internal operation.
        /// </summary>
        public AsyncOperationStatus Status
        {
            get { return InternalOp.Status; }
        }

        /// <summary>
        /// Return a Task object to wait on when using async await.
        /// </summary>
        public System.Threading.Tasks.Task<object> Task
        {
            get { return InternalOp.Task; }
        }

        /// <summary>
        /// Creates an <see cref="Awaitable"/> that completes when the operation completes.
        /// </summary>
        /// <remarks>
        /// On failure, throws an <see cref="AsyncOperationHandleException"/> whose <see cref="AsyncOperationHandleException.Handle"/>
        /// carries a reference you must release (via <c>e.Handle.Release()</c>). The returned <see cref="Awaitable"/> can only be awaited once.
        /// </remarks>
        /// <returns>An awaitable that completes when the operation completes, or throws on failure.</returns>
        public Awaitable ToAwaitable()
        {
            return ToAwaitable(CancellationToken.None);
        }

        /// <summary>
        /// Creates an <see cref="Awaitable"/> that completes when the operation completes, or is canceled when
        /// <paramref name="cancellationToken"/> is canceled. See the generic <see cref="AsyncOperationHandle{TObject}.ToAwaitable(CancellationToken)"/>
        /// overload for details.
        /// </summary>
        /// <param name="cancellationToken">Token that cancels the awaitable and releases this handle.</param>
        /// <returns>An awaitable that completes when the operation completes, or throws on failure or cancellation.</returns>
        public Awaitable ToAwaitable(CancellationToken cancellationToken)
        {
            // See AwaitableOperationDriver for the shared reference-counting/cancellation rationale.
            var source = new AwaitableCompletionSource();
            AwaitableOperationDriver.Drive(this, cancellationToken,
                completeSource: handle => CompleteSource(source, handle),
                setCanceled: () => source.TrySetCanceled());
            return source.Awaitable;
        }

        /// <summary>
        /// Creates an <see cref="Awaitable"/> tied to <paramref name="owner"/>'s destruction - shorthand for
        /// <c>ToAwaitable(owner.destroyCancellationToken)</c>. See the generic
        /// <see cref="AsyncOperationHandle{TObject}.ToAwaitable(MonoBehaviour)"/> overload for the
        /// Destroy-vs-Disable scoping caveat.
        /// </summary>
        /// <param name="owner">The component whose destruction cancels the awaitable and releases this handle.</param>
        /// <returns>An awaitable that completes when the operation completes, or throws on failure or cancellation.</returns>
        public Awaitable ToAwaitable(MonoBehaviour owner)
        {
            return ToAwaitable(owner.destroyCancellationToken);
        }

        static void CompleteSource(AwaitableCompletionSource source, AsyncOperationHandle handle)
        {
            if (handle.Status == AsyncOperationStatus.Failed)
            {
                var opEx = handle.OperationException;
                var message = opEx?.Message ?? $"Addressables operation '{handle.DebugName}' failed.";
                source.SetException(new AsyncOperationHandleException(handle.Acquire(), message, opEx));
            }
            else
                source.SetResult();
        }

        /// <summary>
        /// Enables directly awaiting this handle, e.g. <c>await handle;</c>.
        /// Declared as an instance member (rather than only an extension method) so that
        /// <c>await</c> works without requiring a <c>using UnityEngine.ResourceManagement.AsyncOperations;</c>
        /// directive. See <see cref="ToAwaitable"/> for failure and single-use semantics.
        /// </summary>
        /// <returns>The awaiter used by the compiler-generated await code.</returns>
        public Awaitable.Awaiter GetAwaiter()
        {
            return ToAwaitable().GetAwaiter();
        }

        object IEnumerator.Current
        {
            get { return Result; }
        }

        /// <summary>
        /// Overload for <see cref="IEnumerator.MoveNext"/>.
        /// </summary>
        /// <returns>Returns true if the enumerator can advance to the next element in the collectin. Returns false otherwise.</returns>
        bool IEnumerator.MoveNext()
        {
            return !IsDone;
        }

        /// <summary>
        /// Overload for <see cref="IEnumerator.Reset"/>.
        /// </summary>
        void IEnumerator.Reset()
        {
        }

        /// <summary>
        /// Synchronously complete the async operation.
        /// </summary>
        /// <returns>The result of the operation or null.</returns>
        public object WaitForCompletion()
        {
            if (IsValid() && !InternalOp.IsDone)
                InternalOp.WaitForCompletion();
            if (IsValid())
                return Result;
            return null;
        }
    }
}
