using System.IO;
using NUnit.Framework;
using UnityEngine.ResourceManagement;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
#endif

namespace AddressableTests.FastModeInitTests
{
#if UNITY_EDITOR
    public class FastModeInitializationTests : AddressablesTestFixture
    {
        protected override TestBuildScriptMode BuildScriptMode => TestBuildScriptMode.Fast;

        [TestCase(true)]
        [TestCase(false)]
        public void FastModeInit_EventViewerSetup_InitializesPostProfilerEventsValue(bool sendProfilerEvents)
        {
            //Setup
            bool originalValue = ProjectConfigData.PostProfilerEvents;
            ProjectConfigData.PostProfilerEvents = sendProfilerEvents;
            var settings = AddressableAssetSettings.Create(Path.Combine(GetGeneratedAssetsPath(), "Settings"), "AddressableAssetSettings.Tests", false, true);

            //Test
            FastModeInitializationOperation fmInit = new FastModeInitializationOperation(m_Addressables, settings);
            fmInit.InvokeExecute();

            //Assert
            Assert.AreEqual(sendProfilerEvents, m_Addressables.ResourceManager.postProfilerEvents);

            //Cleanup
            ProjectConfigData.PostProfilerEvents = originalValue;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void FastModeInit_EventViewerSetup_InitializesDiagnosticEventCollectorCorrectly(bool sendProfilerEvents)
        {
            //Setup
            bool originalValue = ProjectConfigData.PostProfilerEvents;
            ProjectConfigData.PostProfilerEvents = sendProfilerEvents;
            var settings = AddressableAssetSettings.Create(Path.Combine(GetGeneratedAssetsPath(), "Settings"), "AddressableAssetSettings.Tests", false, true);

            //Test
            FastModeInitializationOperation fmInit = new FastModeInitializationOperation(m_Addressables, settings);
            fmInit.InvokeExecute();

            //Assert
            if (sendProfilerEvents)
                Assert.IsNotNull(fmInit.m_Diagnostics, "Diagnostic event collector was null when send profiler events was set to true.");
            else
                Assert.IsNull(fmInit.m_Diagnostics, "Diagnostic event collector was not null when send profiler events was false.");

            //Cleanup
            ProjectConfigData.PostProfilerEvents = originalValue;
        }

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
    }
#endif
}
