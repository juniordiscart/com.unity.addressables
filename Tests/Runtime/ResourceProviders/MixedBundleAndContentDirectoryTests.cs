#if ENABLE_CONTENT_DIRECTORIES
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
#endif

namespace UnityEngine.AddressableAssets.ResourceProviders.Tests
{
    /// <summary>
    /// Integration tests for builds that mix AssetBundle groups and Content Directory groups in a
    /// single Schema Driven build. These guard against regressions where the new Content Directory
    /// backend interferes with the established AssetBundle path - in particular that
    /// <see cref="AddressablesImpl.GetDownloadSizeAsync(object)"/> still reports accurate sizes for
    /// remote bundles when Content Directory content is also present in the catalog.
    ///
    /// The fixture builds three groups:
    ///  - A <see cref="ContentDirectoryGroupSchema"/> group (local).
    ///  - A <see cref="BundledAssetGroupSchema"/> group with UseUnityWebRequestForLocalBundles = true.
    ///    The bundle lives on local disk but is genuinely downloaded through UnityWebRequest by the
    ///    AssetBundleProvider (this is the "simulated remote" download path). Because the load path is
    ///    local, it still reports a download size of 0.
    ///  - A <see cref="BundledAssetGroupSchema"/> group built to a local ServerData folder but baked
    ///    with an http:// load path so its bundle locations are treated as remote
    ///    (<see cref="ResourceManagerConfig.IsPathRemote"/>) and report a real download size.
    /// </summary>
    public class MixedBundleAndContentDirectoryTests : AddressablesTestFixture
    {
        public static string RemoteHost
        {
            get
            {
                var server = Singleton?.RemoteContentServer;
                if (server != null && server.IsRunning)
                {
                    return $"http://{server.IPAddress}:{server.Port}/";
                }

                return string.Empty;
            }
        }
        static MixedBundleAndContentDirectoryTests Singleton;

        protected override TestBuildScriptMode BuildScriptMode => TestBuildScriptMode.SchemaDriven;

        const string k_CdGroupName = "MixedContentDirectoryGroup";
        const string k_LocalUwrBundleGroupName = "MixedLocalUWRBundleGroup";
        const string k_RemoteBundleGroupName = "MixedRemoteBundleGroup";

        const string k_CdAssetKey = "mixed_cd_asset";
        const string k_LocalUwrAssetKey = "mixed_local_uwr_asset";
        const string k_RemoteAssetKey = "mixed_remote_asset";
        const string k_RemoteAssetKey2 = "mixed_remote_asset2";

        const int k_MaxConcurrentRequests = 3;

        // The remote bundles are staged under StreamingAssets/<this folder> at build time (see
        // RunBuilder) so they ship with a standalone player and are servable in both the editor and a
        // player. The per-fixture [BuildTarget]/<fixture> layout lives beneath it.
        const string k_StreamingHostSubFolder = "SD";

        // In-process HTTP server that serves the staged bundles so the "remote" bundles are genuinely
        // downloaded over the network rather than read off disk.
        internal StaticFileServer RemoteContentServer;

        // The genuine http download is exercised in the editor and in standalone desktop players,
        // where StreamingAssets is a real File-readable directory and HttpListener is available.
        // Mobile/WebGL are out of scope (StreamingAssets is not File-accessible there).
        static bool RemoteHostingSupported =>
            Application.isEditor
            || Application.platform == RuntimePlatform.WindowsPlayer
            || Application.platform == RuntimePlatform.OSXPlayer
            || Application.platform == RuntimePlatform.LinuxPlayer;

        // Start the server once for the fixture. The remote bundles are staged under StreamingAssets by
        // the build (IPrebuildSetup), which has already run by the time the play-mode OneTimeSetUp
        // executes. Application.streamingAssetsPath resolves to the project's Assets/StreamingAssets in
        // the editor and to the packaged StreamingAssets folder in a standalone player, so the same
        // server root works in both modes.
        [OneTimeSetUp]
        public void StartRemoteContentServer()
        {
            // Only stand the server up where the genuine download runs and the files are reachable.
            if (!RemoteHostingSupported)
                return;
            string serverRoot = Path.Combine(Application.streamingAssetsPath, k_StreamingHostSubFolder);
            RemoteContentServer = new StaticFileServer(serverRoot);
            RemoteContentServer.Start();
            if(Singleton == null)
                Singleton = this;
        }

