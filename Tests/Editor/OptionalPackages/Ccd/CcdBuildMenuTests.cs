#if (ENABLE_CCD && ENABLE_MOQ)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using System.Threading.Tasks;
using Moq;
using Unity.Services.Ccd.Management;
using Unity.Services.Ccd.Management.Models;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine.AddressableAssets;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Runtime.Remoting.Contexts;
using static UnityEngine.Networking.UnityWebRequest;
using static UnityEditor.MaterialProperty;
using UnityEditor.PackageManager;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace UnityEditor.AddressableAssets.Tests.OptionalPackages.Ccd
{
    public class CcdBuildMenuTests
    {
        public enum RemoteCatalogType
        {
            None,
            Local,
            Remote
        };
        static RemoteCatalogType[] remoteCatalogTypes = new RemoteCatalogType[] { RemoteCatalogType.None, RemoteCatalogType.Local, RemoteCatalogType.Remote};

        private const int k_SleepTime = 30000;

        // this is all cribbed from AddressableAssetTestsBase
        protected const string k_TestConfigName = "CcdBuildAddressableAssetSettings.Tests";
        protected const string k_ProfileDefault = "Default";
        protected const string k_ProfileAutomaticAndLocal = "CCD - Automatic and Local Hosting";
        protected const string k_ProfileStaticAndLocal = "CCD - Static and Local Hosting";
        protected const string k_ProfileAutomaticAndStatic = "CCD - Automatic and Static Hosting";
        protected const string k_SecondBuildPath = "Second.BuildPath";
        protected const string k_SecondLoadPath = "Second.LoadPath";
        protected const char k_PrefixSeparator = '.';
        protected const string k_ProfileSettingsPath = "Assets/AddressableAssetsData/ProfileDataSourceSettings.asset";
        protected string k_CcdAutomaticBuildPath = $"{AddressableAssetSettings.kCCDBuildDataPath}/ManagedEnvironment/ManagedBucket/ManagedBadge";

        protected string k_CcdAutomaticLoadPath =
            $"https://{CloudProjectSettings.projectId}.client-api.unity3dusercontent.com/client_api/v1/environments/{{CcdManager.EnvironmentName}}/buckets/{{CcdManager.BucketId}}/release_by_badge/{{CcdManager.Badge}}/entry_by_path/content/?path=";


        protected string TestFolder => $"Assets/{TestFolderName}";
        protected string TestFolderName => $"{GetType()}_Tests";
        protected string ConfigFolder => TestFolder + "/Config";
        private AddressableAssetSettings m_Settings;
        private static string m_ProjectId;
        private static string m_EnvironmentName = "production";
        private static string m_EnvironmentId = "14069e00-c1e1-4b63-8d4c-0739213540fb";
        private static string m_DevEnvironmentId = "14069e00-c1e1-4b63-8d4c-0739213540fb";
        private static string m_ManagedBucketId = "05bb444b-5c7e-40ad-a123-fd7596f60784";
        private static string m_StaticBucketId = "ce62fde2-1451-4e0c-adee-1924e95b48e7";
        private static string m_SecondBucketId = "98476627-3c9d-49a2-ac79-b84e3e2b6913";
        // this is used to queue up subsequent calls to ListBuckets
        private Queue<List<CcdBucket>> m_listBucketCalls = new Queue<List<CcdBucket>>();



        private AddressablesDataBuilderInput m_Input;


        object m_CcdManagementInstance;

        CcdManagementServiceSdkMock m_CcdManagementMock;

        string m_ContentStatePath;

        bool m_IsHttpRunning;

        Thread m_HttpServer;


        public static void ListenerCallback(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            // Call EndGetContext to complete the asynchronous operation.
            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerResponse response = context.Response;


            string output;
            var path = context.Request.Url.LocalPath;
            // can't use a switch here due to interpolated path
            if (path == $"/api/unity/legacy/v1/projects/{m_ProjectId}/environments")
            {
                output = "{\"results\": [" +
                         "{\"id\": \"" + m_EnvironmentId + "\", \"name\": \"production\", \"is_default\": true}," +
                         "{\"id\": \"" + m_DevEnvironmentId + "\", \"name\": \"development\", \"is_default\": true}" +
                         "]}";
            }
            else if (path == "/api/auth/v1/genesis-token-exchange/unity/")
            {
                output = "{\"token\": \"mock-token\"}";
            }
            else
            {
                throw new Exception($"Unknown path: {context.Request.Url.LocalPath}");
            }

            Stream stream = response.OutputStream;
            var writer = new StreamWriter(stream);
            writer.Write(output);
            writer.Close();
        }

        private string startHttpServer()
        {
            // get unused port
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();

            var prefix = "http://" + IPAddress.Loopback + ":" + port;
            Debug.Log(prefix);

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(prefix + "/");
            listener.Start();
            Assert.True(listener.IsListening);

            m_IsHttpRunning = true;
            m_HttpServer = new Thread(() =>
            {
                while (m_IsHttpRunning)
                {
                    // GetContext method blocks while waiting for a request.
                    IAsyncResult result = listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
                    // only wait 500ms before we check if we're still running
                    result.AsyncWaitHandle.WaitOne(500);
                    // HttpListenerContext context = listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
                }

                listener.Close();
            });
            m_HttpServer.Start();
            return prefix;
        }

        private void stopHttpServer()
        {
            m_IsHttpRunning = false;
            if (m_HttpServer != null)
            {
                m_HttpServer.Join(k_SleepTime);
            }
        }

        private void deleteContentStateBin()
        {
            if (File.Exists(m_ContentStatePath))
            {
                File.Delete(m_ContentStatePath);
            }
        }

        private void createContentStateBin()
        {
            deleteContentStateBin();
            Directory.CreateDirectory(Path.GetDirectoryName(m_ContentStatePath));
            File.Create(m_ContentStatePath).Close();
        }

        [OneTimeSetUp]
        public void Init()
        {
            AssetDatabase.DeleteAsset(k_ProfileSettingsPath);

            if (Directory.Exists(TestFolder))
            {
                Debug.Log($"{GetType()} (init) - deleting {TestFolder}");
                if (!AssetDatabase.DeleteAsset(TestFolder))
                    Directory.Delete(TestFolder);
            }

            Debug.Log($"{GetType()} (init) - creating {TestFolder}");
            AssetDatabase.CreateFolder("Assets", TestFolderName);
            AssetDatabase.CreateFolder(TestFolder, "Config");

            m_Settings = AddressableAssetSettings.Create(ConfigFolder, k_TestConfigName, true, true);
            // ProfileDataSourceSettings::UpdateCCDDataSourcesAsync uses AddressableAssetSettingsDefaultObject so we have to set it
            if (!Directory.Exists("Assets/AddressableAssetsData"))
                Directory.CreateDirectory("Assets/AddressableAssetsData");
            AddressableAssetSettingsDefaultObject.Settings = m_Settings;

            m_Input = new AddressablesDataBuilderInput(m_Settings);

            // we cannot get the projectId inside of our http server thread so set it as a static value
            m_ProjectId = CloudProjectSettings.projectId;
            var prefix = startHttpServer();

            // We need to override ProfileDataSourceSettings.m_ServicesBasePath with our test http server
            var field = typeof(ProfileDataSourceSettings).GetField("m_ServicesBasePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            field.SetValue(null, prefix);

            // we reset all profiles in here
            buildLocalGroup();

            // now we can create a new profile
            var defaultProfileId = m_Settings.profileSettings.GetProfileId("Default");
            buildGroup1();
            buildGroup2();

            // create ccd automatic profile
            var ccdAutomaticAndLocal = m_Settings.profileSettings.AddProfile(k_ProfileAutomaticAndLocal, defaultProfileId);
            m_Settings.profileSettings.SetValue(ccdAutomaticAndLocal, AddressableAssetSettings.kRemoteBuildPath, k_CcdAutomaticBuildPath);
            m_Settings.profileSettings.SetValue(ccdAutomaticAndLocal, AddressableAssetSettings.kRemoteLoadPath, k_CcdAutomaticLoadPath);
            m_Settings.profileSettings.SetValue(ccdAutomaticAndLocal, k_SecondBuildPath, AddressableAssetSettings.kLocalBuildPathValue);
            m_Settings.profileSettings.SetValue(ccdAutomaticAndLocal, k_SecondLoadPath, AddressableAssetSettings.kLocalLoadPathValue);
            m_Settings.profileSettings.SetValue(ccdAutomaticAndLocal, ProfileDataSourceSettings.ENVIRONMENT_NAME, "production");


            var ccdStaticAndLocal = m_Settings.profileSettings.AddProfile(k_ProfileStaticAndLocal, defaultProfileId);
            m_Settings.profileSettings.SetValue(ccdStaticAndLocal, AddressableAssetSettings.kRemoteBuildPath,
                $"{AddressableAssetSettings.kCCDBuildDataPath}/{m_EnvironmentId}/{m_StaticBucketId}/latest");
            m_Settings.profileSettings.SetValue(ccdStaticAndLocal, AddressableAssetSettings.kRemoteLoadPath,
                $"https://{m_ProjectId}.client-api.unity3dusercontent.com/client_api/v1/environments/production/buckets/{m_StaticBucketId}/release_by_badge/latest/entry_by_path/content/?path=");
            m_Settings.profileSettings.SetValue(ccdStaticAndLocal, k_SecondBuildPath, AddressableAssetSettings.kLocalBuildPathValue);
            m_Settings.profileSettings.SetValue(ccdStaticAndLocal, k_SecondLoadPath, AddressableAssetSettings.kLocalLoadPathValue);
            m_Settings.profileSettings.SetValue(ccdStaticAndLocal, ProfileDataSourceSettings.ENVIRONMENT_NAME, "production");

            var ccdStaticAndAutomatic = m_Settings.profileSettings.AddProfile(k_ProfileAutomaticAndStatic, defaultProfileId);
            m_Settings.profileSettings.SetValue(ccdStaticAndAutomatic, AddressableAssetSettings.kRemoteBuildPath, k_CcdAutomaticBuildPath);
            m_Settings.profileSettings.SetValue(ccdStaticAndAutomatic, AddressableAssetSettings.kRemoteLoadPath, k_CcdAutomaticLoadPath);
            m_Settings.profileSettings.SetValue(ccdStaticAndAutomatic, k_SecondBuildPath, $"{AddressableAssetSettings.kCCDBuildDataPath}/{m_EnvironmentId}/{m_SecondBucketId}/latest");
            m_Settings.profileSettings.SetValue(ccdStaticAndAutomatic, k_SecondLoadPath,
                $"https://{m_ProjectId}.client-api.unity3dusercontent.com/client_api/v1/environments/production/buckets/{m_SecondBucketId}/release_by_badge/latest/entry_by_path/content/?path=");
            m_Settings.profileSettings.SetValue(ccdStaticAndAutomatic, ProfileDataSourceSettings.ENVIRONMENT_NAME, "production");

            setManagedBucketManually("", "", "");
        }

        [SetUp]
        public async Task SetUp()
        {
            // clear out the build directory
            if (Directory.Exists(AddressableAssetSettings.kCCDBuildDataPath))
            {
                Directory.Delete(AddressableAssetSettings.kCCDBuildDataPath, true);
            }

            // Setup fake CcdManagementService
            m_CcdManagementMock = new CcdManagementServiceSdkMock();
            m_CcdManagementMock.Init();

            #if CCD_3_OR_NEWER
                        m_CcdManagementMock.Setup(client => client.ListBucketsAsync(It.IsAny<PageOptions>(), It.IsAny<ListBucketsOptions>())).Returns<PageOptions, ListBucketsOptions>((v, bucketsOptions) =>
            #else
                        m_CcdManagementMock.Setup(client => client.ListBucketsAsync(It.IsAny<PageOptions>())).Returns<PageOptions>((v) =>
            #endif
            {
                if (v.Page == 1)
                {
                    if (m_listBucketCalls.Count == 0)
                    {
                        throw new Exception("no list bucket data found for call");
                    }

                    return Task.FromResult(m_listBucketCalls.Dequeue());
                }
                if (v.Page == 2)
                    throw new CcdManagementException(CcdManagementErrorCodes.OutOfRange, "out of range");
                return null;
            });

            // Refresh data sources to populate our profiles
            Assert.True(await refreshDataSources());

            // Setup ContentStatePath
            var getContentStateBuildPathMethod = m_Input.AddressableSettings.GetType().GetMethod("GetContentStateBuildPath", BindingFlags.NonPublic | BindingFlags.Instance);
            var contentStateBuildPath = (string)getContentStateBuildPathMethod.Invoke(m_Input.AddressableSettings, null);
            m_ContentStatePath = Path.Combine(contentStateBuildPath, "addressables_content_state.bin");
            resetManagedBucket();
        }

        [TearDown]
        public void Teardown()
        {
            m_CcdManagementMock.VerifyAll();
            AssetDatabase.DeleteAsset(k_ProfileSettingsPath);
            deleteContentStateBin();
            if (m_listBucketCalls.Count > 0)
            {
                throw new Exception("not all list bucket calls were made");
            }
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            if (Directory.Exists(TestFolder))
            {
                Debug.Log($"{GetType()} - (cleanup) deleting {TestFolder}");
                AssetDatabase.DeleteAsset(TestFolder);
            }


            if (File.Exists(k_ProfileSettingsPath))
            {
                File.Delete(k_ProfileSettingsPath);
            }

            EditorBuildSettings.RemoveConfigObject(k_TestConfigName);

            stopHttpServer();
        }

        private void createFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                if (!Directory.Exists(fileName))
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                var fs = File.Create(fileName);
                fs.Dispose();
            }

            Assert.True(File.Exists(fileName));
        }

        private void setupRemoteCatalog(RemoteCatalogType remoteCatalogType)
        {
            switch(remoteCatalogType)
            {
                case RemoteCatalogType.None:
                    m_Settings.BuildRemoteCatalog = false;
                    m_Settings.RemoteCatalogBuildPath = new ProfileValueReference();
                    break;
                case RemoteCatalogType.Local:
                    m_Settings.BuildRemoteCatalog = true;
                    m_Settings.RemoteCatalogBuildPath = new ProfileValueReference();
                    m_Settings.RemoteCatalogBuildPath.SetVariableByName(m_Settings, "Local.BuildPath");
                    m_Settings.RemoteCatalogLoadPath.SetVariableByName(m_Settings, "Local.LoadPath");
                    break;
                case RemoteCatalogType.Remote:
                    m_Settings.BuildRemoteCatalog = true;
                    m_Settings.RemoteCatalogBuildPath = new ProfileValueReference();
                    m_Settings.RemoteCatalogBuildPath.SetVariableByName(m_Settings, "Remote.BuildPath");
                    m_Settings.RemoteCatalogLoadPath.SetVariableByName(m_Settings, "Remote.LoadPath");
                    break;
            }
        }

        [Test]
        public async Task AutomaticAndStaticProfileUploadAndReleaseSuccess()
        {
            const string managedFileName = AddressableAssetSettings.kCCDBuildDataPath + "/ManagedEnvironment/ManagedBucket/ManagedBadge/managed_file.txt";
            createFile(managedFileName);
            string staticFileName = $"{AddressableAssetSettings.kCCDBuildDataPath}/{m_EnvironmentId}/{m_StaticBucketId}/latest/static_file.txt";
            createFile(staticFileName);

            var buildResult = new AddressablesPlayerBuildResult
            {
                FileRegistry = new FileRegistry()
            };
            buildResult.FileRegistry.AddFile(managedFileName);
            buildResult.FileRegistry.AddFile(staticFileName);

            var managedEntryId = Guid.Parse("678fcf3b-203c-4c95-9142-3c774de50f94");
            var staticEntryId = Guid.Parse("ca7e45ab-6f51-4286-adbd-d061b14af614");
            m_CcdManagementMock.Setup(client => client.CreateOrUpdateEntryByPathAsync(It.Is<EntryByPathOptions>((v) => v.Path == "managed_file.txt"), It.IsAny<EntryModelOptions>()))
                .Returns(Task.FromResult(new CcdEntry(entryid: managedEntryId)));
            m_CcdManagementMock.Setup(client => client.UploadContentAsync(It.Is<UploadContentOptions>((v) => v.EntryId == managedEntryId))).Returns(Task.FromResult(Task.CompletedTask));
            m_CcdManagementMock.Setup(client => client.CreateOrUpdateEntryByPathAsync(It.Is<EntryByPathOptions>((v) => v.Path == "static_file.txt"), It.IsAny<EntryModelOptions>()))
                .Returns(Task.FromResult(new CcdEntry(entryid: staticEntryId)));
            m_CcdManagementMock.Setup(client => client.UploadContentAsync(It.Is<UploadContentOptions>((v) => v.EntryId == staticEntryId))).Returns(Task.FromResult(Task.CompletedTask));
            m_CcdManagementMock.Setup(client => client.CreateReleaseAsync(It.IsAny<CreateReleaseOptions>())).Returns(Task.FromResult(new CcdRelease()));
            m_CcdManagementMock.Setup(client => client.GetEntryByPathAsync(It.Is<EntryByPathOptions>((v) => v.Path == "addressables_content_state.bin")))
                .Throws(new CcdManagementException(54, "not found"));


            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileAutomaticAndLocal);
            setManagedBucketManually(m_EnvironmentId, m_EnvironmentName, m_ManagedBucketId);
            var result = await CcdBuildEvents.Instance.UploadAndRelease(m_Input, buildResult);
            Assert.True(result);

            File.Delete(managedFileName);
            File.Delete(staticFileName);
        }


        [Test]
        public async Task AutomaticProfileUploadAndReleaseSuccess()
        {
            const string fileName = AddressableAssetSettings.kCCDBuildDataPath + "/ManagedEnvironment/ManagedBucket/ManagedBadge/ccd_file.txt";
            createFile(fileName);

            var buildResult = new AddressablesPlayerBuildResult
            {
                FileRegistry = new FileRegistry()
            };
            buildResult.FileRegistry.AddFile(fileName);

            m_CcdManagementMock.Setup(client => client.CreateOrUpdateEntryByPathAsync(It.IsAny<EntryByPathOptions>(), It.IsAny<EntryModelOptions>())).Returns(Task.FromResult(new CcdEntry()));
            m_CcdManagementMock.Setup(client => client.UploadContentAsync(It.IsAny<UploadContentOptions>())).Returns(Task.FromResult(Task.CompletedTask));
            m_CcdManagementMock.Setup(client => client.GetEntryByPathAsync(It.IsAny<EntryByPathOptions>())).Returns(Task.FromResult(new CcdEntry()));
            m_CcdManagementMock.Setup(client => client.CreateReleaseAsync(It.IsAny<CreateReleaseOptions>())).Returns(Task.FromResult(new CcdRelease()));

            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileAutomaticAndLocal);
            setManagedBucketManually(m_EnvironmentId, m_EnvironmentName, m_ManagedBucketId);
            var result = await CcdBuildEvents.Instance.UploadAndRelease(m_Input, buildResult);
            Assert.True(result);

            File.Delete(fileName);
        }

        [Test]
        public async Task StaticProfileUploadAndReleaseSuccess()
        {
            string fileName = $"{AddressableAssetSettings.kCCDBuildDataPath}/{m_EnvironmentId}/{m_StaticBucketId}/latest/ccd_file.txt";
            createFile(fileName);

            var buildResult = new AddressablesPlayerBuildResult
            {
                FileRegistry = new FileRegistry()
            };
            buildResult.FileRegistry.AddFile(fileName);

            m_CcdManagementMock.Setup(client => client.CreateOrUpdateEntryByPathAsync(It.IsAny<EntryByPathOptions>(), It.IsAny<EntryModelOptions>())).Returns(Task.FromResult(new CcdEntry()));
            m_CcdManagementMock.Setup(client => client.UploadContentAsync(It.IsAny<UploadContentOptions>())).Returns(Task.FromResult(Task.CompletedTask));
            m_CcdManagementMock.Setup(client => client.GetEntryByPathAsync(It.IsAny<EntryByPathOptions>())).Returns(Task.FromResult(new CcdEntry()));
            m_CcdManagementMock.Setup(client => client.CreateReleaseAsync(It.IsAny<CreateReleaseOptions>())).Returns(Task.FromResult(new CcdRelease()));

            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileStaticAndLocal);
            setManagedBucketManually(null, "", null);
            var result = await CcdBuildEvents.Instance.UploadAndRelease(m_Input, buildResult);
            Assert.True(result);

            File.Delete(fileName);
        }

        [Test]
        public async Task UploadAndReleaseNoFile()
        {
            var result = await CcdBuildEvents.Instance.UploadAndRelease(m_Input, new AddressablesPlayerBuildResult());
            Assert.False(result);
        }

        [Test]
        public async Task UploadAndReleaseCreateEntryError()
        {

            string fileName = $"{AddressableAssetSettings.kCCDBuildDataPath}/{m_EnvironmentId}/{m_StaticBucketId}/latest/ccd_file.txt";
            createFile(fileName);

            var buildResult = new AddressablesPlayerBuildResult
            {
                FileRegistry = new FileRegistry()
            };
            buildResult.FileRegistry.AddFile(fileName);

            m_CcdManagementMock.Setup(client => client.CreateOrUpdateEntryByPathAsync(It.IsAny<EntryByPathOptions>(), It.IsAny<EntryModelOptions>())).Throws(InternalErrorException());
            LogAssert.Expect(LogType.Error, new Regex($".*Unable to create entry for ccd_file.txt: crash.*", RegexOptions.Singleline));

            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileStaticAndLocal);
            setManagedBucketManually(null, "", null);
            var result = await CcdBuildEvents.Instance.UploadAndRelease(m_Input, buildResult);
            Assert.False(result);

            File.Delete(fileName);
        }

        [Test]
        public async Task UploadContentStateNoBin()
        {
            m_Settings.BuildRemoteCatalog = true;
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileAutomaticAndLocal);
            setManagedBucketManually(m_EnvironmentId, m_EnvironmentName, m_ManagedBucketId);

            var result = await CcdBuildEvents.Instance.UploadContentState(m_Input, new AddressablesPlayerBuildResult());
            Assert.False(result);
            LogAssert.Expect(LogType.Error, new Regex("^(Content state file is missing)"));
        }

        [Test]
        public async Task UploadContentStateNoBucketSet()
        {
            createContentStateBin();

            m_Settings.BuildRemoteCatalog = true;
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileAutomaticAndLocal);
            setManagedBucketManually(m_EnvironmentId, m_EnvironmentName, null);

            var result = await CcdBuildEvents.Instance.UploadContentState(m_Input, new AddressablesPlayerBuildResult());
            Assert.False(result);
            LogAssert.Expect(LogType.Error, "Content state could not be uploaded as no bucket was specified. This is populated for managed profiles in the VerifyBucket event.");
        }

        [Test]
        public async Task UploadContentStateNoRemoteCatalog()
        {
            m_Settings.BuildRemoteCatalog = false;

            var result = await CcdBuildEvents.Instance.UploadContentState(m_Input, new AddressablesPlayerBuildResult());
            Assert.True(result);
            LogAssert.Expect(LogType.Warning, "Not uploading content state, because 'Build Remote Catalog' is not checked in Addressable Asset Settings. This will disable content updates");
        }

        [Test]
        public async Task UploadContentStateRemoteCatalogPathsLocalHosting()
        {
            m_Settings.BuildRemoteCatalog = true;
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileDefault);
            ;

            var result = await CcdBuildEvents.Instance.UploadContentState(m_Input, new AddressablesPlayerBuildResult());
            Assert.False(result);
            LogAssert.Expect(LogType.Error, "Content state could not be uploaded as the remote catalog is not targeting CCD");
        }

        [Test]
        public async Task UploadContentStateSuccess()
        {
            createContentStateBin();

            m_Settings.BuildRemoteCatalog = true;
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileAutomaticAndLocal);
            setManagedBucketManually(m_EnvironmentId, m_EnvironmentName, m_ManagedBucketId);

            m_CcdManagementMock.Setup(client => client.CreateOrUpdateEntryByPathAsync(It.IsAny<EntryByPathOptions>(), It.IsAny<EntryModelOptions>())).Returns(Task.FromResult(new CcdEntry()));
            m_CcdManagementMock.Setup(client => client.UploadContentAsync(It.IsAny<UploadContentOptions>())).Returns(Task.FromResult(Task.CompletedTask));

            var result = await CcdBuildEvents.Instance.UploadContentState(m_Input, new AddressablesPlayerBuildResult());
            Assert.True(result);
        }

        [Test]
        public async Task UploadContentUploadFailure()
        {
            createContentStateBin();

            m_Settings.BuildRemoteCatalog = true;
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileAutomaticAndLocal);
            setManagedBucketManually(m_EnvironmentId, m_EnvironmentName, m_ManagedBucketId);

            m_CcdManagementMock.Setup(client => client.CreateOrUpdateEntryByPathAsync(It.IsAny<EntryByPathOptions>(), It.IsAny<EntryModelOptions>())).Returns(Task.FromResult(new CcdEntry()));
            m_CcdManagementMock.Setup(client => client.UploadContentAsync(It.IsAny<UploadContentOptions>())).Throws(InternalErrorException());

            var result = await CcdBuildEvents.Instance.UploadContentState(m_Input, new AddressablesPlayerBuildResult());
            Assert.False(result);
            LogAssert.Expect(LogType.Error, $"Unable to upload content state: crash");
        }

        [Test]
        public async Task UploadContentCreateEntryFailure()
        {
            createContentStateBin();

            m_Settings.BuildRemoteCatalog = true;
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileAutomaticAndLocal);
            setManagedBucketManually(m_EnvironmentId, m_EnvironmentName, m_ManagedBucketId);

            m_CcdManagementMock.Setup(client => client.CreateOrUpdateEntryByPathAsync(It.IsAny<EntryByPathOptions>(), It.IsAny<EntryModelOptions>())).Throws(InternalErrorException());

            var result = await CcdBuildEvents.Instance.UploadContentState(m_Input, new AddressablesPlayerBuildResult());
            Assert.False(result);
            LogAssert.Expect(LogType.Error, $"Unable to create entry for content state: crash");
        }


        [Test]
        public async Task DownloadContentStateNoRemoteCatalog()
        {
            m_Input.AddressableSettings.BuildRemoteCatalog = false;
            var result = await CcdBuildEvents.Instance.DownloadContentStateBin(m_Input);
            Assert.True(result);
            LogAssert.Expect(LogType.Warning, "Not downloading content state because 'Build Remote Catalog' is not checked in Addressable Asset Settings. This will disable content updates");
        }

        [Test]
        public async Task DownloadContentStateRemoteCatalogPathsLocalHosting()
        {
            m_Settings.BuildRemoteCatalog = true;
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileDefault);
            ;

            var result = await CcdBuildEvents.Instance.DownloadContentStateBin(m_Input);
            Assert.False(result);
            LogAssert.Expect(LogType.Error, "Content state could not be downloaded as the remote catalog is not targeting CCD");
        }

        [Test]
        public async Task DownloadContentStateNoBin()
        {
            m_Settings.BuildRemoteCatalog = true;
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileAutomaticAndLocal);
            setManagedBucketManually(m_EnvironmentId, m_EnvironmentName, m_ManagedBucketId);

            var error = new CcdManagementException(54, "not found");
            m_CcdManagementMock.Setup(client => client.GetEntryByPathAsync(It.IsAny<EntryByPathOptions>())).Throws(error);

            var result = await CcdBuildEvents.Instance.DownloadContentStateBin(m_Input);
            Assert.True(result);
        }

        [Test]
        public async Task DownloadContentStateNoBucketSet()
        {
            m_Settings.BuildRemoteCatalog = true;
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileAutomaticAndLocal);
            setManagedBucketManually(m_EnvironmentId, m_EnvironmentName, null);

            var result = await CcdBuildEvents.Instance.DownloadContentStateBin(m_Input);
            Assert.False(result);
            LogAssert.Expect(LogType.Error, "Content state could not be downloaded as no bucket was specified. This is populated for managed profiles in the VerifyTargetBucket event.");
        }

        [Test]
        public async Task DownloadContentStateSuccess()
        {
            m_Settings.BuildRemoteCatalog = true;
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileAutomaticAndLocal);
            setManagedBucketManually(m_EnvironmentId, m_EnvironmentName, m_ManagedBucketId);


            m_CcdManagementMock.Setup(client => client.GetEntryByPathAsync(It.IsAny<EntryByPathOptions>())).Returns(Task.FromResult(new CcdEntry()));
            m_CcdManagementMock.Setup(client => client.GetContentAsync(It.IsAny<EntryOptions>())).Returns(Task.FromResult((Stream)new MemoryStream()));

            var result = await CcdBuildEvents.Instance.DownloadContentStateBin(m_Input);
            Assert.True(result);
            Assert.True(File.Exists(m_ContentStatePath));
        }

        [Test]
        public async Task DownloadContentStateGetEntryFailure()
        {
            m_Settings.BuildRemoteCatalog = true;
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileAutomaticAndLocal);
            setManagedBucketManually(m_EnvironmentId, m_EnvironmentName, m_ManagedBucketId);


            m_CcdManagementMock.Setup(client => client.GetEntryByPathAsync(It.IsAny<EntryByPathOptions>())).Throws(this.InternalErrorException());

            var result = await CcdBuildEvents.Instance.DownloadContentStateBin(m_Input);
            Assert.False(result);
            Assert.False(File.Exists(m_ContentStatePath));
            LogAssert.Expect(LogType.Error, $"Unable to get entry for content state addressables_content_state.bin: crash");
        }

        [Test]
        public async Task DownloadContentStateUploadFailure()
        {
            m_Settings.BuildRemoteCatalog = true;
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileAutomaticAndLocal);
            setManagedBucketManually(m_EnvironmentId, m_EnvironmentName, m_ManagedBucketId);


            m_CcdManagementMock.Setup(client => client.GetEntryByPathAsync(It.IsAny<EntryByPathOptions>())).Returns(Task.FromResult(new CcdEntry()));
            m_CcdManagementMock.Setup(client => client.GetContentAsync(It.IsAny<EntryOptions>())).Throws(InternalErrorException);

            var result = await CcdBuildEvents.Instance.DownloadContentStateBin(m_Input);
            Assert.False(result);
            Assert.False(File.Exists(m_ContentStatePath));
            LogAssert.Expect(LogType.Error, $"Unable to upload content state addressables_content_state.bin: crash");
        }

        private void resetManagedBucket()
        {
            var ccdManagedDataField = m_Input.AddressableSettings.GetType().GetField("m_CcdManagedData", BindingFlags.NonPublic | BindingFlags.Instance);
            ccdManagedDataField.SetValue(m_Input.AddressableSettings, new CcdManagedData());
        }


        // the bucket ID is set in VerifyBucket so if you are testing another method in isolation you'll probably need to call this to set the bucket
        private void setManagedBucketManually(string environmentId, string environmentName, string bucketId)
        {
            // VerifyBucket sets the BucketId in m_CcdManagedData which we need to manually populate since it's only
            var ccdManagedDataField = m_Input.AddressableSettings.GetType().GetField("m_CcdManagedData", BindingFlags.NonPublic | BindingFlags.Instance);
            var ccdManagedDataInstance = ccdManagedDataField.GetValue(m_Input.AddressableSettings);
            var bucketIdField = ccdManagedDataInstance.GetType().GetField("BucketId", BindingFlags.Public | BindingFlags.Instance);
            bucketIdField.SetValue(ccdManagedDataInstance, bucketId);

            var environmentIdField = ccdManagedDataInstance.GetType().GetField("EnvironmentId", BindingFlags.Public | BindingFlags.Instance);
            environmentIdField.SetValue(ccdManagedDataInstance, environmentId);

            var environmentNameField = ccdManagedDataInstance.GetType().GetField("EnvironmentName", BindingFlags.Public | BindingFlags.Instance);
            environmentNameField.SetValue(ccdManagedDataInstance, environmentName);
        }

        public void buildLocalGroup()
        {
            m_Settings.profileSettings.CreateValue(ProfileDataSourceSettings.ENVIRONMENT_NAME, "production");
            m_Settings.profileSettings.CreateValue(k_SecondBuildPath, AddressableAssetSettings.kRemoteBuildPathValue);
            m_Settings.profileSettings.CreateValue(k_SecondLoadPath, AddressableAssetSettings.kRemoteLoadPathValue);


            GameObject testObject = new GameObject("DrabCube");
            PrefabUtility.SaveAsPrefabAsset(testObject, TestFolder + "/drab.prefab");
            var assetGUID = AssetDatabase.AssetPathToGUID(TestFolder + "/drab.prefab");

            var group = m_Settings.FindGroup("Default Local Group");
            m_Settings.CreateOrMoveEntry(assetGUID, group);
        }

        public void buildGroup1()
        {
            var group = m_Settings.CreateGroup("First Group", false, false, false, null);

            var schema = group.AddSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(m_Settings, AddressableAssetSettings.kRemoteBuildPath);
            schema.LoadPath.SetVariableByName(m_Settings, AddressableAssetSettings.kRemoteLoadPath);
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;

            group.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;

            GameObject testObject = new GameObject("RedCube");
            PrefabUtility.SaveAsPrefabAsset(testObject, TestFolder + "/red.prefab");
            var assetGUID = AssetDatabase.AssetPathToGUID(TestFolder + "/red.prefab");
            m_Settings.CreateOrMoveEntry(assetGUID, group);
        }

        public void buildGroup2()
        {
            var group = m_Settings.CreateGroup("Second Group", false, false, false, null);

            var schema = group.AddSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(m_Settings, k_SecondBuildPath);
            schema.LoadPath.SetVariableByName(m_Settings, k_SecondLoadPath);
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;

            group.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;

            GameObject testObject = new GameObject("BlueCube");
            PrefabUtility.SaveAsPrefabAsset(testObject, TestFolder + "/blue.prefab");
            var assetGUID = AssetDatabase.AssetPathToGUID(TestFolder + "/blue.prefab");
            m_Settings.CreateOrMoveEntry(assetGUID, group);
        }

        public async Task<bool> refreshDataSources()
        {
            var managedBucket = new CcdBucket(id: Guid.Parse(m_ManagedBucketId), name: EditorUserBuildSettings.activeBuildTarget.ToString(), attributes: new CcdBucketAttributes(promoteOnly: false));
            var staticBucket = new CcdBucket(id: Guid.Parse(m_StaticBucketId), name: "Static Bucket", attributes: new CcdBucketAttributes(promoteOnly: false));
            var secondBucket = new CcdBucket(id: Guid.Parse(m_SecondBucketId), name: "Second Bucket", attributes: new CcdBucketAttributes(promoteOnly: false));
            var buckets = new List<CcdBucket>();
            buckets.Add(managedBucket);
            buckets.Add(staticBucket);
            buckets.Add(secondBucket);

            m_CcdManagementMock.Setup(client => client.ListBadgesAsync(managedBucket.Id, It.IsAny<PageOptions>()))
                .Throws(new CcdManagementException(CcdManagementErrorCodes.OutOfRange, "out of range"));
            m_CcdManagementMock.Setup(client => client.ListBadgesAsync(staticBucket.Id, It.IsAny<PageOptions>()))
                .Throws(new CcdManagementException(CcdManagementErrorCodes.OutOfRange, "out of range"));
            m_CcdManagementMock.Setup(client => client.ListBadgesAsync(secondBucket.Id, It.IsAny<PageOptions>()))
                .Throws(new CcdManagementException(CcdManagementErrorCodes.OutOfRange, "out of range"));
            return await refreshDataSources(buckets);
        }
        public async Task<bool> refreshDataSources(List<CcdBucket> buckets)
        {
            // this is refresh data
            // production
            m_listBucketCalls.Enqueue(buckets);
            // development
            m_listBucketCalls.Enqueue(new List<CcdBucket>());

            // I can't add group types directly as it's internal. So adding my test group types by calling
            var result = await CcdBuildEvents.Instance.RefreshDataSources(m_Input);

            // we have to set the currentEnvironment as that is only set through the GUI by default
            var currentEnvironmentField = ProfileDataSourceSettings.GetSettings().GetType().GetField("currentEnvironment", BindingFlags.NonPublic | BindingFlags.Instance);
            var currentEnvironment = currentEnvironmentField.GetValue(ProfileDataSourceSettings.GetSettings());
            var currentEnvironmentIdField = currentEnvironment.GetType().GetField("id", BindingFlags.Public | BindingFlags.Instance);
            currentEnvironmentIdField.SetValue(currentEnvironment, m_EnvironmentId);
            var currentEnvironmentNameField = currentEnvironment.GetType().GetField("name", BindingFlags.Public | BindingFlags.Instance);
            currentEnvironmentNameField.SetValue(currentEnvironment, "production");
            var currentEnvironmentProjectField = currentEnvironment.GetType().GetField("projectGenesisId", BindingFlags.Public | BindingFlags.Instance);
            currentEnvironmentProjectField.SetValue(currentEnvironment, m_ProjectId);

            return result;
        }


        [Test]
        public async Task VerifyTargetBucketAllLocal([ValueSource(nameof(remoteCatalogTypes))] RemoteCatalogType remoteCatalogType)
        {
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileDefault);
            setupRemoteCatalog(remoteCatalogType);
            var success = await CcdBuildEvents.Instance.VerifyTargetBucket(m_Input);
            Assert.True(success);
