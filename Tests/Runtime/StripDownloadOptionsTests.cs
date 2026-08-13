using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
#endif

namespace AddressableTests
{
    public class StripDownloadOptionsTests : AddressablesTestFixture
    {
        protected string m_PrefabKey = "key";

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
        internal override void Setup(AddressableAssetSettings settings, string tempAssetFolder)
        {
            AddressableAssetGroup syncGroup = settings.CreateGroup("StrippedOptions", false, false, true,
                new List<AddressableAssetGroupSchema>(), typeof(BundledAssetGroupSchema));
            syncGroup.GetSchema<BundledAssetGroupSchema>().StripDownloadOptions = true;

            //Create prefab
            string guid = CreatePrefab(tempAssetFolder + "/test.prefab");
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, syncGroup);
            entry.address = m_PrefabKey;
        }

#endif
        protected override TestBuildScriptMode BuildScriptMode
        {
            get { return TestBuildScriptMode.PackedPlaymode; }
        }

        [Test]
        public void CanLoadPrefabWithStrippedDownloadOptions()
        {
            var loadOp = m_Addressables.LoadAssetAsync<GameObject>(m_PrefabKey);
            var result = loadOp.WaitForCompletion();
            Assert.AreEqual(AsyncOperationStatus.Succeeded, loadOp.Status);
            Assert.NotNull(result);
            Assert.IsNotNull(loadOp.Result);
            Assert.AreEqual(loadOp.Result, result);

            loadOp.Release();
        }

    }
}
