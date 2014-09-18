using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphTools.Graph
{
    class MultiDirectedGraph<TNode, TLabel>
    {
        // Set of nodes
        private HashSet<TNode> nodes = new HashSet<TNode>();

        // Set of edges
        private HashSet<int> edges = new HashSet<int>();

        // Function mapping edge to source
        private Dictionary<int, TNode> source = new Dictionary<int, TNode>();

        // Function mapping edge to target
        private Dictionary<int, TNode> target = new Dictionary<int, TNode>();

        // Function mapping node to outgoing edges
        private Dictionary<TNode, HashSet<int>> outgoing = new Dictionary<TNode, HashSet<int>>();

        // Function mapping node to incoming edges
        private Dictionary<TNode, HashSet<int>> incoming = new Dictionary<TNode, HashSet<int>>();

        // Function mapping implicit edge to count
        private Dictionary<Tuple<TNode, TNode>, int> edgeCounts = new Dictionary<Tuple<TNode, TNode>, int>();

        // Function mapping implicit edge with label to count
        private Dictionary<Tuple<TNode, TNode, TLabel>, int> edgeWithLabelCounts = new Dictionary<Tuple<TNode, TNode, TLabel>, int>();

        // Function mapping node to attribute (label)
        private Dictionary<TNode, TLabel> nodeLabel = new Dictionary<TNode, TLabel>();

        // Function mapping node labels to counts
        private Dictionary<TLabel, int> nodeLabelCounts = new Dictionary<TLabel, int>();

        // Function mapping edge to attribute (label)
        private Dictionary<int, TLabel> edgeLabel = new Dictionary<int, TLabel>();

        // Function mapping edge labels to counts
        private Dictionary<TLabel, int> edgeLabelCounts = new Dictionary<TLabel, int>();

        // Unique edge counter
        private int counter = 0;

        /// <summary>
        /// Gets all the neighbors of the specified node.
        /// A neighbor occurs as often as there are edges connecting it to the specified node.
        /// </summary>
        /// <param name="node">Node to get all neighbors of.</param>
        /// <returns>Multiset of neighbors.</returns>
        public IEnumerable<TNode> Neighbors(TNode node)
        {
            foreach (var eo in outgoing[node])
            {
                yield return target[eo];
            }

            foreach (var ei in incoming[node])
            {
                yield return source[ei];
            }
        }

        /// <summary>
        /// Copies all ingoing and outgoing edges of each node to a single representative node.
        /// </summary>
        /// <param name="nodes">Nodes to merge. Make sure it is materialized after taking a collection from this instance.</param>
        public void MergeNodes(IEnumerable<TNode> nodes)
        {
            // Pick a node to represent the nodes to be merged
            var representative = nodes.First();

            foreach (var node in nodes)
            {
                if (!node.Equals(representative))
                {
                    foreach (var eo in outgoing[node])
                    {
                        AddEdge(representative, target[eo], edgeLabel[eo]);
                    }

                    foreach (var ei in incoming[node])
                    {
                        AddEdge(source[ei], representative, edgeLabel[ei]);
                    }

                    RemoveNode(node);
                }
            }
        }

        /// <summary>
        /// Gets a boolean indicating whether this graph is directed or not.
        /// </summary>
        public bool IsDirected
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Determine the number of edges adjacent to the specified node.
        /// </summary>
        /// <param name="node">Node to get the degree of.</param>
        /// <returns>The number of edges adjacent to the specified node.</returns>
        public int Degree(TNode node)
        {
            return outgoing[node].Count + incoming[node].Count;
        }

        /// <summary>
        /// Get the target node of an edge.
        /// </summary>
        /// <param name="edge">Edge to get the target node of.</param>
        /// <returns>The target node of the specified edge.</returns>
        public TNode Target(int edge)
        {
            return target[edge];
        }

        /// <summary>
        /// Get the source node of an edge.
        /// </summary>
        /// <param name="edge">Edge to get the source node of.</param>
        /// <returns>The source node of the specified edge.</returns>
        public TNode Source(int edge)
        {
            return source[edge];
        }

        /// <summary>
        /// Get the label of a node.
        /// </summary>
        /// <param name="node">Node to retrieve the label of.</param>
        /// <returns>The label of the specified node.</returns>
        public TLabel NodeLabel(TNode node)
        {
            return nodeLabel[node];
        }

        /// <summary>
        /// Get the label of an edge.
        /// </summary>
        /// <param name="edge">Edge to retrieve the label of.</param>
        /// <returns>The label of the specified edge.</returns>
        public TLabel EdgeLabel(int edge)
        {
            return edgeLabel[edge];
        }

        /// <summary>
        /// Sets the label of a node.
        /// </summary>
        /// <param name="node">Node to set the label of.</param>
        /// <param name="label">New label of the node.</param>
        public void SetNodeLabel(TNode node, TLabel label)
        {
            nodeLabel[node] = label;
        }

        /// <summary>
        /// Sets the label of an edge.
        /// </summary>
        /// <param name="edge">Edge to set the label of.</param>
        /// <param name="label">New label of the edge.</param>
        public void SetEdgeLabel(int edge, TLabel label)
        {
            edgeLabel[edge] = label;
        }

        /// <summary>
        /// Induce a subgraph from the given nodes.
        /// </summary>
        /// <param name="nodes">Nodes to induce subgraph on.</param>
        /// <returns>A new graph which is the subgraph of this graph containing the specified nodes and the edges from this graph which have both endpoints in the specified nodes.</returns>
        public MultiDirectedGraph<TNode, TLabel> Induce(IEnumerable<TNode> nodes)
        {
            var induced = new MultiDirectedGraph<TNode, TLabel>();

            // Add nodes
            foreach (var node in nodes)
            {
                induced.AddNode(node, nodeLabel[node]);
            }

            // Add those edges with source and target nodes present in induced graph
            foreach (var s in nodes)
            {
                foreach (var e in outgoing[s])
                {
                    var t = target[e];
                    if (induced.HasNode(t))
                    {
                        induced.AddEdge(s, t, edgeLabel[e]);
                    }
                }
            }

            return induced;
        }

        /// <summary>
        /// Gets or sets the name of the graph.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Get the number of nodes in this graph.
        /// </summary>
        public int NumNodes
        {
            get
            {
                return nodes.Count;
            }
        }

        /// <summary>
        /// Get the number of edges in this graph.
        /// </summary>
        public int NumEdges
        {
            get
            {
                return edges.Count;
            }
        }

        /// <summary>
        /// Get all node labels.
        /// </summary>
        public IEnumerable<TLabel> NodeLabels
        {
            get
            {
                return nodeLabelCounts.Keys;
            }
        }

        /// <summary>
        /// Get all edge labels.
        /// </summary>
        public IEnumerable<TLabel> EdgeLabels
        {
            get
            {
                return edgeLabelCounts.Keys;
            }
        }

        /// <summary>
        /// Get all outgoing edges of a node.
        /// </summary>
        /// <param name="node">The node whose outgoing edges need to be retrieved.</param>
        /// <returns>All outgoing edges of a node.</returns>
        public IEnumerable<int> Out(TNode node)
        {
            return outgoing[node];
        }

        /// <summary>
        /// Get all incoming edges of a node.
        /// </summary>
        /// <param name="node">The node whose incoming edegs need to be retrieved.</param>
        /// <returns>All incoming edges of a node.</returns>
        public IEnumerable<int> In(TNode node)
        {
            return incoming[node];
        }

        /// <summary>
        /// Get all nodes.
        /// </summary>
        public IEnumerable<TNode> Nodes
        {
            get
            {
                return nodes;
            }
        }

        /// <summary>
        /// Get all edges.
        /// </summary>
        public IEnumerable<int> Edges
        {
            get
            {
                return edges;
            }
        }

        /// <summary>
        /// Determine whether the graph contains the given node or not.
        /// </summary>
        /// <param name="node">The node to determine if it is in the graph.</param>
        /// <returns>True if the node is in the graph, false otherwise.</returns>
        public bool HasNode(TNode node)
        {
            return nodes.Contains(node);
        }

        /// <summary>
        /// Attempt to add the node to the graph.
        /// </summary>
        /// <param name="node">The node to add to the graph.</param>
        /// <param name="label">The label of the node to add to the graph.</param>
        /// <returns>True if the node was not yet in the graph and has now been added, false otherwise.</returns>
        public bool AddNode(TNode node, TLabel label = default(TLabel))
        {
            if (nodes.Contains(node))
            {
                return false;
            }
            else
            {
                nodes.Add(node);
                outgoing.Add(node, new HashSet<int>());
                incoming.Add(node, new HashSet<int>());
                nodeLabel.Add(node, label);

                if (!nodeLabelCounts.ContainsKey(label))
                {
                    nodeLabelCounts.Add(label, 0);
                }
                nodeLabelCounts[label] += 1;

                return true;
            }
        }

        /// <summary>
        /// Attempt to remove the node and all its associated edges from the graph.
        /// </summary>
        /// <param name="node">The node to remove from the graph.</param>
        /// <returns>True if the node was in the graph and has now been removed, false otherwise.</returns>
        public bool RemoveNode(TNode node)
        {
            if (nodes.Contains(node))
            {
                var label = nodeLabel[node];
                nodeLabelCounts[label] -= 1;
                if (nodeLabelCounts[label] == 0)
                {
                    nodeLabelCounts.Remove(label);
                }

                var outgoingEdges = outgoing[node].ToArray();
                var incomingEdges = incoming[node].ToArray();

                foreach (var edge in outgoingEdges)
                {
                    RemoveEdge(edge);
                }

                foreach (var edge in incomingEdges)
                {
                    RemoveEdge(edge);
                }

                outgoing.Remove(node);
                incoming.Remove(node);
                nodeLabel.Remove(node);
                nodes.Remove(node);

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determine whether the graph contains the given edge or not.
        /// </summary>
        /// <param name="edge">The edge to determine if it is in the graph.</param>
        /// <returns>True if the edge is in the graph, false otherwise.</returns>
        public bool HasEdge(int edge)
        {
            return edges.Contains(edge);
        }

        /// <summary>
        /// Determine whether the graph contains an edge matching the specified source and target nodes.
        /// </summary>
        /// <param name="s">Source node of potential edges.</param>
        /// <param name="t">Target node of potential edges.</param>
        /// <returns>True if an edge exists which has the source and target nodes, false otherwise.</returns>
        public bool HasEdge(TNode s, TNode t)
        {
            var @implicit = Tuple.Create(s, t);
            return edgeCounts.ContainsKey(@implicit);
        }

        /// <summary>
        /// Determine whether the graph contains an edge matching the specified source and target nodes with the specified label.
        /// </summary>
        /// <param name="s">Source node of potential edges.</param>
        /// <param name="t">Target node of potential edges.</param>
        /// <param name="label">Label of the potential edges.</param>
        /// <returns>True if an edge exists which has the source and target nodes with the label, false otherwise.</returns>
        public bool HasEdge(TNode s, TNode t, TLabel label)
        {
            var @implicit = Tuple.Create(s, t, label);
            return edgeWithLabelCounts.ContainsKey(@implicit);
        }

        /// <summary>
        /// Attempt to add the edge to the graph.
        /// </summary>
        /// <param name="s">Source of the edge.</param>
        /// <param name="t">Target of the edge.</param>
        /// <param name="label">Label of the edge.</param>
        /// <returns>True if the graph has nodes s and t and the edge has now been added, false otherwise.</returns>
        public bool AddEdge(TNode s, TNode t, TLabel label = default(TLabel))
        {
            if (nodes.Contains(s) && nodes.Contains(t))
            {
                int edge = counter++;
                edges.Add(edge);
                source.Add(edge, s);
                target.Add(edge, t);
                outgoing[s].Add(edge);
                incoming[t].Add(edge);
                edgeLabel.Add(edge, label);

                var @implicit = Tuple.Create(s, t);
                if (!edgeCounts.ContainsKey(@implicit))
                {
                    edgeCounts.Add(@implicit, 0);
                }
                edgeCounts[@implicit] += 1;

                var implicitWithLabel = Tuple.Create(s, t, label);
                if (!edgeWithLabelCounts.ContainsKey(implicitWithLabel))
                {
                    edgeWithLabelCounts.Add(implicitWithLabel, 0);
                }
                edgeWithLabelCounts[implicitWithLabel] += 1;

                if (!edgeLabelCounts.ContainsKey(label))
                {
                    edgeLabelCounts.Add(label, 0);
                }
                edgeLabelCounts[label] += 1;

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Attempt to remove the edge from the graph.
        /// </summary>
        /// <param name="edge">The edge to remove from the graph.</param>
        /// <returns>True if the edge was in the graph and has now been removed, false otherwise.</returns>
        public bool RemoveEdge(int edge)
        {
            if (edges.Contains(edge))
            {
                var s = source[edge];
                var t = target[edge];
                var label = edgeLabel[edge];
                edgeLabelCounts[label] -= 1;
                if (edgeLabelCounts[label] == 0)
                {
                    edgeLabelCounts.Remove(label);
                }

                var @implicit = Tuple.Create(s, t);
                edgeCounts[@implicit] -= 1;
                if (edgeCounts[@implicit] == 0)
                {
                    edgeCounts.Remove(@implicit);
                }

                var implicitWithLabel = Tuple.Create(s, t, label);
                edgeWithLabelCounts[implicitWithLabel] -= 1;
                if (edgeWithLabelCounts[implicitWithLabel] == 0)
                {
                    edgeWithLabelCounts.Remove(implicitWithLabel);
                }

                edgeLabel.Remove(edge);
                incoming[t].Remove(edge);
                outgoing[s].Remove(edge);
                target.Remove(edge);
                source.Remove(edge);
                edges.Remove(edge);

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Clones the graph.
        /// </summary>
        /// <returns>A new graph which is an exact copy of this graph.</returns>
        public MultiDirectedGraph<TNode, TLabel> Clone()
        {
            var clone = new MultiDirectedGraph<TNode, TLabel>();

            // Add nodes
            foreach (var node in nodes)
            {
                clone.AddNode(node, nodeLabel[node]);
            }

            // Add edges
            foreach (var s in nodes)
            {
                foreach (var e in outgoing[s])
                {
                    var t = target[e];
                    clone.AddEdge(s, t, edgeLabel[e]);
                }
            }

            return clone;
        }

        /// <summary>
        /// Clones a graph and transforms the nodes using the specified function.
        /// </summary>
        /// <typeparam name="TNewNode"></typeparam>
        /// <param name="convert"></param>
        /// <returns></returns>
        public MultiDirectedGraph<TNewNode, TLabel> Clone<TNewNode>(Func<TNode, TNewNode> convert)
        {
            var clone = new MultiDirectedGraph<TNewNode, TLabel>();

            // Add nodes
            foreach (var node in nodes)
            {
                clone.AddNode(convert(node), nodeLabel[node]);
            }

            // Add edges
            foreach (var s in nodes)
            {
                foreach (var e in outgoing[s])
                {
                    var t = target[e];
                    clone.AddEdge(convert(s), convert(t), edgeLabel[e]);
                }
            }

            return clone;
        }
    }
}
