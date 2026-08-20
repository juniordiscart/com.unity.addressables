using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System;
using System.IO;
using System.Linq;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.TestTools;
#endif

namespace AddressableAssetsIntegrationTests
{
    internal abstract partial class AddressablesIntegrationTests : IPrebuildSetup
    {
        internal protected AddressablesImpl m_Addressables;
        Dictionary<object, int> m_KeysHashSet = new Dictionary<object, int>();
        List<object> m_PrefabKeysList = new List<object>();

        Action<AsyncOperationHandle, Exception> m_PrevHandler;
        protected virtual bool UseJsonCatalog => false;
        protected string BuildSuffix => UseJsonCatalog ? "BASEJSON" : "BASE";
        protected string kCatalogExt => UseJsonCatalog ? ".json" : ".bin";
        protected const string k_TestConfigName = "AddressableAssetSettings.Tests";
        protected const string k_TestConfigFolder = "Assets/AddressableAssetsData_AddressableAssetSettingsIntegrationTests";

        /// <summary>
        /// The type of DataBuilder the test fixture should use for setup (BuildScriptPackedMode, BuildScriptPackedPlayMode, BuildScriptFastMode, etc.)
        /// </summary>
        protected abstract string TypeName { get; }

        protected virtual string PathFormat
        {
            get { return "Assets/{0}_AssetsToDelete_{1}"; }
        }

        protected virtual string GetRuntimePath(string testType, string suffix)
        {
            return string.Format("{0}" + Addressables.LibraryPath + "settings_{1}_TEST_{2}.json", "file://{UnityEngine.Application.dataPath}/../", testType, suffix);
        }

        protected virtual ILocationSizeData CreateLocationSizeData(string name, long size, uint crc, string hash)
        {
            return null;
        }

        private object AssetReferenceObjectKey
        {
            get { return m_PrefabKeysList.FirstOrDefault(s => s.ToString().Contains("AssetReferenceBehavior")); }
        }

        public virtual void Setup()
        {
            AddressablesTestUtility.Setup(TypeName, PathFormat, BuildSuffix, false, UseJsonCatalog);
        }

        [OneTimeTearDown]
        public virtual void DeleteTempFiles()
        {
            m_Addressables?.Dispose();
            m_Addressables = null;

            ResourceManager.ExceptionHandler = m_PrevHandler;
            AddressablesTestUtility.TearDown(TypeName, PathFormat, BuildSuffix);
        }

        // Deletes the persistent catalog cache folder so a catalog cached by a prior
        // test or session cannot be served instead of the catalog under test.
        // The cache lives at {Application.persistentDataPath}/com.unity.addressables/.
        static void ClearCatalogCache()
        {
            string cacheFolder = AddressablesImpl.ResolveInternalId(AddressablesImpl.kCacheDataFolder);
            if (Directory.Exists(cacheFolder))
                Directory.Delete(cacheFolder, true);
        }

        [SetUp]
        public void ClearCatalogCacheBeforeTest()
        {
            ClearCatalogCache();
        }

        int m_StartingOpCount;
        int m_StartingTrackedHandleCount;
        int m_StartingInstanceCount;

        private Action PostTearDownEvent = null;

        [TearDown]
        public void TearDown()
        {
            AssetBundleProvider.WaitForAllUnloadingBundlesToComplete();
            if (m_Addressables != null)
            {
                m_Addressables.ResourceManager.Update(0f);

                Assert.AreEqual(0, m_Addressables.ResourceManager.DeferredCompleteCallbacksCount);
                Assert.AreEqual(0, m_Addressables.ResourceManager.DeferredCallbackCount);

                Assert.AreEqual(m_StartingOpCount, m_Addressables.ResourceManager.OperationCacheCount);
                Assert.AreEqual(m_StartingTrackedHandleCount, m_Addressables.TrackedHandleCount,
                    $"Starting tracked handle count [{m_StartingInstanceCount}], not equal to current tracked handle count [{m_Addressables.TrackedHandleCount}]");
                Assert.AreEqual(m_StartingInstanceCount, m_Addressables.ResourceManager.InstanceOperationCount);

                //If we had left deferred callbacks left then make sure to update
                if (m_Addressables.ResourceManager.DeferredCallbackCount > 0
                    || m_Addressables.ResourceManager.DeferredCompleteCallbacksCount > 0)
                {
                    m_Addressables.ResourceManager.Update(Time.unscaledDeltaTime);
                }
            }

            PostTearDownEvent?.Invoke();
            PostTearDownEvent = null;

            ClearCatalogCache();
        }

        //we must wait for Addressables initialization to complete since we are clearing out all of its data for the tests.
        public bool initializationComplete;
        string currentInitType = null;

