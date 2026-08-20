using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Content;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets.Tests;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEditor.U2D;
using UnityEditor.Presets;
using UnityEditor.TestTools;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders;
using UnityEngine.AddressableAssets.Initialization;
using UnityEditor.Build;
using UnityEngine.AddressableAssets;

namespace BuildLayoutGenerationTaskPerPlatformTests
{
    public abstract class BuildLayoutGenerationTaskTests
    {
        AddressableAssetSettings m_Settings;

        AddressableAssetSettings Settings
        {
            get
            {
                if (m_Settings == null)
                {
                    var path = Path.Combine(m_TestAssetsRoot, "Settings", "AddressableAssetSettings.Tests.asset");
                    m_Settings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(path);
                }

                return m_Settings;
            }
        }

        const string kTempPathPrefix = "Assets/BuildLayoutGenerationTaskTestsData";
        /// <summary>
        /// Per-fixture root so Windows / OSX / Linux test fixtures never share static paths (parallel or overlapping runs).
        /// </summary>
        string m_TestAssetsRoot;
        bool m_PrevGenerateBuildLayout;
        ProjectConfigData.ReportFileFormat m_PrevFileFormat;

        [SetUp]
        public void Setup()
        {
            m_TestAssetsRoot = $"{kTempPathPrefix}_{Guid.NewGuid():N}";
            foreach (var fileFormat in Enum.GetValues(typeof(ProjectConfigData.ReportFileFormat)))
            {
                string layoutFile = BuildLayoutGenerationTask.GetLayoutFilePathForFormat((ProjectConfigData.ReportFileFormat)fileFormat);
                if (File.Exists(layoutFile))
                    File.Delete(layoutFile);
            }

            m_PrevGenerateBuildLayout = ProjectConfigData.GenerateBuildLayout;
            m_PrevFileFormat = ProjectConfigData.BuildLayoutReportFileFormat;
            BundledAssetSchemaBuilder.s_SkipCompilePlayerScripts = true;
            ProjectConfigData.GenerateBuildLayout = true;
            if (Directory.Exists(m_TestAssetsRoot))
                Directory.Delete(m_TestAssetsRoot, true);
            Directory.CreateDirectory(m_TestAssetsRoot);

            m_Settings = AddressableAssetSettings.Create(Path.Combine(m_TestAssetsRoot, "Settings"), "AddressableAssetSettings.Tests", false, true);
        }

        [TearDown]
        public void Teardown()
        {
            BundledAssetSchemaBuilder.s_SkipCompilePlayerScripts = false;
            ProjectConfigData.GenerateBuildLayout = m_PrevGenerateBuildLayout;
            ProjectConfigData.BuildLayoutReportFileFormat = m_PrevFileFormat;
            // Many of the tests keep recreating assets in the same path, so we need to unload them completely so they don't get reused by the next test
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(Settings));
            Resources.UnloadAsset(Settings);

            FileUtil.DeleteFileOrDirectory(m_TestAssetsRoot);
            FileUtil.DeleteFileOrDirectory(m_TestAssetsRoot + ".meta");

            AssetDatabase.Refresh();
        }

        string MakeAddressable(AddressableAssetGroup group, string guid, string address = null)
        {
            var entry = Settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address == null ? Path.GetFileNameWithoutExtension(entry.AssetPath) : address;
            entry.BundleFileId = "GenericFileId";
            return guid;
        }

        // Prefab asset emthods

        string CreatePrefabAsset(string name)
        {
            return CreatePrefabAsset($"{m_TestAssetsRoot}/{name}.prefab", name);
        }