#if !ADDRESSABLES_WITHOUT_GROUP_FIXES
            LogAssert.Expect(LogType.Warning, "No Addressable Asset Groups have been marked remote or the current profile is not using CCD.");
#endif

            m_Settings.BuildRemoteCatalog = true;
            success = await CcdBuildEvents.Instance.VerifyTargetBucket(m_Input);
            Assert.True(success);
#if !ADDRESSABLES_WITHOUT_GROUP_FIXES
            LogAssert.Expect(LogType.Warning, "No Addressable Asset Groups have been marked remote or the current profile is not using CCD.");
            LogAssert.Expect(LogType.Warning, "A remote catalog will be built without any remote Asset Bundles.");
#endif
        }

        private CcdManagementException AlreadyExistsException()
        {
            return new CcdManagementException(CcdManagementErrorCodes.AlreadyExists, "already exists");
        }

        private CcdManagementException InternalErrorException()
        {
            return new CcdManagementException(CcdManagementErrorCodes.InternalError, "crash");
        }

        [Test]
        public async Task VerifyTargetBucketAutomaticAndStaticProfile([ValueSource(nameof(remoteCatalogTypes))] RemoteCatalogType remoteCatalogType)
        {
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileAutomaticAndStatic);
            setupRemoteCatalog(remoteCatalogType);
            var success = await CcdBuildEvents.Instance.VerifyTargetBucket(m_Input);
            Assert.True(success);

            var ccdManagedDataField = m_Input.AddressableSettings.GetType().GetField("m_CcdManagedData", BindingFlags.NonPublic | BindingFlags.Instance);
            var ccdManagedDataInstance = ccdManagedDataField.GetValue(m_Input.AddressableSettings);
            var bucketIdField = ccdManagedDataInstance.GetType().GetField("BucketId", BindingFlags.Public | BindingFlags.Instance);
            var verifiedBucketId = bucketIdField.GetValue(ccdManagedDataInstance);
            Assert.AreEqual(m_ManagedBucketId, verifiedBucketId);
        }

        [Test]
        public async Task VerifyTargetBucketAutomaticProfile([ValueSource(nameof(remoteCatalogTypes))] RemoteCatalogType remoteCatalogType)
        {
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileAutomaticAndLocal);
            setupRemoteCatalog(remoteCatalogType);

            var success = await CcdBuildEvents.Instance.VerifyTargetBucket(m_Input);
            Assert.True(success);

            var ccdManagedDataField = m_Input.AddressableSettings.GetType().GetField("m_CcdManagedData", BindingFlags.NonPublic | BindingFlags.Instance);
            var ccdManagedDataInstance = ccdManagedDataField.GetValue(m_Input.AddressableSettings);
            var bucketIdField = ccdManagedDataInstance.GetType().GetField("BucketId", BindingFlags.Public | BindingFlags.Instance);
            var verifiedBucketId = bucketIdField.GetValue(ccdManagedDataInstance);
            Assert.AreEqual(m_ManagedBucketId, verifiedBucketId);
        }

        [Test]
        public async Task VerifyTargetBucketAutomaticProfileNoManagedData([ValueSource(nameof(remoteCatalogTypes))] RemoteCatalogType remoteCatalogType)
        {
            var managedBucket = new CcdBucket(id: Guid.Parse(m_ManagedBucketId), name: EditorUserBuildSettings.activeBuildTarget.ToString(), attributes: new CcdBucketAttributes(promoteOnly: false));
            m_CcdManagementMock.Setup(client => client.CreateBucketAsync(It.Is<CreateBucketOptions>((v) => v.Name == EditorUserBuildSettings.activeBuildTarget.ToString())))
                .Returns(Task.FromResult(managedBucket));
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileAutomaticAndLocal);
            setupRemoteCatalog(remoteCatalogType);

            resetManagedBucket();

            // this should be a completely new remote project
            await refreshDataSources(new List<CcdBucket>());

            var success = await CcdBuildEvents.Instance.VerifyTargetBucket(m_Input);
            Assert.True(success);

            var ccdManagedDataField = m_Input.AddressableSettings.GetType().GetField("m_CcdManagedData", BindingFlags.NonPublic | BindingFlags.Instance);
            var ccdManagedDataInstance = ccdManagedDataField.GetValue(m_Input.AddressableSettings);
            var bucketIdField = ccdManagedDataInstance.GetType().GetField("BucketId", BindingFlags.Public | BindingFlags.Instance);
            var verifiedBucketId = bucketIdField.GetValue(ccdManagedDataInstance);
            Assert.AreEqual(m_ManagedBucketId, verifiedBucketId);
        }

        [Test]
        public async Task VerifyTargetBucketAutomaticProfileNoManagedDataBucketExists([ValueSource(nameof(remoteCatalogTypes))] RemoteCatalogType remoteCatalogType)
        {
            var managedBucket = new CcdBucket(id: Guid.Parse(m_ManagedBucketId), name: EditorUserBuildSettings.activeBuildTarget.ToString(), attributes: new CcdBucketAttributes(promoteOnly: false));
            m_CcdManagementMock.Setup(client => client.CreateBucketAsync(It.Is<CreateBucketOptions>((v) => v.Name == EditorUserBuildSettings.activeBuildTarget.ToString())))
                .Throws(AlreadyExistsException());
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileAutomaticAndLocal);
            setupRemoteCatalog(remoteCatalogType);

            resetManagedBucket();

            // this should be a completely new remote project
            await refreshDataSources(new List<CcdBucket>());
            // since the bucket already exists we should load the bucket
            m_listBucketCalls.Enqueue(new List<CcdBucket>(){managedBucket});

            var success = await CcdBuildEvents.Instance.VerifyTargetBucket(m_Input);
            Assert.True(success);

            var ccdManagedDataField = m_Input.AddressableSettings.GetType().GetField("m_CcdManagedData", BindingFlags.NonPublic | BindingFlags.Instance);
            var ccdManagedDataInstance = ccdManagedDataField.GetValue(m_Input.AddressableSettings);
            var bucketIdField = ccdManagedDataInstance.GetType().GetField("BucketId", BindingFlags.Public | BindingFlags.Instance);
            var verifiedBucketId = bucketIdField.GetValue(ccdManagedDataInstance);
            Assert.AreEqual(m_ManagedBucketId, verifiedBucketId);
        }


        [Test]
        public async Task VerifyTargetBucketStaticProfile([ValueSource(nameof(remoteCatalogTypes))] RemoteCatalogType remoteCatalogType)
        {
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileStaticAndLocal);
            setupRemoteCatalog(remoteCatalogType);
            var success = await CcdBuildEvents.Instance.VerifyTargetBucket(m_Input);
            Assert.True(success);
        }

        [Test]
        public async Task VerifyTargetBucketOverride([ValueSource(nameof(remoteCatalogTypes))] RemoteCatalogType remoteCatalogType)
        {
            var ccdManagedDataField = m_Input.AddressableSettings.GetType().GetField("m_CcdManagedData", BindingFlags.NonPublic | BindingFlags.Instance);
            var ccdManagedDataInstance = ccdManagedDataField.GetValue(m_Input.AddressableSettings);

            var stateField = ccdManagedDataInstance.GetType().GetField("State", BindingFlags.Public | BindingFlags.Instance);
            var stateEnumValues = stateField.GetValue(ccdManagedDataInstance).GetType().GetEnumValues();
            stateField.SetValue(ccdManagedDataInstance, stateEnumValues.GetValue(2));

            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileAutomaticAndLocal);
            setupRemoteCatalog(remoteCatalogType);
            m_Settings.RemoteCatalogBuildPath.SetVariableByName(m_Settings, "Remote.BuildPath");
            m_Settings.RemoteCatalogLoadPath.SetVariableByName(m_Settings, "Remote.LoadPath");

            var success = await CcdBuildEvents.Instance.VerifyTargetBucket(m_Input);
            Assert.True(success);
        }

        static Tuple<bool, string>[] buildRemoteCatalogValues = new Tuple<bool, string>[]
        {
            new Tuple<bool, string>(true, "Remote Catalog"),
            new Tuple<bool, string>(false, "First Group")
        };

        [Test]
        public async Task VerifyTargetEnvironmentNotFound([ValueSource(nameof(buildRemoteCatalogValues))] Tuple<bool, string> remoteCatalogPair)
        {
            LogAssert.Expect(LogType.Error, new Regex(".*Unable to find remote environment badenvironment.*", RegexOptions.Singleline));
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileAutomaticAndLocal);
            m_Settings.profileSettings.SetValue(m_Settings.activeProfileId, ProfileDataSourceSettings.ENVIRONMENT_NAME, "badenvironment");
            m_Settings.BuildRemoteCatalog = remoteCatalogPair.Item1;
            var success = await CcdBuildEvents.Instance.VerifyTargetBucket(m_Input);
            Assert.False(success);
        }


        [Test]
        public async Task VerifyTargetEnvironmentInternalError([ValueSource(nameof(buildRemoteCatalogValues))] Tuple<bool, string> remoteCatalogPair)
        {
            // we have to delete the automatic profile bucket from the cache so CreateBucketAsync is called
            DeleteBucketFromCache(EditorUserBuildSettings.activeBuildTarget.ToString());

            LogAssert.Expect(LogType.Error, $"Unable to verify target bucket for {remoteCatalogPair.Item2}: crash");
            m_CcdManagementMock.Setup(client => client.CreateBucketAsync(It.Is<CreateBucketOptions>((v) => v.Name == EditorUserBuildSettings.activeBuildTarget.ToString())))
                .Throws<CcdManagementException>(InternalErrorException);
            m_Settings.activeProfileId = m_Settings.profileSettings.GetProfileId(k_ProfileAutomaticAndLocal);
            m_Settings.BuildRemoteCatalog = remoteCatalogPair.Item1;
            var success = await CcdBuildEvents.Instance.VerifyTargetBucket(m_Input);
            Assert.False(success);
        }

        private void DeleteBucketFromCache(string bucketName)
        {
            ProfileGroupType automaticProfileType = null;
            foreach (ProfileGroupType profileType in ProfileDataSourceSettings.GetSettings().profileGroupTypes)
            {
                var bucketNameVariable = profileType.GetVariableBySuffix($"{nameof(CcdBucket)}{nameof(CcdBucket.Name)}");
                if (bucketNameVariable != null && bucketNameVariable.Value == bucketName)
                {
                    automaticProfileType = profileType;
                }
            }
            Assert.IsNotNull(automaticProfileType);
            Assert.True(ProfileDataSourceSettings.GetSettings().profileGroupTypes.Remove(automaticProfileType));
        }
    }
}
#endif