        string m_RuntimeSettingsPath
        {
            get
            {
                var runtimeSettingsPath = AddressablesImpl.RuntimePath + $"/settings{BuildSuffix}.json";
#if UNITY_EDITOR

                runtimeSettingsPath = GetRuntimePath(currentInitType, BuildSuffix);
#endif
                runtimeSettingsPath = AddressablesImpl.ResolveInternalId(runtimeSettingsPath);
                return runtimeSettingsPath;
            }
        }

        IEnumerator Init()
        {
            if (!initializationComplete || TypeName != currentInitType)
            {
                if (m_Addressables == null)
                    m_Addressables = new AddressablesImpl(new DefaultAllocationStrategy());

                if (TypeName != currentInitType)
                {
                    currentInitType = TypeName;
                    yield return m_Addressables.InitializeAsync(m_RuntimeSettingsPath, "BASE", false);

                    foreach (var locator in m_Addressables.ResourceLocators)
                    {
                        if (locator.Keys == null)
                            continue;

                        foreach (var key in locator.Keys)
                        {
                            IList<IResourceLocation> locs;
                            if (locator.Locate(key, typeof(object), out locs))
                            {
                                var isPrefab = locs.All(s => s.InternalId.EndsWith(".prefab"));
                                if (!m_KeysHashSet.ContainsKey(key))
                                {
                                    if (isPrefab)
                                        m_PrefabKeysList.Add(key);
                                    m_KeysHashSet.Add(key, locs.Count);
                                }
                                else
                                {
                                    m_KeysHashSet[key] = m_KeysHashSet[key] + locs.Count;
                                }
                            }
                        }
                    }

                    initializationComplete = true;

                    m_PrevHandler = ResourceManager.ExceptionHandler;
                    ResourceManager.ExceptionHandler = null;
                }
            }

            m_StartingOpCount = m_Addressables.ResourceManager.OperationCacheCount;
            m_StartingTrackedHandleCount = m_Addressables.TrackedHandleCount;
            m_StartingInstanceCount = m_Addressables.ResourceManager.InstanceOperationCount;
        }

        IEnumerator InitWithoutInitializeAsync()
        {
            if (!initializationComplete || TypeName != currentInitType)
            {
                if (m_Addressables == null)
                    m_Addressables = new AddressablesImpl(new DefaultAllocationStrategy());

                currentInitType = TypeName;

                yield return this;

                for (int i = 0; i < 3; i++)
                {
                    var locator = new DynamicResourceLocator(m_Addressables);
                    m_Addressables.AddResourceLocator(locator);
                }

                initializationComplete = true;

                m_PrevHandler = ResourceManager.ExceptionHandler;
                ResourceManager.ExceptionHandler = null;
            }

            m_StartingOpCount = m_Addressables.ResourceManager.OperationCacheCount;
            m_StartingTrackedHandleCount = m_Addressables.TrackedHandleCount;
            m_StartingInstanceCount = m_Addressables.ResourceManager.InstanceOperationCount;
        }

        private void ResetAddressables()
        {
            m_Addressables?.Dispose();
            m_Addressables = null;
            currentInitType = null;
            initializationComplete = false;
        }

        internal class DumbUpdateOperation : AsyncOperationBase<List<IResourceLocator>>
        {
            protected override void Execute()
            {
            }

            public void CallComplete()
            {
                Complete(new List<IResourceLocator>(), true, string.Empty);
            }
        }
    }

#if UNITY_EDITOR
    class AddressablesIntegrationTestsFastMode : AddressablesIntegrationTests
    {
        // Fast mode doesn't do any async loading
        protected override int ExpectedOpCount { get { return 0; } }


        protected override string TypeName
        {
            get { return "BuildScriptFastMode"; }
        }

        protected override string GetRuntimePath(string testType, string suffix)
        {
#if UNITY_EDITOR
            return SessionState.GetString(Addressables.kAddressablesRuntimeDataPath + TypeName + "_" + BuildSuffix, "");
#else
            Assert.Fail("FastMode is editor only");
            return null;
#endif
        }
    }

    class AddressablesIntegrationTestsFastModeJson : AddressablesIntegrationTestsFastMode
    {
        protected override bool UseJsonCatalog => true;
    }

    abstract class AddressablesIntegrationTestsPackedPlayMode : AddressablesIntegrationTests
    {
        protected override string TypeName
        {
            get { return "BuildScriptPackedPlayMode"; }
        }

        protected override string GetRuntimePath(string testType, string suffix)
        {
            return "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/settings" + suffix + ".json";
        }

