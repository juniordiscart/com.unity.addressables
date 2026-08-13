using System.IO;
using NUnit.Framework;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.ResourceProviders;
#if UNITY_EDITOR
using System.Collections;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.TestTools;
#endif

namespace AddressableTests.FastModeInitTests
{
#if UNITY_EDITOR
    public class FastModeInitializationTests : AddressablesTestFixture
    {
        protected override TestBuildScriptMode BuildScriptMode => TestBuildScriptMode.Fast;

        [TestCase(true)]
        [TestCase(false)]
        public void FastModeInitialization_SetsExceptionHandlerToNull_WhenLogRuntimeExceptionsIsOff(bool logRuntimeExceptions)
        {
            //Setup
            var settings = AddressableAssetSettings.Create(Path.Combine(GetGeneratedAssetsPath(), "Settings"), "AddressableAssetSettings.Tests", false, true);
            settings.buildSettings.LogResourceManagerExceptions = logRuntimeExceptions;

            //Test
            FastModeInitializationOperation fmInit = new FastModeInitializationOperation(m_Addressables, settings);
            fmInit.InvokeExecute();

            //Assert
            Assert.AreEqual(logRuntimeExceptions, ResourceManager.ExceptionHandler != null);
        }

        public class FastModeInitializationTestsBuildScriptFastMode : BuildScriptFastMode
        {
        }

        [Test]
        [Ignore("Scriptable Object Compilation issue")]
        public void FastModeInitialization_GetBuilderOfType_ReturnsDirectAndSubclasses()
        {
            var settings = base.CreateSettings("AddressableAssetSettings.Tests", Path.Combine(GetGeneratedAssetsPath(), "Settings"));
            var db = FastModeInitializationOperation.GetBuilderOfType<BuildScriptFastMode>(settings, true);

            // default fast mode should be added on Validate of the settings object
            Assert.IsNotNull(db, "Failed to find the FastMode build script");
            Assert.AreEqual(db.GetType(), typeof(BuildScriptFastMode), "Fast mode build script expected to be BuildScriptFastMode type");

            Assert.IsTrue(settings.AddDataBuilder(settings.CreateScriptAsset<FastModeInitializationTestsBuildScriptFastMode>(), false), "Failed to Add custom buildScript FastMode");
            db = FastModeInitializationOperation.GetBuilderOfType<BuildScriptFastMode>(settings, true);
            Assert.IsNotNull(db, "Failed to find the FastMode build script");
            Assert.AreEqual(db.GetType(), typeof(FastModeInitializationTestsBuildScriptFastMode), "Fast mode build script expected to be FastModeInitializationTestsBuildScriptFastMode type");

            db = FastModeInitializationOperation.GetBuilderOfType<BuildScriptFastMode>(settings, false);
            Assert.IsNotNull(db, "Failed to find the FastMode build script");
            Assert.AreEqual(db.GetType(), typeof(BuildScriptFastMode), "Fast mode build script expected to be BuildScriptFastMode type, where requesting exact type and exists in the settings");
        }

        [UnityTest]
        public IEnumerator FastModeInitialization_AssetDatabaseProvider_simulatedLoadDelayCorrect()
        {
            var settings = base.CreateSettings("AddressableAssetSettings.Tests", Path.Combine(GetGeneratedAssetsPath(), "Settings_LoadDelay"));

            // Check 1 - initialization of provider should correctly set default value to mirror given serialized value in settings
            AssetDatabaseProvider defaultProvider = new AssetDatabaseProvider(settings.SimulatedLoadDelay);
            Assert.AreEqual(defaultProvider.GetLoadDelay(), settings.SimulatedLoadDelay);

            // Check 2 - Calling 'settings.SimulatedLoadDelay = x' should update both serialized value, and live value in provider
            Addressables.Instance.hasStartedInitialization = false;
            var handle = Addressables.Instance.InitializeAsync(); // Ensure ResourceManager is initialized
            yield return handle;

            settings.SimulatedLoadDelay = 0.5f;

            AssetDatabaseProvider assetDatabaseProvider = null;
            foreach (IResourceProvider provider in Addressables.Instance.ResourceManager.ResourceProviders)
            {
                if (provider is AssetDatabaseProvider)
                {
                    assetDatabaseProvider = (AssetDatabaseProvider)provider;
                    break;
                }
            }

            Assert.IsTrue(settings.SimulatedLoadDelay == 0.5f);

            // Only run this test if there is an AssetDatabaseProvider to test i.e. Use Asset Database (fastest) is enabled.
            if(assetDatabaseProvider != null)
                Assert.IsTrue(assetDatabaseProvider.GetLoadDelay() == 0.5f);
        }
    }
#endif
}
