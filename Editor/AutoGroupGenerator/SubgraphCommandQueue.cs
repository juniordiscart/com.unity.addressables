using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Command queue that identifies subgraphs within the dependency graph.
    /// </summary>
    internal class SubgraphCommandQueue : CommandQueue
    {
        #region Static Methods
        public static int CalculateHashForSources(IEnumerable<AssetNode> sources)
        {
            unchecked
            {
                const uint seed = 2166136261u; // FNV-1a 32-bit seed
                const uint prime = 16777619u;

                uint hash = seed;

                foreach (var guid in sources
                             .Select(s => s.Guid.ToString()) // Unity GUID is always 32 hex chars
                             .OrderBy(g => g, StringComparer.Ordinal))
                {
                    for (int i = 0; i < guid.Length; i++)
                    {
                        hash ^= guid[i];
                        hash *= prime;
                    }
                }

                return (int)hash;
            }
        }
        #endregion

        #region Fields
        private readonly DataContainer m_DataContainer;

        private Graph<AssetNode> m_TransposedGraph;

        private Graph<SuperNode> m_Dag;

        private Graph<SuperNode> m_TransposedDag;

        private Dictionary<AssetNode, SuperNode> m_NodeToSuperNodeMap;
        Dictionary<SuperNode, HashSet<AssetNode>> m_SuperNodeToSources;
        Dictionary<SuperNode, HashSet<AssetNode>> m_SuperNodeToPathAssets;

        HashSet<AssetNode> m_InputAssets;

        #endregion

        #region Methods
        public SubgraphCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;

            Title = nameof(SubgraphCommandQueue);
        }

        public override void PreExecute()
        {
            ClearQueue();

            m_DataContainer.Subgraphs = new Dictionary<int, Subgraph>();
            var nodes = m_DataContainer.DependencyGraph.GetAllNodes();
            InitializeDag();

            var info = "Materializing SuperNode Maps... ";
            foreach (var s in m_Dag.GetAllNodes())
            {
                var localS = s;
                AddCommand(() => MapSuperNode(localS), info);
            }

            foreach (var node in nodes)
            {
                var localNode = node;

                AddCommand(() => AddNodeToSubgraph(localNode), localNode.FileName);
            }
        }

        public override void PostExecute()
        {
            SaveOutputReportToFile();
            UnInitialize();
        }

        private void InitializeDag()
        {
             m_InputAssets = m_DataContainer.InputAssets.Select(AssetNode.FromAssetPath).ToHashSet();

            m_Dag = m_DataContainer.DependencyGraph.CollapseToSCCs();
            m_TransposedDag = m_Dag.GetTransposedGraph();

            m_NodeToSuperNodeMap = new();
            m_SuperNodeToSources = new Dictionary<SuperNode, HashSet<AssetNode>>();
            m_SuperNodeToPathAssets = new Dictionary<SuperNode, HashSet<AssetNode>>();

            foreach (var superNode in m_Dag.GetAllNodes())
            {
                foreach (var asset in superNode.Nodes)
                {
                    m_NodeToSuperNodeMap[asset] = superNode;
                }
            }
        }

        void UnInitialize()
        {
            m_InputAssets = null;
            m_Dag = null;
            m_TransposedDag = null;
            m_NodeToSuperNodeMap = null;
            m_SuperNodeToSources = null;
            m_SuperNodeToPathAssets = null;
        }

        void MapSuperNode(SuperNode s)
        {
            m_TransposedDag.FindPathAndLeaves(s, out var path, out var superSources);

            var pathAssets = GetAllAssetNodes(path);
            var sourcesAssets = GetAllAssetNodes(superSources);

            m_SuperNodeToPathAssets.Add(s, pathAssets);
            m_SuperNodeToSources.Add(s, sourcesAssets);
        }

        private void AddNodeToSubgraph(AssetNode node)
        {
            bool sourceFound = TryFindSourcesForNode_DAG(node, out var sources);

            if (!sourceFound)
            {
                return;
            }


            if (sources == null || sources.Count == 0)
            {
                throw new Exception($"Cannot find source nodes for node = {node.FileName}");
            }


            int hash = CalculateHashForSources(sources);

            if (m_DataContainer.Subgraphs.TryGetValue(hash, out var existingSubgraph))
            {
                if (!existingSubgraph.Sources.SetEquals(sources))
                {
                    throw new Exception($"Hash collision = inconsistent sources for subgraph {hash}");
                }
            }
            else
            {

                var newSubgraph = new Subgraph
                {
                    Sources = sources,
                    HashOfSources = hash
                };

                m_DataContainer.Subgraphs.Add(hash, newSubgraph);
            }


            bool result = m_DataContainer.Subgraphs[hash].Nodes.Add(node);


            if (!result)
            {
                throw new Exception($"Unknown Error = node = {node} had added to subgraph ={hash} before");
            }
        }

        private bool TryFindSourcesForNode_DAG(AssetNode targetNode, out HashSet<AssetNode> sources)
        {
            if (!m_NodeToSuperNodeMap.TryGetValue(targetNode, out var targetSuperNode))
            {
                throw new InvalidOperationException("AssetNode not found in collapsed DAG.");
            }

            if (m_SuperNodeToPathAssets[targetSuperNode].Overlaps(m_InputAssets))
            {
                sources = m_SuperNodeToSources[targetSuperNode];
                return true;
            }
            sources = null;
            return false;
        }

        private HashSet<AssetNode> GetAllAssetNodes(HashSet<SuperNode> superNodes)
        {
            var assetNodes = new HashSet<AssetNode>();

            foreach (var superNode in superNodes)
            {
                assetNodes.UnionWith(superNode.Nodes);
            }

            return assetNodes;
        }

        void SaveOutputReportToFile()
        {
            if (!m_DataContainer.Settings.ProcessReport.HasFlag(ProcessStepReport.SubGraphs))
                return;

            var summary = $"Subgraphs.Count = {m_DataContainer.Subgraphs.Count} ";
            var data = new List<Subgraph>(m_DataContainer.Subgraphs.Values);

            JsonReport.SaveJsonReport(GetType(), summary, data);
        }

        #endregion
    }
}