        [OneTimeTearDown]
        public void StopRemoteContentServer()
        {
            RemoteContentServer?.Stop();
            RemoteContentServer = null;
            Singleton = null;

#if UNITY_EDITOR
            // Editor runs leave the project clean. (Player runs can't touch the editor project here;
            // the next build's Setup wipes the staged folder, and it is gitignored regardless.)
            DeleteStagedRemoteContent();
#endif
        }

        // Reset global AssetBundle/UnityWebRequest state between tests. The base fixture only disposes
        // m_Addressables, so a bundle left loaded, an un-drained WebRequestQueue, or cached content
        // from a prior test could otherwise leak in and make tests (e.g. the active-request count
        // check) intermittently fail. Mirrors AssetBundleProviderTests' per-test cleanup.
        [SetUp]
        public void PerTestCleanup()
        {
#if ENABLE_CACHING
            Caching.ClearCache();
#endif
            AssetBundleProvider.WaitForAllUnloadingBundlesToComplete();
            AssetBundle.UnloadAllAssetBundles(true);
            if (m_Addressables != null)
                m_Addressables.WebRequestOverride = null;
        }

        [TearDown]
        public void DrainUnloadingBundles()
        {
            // Let any async bundle unloads finish before the base TearDown disposes Addressables.
            AssetBundleProvider.WaitForAllUnloadingBundlesToComplete();
        }

#if UNITY_EDITOR
        internal override void Setup(AddressableAssetSettings settings, string tempAssetFolder)
        {
            // Make the local build/load paths unique per fixture so this fixture does not share the
            // global Library/com.unity.addressables/aa/<Platform> destination with the other Content
            // Directory fixtures (see GroupAssetEntryProviderIntegrationTests for the full rationale).
            settings.profileSettings.SetValue(settings.activeProfileId, AddressableAssetSettings.kLocalBuildPath,
                $"{AddressableAssetSettings.kLocalBuildPathValue}/{m_UniqueTestName}");
            settings.profileSettings.SetValue(settings.activeProfileId, AddressableAssetSettings.kLocalLoadPath,
                $"{AddressableAssetSettings.kLocalLoadPathValue}/{m_UniqueTestName}");

            settings.profileSettings.SetValue(settings.activeProfileId, AddressableAssetSettings.kRemoteBuildPath,
                $"ServerData/[BuildTarget]/{m_UniqueTestName}");

            settings.profileSettings.SetValue(settings.activeProfileId, AddressableAssetSettings.kRemoteLoadPath,
                "{UnityEngine.AddressableAssets.ResourceProviders.Tests.MixedBundleAndContentDirectoryTests.RemoteHost}/[BuildTarget]/" + $"{m_UniqueTestName}");

            // Remove any staged remote content from a prior run so it never accumulates or serves stale
            // bundles. Runs in the editor every build, regardless of run mode.
            DeleteStagedRemoteContent();

            // Wipe stale outputs from prior runs so a leftover manifest/content hash cannot survive an
            // archive regeneration and cause the runtime lookup to miss.
            string fixtureLibraryDir = Path.Combine(
                Addressables.BuildPath,
                EditorUserBuildSettings.activeBuildTarget.ToString(),
                m_UniqueTestName);
            if (Directory.Exists(fixtureLibraryDir))
                Directory.Delete(fixtureLibraryDir, recursive: true);

            settings.MaxConcurrentWebRequests = k_MaxConcurrentRequests;

            CreateContentDirectoryGroup(settings, tempAssetFolder);
            CreateLocalUwrBundleGroup(settings, tempAssetFolder);
            CreateRemoteBundleGroup(settings, tempAssetFolder);
        }