        string CreatePrefabAsset(string assetPath, string objectName)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = objectName;
            //this is to ensure that bundles are different for every run.
            go.transform.localPosition = UnityEngine.Random.onUnitSphere;
            PrefabUtility.SaveAsPrefabAsset(go, assetPath);
            UnityEngine.Object.DestroyImmediate(go, false);
            return AssetDatabase.AssetPathToGUID(assetPath);
        }

        string CreateScriptableObjectAsset(string assetPath, string objectName)
        {
            TestObject.Create(objectName, assetPath);
            return AssetDatabase.AssetPathToGUID(assetPath);
        }

        string CreateAddressablePrefab(string name, AddressableAssetGroup group)
        {
            string guid = CreatePrefabAsset($"{m_TestAssetsRoot}/{name}.prefab", name);
            return MakeAddressable(group, guid);
        }

        string CreateAddressableScriptableObject(string name, AddressableAssetGroup group)
        {
            string guid = CreateScriptableObjectAsset($"{m_TestAssetsRoot}/{name}.asset", name);
            return MakeAddressable(group, guid);
        }

        bool DeletePrefab(string name)
        {
            string path = $"{m_TestAssetsRoot}/{name}.prefab";
            return AssetDatabase.DeleteAsset(path);
        }

        bool DeleteScriptableObject(string name)
        {
            string path = $"{m_TestAssetsRoot}/{name}.asset";
            return AssetDatabase.DeleteAsset(path);
        }

        // Texture asset creation

        string CreateTexture(string name, int size = 32)
        {
            string assetPath = $"{m_TestAssetsRoot}/{name}.png";
            var texture = new Texture2D(size, size);
            var data = ImageConversion.EncodeToPNG(texture);
            UnityEngine.Object.DestroyImmediate(texture);
            File.WriteAllBytes(assetPath, data);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            return AssetDatabase.AssetPathToGUID(assetPath);
        }

        string CreateAddressableTexture(string name, AddressableAssetGroup group, int size = 32)
        {
            string guid = CreateTexture(name, size);
            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            ti.isReadable = false;
            ti.SaveAndReimport();
            return MakeAddressable(group, guid);
        }

        string CreateSpriteAtlas(string name, string guidTargetTexture)
        {
            var sa = new SpriteAtlas();
            var targetObjects = new UnityEngine.Object[] { AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(guidTargetTexture)) };
            sa.Add(targetObjects);
            string saPath = $"{m_TestAssetsRoot}/{name}.spriteAtlas";
            AssetDatabase.CreateAsset(sa, saPath);
            AssetDatabase.Refresh();
            return AssetDatabase.AssetPathToGUID(saPath);
        }

        bool DeleteSpriteAtlas(string name)
        {
            string assetPath = $"{m_TestAssetsRoot}/{name}.spriteAtlas";
            return AssetDatabase.DeleteAsset(assetPath);
        }

        string CreateSpriteTexture(string name, int size, bool includesSource)
        {
            string guid = CreateTexture(name, size);
            string texturePath = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            importer.textureType = TextureImporterType.Sprite; // creates a sprite subobject
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
            return guid;
        }

        bool DeleteTexture(string name)
        {
            string assetPath = $"{m_TestAssetsRoot}/{name}.png";
            return AssetDatabase.DeleteAsset(assetPath);
        }

        /// <summary>
        /// Adds a component to Prefab that references assetToReference
        /// </summary>
        /// <param name="prefabGUID"></param>
        /// <param name="assetToReferenceGUID"></param>
        void MakePefabReference(string prefabGUID, string assetToReferenceGUID)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(prefabGUID));
            UnityEngine.Object target = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(assetToReferenceGUID));
            prefab.AddComponent<TestBehaviourWithReference>().Reference = target;
        }

        AddressableAssetGroup CreateGroup(string name)
        {
            return Settings.CreateGroup(name, false, false, false, null, typeof(BundledAssetGroupSchema));
        }

#if ENABLE_CONTENT_DIRECTORIES
        AddressableAssetGroup CreateContentDirectoryGroup(string name)
        {
            return Settings.CreateGroup(name, false, false, false, null, typeof(ContentDirectoryGroupSchema));
        }
