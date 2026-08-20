using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace UnityEngine.AddressableAssets.ResourceProviders.Tests
{
    /// <summary>
    /// Minimal read-only HTTP file server for tests that need to serve build output (e.g. "remote"
    /// AssetBundles) over a real network connection rather than reading it off disk. Binds 127.0.0.1
    /// on an OS-assigned free port (so concurrent fixtures never collide) and maps each request's path
    /// 1:1 onto a file under the configured root directory. Designed to be shared across test fixtures.
    /// </summary>
    public sealed class StaticFileServer
    {
        readonly HttpListener m_Listener = new HttpListener();
        readonly string m_RootDirectory;
        Thread m_Thread;
        volatile bool m_Running;
        int m_RequestCount;

        public readonly IPAddress IPAddress;
        public readonly int Port;

        /// <summary>
        /// Number of requests the server has handled - used by tests to confirm content was actually
        /// fetched over HTTP.
        /// </summary>
        public int RequestCount => Volatile.Read(ref m_RequestCount);

        public bool IsRunning { get { return m_Running; } }

        public StaticFileServer(string rootDirectory)
        {
            m_RootDirectory = rootDirectory;

            // Grab a free port by briefly binding one, then hand it to the HttpListener.
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            IPAddress = ((IPEndPoint)probe.LocalEndpoint).Address;
            Port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            var prefix = $"http://{IPAddress}:{Port}/";
            m_Listener.Prefixes.Add(prefix);
        }

        public void Start()
        {
            m_Listener.Start();
            m_Running = true;
            m_Thread = new Thread(Loop) { IsBackground = true, Name = "StaticFileServer" };
            m_Thread.Start();
        }

        void Loop()
        {
            while (m_Running)
            {
                HttpListenerContext context;
                try
                {
                    context = m_Listener.GetContext();
                }
                catch
                {
                    // Listener was stopped/closed while blocked in GetContext - exit the loop.
                    break;
                }

                Interlocked.Increment(ref m_RequestCount);
                ServeFile(context);
            }
        }

        void ServeFile(HttpListenerContext context)
        {
            var response = context.Response;
            try
            {
                string relative = Uri.UnescapeDataString(context.Request.Url.LocalPath.TrimStart('/'));
                string filePath = Path.Combine(m_RootDirectory, relative.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(filePath))
                {
                    byte[] bytes = File.ReadAllBytes(filePath);
                    response.ContentLength64 = bytes.Length;
                    response.OutputStream.Write(bytes, 0, bytes.Length);
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    Debug.LogError($"StaticFileServer 404: no file at '{filePath}' for request " +
                        $"'{context.Request.Url}' (root '{m_RootDirectory}')");
                }
            }
            catch (Exception e)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                Debug.LogError($"StaticFileServer failed serving {context.Request.Url}: {e}");
            }
            finally
            {
                response.OutputStream.Close();
            }
        }

        public void Stop()
        {
            m_Running = false;
            // Stopping/closing can throw if the listener was never started or is already disposed during
            // teardown. That is expected, but log it rather than swallowing it: a silently-eaten error
            // here would make a genuinely misbehaving server very hard to troubleshoot.
            try { m_Listener.Stop(); }
            catch (Exception e) { Debug.Log($"StaticFileServer: HttpListener.Stop() threw, probably safe to ignore: {e}"); }
            m_Thread?.Join(2000);
            try { m_Listener.Close(); }
            catch (Exception e) { Debug.Log($"StaticFileServer: HttpListener.Close() threw, probably safe to ignore: {e}"); }
        }
    }
}