        void CreateContentDirectoryGroup(AddressableAssetSettings settings, string tempAssetFolder)
        {
            AddressableAssetGroup cdGroup = settings.CreateGroup(
                k_CdGroupName, false, false, false, null, typeof(ContentDirectoryGroupSchema));

            var schema = cdGroup.GetSchema<ContentDirectoryGroupSchema>();
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            schema.CatalogId = $"mixed_cd_catalog_{m_UniqueTestName}";

            string prefabPath = CreateAssetPath(tempAssetFolder, k_CdAssetKey, ".prefab");
            string guid = CreatePrefab(prefabPath);
            settings.CreateOrMoveEntry(guid, cdGroup, false, false).address = k_CdAssetKey;
        }

        void CreateLocalUwrBundleGroup(AddressableAssetSettings settings, string tempAssetFolder)
        {
            AddressableAssetGroup group = settings.CreateGroup(
                k_LocalUwrBundleGroupName, false, false, true,
                new List<AddressableAssetGroupSchema>(), typeof(BundledAssetGroupSchema));

            var schema = group.GetSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            schema.UseUnityWebRequestForLocalBundles = true;
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.OnlyHash;

            // Use a dependency-free (empty) prefab so this locally-loaded asset cannot share a bundle
            // (e.g. the built-in shaders bundle) with the remote group. A shared dependency bundle can
            // only have one load path; if it were placed remotely the local asset would fail to load
            // offline and would also report a non-zero download size.
            string prefabPath = CreateAssetPath(tempAssetFolder, k_LocalUwrAssetKey, ".prefab");
            string guid = CreateEmptyPrefab(prefabPath);
            settings.CreateOrMoveEntry(guid, group, false, false).address = k_LocalUwrAssetKey;
        }

        // An empty GameObject (Transform only) references no shaders/materials and contains no
        // MonoBehaviours, so its bundle has no dependency on any shared built-in/MonoScript bundle.
        static string CreateEmptyPrefab(string assetPath)
        {
            var go = new GameObject(Path.GetFileNameWithoutExtension(assetPath));
            PrefabUtility.SaveAsPrefabAsset(go, assetPath);
            UnityEngine.Object.DestroyImmediate(go, false);
            return AssetDatabase.AssetPathToGUID(assetPath);
        }

        void CreateRemoteBundleGroup(AddressableAssetSettings settings, string tempAssetFolder)
        {
            AddressableAssetGroup group = settings.CreateGroup(
                k_RemoteBundleGroupName, false, false, true,
                new List<AddressableAssetGroupSchema>(), typeof(BundledAssetGroupSchema));

            var schema = group.GetSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.OnlyHash;

            // Two dependency-free (empty) prefabs in separate bundles so the size-summation logic is
            // exercised. Empty prefabs are used deliberately: a Cube would reference the built-in
            // Standard shader and pull in the shared unitybuiltinshaders bundle, which is assigned to
            // GetSharedBundleGroup()/DefaultGroup rather than the group that references it - its load
            // path (and therefore whether it counts as a remote download) is then non-deterministic.
            // With empty prefabs each remote asset is backed by exactly one self-contained remote bundle.
            foreach (var key in new[] { k_RemoteAssetKey, k_RemoteAssetKey2 })
            {
                string prefabPath = CreateAssetPath(tempAssetFolder, key, ".prefab");
                string guid = CreateEmptyPrefab(prefabPath);
                settings.CreateOrMoveEntry(guid, group, false, false).address = key;
            }
        }

        protected override void RunBuilder(AddressableAssetSettings settings)
        {
            try
            {
                base.RunBuilder(settings);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"MixedBundleAndContentDirectoryTests: RunBuilder failed: {ex}");
                if (ex.InnerException != null)
                    Debug.LogError($"Inner exception: {ex.InnerException}");
                Assert.Fail($"Addressables build (RunBuilder) failed (see Console). {ex}");
            }

            StageRemoteContentIntoStreamingAssets();
        }

        // Absolute path to the per-fixture remote bundle folder produced by the build
        // (ServerData/<BuildTarget>/<fixture>, resolved against the build working directory).
        string RemoteBuildDir => Path.GetFullPath(Path.Combine(
            "ServerData", EditorUserBuildSettings.activeBuildTarget.ToString(), m_UniqueTestName));

