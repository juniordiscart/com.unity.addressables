using AddressableAssetsIntegrationTests;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Graphs;
using UnityEditor.TestTools;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.TestTools;

namespace UnityEditor.AddressableAssets.Tests
{
    public abstract class ContentUpdateTests : AddressableAssetTestBase
	{
        const string k_AssetBundleProviderId = "UnityEngine.ResourceManagement.ResourceProviders.AssetBundleProvider";

        protected override bool PersistSettings
        {
            get { return true; }
        }

        class ScopedContentState : IDisposable
		{
            AddressableAssetSettings m_Settings;
            AddressableAssetGroup m_AddedGroup;
            public string ContentStatePath { get; private set; }
            public string TypeTreeDataPath { get; private set; }
            public ScopedContentState(AddressableAssetSettings settings, string assetGUIDToAdd)
            {
                m_Settings = settings;
                m_AddedGroup = m_Settings.CreateGroup("LocalStuff", false, false, false, null);
                var schema = m_AddedGroup.AddSchema<BundledAssetGroupSchema>();
                schema.BuildPath.SetVariableByName(m_Settings, AddressableAssetSettings.kLocalBuildPath);
                schema.LoadPath.SetVariableByName(m_Settings, AddressableAssetSettings.kLocalLoadPath);
			schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                m_AddedGroup.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;

                m_Settings.CreateOrMoveEntry(assetGUIDToAdd, m_AddedGroup);
                var context = new AddressablesDataBuilderInput(m_Settings);

                var op = m_Settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(context);

			Assert.IsTrue(string.IsNullOrEmpty(op.Error), op.Error);
                ContentStatePath = Path.GetDirectoryName(Application.dataPath) + "/" + Addressables.LibraryPath + PlatformMappingService.GetPlatformPathSubFolder() + "/addressables_content_state.bin";
                TypeTreeDataPath = Path.GetDirectoryName(Application.dataPath) + "/" + Addressables.LibraryPath + "/aa/" + PlatformMappingService.GetPlatformPathSubFolder() + "/" + BuildScriptPackedMode.kTypeTreeDataFileName;
            }

            public void Dispose()
            {
                m_Settings.RemoveGroup(m_AddedGroup);
            }
        }

        [Test]
        public void CanCreateContentStateData()
        {
            using (var contentState = new ScopedContentState(Settings, m_AssetGUID))
            {
                var cacheData = ContentUpdateScript.LoadContentState(contentState.ContentStatePath);
			Assert.NotNull(cacheData);
            }
        }
#if UNITY_6000_5_OR_NEWER

        [Test]
        public void ContentUpdateState_WithTTExtractionEnabled_ContainsExpectedTypeTreeHashes()
        {
            var prevTTSetting = Settings.ExtractTypeTreeData;
            Settings.ExtractTypeTreeData = true;
            using (var contentState = new ScopedContentState(Settings, m_AssetGUID))
            {
                var cacheData = ContentUpdateScript.LoadContentState(contentState.ContentStatePath);
                Assert.NotNull(cacheData);
                Assert.NotNull(cacheData.typeTreeHashes);
                Assert.Greater(cacheData.typeTreeHashes.Length, 0);
                Assert.IsTrue(File.Exists(contentState.TypeTreeDataPath));
                var hashes = ContentBuildInterface.LoadTypeTreeDataHashesFromFile(contentState.TypeTreeDataPath);
                Assert.AreEqual(hashes.Length, cacheData.typeTreeHashes.Length);
                for(int i = 0;  i < hashes.Length; i++)
                    Assert.AreEqual(hashes[i], cacheData.typeTreeHashes[i]);
            }
            Settings.ExtractTypeTreeData = prevTTSetting;
        }

        [Test]
        public void ContentUpdateState_WithTTExtractionDisabled_DoesntContainTypeTreeHashes()
        {
            var prevTTSetting = Settings.ExtractTypeTreeData;
            Settings.ExtractTypeTreeData = false;
            using (var contentState = new ScopedContentState(Settings, m_AssetGUID))
            {
                var cacheData = ContentUpdateScript.LoadContentState(contentState.ContentStatePath);
                Assert.NotNull(cacheData);
                Assert.IsTrue(cacheData.typeTreeHashes == null || cacheData.typeTreeHashes.Length == 0);
                Assert.IsFalse(File.Exists(contentState.TypeTreeDataPath));
            }
            Settings.ExtractTypeTreeData = prevTTSetting;
		}

        [Test]
        public void ContentUpdateState_StrippingTypeTreeHashes_ProducesExpectedFile()
        {
            var prevTTSetting = Settings.ExtractTypeTreeData;
            Settings.ExtractTypeTreeData = true;
            using (var contentState = new ScopedContentState(Settings, m_AssetGUID))
            {
                var hashes = ContentBuildInterface.LoadTypeTreeDataHashesFromFile(contentState.TypeTreeDataPath);
                Assert.NotNull(hashes);
                Assert.Greater(hashes.Length, 1);

                var hashesToStrip = new Hash128[] { hashes[0] }; //strip the first one
                var tempPath = Path.GetTempFileName();
                ContentBuildInterface.StripTypeTreeDataFromFile(hashesToStrip, contentState.TypeTreeDataPath, tempPath);
                var remainingHashes = ContentBuildInterface.LoadTypeTreeDataHashesFromFile(tempPath);
                Assert.NotNull(remainingHashes);
                Assert.AreEqual(remainingHashes.Length, hashes.Length - 1);
            }
            Settings.ExtractTypeTreeData = prevTTSetting;
        }
#endif
		[Test]
		public void ContentState_WithDisabledGroups_DoesNotInclude_EntriesFromGroup()
		{
			var group = Settings.CreateGroup("RemoteStuff", false, false, false, null);
			var schema = group.AddSchema<BundledAssetGroupSchema>();
			schema.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kRemoteBuildPath);
			schema.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kRemoteLoadPath);
			schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
			group.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;

			Settings.CreateOrMoveEntry(m_AssetGUID, group);

			var group2 = Settings.CreateGroup("LocalStuff", false, false, false, null);
			var schema2 = group2.AddSchema<BundledAssetGroupSchema>();
			schema2.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kRemoteBuildPath);
			schema2.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kRemoteLoadPath);
			schema2.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
			group2.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;
			Settings.CreateOrMoveEntry(m_SceneGuids[0], group2);


			var context = new AddressablesDataBuilderInput(Settings);

			var op = Settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(context);

			Assert.IsTrue(string.IsNullOrEmpty(op.Error), op.Error);
			var tempPath = Path.GetDirectoryName(Application.dataPath) + "/" + Addressables.LibraryPath + PlatformMappingService.GetPlatformPathSubFolder() + "/addressables_content_state.bin";
			var cacheData = ContentUpdateScript.LoadContentState(tempPath);
			Assert.NotNull(cacheData);
			Assert.NotNull(cacheData.cachedInfos.FirstOrDefault(s => s.asset.guid.ToString() == m_AssetGUID));

			schema.IncludeInBuild = false;
			context = new AddressablesDataBuilderInput(Settings);
			op = Settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(context);
			Assert.IsTrue(string.IsNullOrEmpty(op.Error), op.Error);
			tempPath = Path.GetDirectoryName(Application.dataPath) + "/" + Addressables.LibraryPath + PlatformMappingService.GetPlatformPathSubFolder() + "/addressables_content_state.bin";
			cacheData = ContentUpdateScript.LoadContentState(tempPath);
			Assert.NotNull(cacheData);
			Assert.IsNull(cacheData.cachedInfos.FirstOrDefault(s => s.asset.guid.ToString() == m_AssetGUID));

			Settings.RemoveGroup(group);
			Settings.RemoveGroup(group2);
		}

#if !ENABLE_JSON_CATALOG
		[Test]
        public void CreateCustomLocator_ReturnsLocatorWithUniqueId()
		{
			ContentCatalogData ccd = new ContentCatalogData();
			ccd.SetData(new List<ContentCatalogDataEntry>());
            var data = ccd.SerializeToByteArray();
            var newCCD = new ContentCatalogData(new BinaryStorageBuffer.Reader(data));
            IResourceLocator map = newCCD.CreateCustomLocator("test");
			Assert.AreEqual("test", map.LocatorId);
		}
