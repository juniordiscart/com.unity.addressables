using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AddressableTests.SyncAddressables
{
    public abstract class SyncAddressableTests : AddressablesTestFixture
    {
        protected string m_PrefabKey = "syncprefabkey";
        protected string m_PrefabKey2 = "syncprefabkey2";
        protected string m_PrefabKey3 = "syncprefabkey3";
        protected string m_PrefabWithDep = "syncprefabwithdep";
        protected string m_InvalidKey = "notarealkey";
        protected string m_SceneKey = "syncscenekey";
        protected string m_ResourcePrefabKey = "resourceprefabkey";

#if UNITY_EDITOR
        private AddressableAssetSettings m_settingsInstance;
        protected AddressableAssetSettings m_Settings
        {
            get
            {
                if (m_settingsInstance == null)
                    m_settingsInstance = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(Path.Combine(GetGeneratedAssetsPath(), "Settings", "AddressableAssetSettings.Tests.asset"));
                return m_settingsInstance;
            }
        }
#endif

#if UNITY_EDITOR
        internal override void Setup(AddressableAssetSettings settings, string tempAssetFolder)
        {
            AddressableAssetGroup syncGroup = settings.CreateGroup("SyncAddressables", false, false, true,
                new List<AddressableAssetGroupSchema>(), typeof(BundledAssetGroupSchema));
            syncGroup.GetSchema<BundledAssetGroupSchema>().BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.OnlyHash;

            AddressableAssetGroup syncGroup2 = settings.CreateGroup("SyncAddressables2", false, false, true,
                new List<AddressableAssetGroupSchema>(), typeof(BundledAssetGroupSchema));
            syncGroup2.GetSchema<BundledAssetGroupSchema>().BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.OnlyHash;

            //Create prefab
            string guid = CreatePrefab(tempAssetFolder + "/synctest.prefab");
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, syncGroup);
            entry.address = m_PrefabKey;

            string path = Path.Combine($"{GetGeneratedAssetsPath()}", "Resources");
            Directory.CreateDirectory(path);

            string resourceGuid = CreatePrefab(Path.Combine(path, "synctest.prefab"));
            AddressableAssetEntry resourceEntry = settings.CreateOrMoveEntry(resourceGuid, syncGroup);
            resourceEntry.address = m_ResourcePrefabKey;

            //Create prefab
            string guid2 = CreatePrefab(tempAssetFolder + "/synctest2.prefab");
            AddressableAssetEntry entry2 = settings.CreateOrMoveEntry(guid2, syncGroup2);
            entry2.address = m_PrefabKey2;

            //Create Scenes
            string sceneGuid = CreateScene($"{tempAssetFolder}/SyncTestScene.unity");
            AddressableAssetEntry sceneEntry = settings.CreateOrMoveEntry(sceneGuid, syncGroup);
            sceneEntry.address = m_SceneKey;

#if ENABLE_ASYNC_ASSETBUNDLE_UWR
            AddressableAssetGroup syncGroup3 = settings.CreateGroup("SyncAddressables3", false, false, true,
                new List<AddressableAssetGroupSchema>(), typeof(BundledAssetGroupSchema));
            syncGroup3.GetSchema<BundledAssetGroupSchema>().BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.OnlyHash;
            syncGroup3.GetSchema<BundledAssetGroupSchema>().UseUnityWebRequestForLocalBundles = true;

            //Create prefab
            string guid3 = CreatePrefab(tempAssetFolder + "/synctest3.prefab");
            AddressableAssetEntry entry3 = settings.CreateOrMoveEntry(guid3, syncGroup3);
            entry3.address = m_PrefabKey3;

            // Create prefab with dependency
            {
                AddressableAssetGroup syncGroupMat = settings.CreateGroup("SyncAddressablesMatGroup", false, false, true,
                    new List<AddressableAssetGroupSchema>(), typeof(BundledAssetGroupSchema));
                syncGroupMat.GetSchema<BundledAssetGroupSchema>().BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.OnlyHash;
                syncGroupMat.GetSchema<BundledAssetGroupSchema>().UseUnityWebRequestForLocalBundles = true;

                //Create material
                var mat = new Material(Shader.Find("Unlit/Color"));
                string matPath = tempAssetFolder + "/synctestmaterial.mat";
                AssetDatabase.CreateAsset(mat, matPath);
                string matGuid = AssetDatabase.AssetPathToGUID(matPath);
                settings.CreateOrMoveEntry(matGuid, syncGroupMat);

                AddressableAssetGroup syncGroupPrefabWithDeps = settings.CreateGroup("SyncAddressablesPrefabWithDepsGroup", false, false, true,
                    new List<AddressableAssetGroupSchema>(), typeof(BundledAssetGroupSchema));
                syncGroupPrefabWithDeps.GetSchema<BundledAssetGroupSchema>().BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.OnlyHash;
                syncGroupPrefabWithDeps.GetSchema<BundledAssetGroupSchema>().UseUnityWebRequestForLocalBundles = true;

                //Create prefab
                string prefabDepPath = tempAssetFolder + "/synctestprefabwithdeps.prefab";
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var renderer = go.GetComponent<Renderer>();
                renderer.sharedMaterials = new Material[] { mat };
                PrefabUtility.SaveAsPrefabAsset(go, prefabDepPath);
                Object.DestroyImmediate(go, false);
                string prefabDepsGuid = AssetDatabase.AssetPathToGUID(prefabDepPath);
                AddressableAssetEntry prefabDepsEntry = settings.CreateOrMoveEntry(prefabDepsGuid, syncGroupPrefabWithDeps);
                prefabDepsEntry.address = m_PrefabWithDep;
            }
#endif
        }

#endif

        protected void ReleaseOp(AsyncOperationHandle handle)
        {
            if (handle.IsValid())
                handle.Release();
            Assert.IsFalse(handle.IsValid());
        }

        [Test]
        public void SyncAddressableLoad_CompletesWithValidKey()
        {
            var loadOp = m_Addressables.LoadAssetAsync<GameObject>(m_PrefabKey);
            var result = loadOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp.Status);
            Assert.NotNull(result);
            Assert.IsNotNull(loadOp.Result);
            Assert.AreEqual(loadOp.Result, result);

            //Cleanup
            ReleaseOp(loadOp);
        }

        [Test]
        public void SyncAddressableLoad_CompletesWithValidKeyWhenLoadedFromResources()
        {
            //string path = Path.Combine($"{GetGeneratedAssetsPath()}", "Resources");

            IResourceLocation loc = new ResourceLocationBase(m_ResourcePrefabKey, "synctest", typeof(LegacyResourcesProvider).FullName, typeof(GameObject));
            if (!m_Addressables.ResourceManager.ResourceProviders.Any(rp => rp.GetType() == typeof(LegacyResourcesProvider)))
                m_Addressables.ResourceManager.ResourceProviders.Add(new LegacyResourcesProvider());

            var loadOp = m_Addressables.LoadAssetAsync<GameObject>(loc);
            var result = loadOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp.Status);
            Assert.NotNull(result);
            Assert.IsNotNull(loadOp.Result);
            Assert.AreEqual(loadOp.Result, result);
            //Cleanup
            ReleaseOp(loadOp);
        }

        [Test]
        public void SyncAddressableLoadAssets_CompletesWithValidKey()
        {
            var loadOp = m_Addressables.LoadAssetsAsync<GameObject>(new List<string>() {m_PrefabKey, m_SceneKey}, null, Addressables.MergeMode.Union, false);
            var result = loadOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp.Status);
            Assert.NotNull(result);
            Assert.IsNotNull(loadOp.Result);
            Assert.AreEqual(loadOp.Result, result);

            //Cleanup
            ReleaseOp(loadOp);
        }

        [Test]
        [Timeout(3000)]
        public void SyncAddressables_DoesntHang_WhenCallingMultiple_BackToBack()
        {
            var pf1 = m_Addressables.LoadAssetAsync<GameObject>(m_PrefabKey).WaitForCompletion();
            var pf2 = m_Addressables.LoadAssetAsync<GameObject>(m_PrefabKey2).WaitForCompletion();

            Assert.IsNotNull(pf1);
            Assert.IsNotNull(pf2);

            m_Addressables.ReleaseInstance(pf1);
            m_Addressables.ReleaseInstance(pf2);
        }

        [Test]
        [Timeout(3000)]
        public void ProviderOperation_WaitForCompletion_AfterOpComplete_DoesntHang()
        {
            ProviderOperation<GameObject> providerOp = new ProviderOperation<GameObject>();
            providerOp.Init(m_Addressables.ResourceManager, new BundledAssetProvider(),
                new ResourceLocationBase(m_PrefabKey, m_PrefabKey, typeof(BundledAssetProvider).FullName, typeof(GameObject)),
                new AsyncOperationHandle<IList<AsyncOperationHandle>>());
            providerOp.HasExecuted = true;
            providerOp.ProviderCompleted(default(GameObject), true, null);

            providerOp.WaitForCompletion();
        }

        [Test]
        public void SyncAddressableLoadAssets_CompletesWithInvalidKey()
        {
            var loadOp = m_Addressables.LoadAssetsAsync<GameObject>(new List<string>() {m_PrefabKey, m_SceneKey, "bad key"}, null, Addressables.MergeMode.Union, false);
            var result = loadOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp.Status);
            Assert.NotNull(result);
            Assert.IsNotNull(loadOp.Result);
            Assert.AreEqual(loadOp.Result, result);

            //Cleanup
            ReleaseOp(loadOp);
        }

        [Test]
        public void SyncAddressableLoad_CompletesWithInvalidKey()
        {
            var loadOp = m_Addressables.LoadAssetAsync<GameObject>(m_InvalidKey);
            Assert.IsNull(loadOp.WaitForCompletion());
            LogAssert.Expect(LogType.Error, new Regex("InvalidKeyException*"));
            Assert.AreEqual(AsyncOperationStatus.Failed, loadOp.Status);
            Assert.IsNull(loadOp.Result);
            //Cleanup
            ReleaseOp(loadOp);
        }

        [Test]
        public void CheckForCatalogUpdates_CompletesSynchronously()
        {
            var checkForUpdates = m_Addressables.CheckForCatalogUpdates(false);
            Assert.IsNotNull(checkForUpdates.WaitForCompletion());
            Assert.AreEqual(AsyncOperationStatus.Succeeded, checkForUpdates.Status);
            Assert.IsTrue(checkForUpdates.IsDone);

            //Cleanup
            ReleaseOp(checkForUpdates);
        }

        [Test]
        public void CheckForCatalogUpdates_CompletesSynchronously_WhenAutoReleaseHandle()
        {
            var checkForUpdates = m_Addressables.CheckForCatalogUpdates();
            checkForUpdates.WaitForCompletion();
            Assert.IsFalse(checkForUpdates.IsValid());
            Assert.IsTrue(checkForUpdates.IsDone);
        }

        [Test]
        public void UpdateCatalogs_CompletesSynchronously()
        {
            var updateCatalogs = m_Addressables.UpdateCatalogs(null, false);
            Assert.IsNull(updateCatalogs.WaitForCompletion());
            LogAssert.Expect(LogType.Error, new Regex("Content update not available*"));
            LogAssert.Expect(LogType.Error, new Regex(".*ChainOperation.*"));
            Assert.AreEqual(AsyncOperationStatus.Failed, updateCatalogs.Status);
            Assert.IsTrue(updateCatalogs.IsDone);

            //Cleanup
            ReleaseOp(updateCatalogs);
        }

        [Test]
        public void GetDownloadSizeAsync_CompletesSynchronously()
        {
            var getDownloadSize = m_Addressables.GetDownloadSizeAsync((object)m_PrefabKey);
            Assert.IsNotNull(getDownloadSize.WaitForCompletion());
            Assert.AreEqual(AsyncOperationStatus.Succeeded, getDownloadSize.Status);
            Assert.IsTrue(getDownloadSize.IsDone);

            //Cleanup
            ReleaseOp(getDownloadSize);
        }

        [Test]
        public void DownloadDependencies_CompletesSynchronously()
        {
            var downloadDependencies = m_Addressables.DownloadDependenciesAsync((object)m_PrefabKey);
            var result = downloadDependencies.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, downloadDependencies.Status);
            Assert.IsNotNull(result);
            Assert.IsTrue(downloadDependencies.IsDone);

            //Cleanup
            ReleaseOp(downloadDependencies);
        }

        [Test]
        public void ClearDependencyCache_CompletesSynchronously()
        {
            var clearCache = m_Addressables.ClearDependencyCacheAsync((object)m_PrefabKey, false);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, clearCache.Status);
            Assert.IsTrue(clearCache.WaitForCompletion());
            Assert.IsTrue(clearCache.IsDone);

            //Cleanup
            ReleaseOp(clearCache);
        }

        [Test]
        public void InstantiateSync_CompletesSuccessfully_WithValidKey()
        {
            var loadOp = m_Addressables.InstantiateAsync(m_PrefabKey);
            var result = loadOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp.Status);
            Assert.IsNotNull(result);
            Assert.IsNotNull(loadOp.Result);
            Assert.AreEqual(loadOp.Result, result);

            //Cleanup
            ReleaseOp(loadOp);
        }

        [Test]
        public void InstantiateMultipleObjectsSync_CompletesSuccessfully_WithValidKey()
        {
            var loadOp1 = m_Addressables.InstantiateAsync(m_PrefabKey);
            var result1 = loadOp1.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp1.Status);
            Assert.IsNotNull(result1);
            Assert.IsNotNull(loadOp1.Result);
            Assert.AreEqual(loadOp1.Result, result1);

            var loadOp2 = m_Addressables.InstantiateAsync(m_PrefabKey);
            var result2 = loadOp2.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp2.Status);
            Assert.IsNotNull(result2);
            Assert.IsNotNull(loadOp2.Result);
            Assert.AreEqual(loadOp2.Result, result2);

            Assert.AreNotEqual(result1, result2);

            //Cleanup
            ReleaseOp(loadOp1);
            ReleaseOp(loadOp2);
        }

        [Test]
        public void InstantiateSync_CompletesSuccessfully_WithInvalidKey()
        {
            var loadOp = m_Addressables.InstantiateAsync(m_InvalidKey);
            LogAssert.Expect(LogType.Error, new Regex("InvalidKeyException*"));
            Assert.AreEqual(AsyncOperationStatus.Failed, loadOp.Status);
            Assert.IsNull(loadOp.WaitForCompletion());
            Assert.IsNull(loadOp.Result);

            //Cleanup
            ReleaseOp(loadOp);
        }

        [Test]
        public void RequestingResourceLocation_CompletesSynchronously()
        {
            var requestOp = m_Addressables.LoadResourceLocationsAsync(m_PrefabKey);
            var result = requestOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, requestOp.Status);
            Assert.IsNotNull(result);
            Assert.IsNotNull(requestOp.Result);
            Assert.AreEqual(requestOp.Result, result);

            //Cleanup
            ReleaseOp(requestOp);
        }

        [Test]
        public void RequestingResourceLocations_CompletesSynchronously()
        {
            var requestOp = m_Addressables.LoadResourceLocationsAsync(new List<string>() {m_PrefabKey, m_SceneKey}, Addressables.MergeMode.Union);
            var result = requestOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, requestOp.Status);
            Assert.IsNotNull(result);
            Assert.IsNotNull(requestOp.Result);
            Assert.AreEqual(requestOp.Result, result);

            //Cleanup
            ReleaseOp(requestOp);
        }

        [Test]
        public void LoadContentCatalogSynchronously_SuccessfullyCompletes_WithValidPath()
        {
            string catalogPath = m_RuntimeSettingsPath.Replace("settings", "catalog");

            //There's no catalog created for fast mode.  Creating one at this point had issues on CI
            if (catalogPath.StartsWith("GUID:"))
                Assert.Ignore();

            var loadCatalogOp = m_Addressables.LoadContentCatalogAsync(catalogPath, false);
            var result = loadCatalogOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadCatalogOp.Status);
            Assert.IsNotNull(loadCatalogOp.Result);
            Assert.AreEqual(loadCatalogOp.Result, result);

            //Cleanup
            ReleaseOp(loadCatalogOp);
        }

        [Test]
        public void LoadContentCatalogSynchronously_SuccessfullyCompletes_WithInvalidPath()
        {
            //Removing need to check for each individual error message since it's brittle and not the purpose of this test
            bool savedLogAssertState = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            var loadCatalogOp = m_Addressables.LoadContentCatalogAsync("not a real path.json", false);

            var result = loadCatalogOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Failed, loadCatalogOp.Status);
            Assert.IsNull(result);
            Assert.IsNull(loadCatalogOp.Result);

            //Cleanup
            ReleaseOp(loadCatalogOp);
            LogAssert.ignoreFailingMessages = savedLogAssertState;
        }

        [Test]
        public void InstanceOperation_WithFailedBundleLoad_CompletesSync()
        {
            var depOp = m_Addressables.LoadAssetAsync<GameObject>(m_InvalidKey);
            LogAssert.Expect(LogType.Error, new Regex("InvalidKeyException*"));

            var instanceOperation = new ResourceManager.InstanceOperation();
            instanceOperation.Init(m_Addressables.ResourceManager, new InstanceProvider(), new InstantiationParameters(), depOp);
            //Since we're calling the operation directly we need to simulate the full workflow of the additional ref count during load
            instanceOperation.IncrementReferenceCount();

            instanceOperation.WaitForCompletion();
            LogAssert.Expect(LogType.Error, new Regex("InvalidKeyException*"));

            Assert.IsTrue(instanceOperation.IsDone);
            m_Addressables.Release(depOp);
        }

        [Test]
        public void InstantiateSync_InvalidKeyExceptionCorrectlyThrown()
        {
            var prevHandler = ResourceManager.ExceptionHandler;
            ResourceManager.ExceptionHandler = (handle, exception) =>
            {
                Assert.AreEqual(typeof(InvalidKeyException), exception.GetType(),
                    "Exception thrown is not of the correct type.");
            };
            var depOp = m_Addressables.LoadAssetAsync<GameObject>(m_InvalidKey);
            m_Addressables.Release(depOp);
            ResourceManager.ExceptionHandler = prevHandler;
        }

        [Test]
        public void InstanceOperation_WithSuccessfulBundleLoad_CompletesSync()
        {
            var depOp = m_Addressables.LoadAssetAsync<GameObject>(m_PrefabKey);
            var instanceOperation = new ResourceManager.InstanceOperation();
            instanceOperation.Init(m_Addressables.ResourceManager, new InstanceProvider(), new InstantiationParameters(), depOp);
            //Since we're calling the operation directly we need to simulate the full workflow of the additional ref count during load
            instanceOperation.IncrementReferenceCount();

            instanceOperation.WaitForCompletion();

            Assert.IsTrue(instanceOperation.IsDone);
            m_Addressables.Release(depOp);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CleanBundleCache_CompletesSynchronously(bool forceSingleThreading)
        {
#if ENABLE_CACHING
            if (BuildScriptMode == TestBuildScriptMode.Fast || BuildScriptMode == TestBuildScriptMode.Virtual)
                Assert.Ignore("Bundle caching does not occur when using this playmode.");

            var cleanOp = m_Addressables.CleanBundleCache(null, forceSingleThreading);
            cleanOp.WaitForCompletion();

            Assert.AreEqual(AsyncOperationStatus.Succeeded, cleanOp.Status);
            Assert.IsTrue(cleanOp.IsDone);

            //Cleanup
            ReleaseOp(cleanOp);
#else
            Assert.Ignore("Caching not enabled.");
#endif
        }

        class FailedAssetBundleResource : IAssetBundleResource
        {
            public AssetBundle GetAssetBundle()
            {
                return null;
            }
        }
    }

    //This class is made because of, and should be refactored away when resolved, bug: https://jira.unity3d.com/browse/ADDR-1215
    public abstract class SyncAddressablesWithSceneTests : SyncAddressableTests
    {
        [UnityTest]
        public IEnumerator LoadingScene_Synchronously_ActivateOnLoadDisabled_Completes()
        {
            var loadOp = m_Addressables.LoadSceneAsync(m_SceneKey, new LoadSceneParameters(LoadSceneMode.Additive), false);
            bool callbackCompleted = false;
            loadOp.Completed += handle => callbackCompleted = true;
            var result = loadOp.WaitForCompletion();

            Assert.IsTrue(callbackCompleted, "When activateOnLoad is disabled, scene load operation expected to complete in WaitForCompletion");
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp.Status);
            Assert.IsNotNull(result);
            Assert.IsTrue(loadOp.IsDone);
            Assert.AreEqual(loadOp.Result, result);
            Assert.IsTrue(Mathf.Approximately(result.m_Operation.progress, 0.9f), "SceneLoading WaitForCompletion expected to end on 0.9 activation");

            // complete the activation step
            yield return result.ActivateAsync();
            yield return loadOp;

            //Cleanup
            var unloadHandle = m_Addressables.UnloadSceneAsync(loadOp);
            yield return unloadHandle;
            ReleaseOp(loadOp);
        }

        [UnityTest]
        public IEnumerator LoadingScene_Synchronously_ActivateOnLoad_CompletesAsynchronously()
        {
            var loadOp = m_Addressables.LoadSceneAsync(m_SceneKey, new LoadSceneParameters(LoadSceneMode.Additive), true);
            bool callbackCompleted = false;
            loadOp.Completed += handle => callbackCompleted = true;
            var result = loadOp.WaitForCompletion();
            Assert.IsNotNull(result);
            Assert.AreEqual(AsyncOperationStatus.None, loadOp.Status);
            Assert.IsFalse(loadOp.IsDone);
            Assert.IsFalse(callbackCompleted, "When activateOnLoad is enabled, scene load operation expected to complete after asynchronous activation");
            Assert.IsTrue(Mathf.Approximately(result.m_Operation.progress, 0.9f), "SceneLoading WaitForCompletion expected to end on 0.9 activation");

            // complete the activation step
            yield return loadOp;
            Assert.IsTrue(callbackCompleted, "When activateOnLoad is enabled, scene load operation expected to complete after asynchronous activation");
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp.Status);
            Assert.IsTrue(result.m_Operation.isDone);
            Assert.AreEqual(loadOp.Result.Scene, result.Scene);

            //Cleanup
            var unloadHandle = m_Addressables.UnloadSceneAsync(loadOp);
            yield return unloadHandle;
            ReleaseOp(loadOp);
        }

        [UnityTest]
        public IEnumerator UnloadingScene_Synchronously_WhenAutoReleasingHandle_LogsWarning()
        {
            var loadOp = m_Addressables.LoadSceneAsync(m_SceneKey, new LoadSceneParameters(LoadSceneMode.Additive));
            loadOp.WaitForCompletion();
            yield return loadOp;

            var unloadOp = m_Addressables.UnloadSceneAsync(loadOp);
            LogAssert.Expect(LogType.Warning, "Cannot unload a Scene with WaitForCompletion. Scenes must be unloaded asynchronously.");
            unloadOp.WaitForCompletion();
            yield return unloadOp;

            Assert.IsFalse(unloadOp.IsValid());
            Assert.IsTrue(unloadOp.IsDone);

            ReleaseOp(loadOp);
        }

        [UnityTest]
        public IEnumerator UnloadingScene_Synchronously_LogsWarning()
        {
            var loadOp = m_Addressables.LoadSceneAsync(m_SceneKey, new LoadSceneParameters(LoadSceneMode.Additive));
            loadOp.WaitForCompletion();
            yield return loadOp;

            var unloadOp = m_Addressables.UnloadSceneAsync(loadOp, UnloadSceneOptions.None, false);
            LogAssert.Expect(LogType.Warning, "Cannot unload a Scene with WaitForCompletion. Scenes must be unloaded asynchronously.");
            unloadOp.WaitForCompletion();
            yield return unloadOp;

            Assert.AreEqual(AsyncOperationStatus.Succeeded, unloadOp.Status);
            Assert.IsTrue(unloadOp.IsValid());
            Assert.IsTrue(unloadOp.IsDone);

            //Cleanup
            ReleaseOp(unloadOp);
            ReleaseOp(loadOp);
        }
    }