        // Absolute path to the staged copy under StreamingAssets, preserving the <BuildTarget>/<fixture>
        // layout so the StaticFileServer maps request URLs onto it 1:1.
        string StagedRemoteDir => Path.Combine(
            Application.streamingAssetsPath, k_StreamingHostSubFolder,
            EditorUserBuildSettings.activeBuildTarget.ToString(), m_UniqueTestName);

        // Copy the freshly-built remote bundles into Assets/StreamingAssets so Unity packages them into
        // a standalone player build and the in-player server can host them. The remote group uses empty
        // prefabs with PackSeparately and no shared bundles, so the whole fixture folder is exactly the
        // set of bundles the catalog references; their hashes match the catalog. The files are written
        // with raw File IO, so they are invisible to the AssetDatabase (no .meta, not imported) until a
        // refresh - and a standalone player build only packages StreamingAssets content the AssetDatabase
        // knows about, so the refresh is required for the player-test variant to ship these bundles.
        void StageRemoteContentIntoStreamingAssets()
        {
            if (!Directory.Exists(RemoteBuildDir))
            {
                Debug.LogError($"MixedBundleAndContentDirectoryTests: expected remote build output at '{RemoteBuildDir}' was not produced.");
                return;
            }

            string dest = StagedRemoteDir;
            Directory.CreateDirectory(Path.GetDirectoryName(dest));
            foreach (string src in Directory.GetFiles(RemoteBuildDir, "*", SearchOption.AllDirectories))
            {
                string relative = src.Substring(RemoteBuildDir.Length).TrimStart(Path.DirectorySeparatorChar, '/');
                string target = Path.Combine(dest, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(src, target, overwrite: true);
            }
        }

        // Delete the staged StreamingAssets copy (and its .meta) so the project stays clean.
        void DeleteStagedRemoteContent()
        {
            string hostRoot = Path.Combine(Application.streamingAssetsPath, k_StreamingHostSubFolder);
            if (Directory.Exists(hostRoot))
                Directory.Delete(hostRoot, recursive: true);
            string meta = hostRoot + ".meta";
            if (File.Exists(meta))
                File.Delete(meta);
        }
#endif

        protected override IEnumerator InitAddressables()
        {
            var op = m_Addressables.InitializeAsync(m_RuntimeSettingsPath, null, false);
            yield return op;
            if (op.Status != AsyncOperationStatus.Succeeded)
            {
                var details = op.OperationException?.ToString() ?? "(no OperationException on handle)";
                Debug.LogError($"MixedBundleAndContentDirectoryTests: InitializeAsync failed, status={op.Status}. {details}");
                Assert.Fail($"InitializeAsync failed: Status={op.Status}. OperationException: {details}");
            }

            OnRuntimeSetup();
            if (op.IsValid())
                op.Release();
        }

        // Sums the catalog-reported download size of the distinct remote bundle dependencies for the
        // given asset locations, and reports how many distinct remote bundles were counted. Mirrors
        // what GetDownloadSizeAsync does internally, but reads the size straight off
        // AssetBundleRequestOptions so the assertion is independent of that operation.
        long SumRemoteBundleSizes(IList<IResourceLocation> locations, out int distinctRemoteBundleCount)
        {
            long total = 0;
            // Dedup by transformed id: a bundle shared between assets (e.g. the built-in shaders
            // bundle) appears in multiple dependency lists but must only be counted once, matching
            // GetDownloadSizeAsync's distinct-locations behavior.
            var seenIds = new HashSet<string>();
            var remoteIds = new HashSet<string>();
            foreach (var loc in locations)
            {
                if (!loc.HasDependencies)
                    continue;
                foreach (var dep in loc.Dependencies)
                {
                    if (!(dep.Data is AssetBundleRequestOptions options))
                        continue;
                    var id = m_Addressables.ResourceManager.TransformInternalId(dep);
                    if (!seenIds.Add(id))
                        continue;
                    if (ResourceManagerConfig.IsPathRemote(id))
                    {
                        remoteIds.Add(id);
                        total += options.BundleSize;
                    }
                }
            }
            distinctRemoteBundleCount = remoteIds.Count;
            return total;
        }

        // Returns the transformed (runtime-resolved) internal id of the AssetBundle location that
        // backs the given asset location, or null if the asset is not bundle-backed (e.g. Content
        // Directory content). This is the actual string the loader hands to UnityWebRequest, so
        // asserting on it proves where the content is really loaded from.
        string GetBackingBundleId(IResourceLocation assetLocation)
        {
            if (!assetLocation.HasDependencies)
                return null;
            foreach (var dep in assetLocation.Dependencies)
            {
                if (dep.Data is AssetBundleRequestOptions)
                    return m_Addressables.ResourceManager.TransformInternalId(dep);
            }
            return null;
        }

        [Test]
        public void GetResourceLocations_ResolvesAllThreeContentTypes()
        {
            // Each asset has exactly one address, so its key must resolve to exactly one location, and
            // that location must belong to the asset we asked for. Asserting the exact count and the
            // PrimaryKey guards against Content Directory and AssetBundle location data getting crossed
            // in the merged catalog.
            Assert.IsTrue(m_Addressables.GetResourceLocations(k_CdAssetKey, typeof(GameObject), out var cdLocs),
                "Content Directory asset should resolve a location");
            Assert.AreEqual(1, cdLocs.Count, "Content Directory key should resolve to exactly one location");
            Assert.AreEqual(k_CdAssetKey, cdLocs[0].PrimaryKey, "Resolved location should be the Content Directory asset");

            Assert.IsTrue(m_Addressables.GetResourceLocations(k_LocalUwrAssetKey, typeof(GameObject), out var uwrLocs),
                "Local UWR bundle asset should resolve a location");
            Assert.AreEqual(1, uwrLocs.Count, "Local UWR bundle key should resolve to exactly one location");
            Assert.AreEqual(k_LocalUwrAssetKey, uwrLocs[0].PrimaryKey, "Resolved location should be the local UWR bundle asset");

            Assert.IsTrue(m_Addressables.GetResourceLocations(k_RemoteAssetKey, typeof(GameObject), out var remoteLocs),
                "Remote bundle asset should resolve a location");
            Assert.AreEqual(1, remoteLocs.Count, "Remote bundle key should resolve to exactly one location");
            Assert.AreEqual(k_RemoteAssetKey, remoteLocs[0].PrimaryKey, "Resolved location should be the remote bundle asset");
        }

        [Test]
        public void ContentLocations_HaveExpectedLocality_RemoteBundleIsHttp_LocalAndContentDirectoryAreNot()
        {
            if (!RemoteHostingSupported)
                Assert.Ignore("Remote http hosting is only available in the editor and desktop standalone players (see RemoteHostingSupported).");

            // Assert on the literal load-path strings (not just the IsPathRemote helper): the remote
            // bundle must be fetched over http://, while the local UWR bundle and the Content Directory
            // content must resolve to local paths.

            // Remote bundle: its backing AssetBundle location must be an http:// URL.
            Assert.IsTrue(m_Addressables.GetResourceLocations(k_RemoteAssetKey, typeof(object), out var remoteLocs));
            string remoteBundleId = GetBackingBundleId(remoteLocs[0]);
            Assert.IsNotNull(remoteBundleId, "Remote asset should be backed by an AssetBundle location");
            StringAssert.StartsWith("http://", remoteBundleId,
                $"Remote bundle must load over http; got '{remoteBundleId}'");
            Assert.IsTrue(ResourceManagerConfig.IsPathRemote(remoteBundleId),
                "Remote bundle path should be classified as remote");

            // Local UWR bundle: UseUnityWebRequestForLocalBundles changes the transport but not the
            // locality - the backing bundle must still resolve to a local (non-http) path.
            Assert.IsTrue(m_Addressables.GetResourceLocations(k_LocalUwrAssetKey, typeof(object), out var localLocs));
            string localBundleId = GetBackingBundleId(localLocs[0]);
            Assert.IsNotNull(localBundleId, "Local UWR asset should be backed by an AssetBundle location");
            Assert.IsFalse(localBundleId.StartsWith("http", StringComparison.OrdinalIgnoreCase),
                $"Local UWR bundle must not be remote; got '{localBundleId}'");
            Assert.IsFalse(ResourceManagerConfig.IsPathRemote(localBundleId),
                "Local UWR bundle path should not be classified as remote");

            // Content Directory: the location carries the CD's own LoadPath, which must be local.
            Assert.IsTrue(m_Addressables.GetResourceLocations(k_CdAssetKey, typeof(GameObject), out var cdLocs));
            var cdData = cdLocs[0].Data as ContentDirectoryAssetData;
            Assert.IsNotNull(cdData, "Content Directory location should carry ContentDirectoryAssetData");
            string cdLoadPath = AddressablesImpl.ResolveInternalId(cdData.LoadPath);
            Assert.IsFalse(cdLoadPath.StartsWith("http", StringComparison.OrdinalIgnoreCase),
                $"Content Directory must be local; got '{cdLoadPath}'");
            Assert.IsFalse(ResourceManagerConfig.IsPathRemote(cdLoadPath),
                "Content Directory load path should not be classified as remote");
        }

        [UnityTest]
        public IEnumerator LoadAsset_RemoteBundleOverHttp_And_LocalBundle_AndContentDirectory_AllLoad()
        {
#if ENABLE_CACHING
            Caching.ClearCache();
#endif
            // Content Directory asset loads from its local mount.
            var cdHandle = m_Addressables.LoadAssetAsync<GameObject>(k_CdAssetKey);
            yield return cdHandle;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, cdHandle.Status,
                "Content Directory asset should load from local content");
            Assert.IsNotNull(cdHandle.Result);

            // Local UWR bundle loads alongside the Content Directory content.
            var bundleHandle = m_Addressables.LoadAssetAsync<GameObject>(k_LocalUwrAssetKey);
            yield return bundleHandle;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, bundleHandle.Status, "Local bundle asset should load alongside CD content");
            Assert.IsNotNull(bundleHandle.Result);

