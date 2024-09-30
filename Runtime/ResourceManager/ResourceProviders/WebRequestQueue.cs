using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Represents a web request stored in the <seealso cref="WebRequestQueue"/>.
    /// </summary>
    public class WebRequestQueueOperation
    {
        private bool m_Completed = false;

        /// <summary>
        /// Stores the async operation object returned from sending the web request.
        /// </summary>
        public UnityWebRequestAsyncOperation Result;

        /// <summary>
        /// Event that is invoked when the async operation is complete.
        /// </summary>
        public Action<UnityWebRequestAsyncOperation> OnComplete;

        /// <summary>
        /// Indicates that the async operation is complete.
        /// </summary>
        public bool IsDone
        {
            get { return m_Completed || Result != null; }
        }

        internal UnityWebRequest m_WebRequest;

        /// <summary>
        /// The web request.
        /// </summary>
        public UnityWebRequest WebRequest
        {
            get { return m_WebRequest; }
            internal set { m_WebRequest = value; }
        }

        /// <summary>
        /// Initializes and returns an instance of WebRequestQueueOperation.
        /// </summary>
        /// <param name="request">The web request associated with this object.</param>
        public WebRequestQueueOperation(UnityWebRequest request)
        {
            m_WebRequest = request;
        }

        internal void Complete(UnityWebRequestAsyncOperation asyncOp)
        {
            m_Completed = true;
            Result = asyncOp;
            OnComplete?.Invoke(Result);
        }
    }


    /// <summary>
    /// Represents a queue of web requests. Completed requests are removed from the queue.
    /// </summary>
    public static class WebRequestQueue
    {
        internal static int s_MaxRequest = 3;
        internal static Queue<WebRequestQueueOperation> s_QueuedOperations = new Queue<WebRequestQueueOperation>();
        internal static List<UnityWebRequestAsyncOperation> s_ActiveRequests = new List<UnityWebRequestAsyncOperation>();

        /// <summary>
        /// Sets the max number of web requests running at the same time.
        /// </summary>
        /// <param name="maxRequests">The max number of web requests.</param>
        public static void SetMaxConcurrentRequests(int maxRequests)
        {
            if (maxRequests < 1)
                throw new ArgumentException("MaxRequests must be 1 or greater.", "maxRequests");
            s_MaxRequest = maxRequests;
        }

        /// <summary>
        /// Adds a web request to the queue.
        /// </summary>
        /// <param name="request">The web request.</param>
        /// <returns>Returns an object representing the web request.</returns>
        public static WebRequestQueueOperation QueueRequest(UnityWebRequest request)
        {
            WebRequestQueueOperation queueOperation = new WebRequestQueueOperation(request);
            if (s_ActiveRequests.Count < s_MaxRequest)
                BeginWebRequest(queueOperation);
            else
                s_QueuedOperations.Enqueue(queueOperation);

            return queueOperation;
        }

        /// <summary>
        /// Synchronously waits for a particular web request to be completed.
        /// </summary>
        /// <param name="request">The web request.</param>
        /// <param name="millisecondsTimeout">The amount of time in milliseconds spent waiting per iteration before checking if the request is complete.</param>
        public static void WaitForRequestToBeActive(WebRequestQueueOperation request, int millisecondsTimeout)
        {
            var completedRequests = new List<UnityWebRequestAsyncOperation>();
            while (s_QueuedOperations.Contains(request))
            {
                completedRequests.Clear();
                foreach (UnityWebRequestAsyncOperation webRequestAsyncOp in s_ActiveRequests)
                {
                    if (UnityWebRequestUtilities.IsAssetBundleDownloaded(webRequestAsyncOp))
                        completedRequests.Add(webRequestAsyncOp);
                }

                foreach (UnityWebRequestAsyncOperation webRequestAsyncOp in completedRequests)
                {
                    bool requestIsActive = s_QueuedOperations.Peek() == request;
                    webRequestAsyncOp.completed -= OnWebAsyncOpComplete;
                    OnWebAsyncOpComplete(webRequestAsyncOp);
                    if (requestIsActive)
                        return;
                }

                System.Threading.Thread.Sleep(millisecondsTimeout);
            }
        }

        internal static void DequeueRequest(UnityWebRequestAsyncOperation operation)
        {
            operation.completed -= OnWebAsyncOpComplete;
            OnWebAsyncOpComplete(operation);
        }

        private static void OnWebAsyncOpComplete(AsyncOperation operation)
        {
            OnWebAsyncOpComplete(operation as UnityWebRequestAsyncOperation);
        }

        private static void OnWebAsyncOpComplete(UnityWebRequestAsyncOperation operation)
        {
            if (s_ActiveRequests.Remove(operation) && s_QueuedOperations.Count > 0)
            {
                var nextQueuedOperation = s_QueuedOperations.Dequeue();
                BeginWebRequest(nextQueuedOperation);
            }
        }

        static void BeginWebRequest(WebRequestQueueOperation queueOperation)
        {
            var request = queueOperation.m_WebRequest;
            UnityWebRequestAsyncOperation webRequestAsyncOp = null;
            try
            {
                webRequestAsyncOp = request.SendWebRequest();
                if (webRequestAsyncOp != null)
                {
                    s_ActiveRequests.Add(webRequestAsyncOp);

                    if (webRequestAsyncOp.isDone)
                        OnWebAsyncOpComplete(webRequestAsyncOp);
                    else
                        webRequestAsyncOp.completed += OnWebAsyncOpComplete;
                }
                else
                {
                    OnWebAsyncOpComplete(null);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            queueOperation.Complete(webRequestAsyncOp);
        }
    }
}