#if UNITY_EDITOR
    class SyncAddressableTests_FastMode : SyncAddressablesWithSceneTests
    {
        protected override TestBuildScriptMode BuildScriptMode
        {
            get { return TestBuildScriptMode.Fast; }
        }

        [Test]
        [Timeout(3000)]
        public void FastModeInitializeOperation_CompletesSync()
        {
            FastModeInitializationOperation fmio = new FastModeInitializationOperation(m_Addressables, m_Settings);
            fmio.WaitForCompletion();
        }

        [Test]
        public void DownloadDependencies_CompletesSynchronously_WhenAutoReleased()
        {
            var downloadDependencies = m_Addressables.DownloadDependenciesAsync((object)m_PrefabKey, true);
            downloadDependencies.WaitForCompletion();
            Assert.IsTrue(downloadDependencies.IsDone);
        }
    }

    class SyncAddressableTests_VirtualMode : SyncAddressablesWithSceneTests
    {
        protected override TestBuildScriptMode BuildScriptMode
        {
            get { return TestBuildScriptMode.Virtual; }
        }
    }

    class SyncAddressableTests_PackedPlaymodeMode_SceneTests : SyncAddressablesWithSceneTests
    {
        protected override TestBuildScriptMode BuildScriptMode
        {
            get { return TestBuildScriptMode.PackedPlaymode; }
        }
    }

    class SyncAddressableTests_PackedPlaymodeMode_UnityWebRequestTests : SyncAddressableTests_UnityWebRequestTests
    {
        protected override TestBuildScriptMode BuildScriptMode
        {
            get { return TestBuildScriptMode.PackedPlaymode; }
        }
    }
