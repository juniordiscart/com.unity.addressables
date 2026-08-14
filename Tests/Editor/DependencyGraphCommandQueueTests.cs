using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AutoGroupGenerator;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Tests
{
    /// <summary>
    /// Regression tests for dependency graph construction when every direct dependency is skipped
    /// (e.g. .txt, .cs) so no AddEdge runs—the source asset must still appear as a node.
    /// </summary>
    internal class DependencyGraphCommandQueueTests
    {
        const string k_TestRootFolder = "Assets/DepGraphCmdQueue2049_TestAssets";

        [Serializable]
        class HolderForTxtDependency : ScriptableObject
        {
            public TextAsset Content;
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(k_TestRootFolder))
            {
                AssetDatabase.DeleteAsset(k_TestRootFolder);
            }
        }

        static void InitQueuePrivateStateForTests(DependencyGraphCommandQueue queue)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = typeof(DependencyGraphCommandQueue);
            type.GetField("m_AssetIgnoreCache", flags).SetValue(queue, new Dictionary<string, bool>());
            type.GetField("m_AutoGroupGeneratorSettingsFiles", flags)
                .SetValue(queue, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            type.GetField("m_LoadedAssetCount", flags).SetValue(queue, 0);
        }

        [Test]
        public void AddAssetToDependencyGraph_WhenAllDirectDependenciesAreSkipped_StillAddsNodeForAsset()
        {
            if (AssetDatabase.IsValidFolder(k_TestRootFolder))
            {
                AssetDatabase.DeleteAsset(k_TestRootFolder);
            }

            AssetDatabase.CreateFolder("Assets", Path.GetFileName(k_TestRootFolder));

            var txtPath = k_TestRootFolder + "/content.txt";
            File.WriteAllText(txtPath, "dependency content for DepGraph test");
            AssetDatabase.ImportAsset(txtPath);

            var holderPath = k_TestRootFolder + "/holder.asset";
            var holder = ScriptableObject.CreateInstance<HolderForTxtDependency>();
            holder.Content = AssetDatabase.LoadAssetAtPath<TextAsset>(txtPath);
            AssetDatabase.CreateAsset(holder, holderPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var deps = AssetDatabase.GetDependencies(holderPath, false);
            Assert.Greater(deps.Length, 0,
                "Precondition: holder should list at least one dependency so we exercise the all-skipped branch.");

            var settings = ScriptableObject.CreateInstance<AutoGroupGeneratorSettings>();

            var dataContainer = new DataContainer
            {
                DependencyGraph = new DependencyGraph(),
                Settings = settings
            };

            var queue = new DependencyGraphCommandQueue(dataContainer);
            InitQueuePrivateStateForTests(queue);

            queue.AddAssetToDependencyGraph(holderPath);

            var holderNode = AssetNode.FromAssetPath(holderPath);
            Assert.NotNull(holderNode);
            Assert.That(dataContainer.DependencyGraph.GetAllNodes(), Contains.Item(holderNode),
                "Asset must remain in the graph when every listed dependency is skipped by SkipAsset.");
        }
    }
}