#endif
		[Test]
		public void DownloadBinFileToTempLocation_DoesNotThrowError_WhenDownloadFails()
		{
			Assert.DoesNotThrow(() =>
			{
				var returnValue = ContentUpdateScript.DownloadBinFileToTempLocation("http://notarealurl.com/addressable_state.bin");
				Assert.AreEqual(ContentUpdateScript.PreviousContentStateFileCachePath, returnValue);
			});
		}

		[Test]
		public void PrepareContentUpdate()
		{
			var group = Settings.CreateGroup("LocalStuff2", false, false, false, null);
			var schema = group.AddSchema<BundledAssetGroupSchema>();
			schema.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalBuildPath);
			schema.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);
			schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
			group.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;

			var entry = Settings.CreateOrMoveEntry(m_AssetGUID, group);
			entry.address = "test";

			var context = new AddressablesDataBuilderInput(Settings);

			Settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(context);

			var path = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
			var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			obj.GetComponent<Transform>().SetPositionAndRotation(new Vector3(10, 10, 10), Quaternion.identity);
			PrefabUtility.SavePrefabAsset(obj);
			AssetDatabase.SaveAssets();
			var tempPath = Path.GetDirectoryName(Application.dataPath) + "/" + Addressables.LibraryPath + PlatformMappingService.GetPlatformPathSubFolder() + "/addressables_content_state.bin";
			var modifiedEntries = ContentUpdateScript.GatherModifiedEntries(Settings, tempPath);
			Assert.IsNotNull(modifiedEntries);
			Assert.GreaterOrEqual(modifiedEntries.Count, 1);
			ContentUpdateScript.CreateContentUpdateGroup(Settings, modifiedEntries, "Content Update");
			var contentGroup = Settings.FindGroup("Content Update");
			Assert.IsNotNull(contentGroup);
			var movedEntry = contentGroup.GetAssetEntry(m_AssetGUID);
			Assert.AreSame(movedEntry, entry);
			Settings.RemoveGroup(group);
		}

		[Test]
		public void GatherModifiedEntries_WhenDependencyBundleNameIsSame_DependencyIsNotFlaggedAsModified()
		{
			// Create assets
			GameObject mainObject = new GameObject("mainObject");
			Material mat = new Material(Shader.Find("Transparent/Diffuse"));
			mainObject.AddComponent<MeshRenderer>().material = mat;

			string mainAssetPath = GetAssetPath("mainObject.prefab");
			string staticAssetPath = GetAssetPath("staticObject.mat");

			AssetDatabase.CreateAsset(mat, staticAssetPath);
			PrefabUtility.SaveAsPrefabAsset(mainObject, mainAssetPath);
			AssetDatabase.SaveAssets();

			// Create addressables
			AddressableAssetGroup mainAssetGroup = Settings.CreateGroup("PrefabGroup", false, false, false, null);
			AddressableAssetGroup staticContentGroup = Settings.CreateGroup("MatGroup", false, false, false, null);

			var schema = mainAssetGroup.AddSchema<BundledAssetGroupSchema>();
			schema.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalBuildPath);
			schema.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);
			schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
			mainAssetGroup.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;

			schema = staticContentGroup.AddSchema<BundledAssetGroupSchema>();
			schema.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalBuildPath);
			schema.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);
			schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
			staticContentGroup.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;

			AddressableAssetEntry mainEntry = Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(mainAssetPath), mainAssetGroup);
			AddressableAssetEntry staticEntry = Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(staticAssetPath), staticContentGroup);

			// Build
			var context = new AddressablesDataBuilderInput(Settings);
			Settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(context);

			// Modify assets
			var mainAsset = AssetDatabase.LoadAssetAtPath<GameObject>(mainAssetPath);
			mainAsset.GetComponent<Transform>().SetPositionAndRotation(new Vector3(10, 10, 10), Quaternion.identity);
			PrefabUtility.SavePrefabAsset(mainAsset);
			AssetDatabase.SaveAssets();

			// Test
			var tempPath = Path.GetDirectoryName(Application.dataPath) + "/Library/com.unity.addressables/" + PlatformMappingService.GetPlatformPathSubFolder() + "/addressables_content_state.bin";
			var modifiedEntries = ContentUpdateScript.GatherModifiedEntries(Settings, tempPath);

			Assert.AreEqual(1, modifiedEntries.Count);
			Assert.AreSame(modifiedEntries[0], mainEntry);

			// Cleanup
			GameObject.DestroyImmediate(mainObject);

			Settings.RemoveGroup(mainAssetGroup);
			Settings.RemoveGroup(staticContentGroup);

			AssetDatabase.DeleteAsset(mainAssetPath);
			AssetDatabase.DeleteAsset(staticAssetPath);
		}

		[Test]
		public void GatherModifiedEntries_WhenDependencyBundleNameIsChanged_DependencyIsFlaggedAsModified()
		{
			// Create assets
			GameObject mainObject = new GameObject("mainObject");
			Material mat = new Material(Shader.Find("Transparent/Diffuse"));
			mainObject.AddComponent<MeshRenderer>().material = mat;

			string mainAssetPath = GetAssetPath("mainObject.prefab");
			string staticAssetPath = GetAssetPath("staticObject.mat");

			AssetDatabase.CreateAsset(mat, staticAssetPath);
			PrefabUtility.SaveAsPrefabAsset(mainObject, mainAssetPath);
			AssetDatabase.SaveAssets();

			// Create addressables
			AddressableAssetGroup mainAssetGroup = Settings.CreateGroup("PrefabGroup2", false, false, false, null);
			AddressableAssetGroup staticContentGroup = Settings.CreateGroup("MatGroup2", false, false, false, null);

			var schema = mainAssetGroup.AddSchema<BundledAssetGroupSchema>();
			schema.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalBuildPath);
			schema.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);
			schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
			mainAssetGroup.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;

			schema = staticContentGroup.AddSchema<BundledAssetGroupSchema>();
			schema.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalBuildPath);
			schema.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);
			schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
			staticContentGroup.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;

			AddressableAssetEntry mainEntry = Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(mainAssetPath), mainAssetGroup);
			AddressableAssetEntry staticEntry = Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(staticAssetPath), staticContentGroup);

			// Build
			var context = new AddressablesDataBuilderInput(Settings);
			Settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(context);

			// Modify assets
			var mainAsset = AssetDatabase.LoadAssetAtPath<GameObject>(mainAssetPath);
			mainAsset.GetComponent<Transform>().SetPositionAndRotation(new Vector3(10, 10, 10), Quaternion.identity);
			PrefabUtility.SavePrefabAsset(mainAsset);
			staticContentGroup.GetSchema<BundledAssetGroupSchema>().InternalBundleIdMode = BundledAssetGroupSchema.BundleInternalIdMode.GroupGuidProjectIdEntriesHash;
			AssetDatabase.SaveAssets();

			// Test
			var tempPath = Path.GetDirectoryName(Application.dataPath) + "/Library/com.unity.addressables/" + PlatformMappingService.GetPlatformPathSubFolder() + "/addressables_content_state.bin";
			var modifiedEntries = ContentUpdateScript.GatherModifiedEntries(Settings, tempPath);

			Assert.AreEqual(2, modifiedEntries.Count);
			Assert.IsTrue(modifiedEntries.Contains(staticEntry));

			// Cleanup
			GameObject.DestroyImmediate(mainObject);

			Settings.RemoveGroup(mainAssetGroup);
			Settings.RemoveGroup(staticContentGroup);

			AssetDatabase.DeleteAsset(mainAssetPath);
			AssetDatabase.DeleteAsset(staticAssetPath);
		}

		[Test]
		public void GetStaticContentDependenciesOfModifiedEntries_FlagsEntryDependencies_WithStaticContentEnabled()
		{
			var mainAssetGroup = Settings.CreateGroup("MainAssetGroup", false, false, false, null,
				typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));

			var staticContentGroup = Settings.CreateGroup("StaticContentGroup", false, false, false, null,
				typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));

			mainAssetGroup.GetSchema<ContentUpdateGroupSchema>().StaticContent = true;
			staticContentGroup.GetSchema<ContentUpdateGroupSchema>().StaticContent = true;

			GameObject mainObject = new GameObject("mainObject");
			Material mat = new Material(Shader.Find("Transparent/Diffuse"));
			mainObject.AddComponent<MeshRenderer>().material = mat;

			string mainAssetPath = GetAssetPath("mainObject.prefab");
			string staticAssetPath = GetAssetPath("staticObject.mat");

			AssetDatabase.CreateAsset(mat, staticAssetPath);
			PrefabUtility.SaveAsPrefabAsset(mainObject, mainAssetPath);
			AssetDatabase.SaveAssets();

			var mainEntry = Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(mainAssetPath), mainAssetGroup);
			var staticEntry = Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(staticAssetPath), staticContentGroup);

			List<AddressableAssetEntry> modifiedEntries = new List<AddressableAssetEntry>()
			{
				mainEntry
			};

			Dictionary<AddressableAssetEntry, List<AddressableAssetEntry>> staticDependencies = new Dictionary<AddressableAssetEntry, List<AddressableAssetEntry>>()
			{
				{mainEntry, new List<AddressableAssetEntry>()}
			};
			ContentUpdateScript.GetStaticContentDependenciesForEntries(Settings, ref staticDependencies, null);

			Assert.AreEqual(1, staticDependencies.Count);
			Assert.AreEqual(1, staticDependencies[mainEntry].Count);
			Assert.IsTrue(staticDependencies[mainEntry].Contains(staticEntry));

			//Cleanup
			GameObject.DestroyImmediate(mainObject);

			Settings.RemoveGroup(mainAssetGroup);
			Settings.RemoveGroup(staticContentGroup);

			AssetDatabase.DeleteAsset(mainAssetPath);
			AssetDatabase.DeleteAsset(staticAssetPath);
		}

		[Test]
		public void GetStaticContentDependenciesOfModifiedEntries_DoesNotFlagEntryDependencies_WithStaticContentDisabled()
		{
			var mainAssetGroup = Settings.CreateGroup("MainAssetGroup", false, false, false, null,
				typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));

			var dynamicContentGroup = Settings.CreateGroup("DynamicContentGroup", false, false, false, null,
				typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));

			mainAssetGroup.GetSchema<ContentUpdateGroupSchema>().StaticContent = true;
			dynamicContentGroup.GetSchema<ContentUpdateGroupSchema>().StaticContent = false;

			GameObject mainObject = new GameObject("mainObject");
			Material mat = new Material(Shader.Find("Transparent/Diffuse"));
			mainObject.AddComponent<MeshRenderer>().material = mat;

			string mainAssetPath = GetAssetPath("mainObject.prefab");
			string dynamicAssetPath = GetAssetPath("dynamicObject.mat");

			AssetDatabase.CreateAsset(mat, dynamicAssetPath);
			PrefabUtility.SaveAsPrefabAsset(mainObject, mainAssetPath);
			AssetDatabase.SaveAssets();

			var mainEntry = Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(mainAssetPath), mainAssetGroup);
			Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(dynamicAssetPath), dynamicContentGroup);

			List<AddressableAssetEntry> modifiedEntries = new List<AddressableAssetEntry>()
			{
				mainEntry
			};

			Dictionary<AddressableAssetEntry, List<AddressableAssetEntry>> staticDependencies = new Dictionary<AddressableAssetEntry, List<AddressableAssetEntry>>()
			{
				{mainEntry, new List<AddressableAssetEntry>()}
			};
			ContentUpdateScript.GetStaticContentDependenciesForEntries(Settings, ref staticDependencies, null);

			Assert.AreEqual(1, staticDependencies.Count);
			Assert.AreEqual(0, staticDependencies[mainEntry].Count);

			//Cleanup
			GameObject.DestroyImmediate(mainObject);

			Settings.RemoveGroup(mainAssetGroup);
			Settings.RemoveGroup(dynamicContentGroup);

			AssetDatabase.DeleteAsset(mainAssetPath);
			AssetDatabase.DeleteAsset(dynamicAssetPath);
		}

		[Test]
		public void GetStaticContentDependenciesOfModifiedEntries_WhenFolderHasModifiedImplicitAsset_DependenciesFlaggedAsModified()
		{
			var mainAssetGroup = Settings.CreateGroup("MainAssetGroup", false, false, false, null,
				typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));

			mainAssetGroup.GetSchema<ContentUpdateGroupSchema>().StaticContent = true;
			mainAssetGroup.GetSchema<BundledAssetGroupSchema>().InternalBundleIdMode = BundledAssetGroupSchema.BundleInternalIdMode.GroupGuidProjectIdEntriesHash;

			GameObject mainObject = new GameObject("mainObject");
			Material mat = new Material(Shader.Find("Transparent/Diffuse"));
			mainObject.AddComponent<MeshRenderer>().material = mat;

			string folderName = "ContentUpdateFolder";
			string folderGuid = AssetDatabase.CreateFolder(ConfigFolder, folderName);
			string folderPath = Path.Combine(ConfigFolder, folderName).Replace("\\", "/");

			string mainAssetPath = Path.Combine(folderPath, "mainAsset.prefab").Replace("\\", "/");
			string subAssetPath = Path.Combine(folderPath, "subAsset.mat").Replace("\\", "/");

			AssetDatabase.CreateAsset(mat, subAssetPath);
			PrefabUtility.SaveAsPrefabAsset(mainObject, mainAssetPath);
			AssetDatabase.SaveAssets();

			string mainAssetGuid = AssetDatabase.AssetPathToGUID(mainAssetPath);
			string subAssetGuid = AssetDatabase.AssetPathToGUID(subAssetPath);

			var folderEntry = Settings.CreateOrMoveEntry(folderGuid, mainAssetGroup);
			var entries = new List<AddressableAssetEntry>();
			folderEntry.GatherAllAssets(entries, false, false, true);
			AddressableAssetEntry mainAssetEntry = entries.Find(x => x.guid == mainAssetGuid);

			var staticDependencies = new Dictionary<AddressableAssetEntry, List<AddressableAssetEntry>>()
			{
				{mainAssetEntry, new List<AddressableAssetEntry>()}
			};

			string cachedBundleName = "cachedBundleName";
			var groupGuidToCacheBundleName = new Dictionary<string, string>()
			{
				{folderGuid, cachedBundleName},
				{mainAssetGuid, cachedBundleName},
				{subAssetGuid, cachedBundleName},
			};
			ContentUpdateScript.GetStaticContentDependenciesForEntries(Settings, ref staticDependencies, groupGuidToCacheBundleName);

			Assert.AreEqual(1, staticDependencies.Count);
			Assert.AreEqual(2, staticDependencies[mainAssetEntry].Count);

			//Cleanup
			GameObject.DestroyImmediate(mainObject);
			Settings.RemoveGroup(mainAssetGroup);
			AssetDatabase.DeleteAsset(folderPath);
		}

		static IResourceLocator GetLocatorFromCatalog(IEnumerable<string> paths)
		{
			foreach (var p in paths)
            {
                if (Path.GetFileNameWithoutExtension(p).EndsWith("catalog"))
#if ENABLE_JSON_CATALOG
                    return ContentCatalogData.LoadFromFile(p).CreateCustomLocator();
#else

                    return ContentCatalogData.LoadFromFile(p, true).CreateCustomLocator();
#endif
            }
			return null;
		}

