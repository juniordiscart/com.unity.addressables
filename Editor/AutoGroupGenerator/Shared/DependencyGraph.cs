using System;
using System.Collections.Generic;
using UnityEditor;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Graph of asset dependencies used by AutoGroupGenerator.
    /// </summary>
    public class DependencyGraph : Graph<AssetNode>
    {
        #region Types
        /// <summary>
        /// Serializable payload for persisting dependency graphs.
        /// </summary>
        [Serializable]
        public class SerializedData
        {
            #region Fields
            /// <summary>
            /// The serialized graph representation.
            /// </summary>
            public Graph<int>.SerializableData Graph = new();

            /// <summary>
            /// Mapping of GUID strings to serialized indices.
            /// </summary>
            public List<SerializableKeyValue> IndexEntries = new();
            #endregion
        }

        /// <summary>
        /// Serializable mapping entry for GUID indices.
        /// </summary>
        [Serializable]
        public class SerializableKeyValue
        {
            /// <summary>
            /// GUID key string.
            /// </summary>
            public string Key;

            /// <summary>
            /// Integer index assigned to the GUID.
            /// </summary>
            public int Value;
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Rehydrates a <see cref="DependencyGraph"/> from serialized data.
        /// </summary>
        /// <param name="serializedData">Serialized graph payload.</param>
        /// <returns>The deserialized dependency graph.</returns>
        public static DependencyGraph Deserialize(SerializedData serializedData)
        {
            var invertedDictionary = new Dictionary<int, string>();

            foreach (var entry in serializedData.IndexEntries)
            {
                invertedDictionary.Add(entry.Value, entry.Key);
            }

            Graph<AssetNode> graph = Graph<int>.FromSerializableData(serializedData.Graph)
                .ConvertNodeType(ConvertIndexToGuid);

            return new DependencyGraph(graph);

            AssetNode ConvertIndexToGuid(int nodeIndex)
            {
                var guidString = invertedDictionary[nodeIndex];

                return AssetNode.FromGuidString(guidString);
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Initializes an empty dependency graph.
        /// </summary>
        public DependencyGraph()
        {
        }

        /// <summary>
        /// Initializes the graph from an existing <see cref="Graph{T}"/>.
        /// </summary>
        /// <param name="graph">Source graph to copy.</param>
        public DependencyGraph(Graph<AssetNode> graph)
        {
            FromGraph(graph);
        }

        /// <summary>
        /// Adds a dependency edge between two asset paths.
        /// </summary>
        /// <param name="pathA">Source asset path.</param>
        /// <param name="pathB">Dependent asset path.</param>
        public void AddEdge(string pathA, string pathB)
        {
            var guidA = AssetDatabase.GUIDFromAssetPath(pathA);
            var guidB = AssetDatabase.GUIDFromAssetPath(pathB);

            base.AddEdge(new AssetNode(guidA), new AssetNode(guidB));
        }

        /// <summary>
        /// Adds a node for the asset path.
        /// </summary>
        /// <param name="path">Asset path to add.</param>
        public void AddNode(string path)
        {
            var guid = AssetDatabase.GUIDFromAssetPath(path);

            base.AddNode(new AssetNode(guid));
        }

        /// <summary>
        /// Counts outgoing edges from the specified node.
        /// </summary>
        /// <param name="node">Node to count edges for.</param>
        /// <returns>The number of outgoing edges.</returns>
        public int CountOutgoingEdges(AssetNode node)
        {
            return GetNeighbors(node).Count;
        }

        private void FromGraph(Graph<AssetNode> graph)
        {
            _adjacencyList = new Dictionary<AssetNode, List<AssetNode>>();

            foreach (var node in graph.GetAllNodes())
            {
                _adjacencyList.Add(node, graph.GetNeighbors(node));
            }
        }

        /// <summary>
        /// Serializes the graph into a persistable payload.
        /// </summary>
        /// <returns>The serialized graph data.</returns>
        public SerializedData Serialize()
        {
            var serializedData = new SerializedData();

            int index = 0;

            serializedData.Graph = ConvertNodeType(ConvertGuidToIndex).ToSerializableData();

            return serializedData;

            int ConvertGuidToIndex(AssetNode node)
            {
                var guidString = node.Guid.ToString();

                var existingEntry = serializedData.IndexEntries.Find(kv => kv.Key == guidString);
                if (existingEntry != null)
                {
                    return existingEntry.Value;
                }


                index++;

                serializedData.IndexEntries.Add(new SerializableKeyValue
                {
                    Key = guidString,
                    Value = index,
                });

                return index;
            }
        }
        #endregion
    }
}
