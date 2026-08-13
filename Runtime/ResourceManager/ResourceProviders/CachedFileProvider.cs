using System;
using System.ComponentModel;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.Exceptions;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    /// <summary>
    /// Downloads remote files to a local cache and returns the path.  This path can be used to load the data with other providers such as <see cref="BinaryDataProvider"/>.
    /// </summary>
    [DisplayName("Cached File Provider")]
    public class CachedFileProvider : ResourceProviderBase
    {
        internal class InternalOp
        {
            CachedFileProvider m_Provider;
            UnityWebRequestAsyncOperation m_RequestOperation;
            WebRequestQueueOperation m_RequestQueueOperation;
            ProvideHandle m_PI;
            bool m_IgnoreFailures;
            private bool m_Complete = false;
            private int m_Timeout = 0;
            private string m_CachePath;

            private float GetPercentComplete()
            {
                return m_RequestOperation != null ? m_RequestOperation.progress : 0.0f;
            }

            public void Start(ProvideHandle provideHandle, CachedFileProvider rawProvider)
            {
                m_PI = provideHandle;
                m_PI.SetWaitForCompletionCallback(WaitForCompletionHandler);
                provideHandle.SetProgressCallback(GetPercentComplete);
                m_Provider = rawProvider;

                // override input options with options from Location if included
                if (m_PI.Location.Data is ProviderLoadRequestOptions providerData)
                {
                    m_IgnoreFailures = providerData.IgnoreFailures;
                    m_Timeout = providerData.WebRequestTimeout;
                    m_CachePath = $"{Application.temporaryCachePath}/{providerData.LocalCachePath}";
                }
                else
                {
                    m_CachePath = string.Empty;
                    m_IgnoreFailures = false;
                    m_Timeout = 0;
                }

                if (File.Exists(m_CachePath))
                {
                    m_Complete = true;
                    m_PI.Complete(m_CachePath, true, null);
                }
                else
                {
                    var remotePath = m_PI.ResourceManager.TransformInternalId(m_PI.Location);
                    if (ResourceManagerConfig.ShouldPathUseWebRequest(remotePath))
                    {
                        if (string.IsNullOrEmpty(m_CachePath))
                            m_CachePath = Path.GetTempFileName();

                        var cacheDir = Path.GetDirectoryName(m_CachePath);
                        if (!Directory.Exists(cacheDir))
                            Directory.CreateDirectory(cacheDir);
                        SendWebRequest(remotePath, m_CachePath);
                    }
                    else if (File.Exists(remotePath))
                    {
                        if (string.IsNullOrEmpty(m_CachePath))
                        {
                            m_PI.Complete(remotePath, true, null);
                        }
                        else
                        {
                            if (File.Exists(m_CachePath))
                                File.Delete(m_CachePath);
                            File.WriteAllBytes(m_CachePath, File.ReadAllBytes(remotePath));
                            m_PI.Complete(m_CachePath, true, null);
                        }
                    }
                }
            }

            bool WaitForCompletionHandler()
            {
                if (m_Complete)
                    return true;

                if (m_RequestOperation != null)
                {
                    if (m_RequestOperation.isDone && !m_Complete)
                        RequestOperation_completed(m_RequestOperation);
                    else if (!m_RequestOperation.isDone)
                        return false;
                }

                return m_Complete;
            }

            private void RequestOperation_completed(AsyncOperation op)
            {
                if (m_Complete)
                    return;

                if (op is UnityWebRequestAsyncOperation webOp)
                {
                    if (UnityWebRequestUtilities.RequestHasErrors(webOp.webRequest, out UnityWebRequestResult uwrResult))
                    {
                        m_PI.Complete(string.Empty, false, new RemoteProviderException(nameof(CachedFileProvider) + " - unable to load from url : {webReq.url}", m_PI.Location, uwrResult));
                    }
                    else
                    {
                        m_PI.Complete(m_CachePath, true, null);
                    }
                }
                else
                {
                    m_PI.Complete(string.Empty, false, new RemoteProviderException(nameof(CachedFileProvider) + " - unable to load from unknown url.", m_PI.Location));
                }
                m_Complete = true;
            }

            protected virtual void SendWebRequest(string remotePath, string cachePath)
            {
                UnityWebRequest request = new UnityWebRequest(remotePath, UnityWebRequest.kHttpVerbGET, new DownloadHandlerFile(cachePath, false), null);
                if (m_Timeout > 0)
                    request.timeout = m_Timeout;

                m_PI.ResourceManager.WebRequestOverride?.Invoke(request);
                m_RequestQueueOperation = WebRequestQueue.QueueRequest(request);
                if (m_RequestQueueOperation.IsDone)
                {
                    m_RequestOperation = m_RequestQueueOperation.Result;
                    if (m_RequestOperation.isDone)
                        RequestOperation_completed(m_RequestOperation);
                    else
                        m_RequestOperation.completed += RequestOperation_completed;
                }
                else
                {
                    m_RequestQueueOperation.OnComplete += asyncOperation =>
                    {
                        m_RequestOperation = asyncOperation;
                        m_RequestOperation.completed += RequestOperation_completed;
                    };
                }
            }
        }

        /// <summary>
        /// Provides the local file path of the cached file.
        /// </summary>
        /// <param name="provideHandle">The data needed by the provider to perform the load.</param>
        public override void Provide(ProvideHandle provideHandle)
        {
            new InternalOp().Start(provideHandle, this);
        }
    }
}