#if ENABLE_JSON_CATALOG

		[Test]
		public void WhenContentUpdated_NewCatalogRetains_OldCatalogBundleLoadData()
		{
			var group = Settings.CreateGroup("LocalStuff3", false, false, false, null);
			Settings.BuildRemoteCatalog = true;
			Settings.RemoteCatalogBuildPath = new ProfileValueReference();
			Settings.RemoteCatalogBuildPath.SetVariableByName(Settings, AddressableAssetSettings.kRemoteBuildPath);
			Settings.RemoteCatalogLoadPath = new ProfileValueReference();
			Settings.RemoteCatalogLoadPath.SetVariableByName(Settings, AddressableAssetSettings.kRemoteLoadPath);
			var schema = group.AddSchema<BundledAssetGroupSchema>();
			schema.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalBuildPath);
			schema.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);
			schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
			schema.UseAssetBundleCrc = true;
			schema.UseAssetBundleCache = true;
			group.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;
			Settings.CreateOrMoveEntry(m_AssetGUID, group);
			var context = new AddressablesDataBuilderInput(Settings);

			var op = Settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(context);
			Assert.IsTrue(string.IsNullOrEmpty(op.Error), op.Error);
			var origLocator = GetLocatorFromCatalog(op.FileRegistry.GetFilePaths());
			Assert.NotNull(origLocator);

			var tempPath = Path.GetDirectoryName(Application.dataPath) + "/" + Addressables.LibraryPath + PlatformMappingService.GetPlatformPathSubFolder() + "/addressables_content_state.bin";
			var contentState = ContentUpdateScript.LoadContentState(tempPath);
			Assert.NotNull(contentState);
			Assert.NotNull(contentState.cachedBundles);
			Assert.AreEqual(1, contentState.cachedBundles.Length);
			var buildOp = ContentUpdateScript.BuildContentUpdate(Settings, tempPath);
			Assert.IsNotNull(buildOp);
			Assert.IsTrue(string.IsNullOrEmpty(buildOp.Error));

			var updatedLocator = GetLocatorFromCatalog(buildOp.FileRegistry.GetFilePaths());
			Assert.IsNotNull(updatedLocator);

			foreach (var k in updatedLocator.Keys)
			{
				if (origLocator.Locate(k, typeof(IAssetBundleResource), out var origLocs))
				{
					Assert.IsTrue(updatedLocator.Locate(k, typeof(IAssetBundleResource), out var updatedLocs));
					Assert.AreEqual(1, origLocs.Count);
					Assert.AreEqual(1, updatedLocs.Count);
					var oLoc = origLocs[0];
					var uLoc = updatedLocs[0];
					Assert.NotNull(oLoc.Data);
					Assert.NotNull(uLoc.Data);
					var oData = oLoc.Data as AssetBundleRequestOptions;
					var uData = uLoc.Data as AssetBundleRequestOptions;
					Assert.NotNull(oData);
					Assert.NotNull(uData);
					Assert.AreEqual(oData.Hash, uData.Hash);
					Assert.AreEqual(oData.Crc, uData.Crc);
				}
			}

			Settings.RemoveGroup(group);
		}
