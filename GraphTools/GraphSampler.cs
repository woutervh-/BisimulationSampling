using GraphTools.Graph;
using GraphTools.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphTools
{
    /// <summary>
    /// Allows sampling of graphs.
    /// </summary>
    static class GraphSampler
    {
        /// <summary>
        /// Sample nodes randomly uniformly.
        /// </summary>
        /// <typeparam name="TNode">Node type.</typeparam>
        /// <param name="graph">Graph to sample nodes from.</param>
        /// <param name="n">Upper bound on the number of nodes to sample.</param>
        /// <returns></returns>
        public static IEnumerable<TNode> RN<TNode, TLabel>(this MultiDirectedGraph<TNode, TLabel> graph, int n)
        {
            return Utils.Shuffled(graph.Nodes.ToArray()).Take(Math.Min(n, graph.NumNodes)).ToArray();
        }

        /// <summary>
        /// Takes the first n nodes, ordered ascendingly by degree.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <param name="graph"></param>
        /// <param name="n">Upper bound on the number of nodes to sample.</param>
        /// <returns></returns>
        public static IEnumerable<TNode> LowDegreeFirst<TNode, TLabel>(this MultiDirectedGraph<TNode, TLabel> graph, int n)
        {
            // Sort nodes by their degree in ascending order
            TNode[] nodes = Utils.Shuffled(graph.Nodes.ToArray()).ToArray();
            Array.Sort(nodes, (n1, n2) => graph.Degree(n1).CompareTo(graph.Degree(n2)));

            // Take lowest degree nodes first
            return nodes.Take(Math.Min(n, graph.NumNodes));
        }

        /// <summary>
        /// Groups nodes by labels, each group in turn can add one of its nodes to the sample.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TLabel"></typeparam>
        /// <param name="graph"></param>
        /// <param name="labels"></param>
        /// <param name="n">Upper bound on the number of nodes to sample.</param>
        /// <returns></returns>
        public static IEnumerable<TNode> GreedyLabels<TNode, TLabel>(this MultiDirectedGraph<TNode, TLabel> graph, int n)
        {
            var nodeLabels = graph.NodeLabels.ToList();
            var groupCounts = new Dictionary<TLabel, int>();
            var groups = new Dictionary<TLabel, TNode[]>();
            var sample = new HashSet<TNode>();

            // Count how many nodes per label
            foreach (var node in graph.Nodes)
            {
                var label = graph.NodeLabel(node);

                if (!groupCounts.ContainsKey(label))
                {
                    groupCounts.Add(label, 0);
                }

                groupCounts[label] += 1;
            }

            // Construct groups dictionary
            foreach (var node in graph.Nodes)
            {
                var label = graph.NodeLabel(node);

                if (!groups.ContainsKey(label))
                {
                    groups.Add(label, new TNode[groupCounts[label]]);
                    groupCounts[label] -= 1;
                }

                groups[label][groupCounts[label]] = node;
                groupCounts[label] -= 1;
            }

            // Reset counts to 0 and shuffle groups
            foreach (var label in nodeLabels)
            {
                groupCounts[label] += 1;
                groups[label].Shuffle();
            }

            int i = 0;
            int m = nodeLabels.Count;
            while (n > 0 && m > 0)
            {
                var label = nodeLabels[i];

                // Check if label group has at least one node
                if (groups[label].Length > groupCounts[label])
                {
                    // Pick random node from this label group and add it to the sample
                    var node = groups[label][groupCounts[label]];
                    groupCounts[label] += 1;
                    sample.Add(node);
                    n -= 1;
                    i += 1;
                }
                else
                {
                    // Remove this group from the possible set of groups to pick from
                    nodeLabels.RemoveAt(i);
                    m -= 1;
                }

                // Avoid division by zero
                if (m > 0)
                {
                    // Ensure i is a valid index
                    i %= m;
                }
            }

            return sample;
        }

        /// <summary>
        /// Sample edges randomly uniformly.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <param name="graph"></param>
        /// <param name="m">Upper bound on the number of edges to sample.</param>
        /// <returns></returns>
        public static IEnumerable<TNode> RE<TNode, TLabel>(this MultiDirectedGraph<TNode, TLabel> graph, int m)
        {
            var edges = Utils.Shuffled(graph.Edges.ToArray()).Take(Math.Min(m, graph.NumEdges)).ToArray();
            var nodes = new HashSet<TNode>();

            foreach (var edge in edges)
            {
                var s = graph.Source(edge);
                var t = graph.Target(edge);

                if (!nodes.Contains(s))
                {
                    nodes.Add(s);
                }

                if (!nodes.Contains(t))
                {
                    nodes.Add(t);
                }
            }

            return nodes;
        }

        /// <summary>
        /// Do a BFS sample with random seed nodes. Only use each edge label once for each outgoing edge of a node.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <param name="graph"></param>
        /// <param name="n">Upper bound on the number of nodes to sample.</param>
        /// <returns></returns>
        public static IEnumerable<TNode> DistinctLabelsSB<TNode, TLabel>(this MultiDirectedGraph<TNode, TLabel> graph, int n)
        {
            var Q = new Queue<TNode>();
            var V = new HashSet<TNode>();
            var nodes = graph.Nodes.ToArray();
            // Possible improvement: order seed nodes by degree
            Utils.Shuffle(nodes);
            int seedIndex = 0;

            // Breadth-first walk while we need to add nodes
            while (n > 0)
            {
                if (Q.Count <= 0)
                {
                    // Find next seed node, from an undiscovered connected component, and resume from there
                    while (V.Contains(nodes[seedIndex]))
                    {
                        seedIndex += 1;

                        // If we ran out of nodes early
                        if (seedIndex == nodes.Length)
                        {
                            return V;
                        }
                    }
                    var seed = nodes[seedIndex];

                    // Add seed to queue and set of nodes
                    Q.Enqueue(seed);
                    V.Add(seed);
                    n -= 1;
                }

                // Possible improvement: select by (edge label, target node label)
                var u = Q.Dequeue();
                var N = graph.Out(u).GroupBy(eo => graph.EdgeLabel(eo)).Select(group => graph.Target(group.First()));
                foreach (var v in N)
                {
                    if (!V.Contains(v) && n > 0)
                    {
                        Q.Enqueue(v);
                        V.Add(v);
                        n -= 1;
                    }
                }
            }

            return V;
        }

        /// <summary>
        /// Sample nodes by walking the graph. The walk is done using a generic queue.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TLabel"></typeparam>
        /// <typeparam name="TQueue"></typeparam>
        /// <param name="graph"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static HashSet<TNode> QueuedSampler<TNode, TLabel, TQueue>(this MultiDirectedGraph<TNode, TLabel> graph, int n) where TQueue : GenericQueue<TNode>, new()
        {
            var V = new HashSet<TNode>();
            var Q = new TQueue();
            var nodes = graph.Nodes.ToArray();
            Utils.Shuffle(nodes);
            int seedIndex = 0;

            // Walk while we need to add nodes
            while (n > 0)
            {
                if (Q.Count <= 0)
                {
                    // Find next seed node, from an undiscovered connected component, and resume from there
                    while (V.Contains(nodes[seedIndex]))
                    {
                        seedIndex += 1;

                        // If we ran out of nodes early
                        if (seedIndex == nodes.Length)
                        {
                            return V;
                        }
                    }
                    var seed = nodes[seedIndex];

                    // Add seed to queue and set of nodes
                    Q.Put(seed);
                    V.Add(seed);
                    n -= 1;
                }

                var u = Q.Take();
                var N = graph.Out(u).Select(eo => graph.Target(eo));
                foreach (var v in N)
                {
                    if (!V.Contains(v) && n > 0)
                    {
                        Q.Put(v);
                        V.Add(v);
                        n -= 1;
                    }
                }
            }

            return V;
        }
    }
}
