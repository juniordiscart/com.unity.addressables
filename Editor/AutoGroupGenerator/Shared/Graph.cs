using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Generic directed graph with traversal and analysis helpers.
    /// </summary>
    /// <typeparam name="T">The node value type stored in the graph.</typeparam>
    [Serializable]
    public class Graph<T> where T : IEquatable<T>
    {
        #region Types
        /// <summary>
        /// Serializable representation of a node and its neighbors.
        /// </summary>
        [Serializable]
        public class SerializableNode
        {
            /// <summary>
            /// The node value.
            /// </summary>
            public T Node;

            /// <summary>
            /// The neighboring node values.
            /// </summary>
            public List<T> Neighbors = new();
        }

        /// <summary>
        /// Serializable payload for an entire graph.
        /// </summary>
        [Serializable]
        public class SerializableData
        {
            /// <summary>
            /// Collection of serialized nodes.
            /// </summary>
            public List<SerializableNode> Nodes = new();
        }
        #endregion

        #region Fields
        /// <summary>
        /// Adjacency list mapping nodes to outgoing neighbors.
        /// </summary>
        [NonSerialized]
        protected Dictionary<T, List<T>> _adjacencyList;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the number of nodes in the graph.
        /// </summary>
        public int NodeCount => _adjacencyList.Count;
        #endregion

        #region Methods
        /// <summary>
        /// Initializes an empty graph.
        /// </summary>
        public Graph()
        {
            _adjacencyList = new Dictionary<T, List<T>>();
        }

        /// <summary>
        /// Adds an edge between two nodes, adding missing nodes as needed.
        /// </summary>
        /// <param name="fromNode">The source node.</param>
        /// <param name="toNode">The destination node.</param>
        public virtual void AddEdge(T fromNode, T toNode)
        {
            AddNode(fromNode);
            AddNode(toNode);

            _adjacencyList[fromNode].Add(toNode);
        }

        /// <summary>
        /// Adds a node to the graph if it does not already exist.
        /// </summary>
        /// <param name="node">The node to add.</param>
        public virtual void AddNode(T node)
        {
            if (!_adjacencyList.ContainsKey(node))
            {
                _adjacencyList[node] = new List<T>();
            }
        }

        /// <summary>
        /// Gets the neighbors for the specified node.
        /// </summary>
        /// <param name="node">The node to query.</param>
        /// <returns>A list of neighboring nodes.</returns>
        public List<T> GetNeighbors(T node)
        {
            return _adjacencyList.TryGetValue(node, out List<T> neighbors) ?
                neighbors : new List<T>();
        }

        /// <summary>
        /// Gets all nodes in the graph.
        /// </summary>
        /// <returns>A list of all nodes.</returns>
        public List<T> GetAllNodes()
        {
            return new List<T>(_adjacencyList.Keys);
        }

        /// <summary>
        /// Builds the transposed graph with all edges reversed.
        /// </summary>
        /// <returns>The transposed graph.</returns>
        public Graph<T> GetTransposedGraph()
        {
            var transposed = new Graph<T>();

            foreach (var node in _adjacencyList.Keys)
            {
                transposed.AddNode(node);
            }

            foreach (var from in _adjacencyList)
            {
                foreach (var to in from.Value)
                {
                    transposed.AddEdge(to, from.Key);
                }
            }

            return transposed;
        }

        /// <summary>
        /// Serializes the graph into a data transfer object.
        /// </summary>
        /// <returns>The serializable data.</returns>
        public SerializableData ToSerializableData()
        {
            var data = new SerializableData();

            foreach (var kvp in _adjacencyList)
            {
                data.Nodes.Add(new SerializableNode
                {
                    Node = kvp.Key,
                    Neighbors = new List<T>(kvp.Value)
                });
            }

            return data;
        }

        /// <summary>
        /// Rehydrates a graph from serialized data.
        /// </summary>
        /// <param name="data">The serialized graph data.</param>
        /// <returns>The reconstructed graph.</returns>
        public static Graph<T> FromSerializableData(SerializableData data)
        {
            var graph = new Graph<T>();

            graph._adjacencyList.Clear();

            foreach (var serializedNode in data.Nodes)
            {
                graph._adjacencyList[serializedNode.Node] = serializedNode.Neighbors != null
                    ? new List<T>(serializedNode.Neighbors)
                    : new List<T>();
            }

            return graph;
        }

        /// <summary>
        /// Finds all leaf nodes reachable from the given node.
        /// </summary>
        /// <param name="node">The node to start from.</param>
        /// <returns>The set of reachable leaf nodes.</returns>
        public HashSet<T> FindLeafNodes(T node)
        {
            var visited = new HashSet<T>();
            var roots = new HashSet<T>();

            void Dfs(T current)
            {
                if (visited.Contains(current))
                {
                    return;
                }

                visited.Add(current);

                var neighbors = GetNeighbors(current);
                if (neighbors == null || neighbors.Count == 0)
                {
                    roots.Add(current);

                    return;
                }

                foreach (var neighbor in neighbors)
                {
                    Dfs(neighbor);
                }
            }

            Dfs(node);

            return roots;
        }

        /// <summary>
        /// Finds every node along paths from the starting node to leaves.
        /// </summary>
        /// <param name="node">The node to start from.</param>
        /// <returns>All nodes encountered along paths to leaves.</returns>
        public HashSet<T> FindPathConeToLeaves(T node)
        {
            var visited = new HashSet<T>();
            var result = new HashSet<T>();

            void Dfs(T current)
            {
                if (visited.Contains(current))
                {
                    return;
                }

                visited.Add(current);
                result.Add(current);

                var neighbors = GetNeighbors(current);
                if (neighbors == null || neighbors.Count == 0)
                {
                    return;
                }

                foreach (var neighbor in neighbors)
                {
                    Dfs(neighbor);
                }
            }

            Dfs(node);

            return result;
        }

        /// <summary>
        /// Finds nodes along paths from the starting node and the leaf nodes reached.
        /// </summary>
        /// <param name="node">The node to start from.</param>
        /// <param name="path">Outputs the nodes along the traversed paths.</param>
        /// <param name="leaves">Outputs the leaf nodes that were reached.</param>
        public void FindPathAndLeaves(T node, out HashSet<T> path, out HashSet<T> leaves)
        {
            path = new HashSet<T>();
            leaves = new HashSet<T>();
            var visited = new HashSet<T>();

            FindPathAndLeavesRecursive(node, path, leaves, visited);
        }


        private void FindPathAndLeavesRecursive(T current, HashSet<T> path, HashSet<T> leaves, HashSet<T> visited)
        {
            if (visited.Contains(current))
            {
                return;
            }

            visited.Add(current);
            path.Add(current);

            var neighbors = GetNeighbors(current);
            if (neighbors == null || neighbors.Count == 0)
            {
                leaves.Add(current);

                return;
            }

            foreach (var neighbor in neighbors)
            {
                FindPathAndLeavesRecursive(neighbor, path, leaves, visited);
            }
        }

        /// <summary>
        /// Finds all paths from the start node to any leaf node.
        /// </summary>
        /// <param name="startNode">The node to start from.</param>
        /// <returns>A list of paths, where each path is a list of nodes.</returns>
        public List<List<T>> FindAllPathsToLeafNodes(T startNode)
        {
            var allPaths = new List<List<T>>();

            Stack<Tuple<T, List<T>>> stack = new Stack<Tuple<T, List<T>>>();

            stack.Push(Tuple.Create(startNode, new List<T> { startNode }));

            while (stack.Count > 0)
            {
                var (currentNode, currentPath) = stack.Pop();

                var neighbors = GetNeighbors(currentNode);

                if (neighbors.Count == 0)
                {
                    allPaths.Add(new List<T>(currentPath));

                    continue;
                }

                foreach (var neighbor in neighbors)
                {
                    if (!currentPath.Contains(neighbor)) // Avoid revisiting nodes in the current path
                    {
                        List<T> newPath = new List<T>(currentPath) { neighbor };
                        stack.Push(Tuple.Create(neighbor, newPath));
                    }
                    else
                    {

                        allPaths.Add(new List<T>(currentPath));
                    }
                }
            }

            return allPaths;
        }

        /// <summary>
        /// Performs a depth-first traversal starting at the given node.
        /// </summary>
        /// <param name="startNode">The node to start from.</param>
        /// <param name="onVisit">Callback invoked when a node is visited.</param>
        public void DepthFirstSearch(T startNode, Action<T> onVisit)
        {
            var visited = new HashSet<T>();

            DepthFirstSearchIterative(startNode, visited, onVisit);
        }

        /// <summary>
        /// Performs a depth-first traversal with a provided visited set.
        /// </summary>
        /// <param name="startNode">The node to start from.</param>
        /// <param name="visited">Set of already-visited nodes.</param>
        /// <param name="onVisit">Callback invoked when a node is visited.</param>
        public void DepthFirstSearchIterative(T startNode, HashSet<T> visited, Action<T> onVisit)
        {
            var stack = new Stack<T>();

            stack.Push(startNode);

            while (stack.Count > 0)
            {
                T node = stack.Pop();

                if (!visited.Contains(node))
                {
                    visited.Add(node);

                    onVisit?.Invoke(node);

                    foreach (var neighbor in GetNeighbors(node))
                    {
                        if (!visited.Contains(neighbor))
                        {
                            stack.Push(neighbor);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds all paths between a start and end node using DFS.
        /// </summary>
        /// <param name="startNode">The node to start from.</param>
        /// <param name="endNode">The node to end at.</param>
        /// <returns>A list of paths between the nodes.</returns>
        public List<List<T>> DepthFirstSearchForAllPaths(T startNode, T endNode)
        {
            var allPaths = new List<List<T>>();

            var stack = new Stack<Tuple<T, List<T>>>();

            stack.Push(Tuple.Create(startNode, new List<T> { startNode }));

            while (stack.Count > 0)
            {
                var (currentNode, currentPath) = stack.Pop();

                if (EqualityComparer<T>.Default.Equals(currentNode, endNode))
                {
                    allPaths.Add(new List<T>(currentPath));
                }

                foreach (var neighbor in GetNeighbors(currentNode))
                {
                    if (!currentPath.Contains(neighbor))
                    {
                        var newPath = new List<T>(currentPath) { neighbor };

                        stack.Push(Tuple.Create(neighbor, newPath));
                    }
                }
            }

            return allPaths;
        }

        /// <summary>
        /// Finds all paths from a start node to nodes matching a condition.
        /// </summary>
        /// <param name="startNode">The node to start from.</param>
        /// <param name="endNodeCondition">Predicate identifying acceptable end nodes.</param>
        /// <returns>A list of matching paths.</returns>
        public List<List<T>> DepthFirstSearchForAllPaths(T startNode, Predicate<T> endNodeCondition)
        {
            var allPaths = new List<List<T>>();

            var stack = new Stack<Tuple<T, List<T>>>();

            stack.Push(Tuple.Create(startNode, new List<T> { startNode }));

            while (stack.Count > 0)
            {
                var (currentNode, currentPath) = stack.Pop();

                if (endNodeCondition(currentNode))
                {
                    allPaths.Add(new List<T>(currentPath));
                }

                foreach (var neighbor in GetNeighbors(currentNode))
                {
                    if (!currentPath.Contains(neighbor))
                    {
                        var newPath = new List<T>(currentPath) { neighbor };

                        stack.Push(Tuple.Create(neighbor, newPath));
                    }
                    else
                    {

                        allPaths.Add(new List<T>(currentPath));
                    }
                }
            }

            return allPaths;
        }

        /// <summary>
        /// Performs a breadth-first traversal starting at the given node.
        /// </summary>
        /// <param name="startNode">The node to start from.</param>
        /// <param name="onVisit">Callback invoked when a node is visited.</param>
        public void BreadthFirstSearch(T startNode, Action<T> onVisit)
        {
            var visited = new HashSet<T>();

            var queue = new Queue<T>();

            visited.Add(startNode);

            queue.Enqueue(startNode);

            while (queue.Count > 0)
            {
                T node = queue.Dequeue();

                onVisit?.Invoke(node);

                foreach (var neighbor in GetNeighbors(node))
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);

                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        /// <summary>
        /// Converts the graph to use a different node type.
        /// </summary>
        /// <typeparam name="TResult">The destination node type.</typeparam>
        /// <param name="converter">Converter from the source node type to the destination type.</param>
        /// <returns>A graph with converted node values.</returns>
        public Graph<TResult> ConvertNodeType<TResult>(Func<T, TResult> converter) where TResult : IEquatable<TResult>
        {
            var destinationAdjacencyList = new Dictionary<TResult, List<TResult>>();

            foreach (var node in _adjacencyList)
            {
                TResult newNode = converter(node.Key);

                var neighbors = new List<TResult>();

                foreach (var neighbor in node.Value)
                {

                    neighbors.Add(converter(neighbor));
                }

                destinationAdjacencyList.Add(newNode, neighbors);
            }

            return new Graph<TResult>
            {
                _adjacencyList = destinationAdjacencyList
            };
        }

        /// <summary>
        /// Creates a subgraph containing only the specified nodes.
        /// </summary>
        /// <param name="subgraphNodes">Nodes to include in the subgraph.</param>
        /// <returns>The resulting subgraph.</returns>
        public Graph<T> GetSubgraph(List<T> subgraphNodes)
        {
            var subgraphAdjacencyList = new Dictionary<T, List<T>>();

            foreach (T node in subgraphNodes)
            {
                if (_adjacencyList.TryGetValue(node, out var sourceNeighbors))
                {
                    List<T> neighbors = sourceNeighbors.Where(subgraphNodes.Contains).ToList();

                    subgraphAdjacencyList.Add(node, neighbors);
                }
            }

            return new Graph<T>
            {
                _adjacencyList = subgraphAdjacencyList
            };
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Converts a directed graph into an undirected graph by mirroring edges.
        /// </summary>
        /// <param name="directedGraph">The directed graph to convert.</param>
        /// <returns>An undirected graph.</returns>
        public static Graph<T> ToUndirected(Graph<T> directedGraph)
        {
            Graph<T> undirectedGraph = new Graph<T>();

            foreach (var node in directedGraph._adjacencyList.Keys)
            {
                foreach (var neighbor in directedGraph._adjacencyList[node])
                {
                    undirectedGraph.AddEdge(node, neighbor);
                    undirectedGraph.AddEdge(neighbor, node);
                }
            }

            return undirectedGraph;
        }

        /// <summary>
        /// Removes a node and its edges from an undirected graph.
        /// </summary>
        /// <param name="undirectedGraph">The graph to modify.</param>
        /// <param name="targetNode">The node to remove.</param>
        public static void RemoveNodeFromUndirectedGraph(Graph<T> undirectedGraph, T targetNode)
        {
            var neighbors = undirectedGraph.GetNeighbors(targetNode);

            if (neighbors.Count > 0)
            {
                foreach (var neighbor in neighbors)
                {
                    var neighborsOfNeighbor = undirectedGraph.GetNeighbors(neighbor);

                    if (neighborsOfNeighbor.Count > 0)
                    {
                        neighborsOfNeighbor.Remove(targetNode);
                    }
                }
            }


            undirectedGraph._adjacencyList.Remove(targetNode);
        }

        /// <summary>
        /// Returns the connected components of an undirected graph.
        /// </summary>
        /// <param name="undirectedGraph">The undirected graph to analyze.</param>
        /// <returns>A list of connected components.</returns>
        public static List<List<T>> GetConnectedComponentsOfUndirectedGraph(Graph<T> undirectedGraph)
        {
            var nodes = undirectedGraph.GetAllNodes();

            var visited = new HashSet<T>();

            var components = new List<List<T>>();

            foreach (var node in nodes)
            {
                if (!visited.Contains(node))
                {

                    var component = new List<T>();

                    undirectedGraph.DepthFirstSearchIterative(node, visited, (currentNode) => component.Add(currentNode));

                    components.Add(component);
                }
            }

            return components;
        }

        /// <summary>
        /// Collapses strongly connected components into a DAG of <see cref="SuperNode"/> instances.
        /// </summary>
        /// <returns>A graph whose nodes represent strongly connected components.</returns>
        public Graph<SuperNode> CollapseToSCCs()
        {
            int index = 0;
            var indices = new Dictionary<T, int>();
            var lowlinks = new Dictionary<T, int>();
            var onStack = new HashSet<T>();
            var stack = new Stack<T>();
            var rawSCCs = new List<HashSet<T>>();

            void StrongConnect(T node)
            {
                indices[node] = index;
                lowlinks[node] = index;
                index++;
                stack.Push(node);
                onStack.Add(node);

                foreach (var neighbor in GetNeighbors(node))
                {
                    if (!indices.ContainsKey(neighbor))
                    {
                        StrongConnect(neighbor);
                        lowlinks[node] = Math.Min(lowlinks[node], lowlinks[neighbor]);
                    }
                    else if (onStack.Contains(neighbor))
                    {
                        lowlinks[node] = Math.Min(lowlinks[node], indices[neighbor]);
                    }
                }

                if (lowlinks[node] == indices[node])
                {
                    var scc = new HashSet<T>();
                    T w;
                    do
                    {
                        w = stack.Pop();
                        onStack.Remove(w);
                        scc.Add(w);
                    } while (!EqualityComparer<T>.Default.Equals(w, node));

                    rawSCCs.Add(scc);
                }
            }

            foreach (var node in GetAllNodes())
            {
                if (!indices.ContainsKey(node))
                    StrongConnect(node);
            }

            var nodeToSuper = new Dictionary<T, SuperNode>();
            var superNodes = new List<SuperNode>();

            foreach (var scc in rawSCCs)
            {
                var assetNodes = scc.Cast<AssetNode>(); // Assumes T is AssetNode
                var superNode = new SuperNode(assetNodes);
                superNodes.Add(superNode);
                foreach (var node in scc)
                {
                    nodeToSuper[node] = superNode;
                }
            }

            var dag = new Graph<SuperNode>();
            foreach (var super in superNodes)
            {
                dag.AddNode(super);
            }

            foreach (var from in _adjacencyList.Keys)
            {
                foreach (var to in _adjacencyList[from])
                {
                    var fromSuper = nodeToSuper[from];
                    var toSuper = nodeToSuper[to];
                    if (!ReferenceEquals(fromSuper, toSuper))
                    {
                        dag.AddEdge(fromSuper, toSuper);
                    }
                }
            }

            return dag;
        }
        #endregion
    }
}