#endif

		[Test]
		public void IsCacheDataValid_WhenNoPreviousRemoteCatalogPath_ReturnsFalseWithError()
		{
			AddressablesContentState cacheData = new AddressablesContentState();
			cacheData.editorVersion = Application.unityVersion;
			Assert.IsFalse(ContentUpdateScript.IsCacheDataValid(Settings, cacheData));
			LogAssert.Expect(LogType.Error, new Regex("Previous build had 'Build Remote Catalog' disabled.*"));
		}

		[Test]
		public void IsCacheDataValid_WhenRemoteCatalogDisabled_ReturnsFalseWithError()
		{
			AddressablesContentState cacheData = new AddressablesContentState();
			cacheData.editorVersion = Application.unityVersion;
			cacheData.remoteCatalogLoadPath = "somePath";
			var oldSetting = Settings.BuildRemoteCatalog;
			Settings.BuildRemoteCatalog = false;
			Assert.IsFalse(ContentUpdateScript.IsCacheDataValid(Settings, cacheData));
			LogAssert.Expect(LogType.Error, new Regex("Current settings have 'Build Remote Catalog' disabled.*"));
			Settings.BuildRemoteCatalog = oldSetting;
		}

		[Test]
		public void IsCacheDataValid_WhenMismatchedCatalogPaths_ReturnsFalseWithError()
		{
			AddressablesContentState cacheData = new AddressablesContentState();
			cacheData.editorVersion = Application.unityVersion;
			cacheData.remoteCatalogLoadPath = "somePath";
			var oldSetting = Settings.BuildRemoteCatalog;
			Settings.BuildRemoteCatalog = true;
			Assert.IsFalse(ContentUpdateScript.IsCacheDataValid(Settings, cacheData));
			LogAssert.Expect(LogType.Error, new Regex("Current 'Remote Catalog Load Path' does not match load path of original player.*"));
			Settings.BuildRemoteCatalog = oldSetting;
		}

		[Test]
		public void IsCacheDataValid_WhenMismatchedEditorVersions_LogsWarning()
		{
			AddressablesContentState cacheData = new AddressablesContentState();
			cacheData.editorVersion = "invalid";
			Assert.IsFalse(ContentUpdateScript.IsCacheDataValid(Settings, cacheData));
			LogAssert.Expect(LogType.Warning, new Regex(".*with version `" + cacheData.editorVersion + "`.*"));
			LogAssert.Expect(LogType.Error, new Regex("Previous.*"));
		}

		[Test]
		[Ignore("Editor crash that's being fixed: https://jira.unity3d.com/browse/BPSBP-110")]
		public void BuildContentUpdate_DoesNotDeleteBuiltData()
		{
			var oldSetting = Settings.BuildRemoteCatalog;
			Settings.BuildRemoteCatalog = true;
			var group = Settings.CreateGroup("LocalStuff3", false, false, false, null);
			var schema = group.AddSchema<BundledAssetGroupSchema>();
			schema.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalBuildPath);
			schema.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);
			schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
			group.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;
			Settings.CreateOrMoveEntry(m_AssetGUID, group);
			var context = new AddressablesDataBuilderInput(Settings);

			var op = Settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(context);

			Assert.IsTrue(string.IsNullOrEmpty(op.Error), op.Error);
			var tempPath = Path.GetDirectoryName(Application.dataPath) + "/" + Addressables.LibraryPath + PlatformMappingService.GetPlatformPathSubFolder() + "/addressables_content_state.bin";
			ContentUpdateScript.BuildContentUpdate(Settings, tempPath);
			Assert.IsTrue(Directory.Exists(Addressables.BuildPath));
			Settings.BuildRemoteCatalog = oldSetting;
		}

		[Test]
		public void ContentUpdateScenes_PackedTogether_MarksAllScenesModified()
		{
			AddressableAssetGroup group = Settings.CreateGroup("SceneGroup", false, false, false, null, typeof(BundledAssetGroupSchema));
			group.GetSchema<BundledAssetGroupSchema>().BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;

			AddressableAssetEntry scene1 = new AddressableAssetEntry(m_SceneGuids[0], "scene1", group, false);
			AddressableAssetEntry scene2 = new AddressableAssetEntry(m_SceneGuids[1], "scene2", group, false);
			AddressableAssetEntry scene3 = new AddressableAssetEntry(m_SceneGuids[2], "scene3", group, false);

			group.AddAssetEntry(scene1, false);
			group.AddAssetEntry(scene2, false);
			group.AddAssetEntry(scene3, false);

			List<AddressableAssetEntry> modifedEnteries = new List<AddressableAssetEntry>()
			{
				scene1
			};

			ContentUpdateScript.AddAllDependentScenesFromModifiedEntries(modifedEnteries);

			Assert.AreEqual(3, modifedEnteries.Count);
			Assert.AreEqual(scene1, modifedEnteries[0]);
			Assert.AreEqual(scene2, modifedEnteries[1]);
			Assert.AreEqual(scene3, modifedEnteries[2]);

			Settings.RemoveGroup(group);
		}

		[Test]
		public void ContentUpdateScenes_PackedTogetherByLabel_MarksAllScenesModifiedWithSharedLabel()
		{
			AddressableAssetGroup group = Settings.CreateGroup("SceneGroup", false, false, false, null, typeof(BundledAssetGroupSchema));
			group.GetSchema<BundledAssetGroupSchema>().BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel;

			AddressableAssetEntry scene1 = new AddressableAssetEntry(m_SceneGuids[0], "scene1", group, false);
			AddressableAssetEntry scene2 = new AddressableAssetEntry(m_SceneGuids[1], "scene2", group, false);
			AddressableAssetEntry scene3 = new AddressableAssetEntry(m_SceneGuids[2], "scene3", group, false);

			group.AddAssetEntry(scene1, false);
			group.AddAssetEntry(scene2, false);
			group.AddAssetEntry(scene3, false);

			scene1.SetLabel("label", true, true);
			scene3.SetLabel("label", true, true);

			List<AddressableAssetEntry> modifedEnteries = new List<AddressableAssetEntry>()
			{
				scene1
			};

			ContentUpdateScript.AddAllDependentScenesFromModifiedEntries(modifedEnteries);

			Assert.AreEqual(2, modifedEnteries.Count);
			Assert.AreEqual(scene1, modifedEnteries[0]);
			Assert.AreEqual(scene3, modifedEnteries[1]);

			Settings.RemoveGroup(group);
		}

		[Test]
		public void ContentUpdateScenes_PackedSeperately_MarksNoAdditionalScenes()
		{
			AddressableAssetGroup group = Settings.CreateGroup("SceneGroup", false, false, false, null, typeof(BundledAssetGroupSchema));
			group.GetSchema<BundledAssetGroupSchema>().BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;

			AddressableAssetEntry scene1 = new AddressableAssetEntry(m_SceneGuids[0], "scene1", group, false);
			AddressableAssetEntry scene2 = new AddressableAssetEntry(m_SceneGuids[1], "scene2", group, false);
			AddressableAssetEntry scene3 = new AddressableAssetEntry(m_SceneGuids[2], "scene3", group, false);

			group.AddAssetEntry(scene1, false);
			group.AddAssetEntry(scene2, false);
			group.AddAssetEntry(scene3, false);

			List<AddressableAssetEntry> modifedEnteries = new List<AddressableAssetEntry>()
			{
				scene1
			};

			ContentUpdateScript.AddAllDependentScenesFromModifiedEntries(modifedEnteries);

			Assert.AreEqual(1, modifedEnteries.Count);
			Assert.AreEqual(scene1, modifedEnteries[0]);

			Settings.RemoveGroup(group);
		}

		private ContentUpdateScript.ContentUpdateContext GetContentUpdateContext(string contentUpdateTestAssetGUID, string contentUpdateTestCachedAssetHash,
			string contentUpdateTestNewInternalBundleName, string contentUpdateTestNewBundleName, string contentUpdateTestCachedBundlePath, string contentUpdateTestGroupGuid,
			string contentUpdateTestFileName)
		{
			var context = new ContentUpdateScript.ContentUpdateContext()
			{
				WriteData = new BundleWriteData(),
				BundleToInternalBundleIdMap = new Dictionary<string, string>(),
				GuidToPreviousAssetStateMap = new Dictionary<string, CachedAssetState>(),
				IdToCatalogDataEntryMap = new Dictionary<string, ContentCatalogDataEntry>(),
				ContentState = new AddressablesContentState(),
				PreviousAssetStateCarryOver = new List<CachedAssetState>(),
				Registry = new FileRegistry()
			};
			AddToContentUpdateContext(context, contentUpdateTestAssetGUID, contentUpdateTestCachedAssetHash,
				contentUpdateTestNewInternalBundleName, contentUpdateTestNewBundleName, contentUpdateTestCachedBundlePath, contentUpdateTestGroupGuid, contentUpdateTestFileName);
			return context;
		}

		private void AddToContentUpdateContext(ContentUpdateScript.ContentUpdateContext context, string contentUpdateTestAssetGUID, string contentUpdateTestCachedAssetHash,
			string contentUpdateTestNewInternalBundleName, string contentUpdateTestNewBundleName, string contentUpdateTestCachedBundlePath, string contentUpdateTestGroupGuid,
			string contentUpdateTestFileName)
		{
			context.BundleToInternalBundleIdMap.Add(contentUpdateTestNewInternalBundleName, contentUpdateTestNewBundleName);
			context.GuidToPreviousAssetStateMap.Add(contentUpdateTestAssetGUID,
				new CachedAssetState()
				{
					bundleFileId = contentUpdateTestCachedBundlePath,
					asset = new AssetState()
					{
						guid = new GUID(contentUpdateTestAssetGUID),
						hash = Hash128.Parse(contentUpdateTestCachedAssetHash)
					},
					dependencies = new AssetState[] { },
					data = null,
					groupGuid = contentUpdateTestGroupGuid
				}
			);

			context.IdToCatalogDataEntryMap.Add(contentUpdateTestNewBundleName,
				new ContentCatalogDataEntry(typeof(IAssetBundleResource), contentUpdateTestNewBundleName, typeof(AssetBundleProvider).FullName, new[] { contentUpdateTestNewBundleName }));
			context.IdToCatalogDataEntryMap.Add(contentUpdateTestAssetGUID,
				new ContentCatalogDataEntry(typeof(IAssetBundleResource), contentUpdateTestAssetGUID, typeof(AssetBundleProvider).FullName, new[] { contentUpdateTestAssetGUID }));

			context.WriteData.AssetToFiles.Add(new GUID(contentUpdateTestAssetGUID), new List<string>() { contentUpdateTestFileName });
			context.WriteData.FileToBundle.Add(contentUpdateTestFileName, contentUpdateTestNewInternalBundleName);
		}

		private AddressableAssetEntry CreateAssetEntry(string guid, AddressableAssetGroup group)
		{
			return new AddressableAssetEntry(guid, guid, group, false);
		}

		readonly string m_ContentUpdateTestAssetGUID = GUID.Generate().ToString();
		const string k_ContentUpdateTestCachedAssetHash = "8888888888888888888";
		const string k_ContentUpdateTestNewInternalBundleName = "bundle";
		const string k_ContentUpdateTestNewBundleName = "fullbundlepath";
		const string k_ContentUpdateTestCachedBundlePath = "cachedBundle";
		const string k_ContentUpdateTestFileName = "testfile";

		[Test]
		public void DetermineRequiredAssetEntryUpdates_AssetWithoutDependenciesAndChangedHashInStaticGroup_ReturnsOnlyRevertToCachedState()
		{
			File.WriteAllText(k_ContentUpdateTestCachedBundlePath, "TestCachedAssetBundle");
			File.WriteAllText(k_ContentUpdateTestNewBundleName, "TestNewBundle");

			var group = Settings.CreateGroup("ContentUpdateTests", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
			string contentUpdateTestGroupGuid = group.Guid;

			group.GetSchema<ContentUpdateGroupSchema>().StaticContent = true;
			var assetEntry = CreateAssetEntry(m_ContentUpdateTestAssetGUID, group);
			assetEntry.m_cachedAssetPath = "path";
			group.AddAssetEntry(assetEntry);

			var context = GetContentUpdateContext(m_ContentUpdateTestAssetGUID, k_ContentUpdateTestCachedAssetHash,
				k_ContentUpdateTestNewInternalBundleName, k_ContentUpdateTestNewBundleName,
				k_ContentUpdateTestCachedBundlePath, contentUpdateTestGroupGuid, k_ContentUpdateTestFileName);

			var ops = RevertUnchangedAssetsToPreviousAssetState.DetermineRequiredAssetEntryUpdates(group, context);

			Assert.AreEqual(assetEntry, ops[0].AssetEntry);
			Assert.AreEqual(k_ContentUpdateTestNewBundleName, ops[0].CurrentBuildPath);
			Assert.AreEqual(k_ContentUpdateTestCachedBundlePath, ops[0].PreviousBuildPath);

			File.Delete(k_ContentUpdateTestCachedBundlePath);
			File.Delete(k_ContentUpdateTestNewBundleName);
			Settings.RemoveGroup(group);
		}

		[Test]
		public void DetermineRequiredAssetEntryUpdates_AssetWithDependenciesAndChangedHashInStaticGroup_ReturnsRevertToCachedStateAndDependencies()
		{
			File.WriteAllText(k_ContentUpdateTestCachedBundlePath, "TestCachedAssetBundle");
			File.WriteAllText(k_ContentUpdateTestNewBundleName, "TestNewBundle");

			var group = Settings.CreateGroup("ContentUpdateTests", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
			string contentUpdateTestGroupGuid = group.Guid;

			group.GetSchema<ContentUpdateGroupSchema>().StaticContent = true;
			var assetEntry = CreateAssetEntry(m_ContentUpdateTestAssetGUID, group);
			assetEntry.m_cachedAssetPath = "path";
			group.AddAssetEntry(assetEntry);

			var context = GetContentUpdateContext(m_ContentUpdateTestAssetGUID, k_ContentUpdateTestCachedAssetHash,
				k_ContentUpdateTestNewInternalBundleName, k_ContentUpdateTestNewBundleName,
				k_ContentUpdateTestCachedBundlePath, contentUpdateTestGroupGuid, k_ContentUpdateTestFileName);
			context.GuidToPreviousAssetStateMap[m_ContentUpdateTestAssetGUID].dependencies = new AssetState[]
			{
				new AssetState()
				{
					guid = GUID.Generate(),
					hash = Hash128.Parse("00000000000000")
				}
			};

			var ops = RevertUnchangedAssetsToPreviousAssetState.DetermineRequiredAssetEntryUpdates(group, context);

			Assert.AreEqual(assetEntry, ops[0].AssetEntry);
			Assert.AreEqual(k_ContentUpdateTestNewBundleName, ops[0].CurrentBuildPath);
			Assert.AreEqual(k_ContentUpdateTestCachedBundlePath, ops[0].PreviousBuildPath);

			File.Delete(k_ContentUpdateTestCachedBundlePath);
			File.Delete(k_ContentUpdateTestNewBundleName);
			Settings.RemoveGroup(group);
		}

		[Test]
		public void DetermineRequiredAssetEntryUpdates_AssetWithChangedGroup_TakesNoAction()
		{
			File.WriteAllText(k_ContentUpdateTestCachedBundlePath, "TestCachedAssetBundle");

			var group = Settings.CreateGroup("ContentUpdateTests", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
			string contentUpdateTestGroupGuid = GUID.Generate().ToString();

			group.GetSchema<ContentUpdateGroupSchema>().StaticContent = true;
			var assetEntry = CreateAssetEntry(m_ContentUpdateTestAssetGUID, group);
			group.AddAssetEntry(assetEntry);

			var context = GetContentUpdateContext(m_ContentUpdateTestAssetGUID, k_ContentUpdateTestCachedAssetHash,
				k_ContentUpdateTestNewInternalBundleName, k_ContentUpdateTestNewBundleName,
				k_ContentUpdateTestCachedBundlePath, contentUpdateTestGroupGuid, k_ContentUpdateTestFileName);

			var ops = RevertUnchangedAssetsToPreviousAssetState.DetermineRequiredAssetEntryUpdates(group, context);

			Assert.IsTrue(ops.Count == 0);

			File.Delete(k_ContentUpdateTestCachedBundlePath);
			Settings.RemoveGroup(group);
		}

		[Test]
		public void DetermineRequiredAssetEntryUpdates_AssetWithChangedHashInNonStaticGroup_TakesNoAction()
		{
			File.WriteAllText(k_ContentUpdateTestCachedBundlePath, "TestCachedAssetBundle");

			var group = Settings.CreateGroup("ContentUpdateTests", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
			string contentUpdateTestGroupGuid = GUID.Generate().ToString();

			group.GetSchema<ContentUpdateGroupSchema>().StaticContent = false;
			var assetEntry = CreateAssetEntry(m_ContentUpdateTestAssetGUID, group);
			group.AddAssetEntry(assetEntry);

			var context = GetContentUpdateContext(m_ContentUpdateTestAssetGUID, k_ContentUpdateTestCachedAssetHash,
				k_ContentUpdateTestNewInternalBundleName, k_ContentUpdateTestNewBundleName,
				k_ContentUpdateTestCachedBundlePath, contentUpdateTestGroupGuid, k_ContentUpdateTestFileName);

			var ops = RevertUnchangedAssetsToPreviousAssetState.DetermineRequiredAssetEntryUpdates(group, context);

			Assert.IsTrue(ops.Count == 0);

			File.Delete(k_ContentUpdateTestCachedBundlePath);
			Settings.RemoveGroup(group);
		}

		[Test]
		public void DetermineRequiredAssetEntryUpdates_EntriesThatTakeNoAction_StillSetBundleFileId()
		{
			File.WriteAllText(k_ContentUpdateTestCachedBundlePath, "TestCachedAssetBundle");

			var group = Settings.CreateGroup("ContentUpdateTests", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
			string contentUpdateTestGroupGuid = GUID.Generate().ToString();

			group.GetSchema<ContentUpdateGroupSchema>().StaticContent = false;
			var assetEntry = CreateAssetEntry(m_ContentUpdateTestAssetGUID, group);
			assetEntry.m_cachedAssetPath = "path";
			group.AddAssetEntry(assetEntry);

			var context = GetContentUpdateContext(m_ContentUpdateTestAssetGUID, k_ContentUpdateTestCachedAssetHash,
				k_ContentUpdateTestNewInternalBundleName, k_ContentUpdateTestNewBundleName,
				k_ContentUpdateTestCachedBundlePath, contentUpdateTestGroupGuid, k_ContentUpdateTestFileName);

			var ops = RevertUnchangedAssetsToPreviousAssetState.DetermineRequiredAssetEntryUpdates(group, context);

			Assert.IsTrue(ops.Count == 0);
			Assert.AreEqual(k_ContentUpdateTestNewBundleName, assetEntry.BundleFileId);

			File.Delete(k_ContentUpdateTestCachedBundlePath);
			Settings.RemoveGroup(group);
		}

		[Test]
		public void DetermineRequiredAssetEntryUpdates_WithMissingBundleFileId_LogsErrorAndTakesNoAction()
		{
			File.WriteAllText(k_ContentUpdateTestCachedBundlePath, "TestCachedAssetBundle");

			var group = Settings.CreateGroup("ContentUpdateTests", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
			string contentUpdateTestGroupGuid = group.Guid;

			group.GetSchema<ContentUpdateGroupSchema>().StaticContent = true;
			var assetEntry = CreateAssetEntry(m_ContentUpdateTestAssetGUID, group);
			assetEntry.m_cachedAssetPath = "path";
			group.AddAssetEntry(assetEntry);

			var context = GetContentUpdateContext(m_ContentUpdateTestAssetGUID, k_ContentUpdateTestCachedAssetHash,
				k_ContentUpdateTestNewInternalBundleName, k_ContentUpdateTestNewBundleName,
				"", contentUpdateTestGroupGuid, k_ContentUpdateTestFileName);

			var ops = RevertUnchangedAssetsToPreviousAssetState.DetermineRequiredAssetEntryUpdates(group, context);

			LogAssert.Expect(LogType.Error, $"CachedAssetState found for {assetEntry.AssetPath} but the bundleFileId was never set on the previous build.");
			Assert.IsTrue(ops.Count == 0);

			File.Delete(k_ContentUpdateTestCachedBundlePath);
			Settings.RemoveGroup(group);
		}

		[Test]
		public void DetermineRequiredAssetEntryUpdates_WithMissingPreviousBundle_LogsWarningAndReturnsRevertOperation()
		{
			var group = Settings.CreateGroup("ContentUpdateTests", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
			string contentUpdateTestGroupGuid = group.Guid;

			group.GetSchema<ContentUpdateGroupSchema>().StaticContent = true;
			var assetEntry = CreateAssetEntry(m_ContentUpdateTestAssetGUID, group);
			assetEntry.m_cachedAssetPath = "path";
			group.AddAssetEntry(assetEntry);

			var context = GetContentUpdateContext(m_ContentUpdateTestAssetGUID, k_ContentUpdateTestCachedAssetHash,
				k_ContentUpdateTestNewInternalBundleName, k_ContentUpdateTestNewBundleName,
				k_ContentUpdateTestCachedBundlePath, contentUpdateTestGroupGuid, k_ContentUpdateTestFileName);

			var ops = RevertUnchangedAssetsToPreviousAssetState.DetermineRequiredAssetEntryUpdates(group, context);
			LogAssert.Expect(LogType.Warning, $"CachedAssetState found for {assetEntry.AssetPath} but the previous bundle at {k_ContentUpdateTestCachedBundlePath} cannot be found. " +
				$"This will not affect loading the bundle in previously built players, but loading the missing bundle in Play Mode using the play mode script " +
                                              $"\"Use Existing Build (requires built groups)\" will fail. This most often occurs because you are running a content update on a build where you " +
                                              $"made changes to a group marked with \"Prevent Updates\"");
            Assert.IsTrue(ops.Count == 1);

            Settings.RemoveGroup(group);
        }

        [Test]
        public void DetermineRequiredAssetEntryUpdates_WithIdenticalBundleId_LogsWarningAndReturnsRevertOperation()
        {
            var group = Settings.CreateGroup("ContentUpdateTests", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            string contentUpdateTestGroupGuid = group.Guid;

            group.GetSchema<ContentUpdateGroupSchema>().StaticContent = true;
            var assetEntry = CreateAssetEntry(m_ContentUpdateTestAssetGUID, group);
            assetEntry.m_cachedAssetPath = "path";
            group.AddAssetEntry(assetEntry);

            var context = GetContentUpdateContext(m_ContentUpdateTestAssetGUID, k_ContentUpdateTestCachedAssetHash,
                k_ContentUpdateTestNewInternalBundleName, k_ContentUpdateTestNewBundleName,
                k_ContentUpdateTestNewBundleName, contentUpdateTestGroupGuid, k_ContentUpdateTestFileName);

            var ops = RevertUnchangedAssetsToPreviousAssetState.DetermineRequiredAssetEntryUpdates(group, context);
            LogAssert.Expect(LogType.Warning, $"CachedAssetState found for {assetEntry.AssetPath} but the previous bundle at {k_ContentUpdateTestNewBundleName} cannot be found. " +
                $"This will not affect loading the bundle in previously built players, but loading the missing bundle in Play Mode using the play mode script " +
                $"\"Use Existing Build (requires built groups)\" will fail. This most often occurs because you are running a content update on a build where you " +
                $"made changes to a group marked with \"Prevent Updates\"");
			Assert.IsTrue(ops.Count == 1);

			Settings.RemoveGroup(group);
		}

		[Test]
		public void DetermineRequiredAssetEntryUpdates_AssetWithMatchingCachedInternalBundleId_TakesNoAction()
		{
			File.WriteAllText(k_ContentUpdateTestCachedBundlePath, "TestCachedAssetBundle");

			var group = Settings.CreateGroup("ContentUpdateTests", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
			string contentUpdateTestGroupGuid = group.Guid;

			group.GetSchema<ContentUpdateGroupSchema>().StaticContent = true;
			var assetEntry = CreateAssetEntry(m_ContentUpdateTestAssetGUID, group);
			group.AddAssetEntry(assetEntry);

			var context = GetContentUpdateContext(m_ContentUpdateTestAssetGUID, k_ContentUpdateTestCachedAssetHash,
				k_ContentUpdateTestNewInternalBundleName, k_ContentUpdateTestNewBundleName,
				k_ContentUpdateTestNewBundleName, contentUpdateTestGroupGuid, k_ContentUpdateTestFileName);

			var ops = RevertUnchangedAssetsToPreviousAssetState.DetermineRequiredAssetEntryUpdates(group, context);

			Assert.IsTrue(ops.Count == 0);

			File.Delete(k_ContentUpdateTestCachedBundlePath);
			Settings.RemoveGroup(group);
		}

		[Test]
		public void ApplyAssetEntryUpdates_InvalidKeyForGuidToPreviousAssetStateMap_DoesNotThrow()
		{
			File.WriteAllText(k_ContentUpdateTestCachedBundlePath, "TestCachedAssetBundle");

			var group = Settings.CreateGroup("ContentUpdateTests", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
			string contentUpdateTestGroupGuid = GUID.Generate().ToString();

			group.GetSchema<ContentUpdateGroupSchema>().StaticContent = false;
			var assetEntry = CreateAssetEntry(m_ContentUpdateTestAssetGUID, group);
			group.AddAssetEntry(assetEntry);

			var context = GetContentUpdateContext(m_ContentUpdateTestAssetGUID, k_ContentUpdateTestCachedAssetHash,
				k_ContentUpdateTestNewInternalBundleName, k_ContentUpdateTestNewBundleName,
				k_ContentUpdateTestCachedBundlePath, contentUpdateTestGroupGuid, k_ContentUpdateTestFileName);
			context.GuidToPreviousAssetStateMap[m_ContentUpdateTestAssetGUID].dependencies = new AssetState[]
			{
				new AssetState()
				{
					guid = GUID.Generate(),
					hash = Hash128.Parse("9823749283742")
				}
			};

			var ops = new List<RevertUnchangedAssetsToPreviousAssetState.AssetEntryRevertOperation>()
			{
				new RevertUnchangedAssetsToPreviousAssetState.AssetEntryRevertOperation()
				{
					PreviousBuildPath = k_ContentUpdateTestCachedBundlePath,
					AssetEntry = assetEntry,
					BundleCatalogEntry = context.IdToCatalogDataEntryMap[m_ContentUpdateTestAssetGUID],
					CurrentBuildPath = k_ContentUpdateTestNewBundleName,
					PreviousAssetState = context.GuidToPreviousAssetStateMap[m_ContentUpdateTestAssetGUID]
				}
			};

			context.ContentState.cachedBundles = new CachedBundleState[] { new CachedBundleState() { bundleFileId = "cachedBundle", data = "string" } };

			Assert.DoesNotThrow(() => RevertUnchangedAssetsToPreviousAssetState.ApplyAssetEntryUpdates(ops, context));

			File.Delete(k_ContentUpdateTestCachedBundlePath);
			Settings.RemoveGroup(group);
		}

		[Test]
		public void ApplyAssetEntryUpdates_DeletesCorrectBundle()
		{
			File.WriteAllText(k_ContentUpdateTestCachedBundlePath, "TestCachedAssetBundle");
			File.WriteAllText(k_ContentUpdateTestNewBundleName, "TestAssetBundle");

			var group = Settings.CreateGroup("ContentUpdateTests", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
			string contentUpdateTestGroupGuid = GUID.Generate().ToString();

			group.GetSchema<ContentUpdateGroupSchema>().StaticContent = false;
			var assetEntry = CreateAssetEntry(m_ContentUpdateTestAssetGUID, group);
			group.AddAssetEntry(assetEntry);

			var context = GetContentUpdateContext(m_ContentUpdateTestAssetGUID, k_ContentUpdateTestCachedAssetHash,
				k_ContentUpdateTestNewInternalBundleName, k_ContentUpdateTestNewBundleName,
				k_ContentUpdateTestCachedBundlePath, contentUpdateTestGroupGuid, k_ContentUpdateTestFileName);

			var ops = new List<RevertUnchangedAssetsToPreviousAssetState.AssetEntryRevertOperation>()
			{
				new RevertUnchangedAssetsToPreviousAssetState.AssetEntryRevertOperation()
				{
					PreviousBuildPath = k_ContentUpdateTestCachedBundlePath,
					AssetEntry = assetEntry,
					BundleCatalogEntry = context.IdToCatalogDataEntryMap[m_ContentUpdateTestAssetGUID],
					CurrentBuildPath = k_ContentUpdateTestNewBundleName,
					PreviousAssetState = context.GuidToPreviousAssetStateMap[m_ContentUpdateTestAssetGUID]
				},
				new RevertUnchangedAssetsToPreviousAssetState.AssetEntryRevertOperation()
				{
					PreviousBuildPath = k_ContentUpdateTestCachedBundlePath,
					AssetEntry = assetEntry,
					BundleCatalogEntry = context.IdToCatalogDataEntryMap[m_ContentUpdateTestAssetGUID],
					CurrentBuildPath = k_ContentUpdateTestNewBundleName,
					PreviousAssetState = context.GuidToPreviousAssetStateMap[m_ContentUpdateTestAssetGUID]
				}
			};

			context.ContentState.cachedBundles = new CachedBundleState[] { new CachedBundleState() { bundleFileId = "cachedBundle", data = "string" } };
			RevertUnchangedAssetsToPreviousAssetState.ApplyAssetEntryUpdates(ops, context);

			Assert.IsTrue(File.Exists(k_ContentUpdateTestCachedBundlePath));
			Assert.IsFalse(File.Exists(k_ContentUpdateTestNewBundleName));

			File.Delete(k_ContentUpdateTestCachedBundlePath);
			Settings.RemoveGroup(group);
		}

		[Test]
		public void ApplyAssetEntryUpdates_RegistryOnlyContainsCachedBundleEntry()
		{
			var group = Settings.CreateGroup("ContentUpdateTests", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
			string contentUpdateTestGroupGuid = GUID.Generate().ToString();

			group.GetSchema<ContentUpdateGroupSchema>().StaticContent = false;
			var assetEntry = CreateAssetEntry(m_ContentUpdateTestAssetGUID, group);
			group.AddAssetEntry(assetEntry);

			var context = GetContentUpdateContext(m_ContentUpdateTestAssetGUID, k_ContentUpdateTestCachedAssetHash,
				k_ContentUpdateTestNewInternalBundleName, k_ContentUpdateTestNewBundleName,
				k_ContentUpdateTestCachedBundlePath, contentUpdateTestGroupGuid, k_ContentUpdateTestFileName);

			var ops = new List<RevertUnchangedAssetsToPreviousAssetState.AssetEntryRevertOperation>()
			{
				new RevertUnchangedAssetsToPreviousAssetState.AssetEntryRevertOperation()
				{
					PreviousBuildPath = k_ContentUpdateTestCachedBundlePath,
					AssetEntry = assetEntry,
					BundleCatalogEntry = context.IdToCatalogDataEntryMap[m_ContentUpdateTestAssetGUID],
					CurrentBuildPath = k_ContentUpdateTestNewBundleName,
					PreviousAssetState = context.GuidToPreviousAssetStateMap[m_ContentUpdateTestAssetGUID]
				},
				new RevertUnchangedAssetsToPreviousAssetState.AssetEntryRevertOperation()
				{
					PreviousBuildPath = k_ContentUpdateTestCachedBundlePath,
					AssetEntry = assetEntry,
					BundleCatalogEntry = context.IdToCatalogDataEntryMap[m_ContentUpdateTestAssetGUID],
					CurrentBuildPath = k_ContentUpdateTestNewBundleName,
					PreviousAssetState = context.GuidToPreviousAssetStateMap[m_ContentUpdateTestAssetGUID]
				}
			};

			context.ContentState.cachedBundles = new CachedBundleState[] { new CachedBundleState() { bundleFileId = "cachedBundle", data = "string" } };
			RevertUnchangedAssetsToPreviousAssetState.ApplyAssetEntryUpdates(ops, context);

			var registryPaths = context.Registry.GetFilePaths();

			Assert.AreEqual(1, registryPaths.Count());
			Assert.AreEqual(k_ContentUpdateTestCachedBundlePath, registryPaths.ElementAt(0));

			Settings.RemoveGroup(group);
		}

		[Test]
		public void ApplyAssetEntryUpdates_WhenAssetAndDependencyAreModifiedAndInSeparateGroups_SetCatalogEntryToCachedBundles()
		{
			GUID depAssetGuid = GUID.Generate();
			string oldDepGroupCachedAssetHash = "1888888888888888888";
			string newDepGroupCachedAssetHash = "1188888888888888888";
			string depGroupNewInternalBundleName = "bundle2";
			string depGroupNewBundleName = "fullbundlepath2";
			string depGroupCachedBundlePath = "cachedBundle2";
			string depGroupFileName = "testfile2";

			AddressableAssetGroup group = Settings.CreateGroup("ContentUpdateTests", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
			string contentUpdateTestGroupGuid = GUID.Generate().ToString();

			AddressableAssetGroup depGroup = Settings.CreateGroup("ContentUpdateTests2", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
			GUID depGroupGuid = GUID.Generate();

			group.GetSchema<ContentUpdateGroupSchema>().StaticContent = false;
			AddressableAssetEntry assetEntry = CreateAssetEntry(m_ContentUpdateTestAssetGUID, group);
			group.AddAssetEntry(assetEntry);

			depGroup.GetSchema<ContentUpdateGroupSchema>().StaticContent = false;
			AddressableAssetEntry depAssetEntry = CreateAssetEntry(depAssetGuid.ToString(), depGroup);
			depGroup.AddAssetEntry(assetEntry);

			ContentUpdateScript.ContentUpdateContext context = GetContentUpdateContext(m_ContentUpdateTestAssetGUID, k_ContentUpdateTestCachedAssetHash,
				k_ContentUpdateTestNewInternalBundleName, k_ContentUpdateTestNewBundleName,
				k_ContentUpdateTestCachedBundlePath, contentUpdateTestGroupGuid, k_ContentUpdateTestFileName);

			AddToContentUpdateContext(context, depAssetGuid.ToString(), oldDepGroupCachedAssetHash,
				depGroupNewInternalBundleName, depGroupNewBundleName,
				depGroupCachedBundlePath, depGroupGuid.ToString(), depGroupFileName);

			var previousDep = new AssetState()
			{
				guid = depAssetGuid,
				hash = Hash128.Parse(oldDepGroupCachedAssetHash)
			};

			var currentDep = new AssetState()
			{
				guid = GUID.Generate(),
				hash = Hash128.Parse(newDepGroupCachedAssetHash)
			};

			context.GuidToPreviousAssetStateMap[m_ContentUpdateTestAssetGUID].dependencies = new AssetState[]
			{
				previousDep
			};

			context.IdToCatalogDataEntryMap[m_ContentUpdateTestAssetGUID].Dependencies.Add(currentDep);

			var ops = new List<RevertUnchangedAssetsToPreviousAssetState.AssetEntryRevertOperation>()
			{
				new RevertUnchangedAssetsToPreviousAssetState.AssetEntryRevertOperation()
				{
					PreviousBuildPath = k_ContentUpdateTestCachedBundlePath,
					AssetEntry = assetEntry,
					BundleCatalogEntry = context.IdToCatalogDataEntryMap[m_ContentUpdateTestAssetGUID],
					CurrentBuildPath = k_ContentUpdateTestNewBundleName,
					PreviousAssetState = context.GuidToPreviousAssetStateMap[m_ContentUpdateTestAssetGUID]
				},
				new RevertUnchangedAssetsToPreviousAssetState.AssetEntryRevertOperation()
				{
					PreviousBuildPath = depGroupCachedBundlePath,
					AssetEntry = depAssetEntry,
					BundleCatalogEntry = context.IdToCatalogDataEntryMap[depAssetGuid.ToString()],
					CurrentBuildPath = depGroupNewBundleName,
					PreviousAssetState = context.GuidToPreviousAssetStateMap[depAssetGuid.ToString()]
				}
			};

			var locations = new List<ContentCatalogDataEntry>();
			context.ContentState.cachedBundles = new CachedBundleState[]
			{
				new CachedBundleState() {bundleFileId = "cachedBundle", data = "string"},
				new CachedBundleState() {bundleFileId = "cachedBundle2", data = "string"}
			};
			RevertUnchangedAssetsToPreviousAssetState.ApplyAssetEntryUpdates(ops, context);

			Assert.AreEqual(k_ContentUpdateTestCachedBundlePath, context.IdToCatalogDataEntryMap[m_ContentUpdateTestAssetGUID].InternalId);
			Assert.AreEqual(depGroupCachedBundlePath, context.IdToCatalogDataEntryMap[depAssetGuid.ToString()].InternalId);

			Settings.RemoveGroup(group);
			Settings.RemoveGroup(depGroup);
		}

		[Test]
		public void RevertBundleDataCorrectlyReplacesToCacheLoadingData()
		{
			string bundleName = "cachedBundleName_containKey";
			string assetBundleProvider = "UnityEngine.ResourceManagement.ResourceProviders.AssetBundleProvider";
			ContentUpdateScript.ContentUpdateContext updateContext = new ContentUpdateScript.ContentUpdateContext();
			updateContext.ContentState = new AddressablesContentState();
			AssetBundleRequestOptions cachedRequestOptions = new AssetBundleRequestOptions() { Crc = 123, Hash = "abc", BundleName = bundleName, BundleSize = 10 };
			updateContext.ContentState.cachedBundles = new CachedBundleState[]
			{
				new CachedBundleState() {bundleFileId = "cachedInternalId", data = cachedRequestOptions}
			};

			AddressableAssetsBuildContext aaContext = new AddressableAssetsBuildContext();
			aaContext.locations = new List<ContentCatalogDataEntry>(2);
			List<object> keys = new List<object>();
			keys.Add("stringLoadKey");

			AssetBundleRequestOptions newLocationData1 = new AssetBundleRequestOptions() { Crc = 456, Hash = "def", BundleName = bundleName, BundleSize = 20 };
			aaContext.locations.Add(new ContentCatalogDataEntry(typeof(AssetBundleResource), "newInternalID", assetBundleProvider, keys, null, newLocationData1));
			AssetBundleRequestOptions newLocationData2 = new AssetBundleRequestOptions() { Crc = 456, Hash = "def", BundleName = "nonCachedBundleName", BundleSize = 20 };
			aaContext.locations.Add(new ContentCatalogDataEntry(typeof(AssetBundleResource), "newInternalID", assetBundleProvider, keys, null, newLocationData2));

			bool reverted = RevertUnchangedAssetsToPreviousAssetState.RevertBundleByNameContains("_containKey", updateContext, aaContext);
			Assert.IsTrue(reverted, "Failed to revert the bundle containing _containsKey");

			// first entry is reverted
			AssetBundleRequestOptions catalogRequestOptions = aaContext.locations[0].Data as AssetBundleRequestOptions;
			Assert.AreEqual(cachedRequestOptions.Crc, catalogRequestOptions.Crc, "Reverted Catalog CRC expected to be the same as the cached value");
			Assert.AreEqual(cachedRequestOptions.Hash, catalogRequestOptions.Hash, "Reverted Catalog Hash expected to be the same as the cached value");
			Assert.AreEqual(cachedRequestOptions.BundleName, catalogRequestOptions.BundleName, "Reverted Catalog BundleName expected to be the same as the cached value");
			Assert.AreEqual(cachedRequestOptions.BundleSize, catalogRequestOptions.BundleSize, "Reverted Catalog BundleSize expected to be the same as the cached value");

			// second entry is not reverted
			catalogRequestOptions = aaContext.locations[1].Data as AssetBundleRequestOptions;
			Assert.AreNotEqual(cachedRequestOptions.Crc, catalogRequestOptions.Crc, "Noncached Catalog CRC expected to be different as the cached value");
			Assert.AreNotEqual(cachedRequestOptions.Hash, catalogRequestOptions.Hash, "Noncached Catalog Hash expected to be different as the cached value");
			Assert.AreNotEqual(cachedRequestOptions.BundleName, catalogRequestOptions.BundleName, "Noncached Catalog BundleName expected to be different as the cached value");
			Assert.AreNotEqual(cachedRequestOptions.BundleSize, catalogRequestOptions.BundleSize, "Noncached Catalog BundleSize expected to be different as the cached value");
		}

		// not found in cache
		// not found in catalog
		// bundeOptions missing

		[Test]
		public void RevertBundleFails_WhenBundleMissingFromCacheButInCatalog()
		{
			string assetBundleProvider = "UnityEngine.ResourceManagement.ResourceProviders.AssetBundleProvider";
			ContentUpdateScript.ContentUpdateContext updateContext = new ContentUpdateScript.ContentUpdateContext();
			updateContext.ContentState = new AddressablesContentState();

			AssetBundleRequestOptions cachedRequestOptions = new AssetBundleRequestOptions() { Crc = 123, Hash = "abc", BundleName = "cachedBundleName", BundleSize = 10 };
			updateContext.ContentState.cachedBundles = new CachedBundleState[]
			{
				new CachedBundleState() {bundleFileId = "cachedInternalId", data = cachedRequestOptions}
			};

			AddressableAssetsBuildContext aaContext = new AddressableAssetsBuildContext();
			aaContext.locations = new List<ContentCatalogDataEntry>(1);
			List<object> keys = new List<object>();
			keys.Add("stringLoadKey");
			AssetBundleRequestOptions locData = new AssetBundleRequestOptions() { Crc = 456, Hash = "def", BundleName = "catalogBundleName", BundleSize = 20 };
			string internalId = "catalogInternalId";
			aaContext.locations.Add(new ContentCatalogDataEntry(typeof(AssetBundleResource), internalId, assetBundleProvider, keys, null, locData));

			LogAssert.Expect(LogType.Error, $"Matching cached update state for {internalId} failed. Content not found in original build.");
			bool reverted = RevertUnchangedAssetsToPreviousAssetState.RevertBundleByNameContains("catalogBundleName", updateContext, aaContext);
			Assert.IsFalse(reverted, "Expected to Fail finding \"_containsKey\" where cached content does not have an entry with that bundle Name");
		}

		[Test]
		public void RevertBundleSucceeds_WhenBundleMissingFromCatalogButInCache()
		{
			string assetBundleProvider = "UnityEngine.ResourceManagement.ResourceProviders.AssetBundleProvider";
			ContentUpdateScript.ContentUpdateContext updateContext = new ContentUpdateScript.ContentUpdateContext();
			updateContext.ContentState = new AddressablesContentState();

			AssetBundleRequestOptions cachedRequestOptions = new AssetBundleRequestOptions() { Crc = 123, Hash = "abc", BundleName = "cachedBundleName", BundleSize = 10 };
			updateContext.ContentState.cachedBundles = new CachedBundleState[]
			{
				new CachedBundleState() {bundleFileId = "cachedInternalId", data = cachedRequestOptions}
			};

			AddressableAssetsBuildContext aaContext = new AddressableAssetsBuildContext();
			aaContext.locations = new List<ContentCatalogDataEntry>(1);
			List<object> keys = new List<object>();
			keys.Add("stringLoadKey");
			AssetBundleRequestOptions locData = new AssetBundleRequestOptions() { Crc = 456, Hash = "def", BundleName = "catalogBundleName", BundleSize = 20 };
			aaContext.locations.Add(new ContentCatalogDataEntry(typeof(AssetBundleResource), "newInternalID", assetBundleProvider, keys, null, locData));

			bool reverted = RevertUnchangedAssetsToPreviousAssetState.RevertBundleByNameContains("cachedBundleName", updateContext, aaContext);
			Assert.IsTrue(reverted, "Expected to succeed where cache exists but not included in current build to be reverted");
		}

		[Test]
		public void RevertBundleSucceeds_WhenBundleMissingFromCacheAndCatalog()
		{
			string assetBundleProvider = "UnityEngine.ResourceManagement.ResourceProviders.AssetBundleProvider";
			ContentUpdateScript.ContentUpdateContext updateContext = new ContentUpdateScript.ContentUpdateContext();
			updateContext.ContentState = new AddressablesContentState();

			updateContext.ContentState.cachedBundles = new CachedBundleState[]
			{
				new CachedBundleState() {bundleFileId = "cachedInternalId", data = null}
			};

			AddressableAssetsBuildContext aaContext = new AddressableAssetsBuildContext();
			aaContext.locations = new List<ContentCatalogDataEntry>(1);
			List<object> keys = new List<object>();
			keys.Add("stringLoadKey");
			aaContext.locations.Add(new ContentCatalogDataEntry(typeof(AssetBundleResource), "newInternalID", assetBundleProvider, keys, null, null));

			bool reverted = RevertUnchangedAssetsToPreviousAssetState.RevertBundleByNameContains("catalogBundleName", updateContext, aaContext);
			Assert.IsTrue(reverted, "Expected to succeed where bundle to revert was not in either cache or current builds");
		}

        static ContentUpdateScript.ContentUpdateContext CreateBuiltinUpdateContext(params CachedBundleState[] cachedBundles)
        {
            return new ContentUpdateScript.ContentUpdateContext
            {
                ContentState = new AddressablesContentState
                {
                    cachedBundles = cachedBundles ?? Array.Empty<CachedBundleState>()
                },
                IdToCatalogDataEntryMap = new Dictionary<string, ContentCatalogDataEntry>(),
                Registry = new FileRegistry()
            };
        }

        AddressableAssetGroup CreateRemoteGroup(string groupName)
        {
            var group = Settings.CreateGroup(groupName, false, false, false, null);
            var schema = group.AddSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kRemoteBuildPath);
            schema.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kRemoteLoadPath);
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            return group;
        }

        [Test]
        public void RevertBundle_BuiltinBundle_WhenContentUnchanged_RevertsMainEntryToPreviousBuild()
        {
            const string bundleName = "defaultgroup_unitybuiltinassets.bundle";

            var cachedRequestOptions = new AssetBundleRequestOptions() { Crc = 123, Hash = "sameHash", BundleName = bundleName, BundleSize = 10 };
            var updateContext = CreateBuiltinUpdateContext(
                new CachedBundleState() { bundleFileId = "cachedBuiltinInternalId", data = cachedRequestOptions });

            var currentRequestOptions = new AssetBundleRequestOptions() { Crc = 456, Hash = "sameHash", BundleName = bundleName, BundleSize = 20 };
            var builtinEntry = new ContentCatalogDataEntry(typeof(AssetBundleResource), "currentBuiltinInternalId", k_AssetBundleProviderId,
                new List<object>() { "builtinPrimaryKey" }, null, currentRequestOptions);

            var aaContext = new AddressableAssetsBuildContext();
            aaContext.locations = new List<ContentCatalogDataEntry>() { builtinEntry };

            bool reverted = RevertUnchangedAssetsToPreviousAssetState.RevertBundleByNameContains(BuildScriptBase.BuiltInBundleBaseName, updateContext, aaContext);

            Assert.IsTrue(reverted);
            Assert.AreEqual("cachedBuiltinInternalId", builtinEntry.InternalId);
            Assert.AreEqual(cachedRequestOptions.Crc, ((AssetBundleRequestOptions)builtinEntry.Data).Crc);
            Assert.AreEqual(cachedRequestOptions.Hash, ((AssetBundleRequestOptions)builtinEntry.Data).Hash);
            Assert.AreEqual(cachedRequestOptions.BundleName, ((AssetBundleRequestOptions)builtinEntry.Data).BundleName);
            Assert.AreEqual(cachedRequestOptions.BundleSize, ((AssetBundleRequestOptions)builtinEntry.Data).BundleSize);
            Assert.AreEqual(1, aaContext.locations.Count);
        }

        [Test]
        public void RevertBundle_BuiltinBundle_WhenContentChanged_RevertsMainEntryAndAddsRemoteDuplicate()
        {
            const string bundleName = "defaultgroup_unitybuiltinassets.bundle";
            const string builtinPrimaryKey = "builtinPrimaryKey";
            const string updatedRemoteBundleKey = "updatedRemoteBundleKey";
            const string remoteBuiltinFileName = "defaultgroup_unitybuiltinassets_remote.bundle";

            var cachedRequestOptions = new AssetBundleRequestOptions() { Crc = 123, Hash = "oldHash", BundleName = bundleName, BundleSize = 10 };
            var updateContext = CreateBuiltinUpdateContext(
                new CachedBundleState() { bundleFileId = "cachedBuiltinInternalId", data = cachedRequestOptions });

            AddressableAssetGroup remoteGroup = null;
            string builtinSourcePath = null;
            string remoteDuplicatePath = null;

            string remoteLoadPathId = Settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kRemoteLoadPath).Id;
            string oldRemoteLoadPath = Settings.profileSettings.GetValueById(Settings.activeProfileId, remoteLoadPathId);
            Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kRemoteLoadPath, "http://fakeremotepath/");

            try
            {
                remoteGroup = CreateRemoteGroup("BuiltinRemoteGroup");

                string sharedBuildPath = Settings.GetSharedBundleGroup().GetSchema<BundledAssetGroupSchema>().BuildPath.GetValue(Settings);
                string remoteBuildPath = remoteGroup.GetSchema<BundledAssetGroupSchema>().BuildPath.GetValue(Settings);
                builtinSourcePath = Path.Combine(sharedBuildPath, bundleName);
                remoteDuplicatePath = Path.Combine(remoteBuildPath, remoteBuiltinFileName);

                Directory.CreateDirectory(Path.GetDirectoryName(builtinSourcePath));
                File.WriteAllText(builtinSourcePath, "builtin bundle");

                var currentRequestOptions = new AssetBundleRequestOptions()
                {
                    Crc = 456,
                    Hash = "newHash",
                    BundleName = bundleName,
                    BundleSize = 20,
                    AssetLoadMode = AssetLoadMode.AllPackedAssetsAndDependencies,
                    Timeout = 9,
                    ChunkedTransfer = true,
                    RedirectLimit = 7,
                    RetryCount = 4,
                    UseUnityWebRequestForLocalBundles = true
                };

                var builtinEntry = new ContentCatalogDataEntry(typeof(AssetBundleResource), builtinSourcePath, k_AssetBundleProviderId,
                    new List<object>() { builtinPrimaryKey }, null, currentRequestOptions);
                var dependentEntry = new ContentCatalogDataEntry(typeof(GameObject), "assetPath", "AssetProvider",
                    new List<object>() { "assetKey" }, new List<object>() { builtinPrimaryKey, updatedRemoteBundleKey }, null);

                var aaContext = new AddressableAssetsBuildContext();
                aaContext.Settings = Settings;
                aaContext.locations = new List<ContentCatalogDataEntry>() { builtinEntry, dependentEntry };
                updateContext.IdToCatalogDataEntryMap[builtinEntry.InternalId] = builtinEntry;

                bool reverted = RevertUnchangedAssetsToPreviousAssetState.RevertBundleByNameContains(BuildScriptBase.BuiltInBundleBaseName, updateContext, aaContext);

                Assert.IsTrue(reverted);
                Assert.AreEqual("cachedBuiltinInternalId", builtinEntry.InternalId);
                Assert.AreEqual(cachedRequestOptions.Crc, ((AssetBundleRequestOptions)builtinEntry.Data).Crc);
                Assert.AreEqual(cachedRequestOptions.Hash, ((AssetBundleRequestOptions)builtinEntry.Data).Hash);
                Assert.AreEqual(cachedRequestOptions.BundleName, ((AssetBundleRequestOptions)builtinEntry.Data).BundleName);
                Assert.AreEqual(cachedRequestOptions.BundleSize, ((AssetBundleRequestOptions)builtinEntry.Data).BundleSize);
                Assert.AreEqual(3, aaContext.locations.Count);

                var remoteBuiltinEntry = aaContext.locations.Single(l =>
                    l.Provider == k_AssetBundleProviderId &&
                    l.Keys.Count > 0 &&
                    (l.Keys[0] as string) == remoteBuiltinFileName);

                Assert.IsTrue(File.Exists(remoteDuplicatePath));
                CollectionAssert.AreEqual(new object[] { remoteBuiltinEntry.Keys[0], updatedRemoteBundleKey }, dependentEntry.Dependencies);
                Assert.IsTrue(updateContext.IdToCatalogDataEntryMap.ContainsKey(remoteBuiltinEntry.InternalId));
                Assert.AreEqual($"{remoteGroup.GetSchema<BundledAssetGroupSchema>().LoadPath.GetValue(Settings).Replace('\\', '/').TrimEnd('/')}/{remoteBuiltinFileName}", remoteBuiltinEntry.InternalId);
                Assert.AreEqual("newHash", ((AssetBundleRequestOptions)remoteBuiltinEntry.Data).Hash);
                Assert.AreEqual(9, ((AssetBundleRequestOptions)remoteBuiltinEntry.Data).Timeout);
                Assert.AreEqual(4, ((AssetBundleRequestOptions)remoteBuiltinEntry.Data).RetryCount);
                Assert.AreEqual(7, ((AssetBundleRequestOptions)remoteBuiltinEntry.Data).RedirectLimit);
                Assert.IsTrue(((AssetBundleRequestOptions)remoteBuiltinEntry.Data).ChunkedTransfer);
                Assert.IsTrue(((AssetBundleRequestOptions)remoteBuiltinEntry.Data).UseUnityWebRequestForLocalBundles);
                Assert.AreEqual("defaultgroup_unitybuiltinassets_remote", ((AssetBundleRequestOptions)remoteBuiltinEntry.Data).BundleName);
            }
            finally
            {
                if (!string.IsNullOrEmpty(builtinSourcePath) && File.Exists(builtinSourcePath))
                    File.Delete(builtinSourcePath);
                if (!string.IsNullOrEmpty(remoteDuplicatePath) && File.Exists(remoteDuplicatePath))
                    File.Delete(remoteDuplicatePath);
                if (remoteGroup != null)
                    Settings.RemoveGroup(remoteGroup);

                Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kRemoteLoadPath, oldRemoteLoadPath);
            }
        }

        [Test]
        public void RevertBundle_BuiltinBundle_WhenPreviousBundleMissing_AddsRemoteDuplicateAndKeepsCurrentEntry()
        {
            const string bundleName = "defaultgroup_unitybuiltinassets.bundle";
            const string builtinPrimaryKey = "builtinPrimaryKey";
            const string remoteBuiltinFileName = "defaultgroup_unitybuiltinassets_remote.bundle";

            var updateContext = CreateBuiltinUpdateContext();

            AddressableAssetGroup remoteGroup = null;
            string builtinSourcePath = null;
            string remoteDuplicatePath = null;

            string remoteLoadPathId = Settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kRemoteLoadPath).Id;
            string oldRemoteLoadPath = Settings.profileSettings.GetValueById(Settings.activeProfileId, remoteLoadPathId);
            Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kRemoteLoadPath, "http://fakeremotepath/");

            try
            {
                remoteGroup = CreateRemoteGroup("BuiltinRemoteGroupNoPrevious");

                string sharedBuildPath = Settings.GetSharedBundleGroup().GetSchema<BundledAssetGroupSchema>().BuildPath.GetValue(Settings);
                string remoteBuildPath = remoteGroup.GetSchema<BundledAssetGroupSchema>().BuildPath.GetValue(Settings);
                builtinSourcePath = Path.Combine(sharedBuildPath, bundleName);
                remoteDuplicatePath = Path.Combine(remoteBuildPath, remoteBuiltinFileName);

                Directory.CreateDirectory(Path.GetDirectoryName(builtinSourcePath));
                File.WriteAllText(builtinSourcePath, "builtin bundle");

                var currentRequestOptions = new AssetBundleRequestOptions() { Crc = 456, Hash = "newHash", BundleName = bundleName, BundleSize = 20 };
                var builtinEntry = new ContentCatalogDataEntry(typeof(AssetBundleResource), builtinSourcePath, k_AssetBundleProviderId,
                    new List<object>() { builtinPrimaryKey }, null, currentRequestOptions);
                var dependentEntry = new ContentCatalogDataEntry(typeof(GameObject), "assetPath", "AssetProvider",
                    new List<object>() { "assetKey" }, new List<object>() { builtinPrimaryKey }, null);

                var aaContext = new AddressableAssetsBuildContext();
                aaContext.Settings = Settings;
                aaContext.locations = new List<ContentCatalogDataEntry>() { builtinEntry, dependentEntry };
                updateContext.IdToCatalogDataEntryMap[builtinEntry.InternalId] = builtinEntry;

                bool reverted = RevertUnchangedAssetsToPreviousAssetState.RevertBundleByNameContains(BuildScriptBase.BuiltInBundleBaseName, updateContext, aaContext);

                Assert.IsTrue(reverted);
                Assert.AreEqual(builtinSourcePath, builtinEntry.InternalId);
                Assert.AreEqual("newHash", ((AssetBundleRequestOptions)builtinEntry.Data).Hash);
                Assert.AreEqual(20, ((AssetBundleRequestOptions)builtinEntry.Data).BundleSize);
                Assert.AreEqual(3, aaContext.locations.Count);

                var remoteBuiltinEntry = aaContext.locations.Single(l =>
                    l.Provider == k_AssetBundleProviderId &&
                    l.Keys.Count > 0 &&
                    (l.Keys[0] as string) == remoteBuiltinFileName);

                Assert.IsTrue(File.Exists(remoteDuplicatePath));
                Assert.AreEqual(remoteBuiltinEntry.Keys[0], dependentEntry.Dependencies[0]);
                Assert.IsTrue(updateContext.IdToCatalogDataEntryMap.ContainsKey(remoteBuiltinEntry.InternalId));
            }
            finally
            {
                if (!string.IsNullOrEmpty(builtinSourcePath) && File.Exists(builtinSourcePath))
                    File.Delete(builtinSourcePath);
                if (!string.IsNullOrEmpty(remoteDuplicatePath) && File.Exists(remoteDuplicatePath))
                    File.Delete(remoteDuplicatePath);
                if (remoteGroup != null)
                    Settings.RemoveGroup(remoteGroup);

                Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kRemoteLoadPath, oldRemoteLoadPath);
            }
        }

        [Test]
        public void RevertBundle_BuiltinBundle_WhenContentChangedAndNoRemoteGroup_Fails()
        {
            const string bundleName = "defaultgroup_unitybuiltinassets.bundle";

            var cachedRequestOptions = new AssetBundleRequestOptions() { Crc = 123, Hash = "oldHash", BundleName = bundleName, BundleSize = 10 };
            var updateContext = CreateBuiltinUpdateContext(
                new CachedBundleState() { bundleFileId = "cachedBuiltinInternalId", data = cachedRequestOptions });

            string sharedBuildPath = Settings.GetSharedBundleGroup().GetSchema<BundledAssetGroupSchema>().BuildPath.GetValue(Settings);
            string builtinSourcePath = Path.Combine(sharedBuildPath, bundleName);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(builtinSourcePath));
                File.WriteAllText(builtinSourcePath, "builtin bundle");

                var currentRequestOptions = new AssetBundleRequestOptions() { Crc = 456, Hash = "newHash", BundleName = bundleName, BundleSize = 20 };
                var builtinEntry = new ContentCatalogDataEntry(typeof(AssetBundleResource), builtinSourcePath, k_AssetBundleProviderId,
                    new List<object>() { "builtinPrimaryKey" }, null, currentRequestOptions);

                var aaContext = new AddressableAssetsBuildContext();
                aaContext.Settings = Settings;
                aaContext.locations = new List<ContentCatalogDataEntry>() { builtinEntry };

                LogAssert.Expect(LogType.Warning, new Regex(".*Addressables will attempt to create a modified duplicate.*"));
                LogAssert.Expect(LogType.Error, new Regex(".*could not locate a remote group.*"));
                bool reverted = RevertUnchangedAssetsToPreviousAssetState.RevertBundleByNameContains(BuildScriptBase.BuiltInBundleBaseName, updateContext, aaContext);

                Assert.IsFalse(reverted);
                Assert.AreEqual(1, aaContext.locations.Count);
            }
            finally
            {
                if (File.Exists(builtinSourcePath))
                    File.Delete(builtinSourcePath);
            }
        }

		[Test]
		[TestCase("{Addressables.RuntimePath}/TargetPlatform/prefabA.bundle", "{Addressables.RuntimePath}/TargetPlatform", "Library/aa/TargetPlatform")]
		[TestCase("{Addressables.RuntimePath}\\TargetPlatform\\prefabA.bundle", "{Addressables.RuntimePath}/TargetPlatform", "Library/aa/TargetPlatform")]
		[TestCase("http://localhost/TargetPlatform/prefabA.bundle", "http://localhost/TargetPlatform", "ServerData/TargetPlatform")]
		public void BundleIdToBuildPath_ReturnsBundleBuildPath(string bundleId, string rootLoadPath, string rootBuildPath)
		{
			string buildPath = RevertUnchangedAssetsToPreviousAssetState.BundleIdToBuildPath(bundleId, rootLoadPath, rootBuildPath);
			Assert.IsTrue(buildPath.StartsWith(rootBuildPath));
		}

		[Test]
		public void GatherModifiedEntries_IncludesDependants()
		{
			// Create assets
			GameObject mainObject = new GameObject("mainObject");
			Material mat = new Material(Shader.Find("Transparent/Diffuse"));
			mainObject.AddComponent<MeshRenderer>().material = mat;

			string mainAssetPath = GetAssetPath("mainObject.prefab");
			string refAssetPath = GetAssetPath("refObject.prefab");
			string materialAssetPath = GetAssetPath("testMaterial.mat");

			AssetDatabase.CreateAsset(mat, materialAssetPath);
			var prefab = PrefabUtility.SaveAsPrefabAsset(mainObject, mainAssetPath);
			GameObject refObject = new GameObject("refObject");
			refObject.AddComponent<ObjectReferenceMonoBehaviour>().m_ObjectReference = prefab;
			PrefabUtility.SaveAsPrefabAsset(refObject, refAssetPath);
			AssetDatabase.SaveAssets();
			AddressableAssetGroup mainAssetGroup = null;

			try
			{
				// Create addressables
				mainAssetGroup = Settings.CreateGroup("TestGroup", false, false, false, null);

				var schema = mainAssetGroup.AddSchema<BundledAssetGroupSchema>();
				schema.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalBuildPath);
				schema.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);
				schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
				mainAssetGroup.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;

				AddressableAssetEntry mainEntry =
					Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(mainAssetPath), mainAssetGroup);
				AddressableAssetEntry refEntry =
					Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(refAssetPath), mainAssetGroup);

				// Build
				var context = new AddressablesDataBuilderInput(Settings);
				Settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(context);

				// Modify assets
				var ma = AssetDatabase.LoadAssetAtPath<Material>(materialAssetPath);
                ma.color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
				EditorUtility.SetDirty(ma);
				AssetDatabase.SaveAssets();

				// Test
				var tempPath = Path.GetDirectoryName(Application.dataPath) + "/Library/com.unity.addressables/" +
							   PlatformMappingService.GetPlatformPathSubFolder() + "/addressables_content_state.bin";
				var modifiedEntries = ContentUpdateScript.GatherModifiedEntries(Settings, tempPath);

				Assert.AreEqual(2, modifiedEntries.Count);
				Assert.IsTrue(modifiedEntries.Contains(mainEntry), "Modified Entries does not include the main prefab");
				Assert.IsTrue(modifiedEntries.Contains(refEntry), "Modified Entries does not include the prefab that is dependant on Prefab that has changed material");
			}
			finally
			{
				// Cleanup
				GameObject.DestroyImmediate(mainObject);
				GameObject.DestroyImmediate(refObject);

				Settings.RemoveGroup(mainAssetGroup);

                AssetDatabase.DeleteAsset(mainAssetPath);
                AssetDatabase.DeleteAsset(refAssetPath);
                AssetDatabase.DeleteAsset(materialAssetPath);
            }
        }

        [Test]
        public void GetEntriesDependentOnModifiedEntries_ReturnsFolderSubAssetsDependentOnModifiedEntry()
        {
            // Create assets
            string folderName = "prefabs";
            string folderPath = GetAssetPath(folderName);
            AssetDatabase.CreateFolder(TestFolder, folderName);

            GameObject mainObject = new GameObject("mainObject");
            Material mat = new Material(Shader.Find("Transparent/Diffuse"));
            mainObject.AddComponent<MeshRenderer>().material = mat;

            string mainAssetPath = GetAssetPath("mainObject.prefab");
            string refAssetPath = GetAssetPath($"{folderName}/refObject.prefab");
            string materialAssetPath = GetAssetPath("testMaterial.mat");

            AssetDatabase.CreateAsset(mat, materialAssetPath);
            var prefab = PrefabUtility.SaveAsPrefabAsset(mainObject, mainAssetPath);
            GameObject refObject = new GameObject("refObject");
            refObject.AddComponent<ObjectReferenceMonoBehaviour>().m_ObjectReference = prefab;
            PrefabUtility.SaveAsPrefabAsset(refObject, refAssetPath);
            AssetDatabase.SaveAssets();
            AddressableAssetGroup mainAssetGroup = null;

            try
            {
                // Create addressables
                mainAssetGroup = Settings.CreateGroup("TestGroup", false, false, false, null);

                var schema = mainAssetGroup.AddSchema<BundledAssetGroupSchema>();
                schema.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalBuildPath);
                schema.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);
                schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                mainAssetGroup.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;

                AddressableAssetEntry mainEntry =
                    Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(mainAssetPath), mainAssetGroup);
                AddressableAssetEntry refEntry =
                    Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(folderPath), mainAssetGroup);

                // Build
                var context = new AddressablesDataBuilderInput(Settings);
                Settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(context);

                // Modify assets
                var ma = AssetDatabase.LoadAssetAtPath<Material>(materialAssetPath);
                ma.color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
                EditorUtility.SetDirty(ma);
                AssetDatabase.SaveAssets();

                // Test
                var tempPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library", "com.unity.addressables",
                    PlatformMappingService.GetPlatformPathSubFolder(), "addressables_content_state.bin");
                var modifiedEntries = ContentUpdateScript.GatherModifiedEntries(Settings, tempPath);

                Assert.AreEqual(2, modifiedEntries.Count);
                foreach (var entry in modifiedEntries)
                {
                    if (entry.IsFolder)
                        Assert.IsTrue(entry.AssetPath == folderPath, "Modified Entries does not include folder");
                    else
                        Assert.IsTrue(entry.AssetPath == mainAssetPath, "Modified Entries does not include the prefab with modified material");
                }
            }
            finally
            {
                // Cleanup
                AssetDatabase.DeleteAsset(folderPath);

                GameObject.DestroyImmediate(mainObject);
                GameObject.DestroyImmediate(refObject);

                Settings.RemoveGroup(mainAssetGroup);

				AssetDatabase.DeleteAsset(mainAssetPath);
				AssetDatabase.DeleteAsset(refAssetPath);
				AssetDatabase.DeleteAsset(materialAssetPath);
			}
		}
	}

    namespace ContentUpdatePerPlatformTests
    {

        [RequirePlatformSupport(BuildTarget.StandaloneWindows, BuildTarget.StandaloneWindows64)]
        public class ContentUpdateTestsWindows : ContentUpdateTests { }

        [RequirePlatformSupport(BuildTarget.StandaloneOSX)]
        public class ContentUpdateTestsOSX : ContentUpdateTests { }

        [RequirePlatformSupport(BuildTarget.StandaloneLinux64)]
        public class ContentUpdateTestsLinux : ContentUpdateTests { }
    }
}

