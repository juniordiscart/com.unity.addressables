using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Command queue that generates the dependency graph of project assets.
    /// </summary>
    internal class DependencyGraphCommandQueue : CommandQueue
    {
        #region Constants
        private static readonly HashSet<string> k_IgnoredExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".hlsl", ".asmdef", ".asmref", ".dll", ".unitypackage", ".txt"
        };

        private static readonly string[] k_IgnoredPaths = new[]
        {
            "Assets/AddressableAssetsData",
            "Assets/StreamingAssets",
            "/Editor/",
            "/Resources/"
        };

        private const string k_AssetsFolder = "Assets/";

        private const string k_PackagesFolder = "Packages/";

        private const int k_MemReliefStride = 1000;
        #endregion

        #region Fields
        private readonly DataContainer m_DataContainer;

        private int m_TotalAssetCount = 0;

        private HashSet<string> m_AutoGroupGeneratorSettingsFiles;

        private Dictionary<string, bool> m_AssetIgnoreCache;

        int m_LoadedAssetCount;
        #endregion

        #region Methods
        public DependencyGraphCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;

            Title = nameof(DependencyGraphCommandQueue);
        }

        public override void PreExecute()
        {
            ClearQueue();

            m_DataContainer.DependencyGraph = new DependencyGraph();

            m_AutoGroupGeneratorSettingsFiles = FindAutoGroupGeneratorDependencies();

            m_AssetIgnoreCache = new Dictionary<string, bool>();

            var assetPaths = AssetDatabase.GetAllAssetPaths();

            m_TotalAssetCount = assetPaths.Length;

            foreach (var assetPath in assetPaths)
            {
                var path = assetPath;

                AddCommand(() => AddAssetToDependencyGraph(path), path);
            }

            if (m_DataContainer.Settings.SaveGraphOnDisk)
            {
                AddCommand(SaveGraphOnDisk, "Saving DependencyGraph");
            }
        }

        public override void PostExecute()
        {
            SaveOutputReportToFile();

            m_AutoGroupGeneratorSettingsFiles = null;
            m_AssetIgnoreCache = null;
        }

        private void AddAssetToDependencyGraph(string assetPath)
        {
            var dependencyGraph = m_DataContainer.DependencyGraph;

            if (SkipAsset(assetPath))
            {
                return;
            }


            string[] dependencies = null;
            try
            {
                dependencies = AssetDatabase.GetDependencies(assetPath, false);
            }
            catch
            {
            }

            if (dependencies == null || dependencies.Length == 0)
            {
                dependencyGraph.AddNode(assetPath);

                return;
            }

            foreach (var dependency in dependencies)
            {
                if (SkipAsset(dependency))
                {
                    continue;
                }

                dependencyGraph.AddEdge(assetPath, dependency);
            }
        }

        private bool SkipAsset(string assetPath)
        {
            if (m_AssetIgnoreCache.TryGetValue(assetPath, out var cachedValue))
            {
                return cachedValue;
            }

            bool shouldIgnoreAsset = ShouldIgnoreAsset(assetPath);

            m_AssetIgnoreCache.Add(assetPath, shouldIgnoreAsset);

            return shouldIgnoreAsset;
        }

        private bool ShouldIgnoreAsset(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath) ||
                AssetDatabase.IsValidFolder(assetPath))
            {
                return true;
            }


            if (!assetPath.StartsWith(k_AssetsFolder, StringComparison.OrdinalIgnoreCase) &&
                !assetPath.StartsWith(k_PackagesFolder, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (assetPath.Contains('[') && assetPath.Contains(']')) // Addressable entries cannot contain brackets
                return true;


            var extension = Path.GetExtension(assetPath);

            if (k_IgnoredExtensions.Contains(extension))
            {
                return true;
            }


            foreach (var ignoredPath in k_IgnoredPaths)
            {
                if (assetPath.Contains(ignoredPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }


            if (m_AutoGroupGeneratorSettingsFiles.Contains(assetPath))
            {
                return true;
            }

            if (m_DataContainer.Settings.ScanForUnsupportedFiles)
            {
                var assetSupported = LoadAssetAndCheckSupported(assetPath);

                m_LoadedAssetCount++;
                if (m_LoadedAssetCount % k_MemReliefStride == 0)
                    EditorUtil.UnloadUnusedEditorMemory();

                if (!assetSupported)
                    return true;
            }

            if (m_DataContainer.Settings.ScanForUnsupportedFiles && !LoadAssetAndCheckSupported(assetPath))
            {
                return true;
            }

            return false;
        }

        private HashSet<string> FindAutoGroupGeneratorDependencies()
        {
            var AutoGroupGeneratorSettingsFiles = new HashSet<string>();

            AutoGroupGeneratorSettingsFiles.UnionWith(AssetDatabaseUtil.FindAssetPathsForType<AutoGroupGeneratorSettings>());
            AutoGroupGeneratorSettingsFiles.UnionWith(AssetDatabaseUtil.FindAssetPathsForType<InputRule>());
            AutoGroupGeneratorSettingsFiles.UnionWith(AssetDatabaseUtil.FindAssetPathsForType<OutputRule>());

            return AutoGroupGeneratorSettingsFiles;
        }

        private bool LoadAssetAndCheckSupported(string assetPath)
        {
            try
            {
                Type mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);

                if (mainAssetType == null || mainAssetType == typeof(DefaultAsset))
                {
                    return false;
                }


                if (!AreAssetFlagsEligibleForBuild(assetPath))
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        private bool AreAssetFlagsEligibleForBuild(string path)
        {
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);

            if (asset == null)
            {
                return false;
            }


            HideFlags flags = asset.hideFlags;

            return (flags & HideFlags.DontSave) == 0 &&
                   (flags & HideFlags.DontSaveInBuild) == 0 &&
                   (flags & HideFlags.DontSaveInEditor) == 0;
        }

        private void SaveGraphOnDisk()
        {
            var data = JsonUtility.ToJson(m_DataContainer.DependencyGraph.Serialize());

            FileUtils.SaveToFile(Constants.FilePaths.DependencyGraphFilePath, data);
        }

        void SaveOutputReportToFile()
        {
            if (!m_DataContainer.Settings.ProcessReport.HasFlag(ProcessStepReport.DependencyGraph))
                return;

            Graph<string> graph = m_DataContainer.DependencyGraph.ConvertNodeType(n => n.AssetPath);
            var summary = $"AssetDatabase.GetAllAssetPaths().Count = {m_TotalAssetCount}, " +
                          $" DependencyGraph.NodeCount = {graph.NodeCount})";
            var data = graph.ToSerializableData();

            JsonReport.SaveJsonReport(GetType(), summary, data);
        }
        #endregion
    }
}