#endif

        void PrintText(BuildLayout layout)
        {
            MemoryStream stream = new MemoryStream();
            BuildLayoutPrinter.WriteBundleLayout(stream, layout);
            string report = Encoding.ASCII.GetString(stream.ToArray());
            Debug.Log(report);
        }

        internal BuildLayout BuildAndExtractLayout()
        {
            return BuildAndExtractLayout(out _);
        }

        internal BuildLayout BuildAndExtractLayout(out AddressablesPlayerBuildResult buildResult)
        {

            var layoutTEPFilePath = string.Empty;
            try
            {
                BuildLayout layout = null;
                BuildLayoutGenerationTask.s_LayoutCompleteCallback = (x, y) => layout = y;
                buildResult = Settings.BuildPlayerContentImpl();
                if (layout != null)
                    layoutTEPFilePath = BuildScriptBase.GetLayoutTEPFilePath(layout.BuildStart);
                return layout;
            }
            finally
            {
                BuildLayoutGenerationTask.s_LayoutCompleteCallback = null;
                if (File.Exists(layoutTEPFilePath))
                    File.Delete(layoutTEPFilePath);
            }
        }

        // regression test for UUM-147985
        [Test]
        public void WriteBuildLog_WhenTimestampedTEPAlreadyExists_SkipsCopyWithoutThrowing()
        {
            ProjectConfigData.BuildLayoutReportFileFormat = ProjectConfigData.ReportFileFormat.JSON;

            BuildLayout layout = BuildAndExtractLayout();
            Assert.IsNotNull(layout);

            string tepSourceDir = Path.Combine(m_TestAssetsRoot, "BuildLog");
            string timestampedTEPPath = BuildScriptBase.GetLayoutTEPFilePath(layout.BuildStart);
            try
            {
                // Simulates the first Play Mode entry: creates the timestamped TEP copy.
                BuildScriptBase.WriteBuildLog(new BuildLog(), tepSourceDir);
                FileAssert.Exists(timestampedTEPPath);

                // Simulates re-entering Play Mode against the same build layout report
                // (same BuildStart, so the same timestamped filename). This used to throw
                // IOException from File.Copy because the destination already existed.
                Assert.DoesNotThrow(() => BuildScriptBase.WriteBuildLog(new BuildLog(), tepSourceDir));
            }
            finally
            {
                if (File.Exists(timestampedTEPPath))
                    File.Delete(timestampedTEPPath);
            }
        }

        class WebExtractSession : IDisposable
        {
            public string DataDirectory;
            public string[] Files;

            public WebExtractSession(string filePath)
            {
                DataDirectory = filePath + "_data";
                if (Directory.Exists(DataDirectory))
                    throw new Exception("Bundle data directory already exists");

                var baseDir = Path.GetDirectoryName(EditorApplication.applicationPath);
                var webExtractFiles = Directory.GetFiles(baseDir, "WebExtract*", SearchOption.AllDirectories);
                string webExtractPath = webExtractFiles[0];

                Assert.IsTrue(File.Exists(filePath), "Param filePath does not point to an existing file.");

                var process = new System.Diagnostics.Process
                {
                    StartInfo =
                {
                    FileName = webExtractPath,
                    Arguments = string.Format(@"""{0}""", filePath),
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
                };
                process.Start();

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var exitCode = process.ExitCode;
                process.Close();

                Assert.AreEqual(0, exitCode);
                Files = Directory.GetFiles(DataDirectory);
            }

            public void Dispose()
            {
                Directory.Delete(DataDirectory, true);
            }
        }

        internal void AssertEditorBundleDetailsMatchPhysicalBundle(string bundlePath, BuildLayout.Bundle bundle)
        {
            Assert.AreEqual(new FileInfo(bundlePath).Length, bundle.FileSize);
            using (var wes = new WebExtractSession(bundlePath))
            {
                Assert.AreEqual(bundle.Files.Sum(x => x.SubFiles.Count), wes.Files.Length);
                foreach (BuildLayout.SubFile sf in bundle.Files.SelectMany(x => x.SubFiles))
                {
                    string filename = Path.Combine(wes.DataDirectory, sf.Name);
                    Assert.AreEqual(sf.Size, new FileInfo(filename).Length);
                }
            }
        }

        [Test]
        //[RequirePlatformSupport(
        //    BuildTarget.StandaloneWindows,
        //    BuildTarget.StandaloneWindows64,
        //    BuildTarget.StandaloneOSX,
        //    BuildTarget.StandaloneLinux64,
        //    BuildTarget.EmbeddedLinux)]
        public void WhenBundleReferencesAnotherBundle_ExternalReferenceExists()
        {
            string layoutFilePath = BuildLayoutGenerationTask.GetLayoutFilePathForFormat(ProjectConfigData.BuildLayoutReportFileFormat);
            AddressableAssetGroup group = null;
            AddressableAssetGroup group2 = null;

            try
            {
                // setup
                group = CreateGroup("Group1");
                string prefabGUID = CreateAddressablePrefab("p1", group);
                group2 = CreateGroup("Group2");
                string g2p1GUID = CreateAddressablePrefab("g2p1", group2);
                MakePefabReference(prefabGUID, g2p1GUID);
                AssetDatabase.SaveAssets();

                BuildLayout layout = BuildAndExtractLayout();

                var layoutGroup1 = layout.Groups.Find((g) => g.Name == "Group1");
                var layoutGroup2 = layout.Groups.Find((g) => g.Name == "Group2");

                // Test
                CollectionAssert.Contains(layoutGroup1.Bundles[0].Dependencies, layoutGroup2.Bundles[0]);
                Assert.AreEqual(layoutGroup1.Bundles[0].Files[0].Assets[0].ExternallyReferencedAssets[0], layoutGroup2.Bundles[0].Files[0].Assets[0]);
            }
            finally // cleanup
            {
                if (group != null)
                    Settings.RemoveGroup(group);
                if (group2 != null)
                    Settings.RemoveGroup(group2);
                if (File.Exists(layoutFilePath))
                    File.Delete(layoutFilePath);
                DeletePrefab("p1");
                DeletePrefab("g2p1");
            }

        }

        [Test]
        public void WhenAssetImplicitlyPulledIntoBundle_ImplicitEntryAndReferencesCreated()
        {
            string layoutFilePath = BuildLayoutGenerationTask.GetLayoutFilePathForFormat(ProjectConfigData.BuildLayoutReportFileFormat);
            AddressableAssetGroup group = null;

            var layoutTEPFilePath = string.Empty;
            try
            {
                // setup
                group = CreateGroup("Group1");
                string prefabGUID = CreateAddressablePrefab("p1", group);
                string aGUID = CreatePrefabAsset("p2");
                MakePefabReference(prefabGUID, aGUID);
                AssetDatabase.SaveAssets();

                BuildLayout layout = BuildAndExtractLayout();
                layoutTEPFilePath = BuildScriptBase.GetLayoutTEPFilePath(layout.BuildStart);

                // Test
                BuildLayout.DataFromOtherAsset oa = layout.Groups[0].Bundles[0].Files[0].OtherAssets.First(x => x.AssetPath.Contains("p2.prefab"));
                Assert.AreEqual(aGUID, oa.AssetGuid);
            }
            finally // cleanup
            {
                if (group != null)
                    Settings.RemoveGroup(group);
                if (File.Exists(layoutFilePath))
                    File.Delete(layoutFilePath);
                if (File.Exists(layoutTEPFilePath))
                    File.Delete(layoutTEPFilePath);
                DeletePrefab("p1");
                DeletePrefab("p2");
            }
        }

        [Test]
        public void WhenBundleContainsMultipleFiles_FilesAndSizesMatchArchiveContent()
        {
            string layoutFilePath = BuildLayoutGenerationTask.GetLayoutFilePathForFormat(ProjectConfigData.BuildLayoutReportFileFormat);
            string scenePath = $"{m_TestAssetsRoot}/scene.unity";
            AddressableAssetGroup groupScenes = null;
            AddressableAssetGroup textureGroup = null;

            try
            {
                // setup
                groupScenes = CreateGroup("SceneGroup");
                textureGroup = CreateGroup("TextureGroup");

                Scene scene1 = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                new GameObject().AddComponent<TestBehaviourWithReference>();
                EditorSceneManager.SaveScene(scene1, scenePath);
                Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(scenePath), groupScenes);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                CreateAddressableTexture("t1", textureGroup, 256);
                AssetDatabase.SaveAssets();

                BuildLayout layout = BuildAndExtractLayout();

                // Test
                BundledAssetGroupSchema schema = Settings.groups.First(x => x.HasSchema<BundledAssetGroupSchema>()).GetSchema<BundledAssetGroupSchema>();
                string path = schema.BuildPath.GetValue(Settings);
                foreach (BuildLayout.Bundle bundle in layout.Groups.SelectMany(x => x.Bundles))
                    AssertEditorBundleDetailsMatchPhysicalBundle(Path.Combine(path, bundle.Name), bundle);
            }
            finally // cleanup
            {
                if (groupScenes != null)
                    Settings.RemoveGroup(groupScenes);
                if (textureGroup != null)
                    Settings.RemoveGroup(textureGroup);
                if (File.Exists(layoutFilePath))
                    File.Delete(layoutFilePath);
                AssetDatabase.DeleteAsset(scenePath);
                DeleteTexture("t1");
            }
        }

        // Even though slim writes is true, the system will enable it if it needs to generate a build layout report
        [Test]
        public void WhenSlimWriteResultsIsTrue_LayoutStillGenerated()
        {
            ProjectConfigData.ReportFileFormat fileFormat = ProjectConfigData.ReportFileFormat.TXT;
            string layoutFilePath = BuildLayoutGenerationTask.GetLayoutFilePathForFormat(fileFormat);
            AddressableAssetGroup group = null;
            bool prevSlim = ScriptableBuildPipeline.slimWriteResults;
            ProjectConfigData.ReportFileFormat prevFileFormat = ProjectConfigData.BuildLayoutReportFileFormat;

            try
            {
                // setup
                ScriptableBuildPipeline.slimWriteResults = true;
                ProjectConfigData.BuildLayoutReportFileFormat = fileFormat;
                group = CreateGroup("Group1");
                CreateAddressablePrefab("p1", group);
                AssetDatabase.SaveAssets();

                BuildAndExtractLayout();

                FileAssert.Exists(layoutFilePath);
            }
            finally // cleanup
            {
                ScriptableBuildPipeline.slimWriteResults = prevSlim;
                ProjectConfigData.BuildLayoutReportFileFormat = prevFileFormat;
                if (group != null)
                    Settings.RemoveGroup(group);
                if (File.Exists(layoutFilePath))
                    File.Delete(layoutFilePath);
                DeletePrefab("p1");
            }
        }

        [Test]
        public void WhenBuildLayoutIsDisabled_BuildLayoutIsNotGenerated()
        {
            ProjectConfigData.ReportFileFormat fileFormat = ProjectConfigData.ReportFileFormat.TXT;
            string layoutFilePath = BuildLayoutGenerationTask.GetLayoutFilePathForFormat(fileFormat);
            AddressableAssetGroup group = null;
            bool prevGenerateBuildLayout = ProjectConfigData.GenerateBuildLayout;
            ProjectConfigData.ReportFileFormat prevFileFormat = ProjectConfigData.BuildLayoutReportFileFormat;

            try
            {
                // setup
                ProjectConfigData.GenerateBuildLayout = false;
                ProjectConfigData.BuildLayoutReportFileFormat = fileFormat;
                group = CreateGroup("Group1");
                CreateAddressablePrefab("p1", group);
                AssetDatabase.SaveAssets();

                BuildAndExtractLayout();

                // Test
                FileAssert.DoesNotExist(layoutFilePath);
            }
            finally // cleanup
            {
                ProjectConfigData.GenerateBuildLayout = prevGenerateBuildLayout;
                ProjectConfigData.BuildLayoutReportFileFormat = prevFileFormat;
                if (group != null)
                    Settings.RemoveGroup(group);
                if (File.Exists(layoutFilePath))
                    File.Delete(layoutFilePath);
                DeletePrefab("p1");
            }
        }

        [Test]
        [TestCase(ProjectConfigData.ReportFileFormat.TXT)]
        [TestCase(ProjectConfigData.ReportFileFormat.JSON)]
        public void WhenBuildLayoutIsEnabled_BuildLayoutIsGenerated(ProjectConfigData.ReportFileFormat format)
        {
            string layoutFilePath = BuildLayoutGenerationTask.GetLayoutFilePathForFormat(format);
            AddressableAssetGroup group = null;
            bool prevGenerateBuildLayout = ProjectConfigData.GenerateBuildLayout;
            ProjectConfigData.ReportFileFormat prevFileFormat = ProjectConfigData.BuildLayoutReportFileFormat;

            try
            {
                // setup
                ProjectConfigData.GenerateBuildLayout = true;
                ProjectConfigData.BuildLayoutReportFileFormat = format;
                group = CreateGroup("Group1");
                CreateAddressablePrefab("p1", group);
                AssetDatabase.SaveAssets();

                BuildAndExtractLayout();

                // Test
                FileAssert.Exists(layoutFilePath);
                if (format == ProjectConfigData.ReportFileFormat.JSON)
                {
                    string text = File.ReadAllText(layoutFilePath);
                    var layout = JsonUtility.FromJson<BuildLayout>(text);
                    Assert.IsNotNull(layout);
                }
            }
            finally // cleanup
            {
                ProjectConfigData.GenerateBuildLayout = prevGenerateBuildLayout;
                ProjectConfigData.BuildLayoutReportFileFormat = prevFileFormat;
                if (group != null)
                    Settings.RemoveGroup(group);
                if (File.Exists(layoutFilePath))
                    File.Delete(layoutFilePath);
                DeletePrefab("p1");
            }
        }

        [Test]
        public void WhenAssetHasStreamedData_IsReportedCorrectly()
        {
            string layoutFilePath = BuildLayoutGenerationTask.GetLayoutFilePathForFormat(ProjectConfigData.BuildLayoutReportFileFormat);
            AddressableAssetGroup group = null;

            var layoutTEPFilePath = string.Empty;
            try
            {
                // setup
                group = CreateGroup("Group1");
                CreateAddressableTexture("t1", group, 256);
                AssetDatabase.SaveAssets();

                BuildLayout layout = BuildAndExtractLayout();
                layoutTEPFilePath = BuildScriptBase.GetLayoutTEPFilePath(layout.BuildStart);

                // Test
                Assert.IsTrue(layout.Groups[0].Bundles[0].Files[0].Assets[0].StreamedSize != 0);
                BuildLayout.SubFile f = layout.Groups[0].Bundles[0].Files[0].SubFiles.First(x => x.Name.EndsWith(".resS"));
                Assert.IsFalse(f.IsSerializedFile);
            }
            finally // cleanup
            {
                if (group != null)
                    Settings.RemoveGroup(group);
                if (File.Exists(layoutFilePath))
                    File.Delete(layoutFilePath);
                if (File.Exists(layoutTEPFilePath))
                    File.Delete(layoutTEPFilePath);
                DeleteTexture("t1");
            }
        }

        [Test]
        public void WhenAllContentsOfAnAssetAreStripped_ExplicitAssetHasNoObjects()
        {
            string layoutFilePath = BuildLayoutGenerationTask.GetLayoutFilePathForFormat(ProjectConfigData.BuildLayoutReportFileFormat);
            AddressableAssetGroup group = null;
            string assetPath = $"{m_TestAssetsRoot}/testpreset.preset";

            var layoutTEPFilePath = string.Empty;
            try
            {
                // setup
                Material obj = new Material(Shader.Find("Transparent/Diffuse"));
                Preset myPreset = new Preset(obj);
                AssetDatabase.CreateAsset(myPreset, assetPath);
                GameObject.DestroyImmediate(obj);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                group = CreateGroup("Group1");
                MakeAddressable(group, guid);
                AssetDatabase.SaveAssets();

                BuildLayout layout = BuildAndExtractLayout();
                layoutTEPFilePath = BuildScriptBase.GetLayoutTEPFilePath(layout.BuildStart);
                // Test
                Assert.AreEqual(0, layout.Groups[0].Bundles[0].Files[0].Assets[0].SerializedSize);
            }
            finally // cleanup
            {
                if (group != null)
                    Settings.RemoveGroup(group);
                if (File.Exists(layoutFilePath))
                    File.Delete(layoutFilePath);
                if (File.Exists(layoutTEPFilePath))
                    File.Delete(layoutTEPFilePath);
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

#if ENABLE_CONTENT_DIRECTORIES
        [Test]
        public void Verify_ContentDirectoryData_IncludedInBuildLayout()
        {
            string layoutFilePath = BuildLayoutGenerationTask.GetLayoutFilePathForFormat(ProjectConfigData.BuildLayoutReportFileFormat);
            AddressableAssetGroup group = null;

            var layoutTEPFilePath = string.Empty;
            try
            {
                // setup
                group = CreateContentDirectoryGroup("ContentDirectoryGroup");
                CreateAddressablePrefab("p1", group);
                AssetDatabase.SaveAssets();

                BuildLayout layout = BuildAndExtractLayout(out AddressablesPlayerBuildResult buildResult);
                layoutTEPFilePath = BuildScriptBase.GetLayoutTEPFilePath(layout.BuildStart);

                string hashPath = $"{layout.LocalCatalogBuildPath}/BuildManifestHash.txt";
                string hash = File.Exists(hashPath) ? File.ReadAllText(hashPath) : "NoHashFound";

                if(hash == "NoHashFound")
                    Assert.Fail("BuildManifestHash.txt not found or empty, cannot complete test");

                Assert.AreEqual(1, layout.ContentDirectories.Count);
                Assert.AreEqual($"{layout.LocalCatalogBuildPath}/{hash}.json", layout.ContentDirectories[0].ManifestPath);
                Assert.AreEqual(ResourceManagerRuntimeData.kCatalogAddress, layout.ContentDirectories[0].CatalogName);

                Assert.IsNotNull(buildResult.ContentDirectoryBuildResults);
                Assert.AreEqual(1, buildResult.ContentDirectoryBuildResults.Count);
                Assert.IsFalse(buildResult.ContentDirectoryBuildResults[0].BuildSessionGUID.Empty(),
                    "ContentDirectoryBuildResult should have a BuildSessionGUID after a content directory build.");

                Assert.IsFalse(layout.ContentDirectories[0].BuildSessionGUID.Empty(),
                    "BuildLayout.ContentDirectory should have a non-empty BuildSessionGUID.");

                Assert.AreEqual(
                    buildResult.ContentDirectoryBuildResults[0].BuildSessionGUID,
                    layout.ContentDirectories[0].BuildSessionGUID,
                    "BuildSessionGUID in the build layout should match the one in the build result.");

                VerifyTEP(buildResult);
            }
            finally // cleanup
            {
                if (group != null)
                    Settings.RemoveGroup(group);
                if (File.Exists(layoutFilePath))
                    File.Delete(layoutFilePath);
                if (File.Exists(layoutTEPFilePath))
                    File.Delete(layoutTEPFilePath);
                DeletePrefab("p1");
            }
        }

        private void VerifyTEP(AddressablesPlayerBuildResult buildResult)
        {
            // Verify BuildContentTEP.json was merged into the main AddressablesBuildTEP.json
            string mainTepPath = Addressables.LibraryPath + "AddressablesBuildTEP.json";
            FileAssert.Exists(mainTepPath);
            string mainTepText = File.ReadAllText(mainTepPath);
            StringAssert.Contains("Building content directory AddressablesMainContentCatalog", mainTepText,
                "AddressablesBuildTEP.json should contain the ContentDirectorySchemaBuilder build scope");

            if (BuildHistory.TryGetFilePath(buildResult.ContentDirectoryBuildResults[0].BuildSessionGUID,
                    "BuildContentTEP.json", out string buildContentTepPath))
            {
                string buildContentTepText = File.ReadAllText(buildContentTepPath);
                int eventsStart = buildContentTepText.IndexOf("\"traceEvents\"", StringComparison.Ordinal);
                Assert.Greater(eventsStart, -1, "BuildContentTEP.json should contain a traceEvents array");
                int nameOffset = buildContentTepText.IndexOf("\"name\":", eventsStart, StringComparison.Ordinal);
                Assert.Greater(nameOffset, -1, "BuildContentTEP.json should contain at least one named event");
                int valueStart = buildContentTepText.IndexOf('"', nameOffset + 7) + 1;
                int valueEnd = buildContentTepText.IndexOf('"', valueStart);
                // this verifies the first value
                string nativeEventName = buildContentTepText.Substring(valueStart, valueEnd - valueStart);
                StringAssert.Contains(nativeEventName, mainTepText,
                    "An event from BuildContentTEP.json should appear in AddressablesBuildTEP.json after the TEP merge");

                // also verify a known value
                StringAssert.Contains("UnifiedBuild", mainTepText,
                    "The UnifiedBuild from BuildContentTEP.json should appear in AddressablesBuildTEP.json after the TEP merge");
            }
            else
            {
                Assert.Fail("BuildContentTEP.json was not found via BuildHistory — cannot verify TEP merge");
            }
        }
#endif

        class SpritePackerScope : IDisposable
        {
            SpritePackerMode m_PrevMode;

            public SpritePackerScope(SpritePackerMode mode)
            {
                m_PrevMode = EditorSettings.spritePackerMode;
                EditorSettings.spritePackerMode = mode;
            }

            public void Dispose()
            {
                EditorSettings.spritePackerMode = m_PrevMode;
            }
        }

        [Test]
#if UNITY_6000_6_OR_NEWER
        [Ignore("SpriteAtlas dependencies to their sprites have been removed.")]
#endif
        public void WhenReferencedObjectIdentifiedWithFilename_ObjectRepresentedInDataFromOtherAssets()
        {
            using (new SpritePackerScope(SpritePackerMode.BuildTimeOnlyAtlas))
            {
                string layoutFilePath = BuildLayoutGenerationTask.GetLayoutFilePathForFormat(ProjectConfigData.BuildLayoutReportFileFormat);
                AddressableAssetGroup group = null;

                try
                {
                    // setup
                    BuildCache.PurgeCache(false);
                    group = CreateGroup("Group1");
                    string textureGUID = CreateSpriteTexture("spritetexture", 256, false);
                    MakeAddressable(group, CreateSpriteAtlas("atlas", textureGUID));
                    AssetDatabase.SaveAssets();

                    BuildLayout layout = BuildAndExtractLayout();

                    // Test
                    BuildLayout.DataFromOtherAsset otherAssets = layout.Groups[0].Bundles[0].Files[0].Assets[0].InternalReferencedOtherAssets[0];
#if UNITY_6000_6_OR_NEWER //In 6000.6 the sprite atlas changed how it handled dependencies
                    Assert.AreEqual(1, layout.Groups[0].Bundles[0].Files[0].Assets[0].InternalReferencedOtherAssets.Count);
#else
                    Assert.AreEqual(2, layout.Groups[0].Bundles[0].Files[0].Assets[0].InternalReferencedOtherAssets.Count);
#endif
                    CollectionAssert.Contains(otherAssets.ReferencingAssets, layout.Groups[0].Bundles[0].Files[0].Assets[0]);
                }
                finally // cleanup
                {
                    if (group != null)
                        Settings.RemoveGroup(group);
                    if (File.Exists(layoutFilePath))
                        File.Delete(layoutFilePath);
                    DeleteSpriteAtlas("atlas");
                    DeleteTexture("spritetexture");
                }
            }
        }

        [Test]
        public void WhenBuildRemoteCatalogIsDisabled_BuildLayoutContainsCatalogHash()
        {
            string layoutFilePath = BuildLayoutGenerationTask.GetLayoutFilePathForFormat(ProjectConfigData.BuildLayoutReportFileFormat);
            AddressableAssetGroup group = null;
            bool prevBuildRemoteCatalog = Settings.BuildRemoteCatalog;

            try
            {
                // setup
                group = CreateGroup("Group1");
                CreateAddressablePrefab("p1", group);
                AssetDatabase.SaveAssets();

                BuildLayout layout = BuildAndExtractLayout();

                // Test
                Assert.IsFalse(string.IsNullOrEmpty(layout.AddressablesRuntimeSettings.CatalogHash), "Catalog Hash was not correctly written to the Layout");
                Assert.AreEqual(32, layout.AddressablesRuntimeSettings.CatalogHash.Length, "Catalog Hash was not correctly written to the Layout, incorrect size for hash");
                Assert.AreEqual(32, layout.BuildResultHash.Length, "Build is expected to have a result hash for the build");
            }
            finally // cleanup
            {
                Settings.BuildRemoteCatalog = prevBuildRemoteCatalog;
                if (group != null)
                    Settings.RemoveGroup(group);
                if (File.Exists(layoutFilePath))
                    File.Delete(layoutFilePath);
                DeletePrefab("p1");
            }
        }

        [Test]
        public void WhenBuildContainsMonoScripts_LayoutDoesNotHaveReferencesToMonoScriptAssets()
        {
            string layoutFilePath = BuildLayoutGenerationTask.GetLayoutFilePathForFormat(ProjectConfigData.BuildLayoutReportFileFormat);
            AddressableAssetGroup group = null;
            bool prevBuildRemoteCatalog = Settings.BuildRemoteCatalog;

            var layoutTEPFilePath = string.Empty;
            try
            {
                // setup
                group = CreateGroup("Group1");
                CreateAddressableScriptableObject("so1", group);
                AssetDatabase.SaveAssets();

                BuildLayout layout = BuildAndExtractLayout();
                layoutTEPFilePath = BuildScriptBase.GetLayoutTEPFilePath(layout.BuildStart);

                // Test
                foreach (BuildLayout.ExplicitAsset explicitAsset in BuildLayoutHelpers.EnumerateAssets(layout))
                {
                    foreach (var referencedAsset in explicitAsset.InternalReferencedExplicitAssets)
                    {
                        Assert.IsNotNull(referencedAsset, "Referenced Asset was null, this was likely a stripped MonoScript");
                        Assert.IsTrue(!referencedAsset.AssetPath.EndsWith(".cs") && referencedAsset.AssetPath.EndsWith(".dll"));
                    }
                    foreach (var referencedAsset in explicitAsset.ExternallyReferencedAssets)
                    {
                        Assert.IsNotNull(referencedAsset, "Referenced Asset was null, this was likely a stripped MonoScript");
                        Assert.IsTrue(!referencedAsset.AssetPath.EndsWith(".cs") && referencedAsset.AssetPath.EndsWith(".dll"));
                    }
                    foreach (var referencedAsset in explicitAsset.InternalReferencedOtherAssets)
                    {
                        Assert.IsNotNull(referencedAsset, "Referenced Asset was null, this was likely a stripped MonoScript");
                        Assert.IsTrue(!referencedAsset.AssetPath.EndsWith(".cs") && referencedAsset.AssetPath.EndsWith(".dll"));
                    }
                }
            }
            finally // cleanup
            {
                Settings.BuildRemoteCatalog = prevBuildRemoteCatalog;
                if (group != null)
                    Settings.RemoveGroup(group);
                if (File.Exists(layoutFilePath))
                    File.Delete(layoutFilePath);
                if (File.Exists(layoutTEPFilePath))
                    File.Delete(layoutTEPFilePath);
                DeleteScriptableObject("so1");
            }
        }

        [Test]
        public void WhenBuildContainsNullGroup_BuildLayoutSucceeds()
        {
            string layoutFilePath = BuildLayoutGenerationTask.GetLayoutFilePathForFormat(ProjectConfigData.BuildLayoutReportFileFormat);
            AddressableAssetGroup group = null;

            try
            {
                // setup
                group = CreateGroup("Group1");
                CreateAddressablePrefab("p1", group);
                AssetDatabase.SaveAssets();

                Settings.groups.Add(null);

                // test
                Assert.DoesNotThrow(() => BuildAndExtractLayout(), "BuildLayoutGenerationTask does not handle null groups.");
            }
            finally // cleanup
            {
                if (group != null)
                    Settings.RemoveGroup(group);
                Settings.RemoveGroup(null);

                if (File.Exists(layoutFilePath))
                    File.Delete(layoutFilePath);
                DeletePrefab("p1");
            }
        }

        [Test]
        public void WhenAddressablesBuildSucceeds_BuildSessionGUIDIsGenerated()
        {
            string layoutFilePath = BuildLayoutGenerationTask.GetLayoutFilePathForFormat(ProjectConfigData.BuildLayoutReportFileFormat);
            AddressableAssetGroup group = null;

            var layoutTEPFilePath = string.Empty;
            try
            {
                group = CreateGroup("Group1");
                CreateAddressablePrefab("p1", group);
                AssetDatabase.SaveAssets();

                BuildLayout layout = BuildAndExtractLayout();
                layoutTEPFilePath = BuildScriptBase.GetLayoutTEPFilePath(layout.BuildStart);

                Assert.IsNotNull(layout, "Build should produce a layout.");
                Assert.IsFalse(layout.AddressablesBuildSessionGUID.Empty(), "AddressablesBuildSessionGUID should be set for every build.");
                Assert.AreEqual(layout.AddressablesBuildSessionGUID, layout.Header.AddressablesBuildSessionGUID, "Header.AddressablesBuildSessionGUID should match layout.AddressablesBuildSessionGUID.");
            }
            finally
            {
                if (group != null)
                    Settings.RemoveGroup(group);
                if (File.Exists(layoutFilePath))
                    File.Delete(layoutFilePath);
                if (File.Exists(layoutTEPFilePath))
                    File.Delete(layoutTEPFilePath);
                DeletePrefab("p1");
            }
        }

        [Test]
        public void WhenTwoIdenticalBuildsRun_BuildSessionGUIDsAreUnique()
        {
            string layoutFilePath = BuildLayoutGenerationTask.GetLayoutFilePathForFormat(ProjectConfigData.BuildLayoutReportFileFormat);
            AddressableAssetGroup group = null;

            try
            {
                group = CreateGroup("Group1");
                CreateAddressablePrefab("p1", group);
                AssetDatabase.SaveAssets();

                BuildLayout layout1 = BuildAndExtractLayout();
                Assert.IsNotNull(layout1, "First build should produce a layout.");
                Assert.IsFalse(layout1.AddressablesBuildSessionGUID.Empty(), "First build should have a AddressablesBuildSessionGUID.");

                BuildLayout layout2 = BuildAndExtractLayout();
                Assert.IsNotNull(layout2, "Second build should produce a layout.");
                Assert.IsFalse(layout2.AddressablesBuildSessionGUID.Empty(), "Second build should have a AddressablesBuildSessionGUID.");

                Assert.AreNotEqual(layout1.AddressablesBuildSessionGUID, layout2.AddressablesBuildSessionGUID, "Two builds must have distinct BuildSessionGUIDs even when content is identical.");
            }
            finally
            {
                if (group != null)
                    Settings.RemoveGroup(group);
                if (File.Exists(layoutFilePath))
                    File.Delete(layoutFilePath);
                DeletePrefab("p1");
            }
        }
    }

    [RequirePlatformSupport(BuildTarget.StandaloneWindows, BuildTarget.StandaloneWindows64)]
    public class BuildLayoutGenerationTaskTestsWindows : BuildLayoutGenerationTaskTests { }

    [RequirePlatformSupport(BuildTarget.StandaloneOSX)]
    public class BuildLayoutGenerationTaskTestsOSX : BuildLayoutGenerationTaskTests { }

    [RequirePlatformSupport(BuildTarget.StandaloneLinux64)]
    public class BuildLayoutGenerationTaskTestsLinux : BuildLayoutGenerationTaskTests { }
}