#endif

    [UnityPlatform(exclude = new[] { RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor })]
    class SyncAddressableTests_PackedMode_UnityWebRequestTests : SyncAddressableTests_UnityWebRequestTests
    {
        protected override TestBuildScriptMode BuildScriptMode
        {
            get { return TestBuildScriptMode.Packed; }
        }
    }

    abstract class SyncAddressableTests_UnityWebRequestTests : SyncAddressableTests
    {
        [Test]
        public void DownloadDependencies_CompletesSynchronously_WhenAutoReleased()
        {
            var downloadDependencies = m_Addressables.DownloadDependenciesAsync((object)m_PrefabKey, true);
            downloadDependencies.WaitForCompletion();
            Assert.IsTrue(downloadDependencies.IsDone);
        }

#if ENABLE_CACHING && ENABLE_ASYNC_ASSETBUNDLE_UWR
        [Test]
        public void SyncAddressableLoad_CachedBundle_WithAutoLoadEnabled_Completes()
        {
            // Cache the bundle
            Caching.ClearCache();
            var initloadOp = m_Addressables.LoadAssetAsync<GameObject>(m_PrefabKey3);
            initloadOp.WaitForCompletion();
            ReleaseOp(initloadOp);

            var loadOp = m_Addressables.LoadAssetAsync<GameObject>(m_PrefabKey3);
            var result = loadOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp.Status);
            Assert.NotNull(result);
            Assert.IsNotNull(loadOp.Result);
            Assert.AreEqual(loadOp.Result, result);

            ReleaseOp(loadOp);
            Caching.ClearCache();
        }

        [Test]
        public void SyncAddressableLoad_NotCachedBundle_WithAutoLoadEnabled_Completes()
        {
            Caching.ClearCache();

            var loadOp = m_Addressables.LoadAssetAsync<GameObject>(m_PrefabKey3);
            var result = loadOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp.Status);
            Assert.NotNull(result);
            Assert.IsNotNull(loadOp.Result);
            Assert.AreEqual(loadOp.Result, result);

            ReleaseOp(loadOp);
            Caching.ClearCache();
        }

        [Test]
        public void SyncAddressableLoad_WhenMaxConcurrentRequestsIsOne_Succeeds()
        {
            int maxRequests = WebRequestQueue.s_MaxRequest;
            try
            {
                WebRequestQueue.SetMaxConcurrentRequests(1);
                Caching.ClearCache();

                var loadOp = m_Addressables.LoadAssetAsync<GameObject>(m_PrefabWithDep);
                Assert.DoesNotThrow(() => loadOp.WaitForCompletion());
                Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp.Status);

                ReleaseOp(loadOp);
            }
            finally
            {
                Caching.ClearCache();
                WebRequestQueue.SetMaxConcurrentRequests(maxRequests);
            }
        }

#endif
    }
}