        public override void Setup()
        {
            AddressablesTestUtility.Setup(PackedBundleDataBuilderTypeName, PathFormat, BuildSuffix, UseUnityWebRequestForLocalBundles, UseJsonCatalog);
            AddressablesTestUtility.Setup(TypeName, PathFormat, BuildSuffix, UseUnityWebRequestForLocalBundles, UseJsonCatalog);
        }

        public override void DeleteTempFiles()
        {
            AddressablesTestUtility.TearDown(PackedBundleDataBuilderTypeName, PathFormat, BuildSuffix);
            AddressablesTestUtility.TearDown(TypeName, PathFormat, BuildSuffix);
        }

        [UnityTest]
        public IEnumerator GetDownloadSize_CalculatesCachedBundles()
        {
            return GetDownloadSize_CalculatesCachedBundlesInternal();
        }

#if !UNITY_PS5
        [UnityTest]
        public IEnumerator GetDownloadSize_WithList_CalculatesCachedBundles()
        {
            return GetDownloadSize_WithList_CalculatesCachedBundlesInternal();
        }
#endif

        [UnityTest]
        public IEnumerator GetDownloadSize_WithList_CalculatesCorrectSize_WhenAssetsReferenceSameBundle()
        {
            return GetDownloadSize_WithList_CalculatesCorrectSize_WhenAssetsReferenceSameBundleInternal();
        }
    }

    abstract class AddressablesIntegrationTestsAllHooksPackedPlayMode : AddressablesIntegrationTestsPackedPlayMode
    {
        protected override string PackedBundleDataBuilderTypeName =>
            typeof(UnityEditor.AddressableAssets.Tests.AllHooksLoggingPackedMode).Name;
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneWindows64)]
    class AddressablesIntegrationTestsAllHooksPackedPlayModeWindowsUseUwr : AddressablesIntegrationTestsAllHooksPackedPlayMode
    {
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneOSX)]
    class AddressablesIntegrationTestsAllHooksPackedPlayModeOSXUseUwr : AddressablesIntegrationTestsAllHooksPackedPlayMode
    {
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneLinux64)]
    class AddressablesIntegrationTestsAllHooksPackedPlayModeLinuxUseUwr : AddressablesIntegrationTestsAllHooksPackedPlayMode
    {
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneWindows64)]
    class AddressablesIntegrationTestsPlayerWindowsUseUwr : AddressablesIntegrationPlayer
    {
        // using UWR should just download and not load the asset bundles

        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneOSX)]
    class AddressablesIntegrationTestsPlayerOSXUseUwr : AddressablesIntegrationPlayer
    {
        // using UWR should just download and not load the asset bundles

        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneLinux64)]
    class AddressablesIntegrationTestsPlayerLinuxUseUwr : AddressablesIntegrationPlayer
    {
        // using UWR should just download and not load the asset bundles

        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    // Player JSON twins
    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneWindows64)]
    class AddressablesIntegrationTestsPlayerWindowsUseUwrJson : AddressablesIntegrationPlayer
    {
        protected override bool UseJsonCatalog => true;
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }
    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneOSX)]
    class AddressablesIntegrationTestsPlayerOSXUseUwrJson : AddressablesIntegrationPlayer
    {
        protected override bool UseJsonCatalog => true;
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }
    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneLinux64)]
    class AddressablesIntegrationTestsPlayerLinuxUseUwrJson : AddressablesIntegrationPlayer
    {
        protected override bool UseJsonCatalog => true;
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneWindows64)]
    class AddressablesIntegrationTestsPackedPlayModeWindowsUseUwr : AddressablesIntegrationTestsPackedPlayMode
    {
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneOSX)]
    class AddressablesIntegrationTestsPackedPlayModeOSXUseUwr : AddressablesIntegrationTestsPackedPlayMode
    {
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneLinux64)]
    class AddressablesIntegrationTestsPackedPlayModeLinuxUseUwr : AddressablesIntegrationTestsPackedPlayMode
    {
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    abstract class AddressablesIntegrationTestsPackedPlayModeJson : AddressablesIntegrationTestsPackedPlayMode
    {
        protected override bool UseJsonCatalog => true;
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneWindows64)]
    class AddressablesIntegrationTestsPackedPlayModeWindowsUseUwrJson : AddressablesIntegrationTestsPackedPlayModeJson
    {
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneOSX)]
    class AddressablesIntegrationTestsPackedPlayModeOSXUseUwrJson : AddressablesIntegrationTestsPackedPlayModeJson
    {
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneLinux64)]
    class AddressablesIntegrationTestsPackedPlayModeLinuxUseUwrJson : AddressablesIntegrationTestsPackedPlayModeJson
    {
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }
#endif

    abstract class AddressablesIntegrationPlayer : AddressablesIntegrationTests
    {
        protected override string TypeName
        {
            get { return "BuildScriptPackedPlayMode"; }
        }

        public override void Setup()
        {
            AddressablesTestUtility.Setup(PackedBundleDataBuilderTypeName, PathFormat, BuildSuffix, UseUnityWebRequestForLocalBundles, UseJsonCatalog);
            AddressablesTestUtility.Setup(TypeName, PathFormat, BuildSuffix, UseUnityWebRequestForLocalBundles, UseJsonCatalog);
        }

        public override void DeleteTempFiles()
        {
            AddressablesTestUtility.TearDown(PackedBundleDataBuilderTypeName, PathFormat, BuildSuffix);
            AddressablesTestUtility.TearDown(TypeName, PathFormat, BuildSuffix);
        }

        protected override string GetRuntimePath(string testType, string suffix)
        {
            return "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/settings" + suffix + ".json";
        }

        protected override ILocationSizeData CreateLocationSizeData(string name, long size, uint crc, string hash)
        {
            return new AssetBundleRequestOptions()
            {
                BundleName = name,
                BundleSize = size,
                Crc = crc,
                Hash = hash
            };
        }

        [UnityTest]
        public IEnumerator GetDownloadSize_CalculatesCachedBundles()
        {
            return GetDownloadSize_CalculatesCachedBundlesInternal();
        }

#if !UNITY_PS5
        [UnityTest]
        public IEnumerator GetDownloadSize_WithList_CalculatesCachedBundles()
        {
            return GetDownloadSize_WithList_CalculatesCachedBundlesInternal();
        }
#endif

        [UnityTest]
        public IEnumerator GetDownloadSize_WithList_CalculatesCorrectSize_WhenAssetsReferenceSameBundle()
        {
            return GetDownloadSize_WithList_CalculatesCorrectSize_WhenAssetsReferenceSameBundleInternal();
        }
    }

