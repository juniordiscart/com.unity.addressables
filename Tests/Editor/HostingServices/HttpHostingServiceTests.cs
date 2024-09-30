using System;
using System.IO;
using System.Net;
using NUnit.Framework;
using UnityEditor.AddressableAssets.HostingServices;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.AddressableAssets.Tests.HostingServices
{
    using Random = System.Random;

    public class HttpHostingServiceTests
    {
        class MyWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                var w = base.GetWebRequest(uri);
                Debug.Assert(w != null);
                w.Timeout = 2000;
                return w;
            }
        }

        HttpHostingService m_Service;
        string m_ContentRoot;
        readonly WebClient m_Client;

        public HttpHostingServiceTests()
        {
            m_Client = new MyWebClient();
        }

        static byte[] GetRandomBytes(int size)
        {
            var rand = new Random();
            var buf = new byte[size];
            rand.NextBytes(buf);
            return buf;
        }

        [OneTimeSetUp]
        public void SetUp()
        {
            m_Service = new HttpHostingService();
            var dirName = Path.GetRandomFileName();
            m_ContentRoot = Path.Combine(Path.GetTempPath(), dirName);
            Assert.IsNotEmpty(m_ContentRoot);
            Directory.CreateDirectory(m_ContentRoot);
            m_Service.HostingServiceContentRoots.Add(m_ContentRoot);
        }

        [TearDown]
        public void Cleanup()
        {
            m_Service.StopHostingService();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(m_ContentRoot) && Directory.Exists(m_ContentRoot))
                Directory.Delete(m_ContentRoot, true);
        }

        [TestCase("subdir", "subdir1")] //"subdir3")]
        [TestCase("subdír☠", "subdirãúñ", TestName = "ShouldServeFilesWSpecialCharacters")] //"subdirü",
        public void ShouldServeRequestedFiles(string subdir1, string subdir2) // string subdir3)
        {
            var fileNames = new[]
            {
                Path.GetRandomFileName(),
                Path.Combine(subdir1, Path.GetRandomFileName()),
                Path.Combine(subdir2, Path.GetRandomFileName())
            };

            foreach (var fileName in fileNames)
            {
                var filePath = Path.Combine(m_ContentRoot, fileName);
                var bytes = GetRandomBytes(1024);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllBytes(filePath, bytes);
                m_Service.StartHostingService();
                Assert.IsTrue(m_Service.IsHostingServiceRunning);
                var url = string.Format("http://127.0.0.1:{0}/{1}", m_Service.HostingServicePort, fileName);
                try
                {
                    var data = m_Client.DownloadData(url);
                    Assert.AreEqual(data.Length, bytes.Length);
                    for (var i = 0; i < data.Length; i++)
                        if (bytes[i] != data[i])
                            Assert.Fail("Data does not match {0} != {1}", bytes[i], data[i]);
                }
                catch (Exception e)
                {
                    Assert.Fail(e.Message);
                }
            }
        }

        private class MockHttpContext : HttpHostingService.IHttpContext
        {
            public Uri Url { get; set; }
            public string ContentType { get; set; }
            public long ContentLength { get; set; }

            public Stream OutputStream { get; set; }

            public Uri GetRequestUrl()
            {
                return Url;
            }

            public void SetResponseContentType(string contentType)
            {
                ContentType = contentType;
            }

            public void SetResponseContentLength(long contentLength)
            {
                ContentLength = contentLength;
            }

            public Stream GetResponseOutputStream()
            {
                return OutputStream;
            }
        }

        [Test]
        public void FileUploadOperationSplitsDownload()
        {

            string subdir1 = "subdir";
            string subdir2 = "subdir1"; // Remove comment when Mono limit Fixed
            string subdir3 = "subdir3";

            var fileNames = new[]
            {
                Path.GetRandomFileName(),
                Path.Combine(subdir1, Path.GetRandomFileName()),
                Path.Combine(subdir2, Path.Combine(subdir3, Path.GetRandomFileName())),
                Path.Combine(subdir3, Path.GetRandomFileName()),
            };

            foreach (var fileName in fileNames)
            {
                var filePath = Path.Combine(m_ContentRoot, fileName);
                var bytes = GetRandomBytes(1024);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllBytes(filePath, bytes);
                // we execute every 250ms so you can divide upload speed by 4
                // to see how much each update will process
                var uploadSpeed = 2048;

                var cleanupCalled = false;
                Action cleanup = () => { cleanupCalled = true; };
                var context = new MockHttpContext();
                using (MemoryStream outputStream = new MemoryStream(1024))
                {
                    context.OutputStream = outputStream;
                    context.Url = new Uri("http://127.0.0.1:55555");
                    var op = new HttpHostingService.FileUploadOperation(context, filePath, uploadSpeed, cleanup);
                    op.Update(null);
                    Assert.AreEqual(512, context.OutputStream.Length);
                    op.Update(null);
                }
                Assert.AreEqual(1024, context.ContentLength);
                Assert.AreEqual("application/octet-stream", context.ContentType);
                Assert.IsTrue(cleanupCalled);
            }
        }

        [Test]
        public void FileUploadOperationCallsCleanupOnError()
        {

            string subdir1 = "subdir";
            var fileName = Path.Combine(subdir1, Path.GetRandomFileName());
            var filePath = Path.Combine(m_ContentRoot, fileName);
            // we intentionally do not initialize a test file
            var uploadSpeed = 2048;

            var exceptionThrown = false;
            var cleanupCalled = false;
            Action cleanup = () => { cleanupCalled = true; };
            var context = new MockHttpContext();
            try
            {
                var _ = new HttpHostingService.FileUploadOperation(context, filePath, uploadSpeed, cleanup);
            }
            catch (Exception e)
            {
                exceptionThrown = true;
            }

            LogAssert.Expect(LogType.Exception, $"DirectoryNotFoundException: Could not find a part of the path \"{filePath}\".");
            Assert.IsTrue(cleanupCalled);
            Assert.IsTrue(exceptionThrown);
        }

        [Test]
        public void FileUploadOperationHandlesError()
        {

            var fileName = Path.GetRandomFileName();

            var filePath = Path.Combine(m_ContentRoot, fileName);
            var bytes = GetRandomBytes(1024);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, bytes);
            // we execute every 250ms so you can divide upload speed by 4
            // to see how much each update will process
            var uploadSpeed = 2048;

            var cleanupCalled = false;
            var exceptionCaught = false;
            Action cleanup = () => { cleanupCalled = true; };
            var context = new MockHttpContext();
            using (MemoryStream outputStream = new MemoryStream(1024))
            {
                try
                {
                    context.OutputStream = outputStream;
                    // close the output stream to trigger an exception on writes
                    outputStream.Close();
                    context.Url = new Uri("http://127.0.0.1:55555");
                    var op = new HttpHostingService.FileUploadOperation(context, filePath, uploadSpeed, cleanup);
                    op.Update(null);
                }
                catch (Exception e)
                {
                    exceptionCaught = true;
                }
            }
            Assert.IsTrue(cleanupCalled);
            Assert.IsTrue(exceptionCaught);
        }


        [Test]
        public void HttpServiceCompletesWithUploadSpeedWhenExpected()
        {
            string subdir1 = "subdir";
            string subdir2 = "subdir1";
            string subdir3 = "subdir3";

            var fileNames = new[]
            {
                Path.GetRandomFileName(),
                Path.Combine(subdir1, Path.GetRandomFileName()),
                Path.Combine(subdir2, Path.Combine(subdir3, Path.GetRandomFileName())),
                Path.Combine(subdir3, Path.GetRandomFileName()),
            };

            m_Service.StartHostingService();
            try
            {
                foreach (var fileName in fileNames)
                {
                    var filePath = Path.Combine(m_ContentRoot, fileName);
                    var bytes = GetRandomBytes(1024);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    File.WriteAllBytes(filePath, bytes);

                    m_Service.UploadSpeed = 2048;

                    Assert.IsTrue(m_Service.IsHostingServiceRunning);
                    var url = string.Format("http://127.0.0.1:{0}/{1}", m_Service.HostingServicePort, fileName);
                    try
                    {
                        var downloadedBytes = m_Client.DownloadData(new Uri(url));
                        Assert.AreEqual(1024, downloadedBytes.Length);
                    }
                    catch (Exception e)
                    {
                        Assert.Fail(e.Message);
                    }
                }
            }
            finally
            {
                m_Service.StopHostingService();
            }
        }

        [Test]
        public void ShouldRespondWithStatus404IfFileDoesNotExist()
        {
            m_Service.StartHostingService();
            Assert.IsTrue(m_Service.IsHostingServiceRunning);
            var url = string.Format("http://127.0.0.1:{0}/{1}", m_Service.HostingServicePort, "foo");
            try
            {
                m_Client.DownloadData(url);
            }
            catch (WebException e)
            {
                var response = (HttpWebResponse)e.Response;
                Assert.AreEqual(response.StatusCode, HttpStatusCode.NotFound);
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        // StartHostingService

        [Test]
        public void StartHostingServiceShould_AssignPortIfUnassigned()
        {
            m_Service.StartHostingService();
            Assert.Greater(m_Service.HostingServicePort, 0);
        }

        // OnBeforeSerialize

        [Test]
        public void OnBeforeSerializeShould_PersistExpectedDataToKeyDataStore()
        {
            m_Service.StartHostingService();
            var port = m_Service.HostingServicePort;
            var data = new KeyDataStore();
            m_Service.OnBeforeSerialize(data);
            Assert.AreEqual(port, data.GetData("HostingServicePort", 0));
        }

        [Test]
        public void OnBeforeSerializeShould_WasEnableCorrectToKeyDataStore()
        {
            m_Service.StartHostingService();
            var data = new KeyDataStore();
            m_Service.OnDisable();
            m_Service.OnBeforeSerialize(data);
            Assert.IsTrue(data.GetData("IsEnabled", false), "Hosting server was started before shutting down. IsEnabled expected to be true");
        }

        // OnAfterDeserialize

        [Test]
        public void OnAfterDeserializeShould_RestoreExpectedDataFromKeyDataStore()
        {
            var data = new KeyDataStore();
            data.SetData("HostingServicePort", 1234);
            m_Service.OnAfterDeserialize(data);
            Assert.AreEqual(1234, m_Service.HostingServicePort);
        }

        // ResetListenPort

        [Test]
        public void ResetListenPortShould_AssignTheGivenPort()
        {
            m_Service.ResetListenPort(1234);
            Assert.AreEqual(1234, m_Service.HostingServicePort);
        }

        [Test]
        public void ResetListenPortShould_AssignRandomPortIfZero()
        {
            var oldPort = m_Service.HostingServicePort;
            m_Service.ResetListenPort();
            m_Service.StartHostingService();
            Assert.Greater(m_Service.HostingServicePort, 0);
            Assert.AreNotEqual(m_Service.HostingServicePort, oldPort);
        }

        [Test]
        public void ResetListenPortShouldNot_StartServiceIfItIsNotRunning()
        {
            m_Service.StopHostingService();
            m_Service.ResetListenPort();
            Assert.IsFalse(m_Service.IsHostingServiceRunning);
        }

        [Test]
        public void ResetListenPortShould_RestartServiceIfRunning()
        {
            m_Service.StartHostingService();
            m_Service.ResetListenPort();
            Assert.IsTrue(m_Service.IsHostingServiceRunning);
        }
    }
}