            // The remote bundle is staged into StreamingAssets at build time and served by the
            // in-process HTTP server. StreamingAssets ships with standalone desktop players and is
            // File-readable there, so the genuine http download runs in the editor and in standalone
            // desktop players. It is skipped on platforms where StreamingAssets is not File-accessible
            // (mobile/WebGL); CD and local-bundle loading above are still covered in every run mode.
            if (RemoteHostingSupported)
            {
                Assert.IsNotNull(RemoteContentServer, "HTTP server should be running where remote hosting is supported");

                // Capture the server request count before/after to prove the load went over the network.
                int requestsBefore = RemoteContentServer.RequestCount;
                var remoteHandle = m_Addressables.LoadAssetAsync<GameObject>(k_RemoteAssetKey);
                yield return remoteHandle;
                Assert.AreEqual(AsyncOperationStatus.Succeeded, remoteHandle.Status,
                    "Remote bundle asset should download over http and load");
                Assert.IsNotNull(remoteHandle.Result);
                Assert.Greater(RemoteContentServer.RequestCount, requestsBefore,
                    "Remote bundle should have been served by the in-process HTTP server");
                remoteHandle.Release();
            }

            cdHandle.Release();
            bundleHandle.Release();
        }

        [UnityTest]
        public IEnumerator GetDownloadSizeAsync_RemoteBundle_ReturnsBundleSize_WithContentDirectoriesPresent()
        {
            if (!RemoteHostingSupported)
                Assert.Ignore("Remote http hosting is only available in the editor and desktop standalone players (see RemoteHostingSupported).");
#if ENABLE_CACHING
            Caching.ClearCache();
#endif
            Assert.IsTrue(m_Addressables.GetResourceLocations(k_RemoteAssetKey, typeof(object), out var locs));
            Assert.AreEqual(1, locs.Count, "Remote bundle key should resolve to exactly one location");

            long expected = SumRemoteBundleSizes(locs, out int remoteBundleCount);
            // The asset is a dependency-free prefab packed into its own bundle (PackSeparately) with no
            // shared built-in/MonoScript bundle, so exactly one remote bundle backs the remote asset.
            Assert.AreEqual(1, remoteBundleCount);

            var dOp = m_Addressables.GetDownloadSizeAsync((object)k_RemoteAssetKey);
            yield return dOp;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, dOp.Status);
            Assert.AreEqual(expected, dOp.Result,
                "GetDownloadSizeAsync must report the remote bundle size unaffected by Content Directory content");
            dOp.Release();
        }

        [UnityTest]
        public IEnumerator GetDownloadSizeAsync_ContentDirectoryAsset_ReturnsZero()
        {
            // Content Directory locations carry ContentDirectoryAssetData (not ILocationSizeData), so
            // they must contribute nothing to the computed download size.
            var dOp = m_Addressables.GetDownloadSizeAsync((object)k_CdAssetKey);
            yield return dOp;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, dOp.Status);
            Assert.AreEqual(0, dOp.Result, "Content Directory content has no remote download size");
            dOp.Release();
        }

        [UnityTest]
        public IEnumerator GetDownloadSizeAsync_LocalBundleForcedUWR_ReturnsZero()
        {
#if ENABLE_CACHING
            Caching.ClearCache();
#endif
            // Regression guard: UseUnityWebRequestForLocalBundles changes how the bundle is loaded but
            // does not make it remote, so its download size must still be reported as 0.
            var dOp = m_Addressables.GetDownloadSizeAsync((object)k_LocalUwrAssetKey);
            yield return dOp;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, dOp.Status);
            Assert.AreEqual(0, dOp.Result,
                "Local bundles report zero download size even with UseUnityWebRequestForLocalBundles enabled");
            dOp.Release();
        }

        [UnityTest]
        public IEnumerator GetDownloadSizeAsync_MixedKeys_SumsOnlyRemoteBundles()
        {
            if (!RemoteHostingSupported)
                Assert.Ignore("Remote http hosting is only available in the editor and desktop standalone players (see RemoteHostingSupported).");
#if ENABLE_CACHING
            Caching.ClearCache();
#endif
            Assert.IsTrue(m_Addressables.GetResourceLocations(k_RemoteAssetKey, typeof(object), out var remoteLocs));
            long expected = SumRemoteBundleSizes(remoteLocs, out int remoteBundleCount);
            Assert.AreEqual(1, remoteBundleCount);

            // A list spanning all three content types: only the remote bundle should contribute.
            var keys = new List<object> { k_CdAssetKey, k_LocalUwrAssetKey, k_RemoteAssetKey };
            var dOp = m_Addressables.GetDownloadSizeAsync(keys);
            yield return dOp;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, dOp.Status);
            Assert.AreEqual(expected, dOp.Result,
                "Only the remote bundle should contribute; CD and local content add nothing");
            dOp.Release();
        }

        [UnityTest]
        public IEnumerator GetDownloadSizeAsync_TwoRemoteAssets_SumsDistinctBundles()
        {
            if (!RemoteHostingSupported)
                Assert.Ignore("Remote http hosting is only available in the editor and desktop standalone players (see RemoteHostingSupported).");
#if ENABLE_CACHING
            Caching.ClearCache();
#endif
            Assert.IsTrue(m_Addressables.GetResourceLocations(k_RemoteAssetKey, typeof(object), out var locs1));
            Assert.AreEqual(1, locs1.Count, "First remote bundle key should resolve to exactly one location");
            Assert.IsTrue(m_Addressables.GetResourceLocations(k_RemoteAssetKey2, typeof(object), out var locs2));
            Assert.AreEqual(1, locs2.Count, "Second remote bundle key should resolve to exactly one location");

            var combined = new List<IResourceLocation>();
            combined.AddRange(locs1);
            combined.AddRange(locs2);
            long expected = SumRemoteBundleSizes(combined, out int remoteBundleCount);
            // Two dependency-free assets, each packed into its own separate bundle, must yield exactly
            // two distinct remote bundles (no shared built-in bundle exists).
            Assert.AreEqual(2, remoteBundleCount, "Two separately-packed remote assets should yield two distinct remote bundles");

            var dOp = m_Addressables.GetDownloadSizeAsync(new List<object> { k_RemoteAssetKey, k_RemoteAssetKey2 });
            yield return dOp;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, dOp.Status);
            Assert.AreEqual(expected, dOp.Result,
                "Two remote assets in separate bundles should sum their distinct bundle sizes");
            dOp.Release();
        }
    }
}
#endif