#if UNITY_EDITOR
    abstract class AddressablesIntegrationTestsAllHooksPlayer : AddressablesIntegrationPlayer
    {
        protected override string PackedBundleDataBuilderTypeName =>
            typeof(UnityEditor.AddressableAssets.Tests.AllHooksLoggingPackedMode).Name;
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneWindows64)]
    class AddressablesIntegrationTestsAllHooksPlayerWindowsUseUwr : AddressablesIntegrationTestsAllHooksPlayer
    {
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneOSX)]
    class AddressablesIntegrationTestsAllHooksPlayerOSXUseUwr : AddressablesIntegrationTestsAllHooksPlayer
    {
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneLinux64)]
    class AddressablesIntegrationTestsAllHooksPlayerLinuxUseUwr : AddressablesIntegrationTestsAllHooksPlayer
    {
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    // --- JSON catalog twins ---

    abstract class AddressablesIntegrationTestsAllHooksPackedPlayModeJson : AddressablesIntegrationTestsAllHooksPackedPlayMode
    {
        protected override bool UseJsonCatalog => true;
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneWindows64)]
    class AddressablesIntegrationTestsAllHooksPackedPlayModeWindowsUseUwrJson : AddressablesIntegrationTestsAllHooksPackedPlayModeJson
    {
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneOSX)]
    class AddressablesIntegrationTestsAllHooksPackedPlayModeOSXUseUwrJson : AddressablesIntegrationTestsAllHooksPackedPlayModeJson
    {
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneLinux64)]
    class AddressablesIntegrationTestsAllHooksPackedPlayModeLinuxUseUwrJson : AddressablesIntegrationTestsAllHooksPackedPlayModeJson
    {
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    abstract class AddressablesIntegrationTestsAllHooksPlayerJson : AddressablesIntegrationTestsAllHooksPlayer
    {
        protected override bool UseJsonCatalog => true;
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneWindows64)]
    class AddressablesIntegrationTestsAllHooksPlayerWindowsUseUwrJson : AddressablesIntegrationTestsAllHooksPlayerJson
    {
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneOSX)]
    class AddressablesIntegrationTestsAllHooksPlayerOSXUseUwrJson : AddressablesIntegrationTestsAllHooksPlayerJson
    {
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }

    [RequirePlatformSupport(UnityEditor.BuildTarget.StandaloneLinux64)]
    class AddressablesIntegrationTestsAllHooksPlayerLinuxUseUwrJson : AddressablesIntegrationTestsAllHooksPlayerJson
    {
        protected override bool UseUnityWebRequestForLocalBundles { get { return true; } }
    }
#endif
}

