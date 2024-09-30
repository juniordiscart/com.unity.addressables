using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets.Tests;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEditor.U2D;
using UnityEditor.Presets;

public class BuildLayoutGenerationTaskTests
{
    AddressableAssetSettings m_Settings;

    AddressableAssetSettings Settings
    {
        get
        {
            if (m_Settings == null)
            {
                var path = Path.Combine(TempPath, "Settings", "/AddressableAssetSettings.Tests.asset");
                m_Settings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(path);
            }

            return m_Settings;
        }
    }

    static string kTempPath = "Assets/BuildLayoutGenerationTaskTestsData";
    static string TempPath;
    static int ExecCount;
    bool m_PrevGenerateBuildLayout;
    ProjectConfigData.ReportFileFormat m_PrevFileFormat;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        ExecCount = 0;
    }

    [SetUp]
    public void Setup()
    {
        TempPath = kTempPath + (ExecCount++).ToString();
        foreach (var fileFormat in Enum.GetValues(typeof(ProjectConfigData.ReportFileFormat)))
        {
            string layoutFile = BuildLayoutGenerationTask.GetLayoutFilePathForFormat((ProjectConfigData.ReportFileFormat)fileFormat);
            if (File.Exists(layoutFile))
                File.Delete(layoutFile);
        }

        m_PrevGenerateBuildLayout = ProjectConfigData.GenerateBuildLayout;
        m_PrevFileFormat = ProjectConfigData.BuildLayoutReportFileFormat;
        BuildScriptPackedMode.s_SkipCompilePlayerScripts = true;
        ProjectConfigData.GenerateBuildLayout = true;
        if (Directory.Exists(TempPath))
            Directory.Delete(TempPath, true);
        Directory.CreateDirectory(TempPath);

        m_Settings = AddressableAssetSettings.Create(Path.Combine(TempPath, "Settings"), "AddressableAssetSettings.Tests", false, true);
    }

    [TearDown]
    public void Teardown()
    {
        BuildScriptPackedMode.s_SkipCompilePlayerScripts = false;
        ProjectConfigData.GenerateBuildLayout = m_PrevGenerateBuildLayout;
        ProjectConfigData.BuildLayoutReportFileFormat = m_PrevFileFormat;
        // Many of the tests keep recreating assets in the same path, so we need to unload them completely so they don't get reused by the next test
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(Settings));
        Resources.UnloadAsset(Settings);

        FileUtil.DeleteFileOrDirectory(TempPath);
        FileUtil.DeleteFileOrDirectory(TempPath + ".meta");

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

    static string CreatePrefabAsset(string name)
    {
        return CreatePrefabAsset($"{TempPath}/{name}.prefab", name);
    }

    static string CreatePrefabAsset(string assetPath, string objectName)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = objectName;
        //this is to ensure that bundles are different for every run.
        go.transform.localPosition = UnityEngine.Random.onUnitSphere;
        PrefabUtility.SaveAsPrefabAsset(go, assetPath);
        UnityEngine.Object.DestroyImmediate(go, false);
        return AssetDatabase.AssetPathToGUID(assetPath);
    }

    static string CreateScriptableObjectAsset(string assetPath, string objectName)
    {
        TestObject.Create(objectName, assetPath);
        return AssetDatabase.AssetPathToGUID(assetPath);
    }

    string CreateAddressablePrefab(string name, AddressableAssetGroup group)
    {
        string guid = CreatePrefabAsset($"{TempPath}/{name}.prefab", name);
        return MakeAddressable(group, guid);
    }

    string CreateAddressableScriptableObject(string name, AddressableAssetGroup group)
    {
        string guid = CreateScriptableObjectAsset($"{TempPath}/{name}.asset", name);
        return MakeAddressable(group, guid);
    }

    bool DeletePrefab(string name)
    {
        string path = $"{TempPath}/{name}.prefab";
        return AssetDatabase.DeleteAsset(path);
    }

    bool DeleteScriptableObject(string name)
    {
        string path = $"{TempPath}/{name}.asset";
        return AssetDatabase.DeleteAsset(path);
    }

    // Texture asset creation

    static string CreateTexture(string name, int size = 32)
    {
        string assetPath = $"{TempPath}/{name}.png";
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

    static string CreateSpriteAtlas(string name, string guidTargetTexture)
    {
        var sa = new SpriteAtlas();
        var targetObjects = new UnityEngine.Object[] {AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(guidTargetTexture))};
        sa.Add(targetObjects);
        string saPath = $"{TempPath}/{name}.spriteAtlas";
        AssetDatabase.CreateAsset(sa, saPath);
        AssetDatabase.Refresh();
        return AssetDatabase.AssetPathToGUID(saPath);
    }

    bool DeleteSpriteAtlas(string name)
    {
        string assetPath = $"{TempPath}/{name}.spriteAtlas";
        return AssetDatabase.DeleteAsset(assetPath);
    }

    static string CreateSpriteTexture(string name, int size, bool includesSource)
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
        string assetPath = $"{TempPath}/{name}.png";
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

    void PrintText(BuildLayout layout)
    {
        MemoryStream stream = new MemoryStream();
        BuildLayoutPrinter.WriteBundleLayout(stream, layout);
        string report = Encoding.ASCII.GetString(stream.ToArray());
        Debug.Log(report);
    }

    internal BuildLayout BuildAndExtractLayout()
    {
        try
        {
            BuildLayout layout = null;
            BuildLayoutGenerationTask.s_LayoutCompleteCallback = (x, y) => layout = y;
            Settings.BuildPlayerContentImpl();
            return layout;
        }
        finally
        {
            BuildLayoutGenerationTask.s_LayoutCompleteCallback = null;
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

            // Test
            CollectionAssert.Contains(layout.Groups[0].Bundles[0].Dependencies, layout.Groups[1].Bundles[0]);
            Assert.AreEqual(layout.Groups[0].Bundles[0].Files[0].Assets[0].ExternallyReferencedAssets[0], layout.Groups[1].Bundles[0].Files[0].Assets[0]);
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

        try
        {
            // setup
            group = CreateGroup("Group1");
            string prefabGUID = CreateAddressablePrefab("p1", group);
            string aGUID = CreatePrefabAsset("p2");
            MakePefabReference(prefabGUID, aGUID);
            AssetDatabase.SaveAssets();

            BuildLayout layout = BuildAndExtractLayout();

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
            DeletePrefab("p1");
            DeletePrefab("p2");
        }
    }

    [Test]
    public void WhenBundleContainsMultipleFiles_FilesAndSizesMatchArchiveContent()
    {
        string layoutFilePath = BuildLayoutGenerationTask.GetLayoutFilePathForFormat(ProjectConfigData.BuildLayoutReportFileFormat);
        string scenePath = $"{TempPath}/scene.unity";
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

        try
        {
            // setup
            group = CreateGroup("Group1");
            CreateAddressableTexture("t1", group, 256);
            AssetDatabase.SaveAssets();

            BuildLayout layout = BuildAndExtractLayout();

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
            DeleteTexture("t1");
        }
    }

    [Test]
    public void WhenAllContentsOfAnAssetAreStripped_ExplicitAssetHasNoObjects()
    {
        string layoutFilePath = BuildLayoutGenerationTask.GetLayoutFilePathForFormat(ProjectConfigData.BuildLayoutReportFileFormat);
        AddressableAssetGroup group = null;
        string assetPath = $"{TempPath}/testpreset.preset";

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

            // Test
            Assert.AreEqual(0, layout.Groups[0].Bundles[0].Files[0].Assets[0].SerializedSize);
        }
        finally // cleanup
        {
            if (group != null)
                Settings.RemoveGroup(group);
            if (File.Exists(layoutFilePath))
                File.Delete(layoutFilePath);
            AssetDatabase.DeleteAsset(assetPath);
        }
    }

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
                Assert.AreEqual(2, layout.Groups[0].Bundles[0].Files[0].Assets[0].InternalReferencedOtherAssets.Count);
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

        try
        {
            // setup
            group = CreateGroup("Group1");
            CreateAddressableScriptableObject("so1", group);
            AssetDatabase.SaveAssets();

            BuildLayout layout = BuildAndExtractLayout();

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
            DeleteScriptableObject("so1");
        }
    }
}
