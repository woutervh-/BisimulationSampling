using GraphTools.Graph;
using GraphTools.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphTools.Distributed
{
    class DistributedUtils
    {
        /// <summary>
        /// Randomly splits the nodes of a graph into P blocks.
        /// </summary>
        /// <typeparam name="TNode">Type of node.</typeparam>
        /// <typeparam name="TLabel">Type of label.</typeparam>
        /// <param name="graph">Graph to partition the nodes of.</param>
        /// <param name="P">Number of partition blocks.</param>
        public static Dictionary<TNode, int> RandomSplit<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, int P)
        {
            var partition = new Dictionary<TNode, int>();
            var nodes = graph.Nodes.ToArray();
            nodes.Shuffle();

            int p = 0;
            for (int i = 0; i < nodes.Length; i++)
            {
                partition.Add(nodes[i], p);
                p += 1;
                p %= P;
            }

            return partition;
        }

        /// <summary>
        /// Splits the nodes of a graph into P blocks by exploring.
        /// </summary>
        /// <typeparam name="TNode">Type of node.</typeparam>
        /// <typeparam name="TLabel">Type of label.</typeparam>
        /// <param name="graph">Graph to partition the nodes of.</param>
        /// <param name="P">Number of partition blocks.</param>
        public static Dictionary<TNode, int> ExploreSplit<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, int P)
        {
            int n = graph.NumNodes;
            // Bucket size
            int B = (n + P - 1) / P;
            var V = new HashSet<TNode>();
            var Q = new AiroQueue<TNode>();
            var nodes = graph.Nodes.ToArray();

            Utils.Shuffle(nodes);
            int seedIndex = 0;
            var partition = new Dictionary<TNode, int>();

            Action<TNode> bucketize = node =>
            {
                partition.Add(node, (n - 1) / B);
            };

            // Walk while we need to add nodes
            while (n > 0)
            {
                if (Q.Count <= 0)
                {
                    // Find next seed node, from an undiscovered connected component, and resume from there
                    while (V.Contains(nodes[seedIndex]))
                    {
                        seedIndex += 1;
                    }
                    var seed = nodes[seedIndex];

                    // Add seed to queue and set of nodes
                    Q.Put(seed);
                    V.Add(seed);
                    bucketize(seed);
                    n -= 1;
                }

                var u = Q.Take();
                var N = graph.Neighbors(u);
                foreach (var v in N)
                {
                    if (!V.Contains(v) && n > 0)
                    {
                        Q.Put(v);
                        V.Add(v);
                        bucketize(v);
                        n -= 1;
                    }
                }
            }

            return partition;
        }

        /// <summary>
        /// Splits the nodes of a graph into P blocks using a multilevel approach.
        /// </summary>
        /// <typeparam name="TNode">Type of node.</typeparam>
        /// <typeparam name="TLabel">Type of label.</typeparam>
        /// <param name="graph"></param>
        /// <param name="P">Number of partition blocks.</param>
        /// <param name="e">Coarsening stop condition.</param>
        /// <param name="M">Coarsening stop condition.</param>
        /// <param name="K">KernighanLin number of iterations.</param>
        public static Dictionary<TNode, int> MetisSplit<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, int P, double e, int M, int K)
        {
            // Collapse the graph
            int counter = 0;
            var mapNodes = new Dictionary<TNode, int>();
            var transformed = Collapse(graph).Clone(node =>
            {
                if (!mapNodes.ContainsKey(node))
                {
                    mapNodes.Add(node, counter);
                    counter += 1;
                }

                return mapNodes[node];
            });

            // Coarsen the graph
            int n = graph.NumNodes;
            var coarsened = Coarsen(transformed, e, M, n / 2);
            var coarseGraph = coarsened.Item1;
            var projections = coarsened.Item2;

            // Partition the coarse graph
            var partition = Balance(coarseGraph.Nodes, node => coarseGraph.NodeLabel(node), 8);

            // Unproject the partition of the coarse graph
            projections.Add(partition);
            var finePartition = new Dictionary<TNode, int>();
            foreach (var node in graph.Nodes)
            {
                int p = mapNodes[node];

                foreach (var projection in projections)
                {
                    p = projection[p];
                }

                finePartition.Add(node, p);
            }

            // Refine the partition
            KernighanLin(graph, finePartition, K);

            return finePartition;
        }

        /// <summary>
        /// Perform random matching partitioning.
        /// Input graph must be weighted on nodes.
        /// Nodes are only matched if they share an edge.
        /// </summary>
        /// <param name="graph">Graph with weighted nodes.</param>
        /// <param name="maxWeight">Maximum weight a matched pair is allowed to have.</param>
        /// <returns>A random matching of the nodes of the graph.</returns>
        public static Dictionary<int, int> RandomMatching(MultiDirectedGraph<int, int> graph, int maxWeight)
        {
            int counter = 0;
            var matches = new Dictionary<int, int>();

            // Visit nodes randomly
            var nodes = graph.Nodes.ToArray();
            nodes.Shuffle();
            foreach (var u in nodes)
            {
                if (!matches.ContainsKey(u))
                {
                    // Node u in unmatched, match it with one of the unmatched neighbors
                    var unmatchedNeighbors = graph.Neighbors(u).Where(v => !u.Equals(v) && !matches.ContainsKey(v) && graph.NodeLabel(u) + graph.NodeLabel(v) <= maxWeight).ToArray();

                    // Only match if such a neighbor exists
                    if (unmatchedNeighbors.Length > 0)
                    {
                        var v = Utils.Shuffled(unmatchedNeighbors).First();
                        matches.Add(u, counter);
                        matches.Add(v, counter);
                    }
                    else
                    {
                        matches.Add(u, counter);
                    }

                    counter += 1;
                }
            }

            return matches;
        }

        /// <summary>
        /// Collapse a graph which contains parallel edges into a graph without parallel edges.
        /// Node labels are all set to 1.
        /// Edge labels indicate how many parallel edges there originally were from the source to the target.
        /// </summary>
        /// <typeparam name="TNode">Type of node.</typeparam>
        /// <typeparam name="TLabel">Type of label.</typeparam>
        /// <param name="graph">Graph to collapse.</param>
        /// <returns>A copy of the original graph with node labels set to 1 and edge labels indicating how often that edge occurred in the original graph.</returns>
        public static MultiDirectedGraph<TNode, int> Collapse<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph)
        {
            var edgeWeights = new Dictionary<Tuple<TNode, TNode>, int>();
            var transformed = new MultiDirectedGraph<TNode, int>();

            // Copy nodes
            foreach (var u in graph.Nodes)
            {
                transformed.AddNode(u, 1);
            }

            // Count edges from u to v
            foreach (var u in graph.Nodes)
            {
                foreach (var eo in graph.Out(u))
                {
                    var v = graph.Target(eo);
                    var t = Tuple.Create(u, v);

                    if (!edgeWeights.ContainsKey(t))
                    {
                        edgeWeights.Add(t, 0);
                    }

                    edgeWeights[t] += 1;
                }
            }

            // Add weighted edges to the transformed graph
            foreach (var kvp in edgeWeights)
            {
                var u = kvp.Key.Item1;
                var v = kvp.Key.Item2;
                var w = kvp.Value;

                transformed.AddEdge(u, v, w);
            }

            return transformed;
        }

        /// <summary>
        /// Coarsen a graph based on random matching.
        /// When two nodes collapse, their weights are added up.
        /// The weights on the edges of two collapsed nodes are added up as well.
        /// </summary>
        /// <param name="graph">Graph to coarsen.</param>
        /// <param name="e">Small value which stops the coarsening if the coarser graph is too much like the finer graph.</param>
        /// <param name="M">Minimum number of nodes the coarse graph should have.</param>
        /// <param name="maxWeight">Maximum weight a single collapsed node is allowed to have.</param>
        /// <returns>The coarsened graph along with a list of projections used to obtain the coarsened graph.</returns>
        public static Tuple<MultiDirectedGraph<int, int>, List<Dictionary<int, int>>> Coarsen(MultiDirectedGraph<int, int> graph, double e, int M, int maxWeight)
        {
            // Coarsen the graph incrementally
            var projections = new List<Dictionary<int, int>>();
            var fineGraph = graph;
            var coarseGraph = graph;
            do
            {
                fineGraph = coarseGraph;
                coarseGraph = new MultiDirectedGraph<int, int>();

                var partition = RandomMatching(fineGraph, maxWeight);
                var inverted = Utils.Invert(partition);
                var edgeWeights = new Dictionary<Tuple<int, int>, int>();

                projections.Add(partition);

                // Add node for each block
                foreach (var match in inverted)
                {
                    int block = match.Key;
                    var nodes = match.Value;
                    int w = 0;

                    foreach (var u in nodes)
                    {
                        w += fineGraph.NodeLabel(u);
                    }

                    coarseGraph.AddNode(block, w);
                }

                // Sum edge weights, removing parallel edges
                foreach (var match in inverted)
                {
                    int sourceBlock = match.Key;
                    var nodes = match.Value;

                    foreach (var u in nodes)
                    {
                        foreach (var eo in fineGraph.Out(u))
                        {
                            var v = fineGraph.Target(eo);
                            var targetBlock = partition[v];
                            int w = fineGraph.EdgeLabel(eo);
                            var t = Tuple.Create(sourceBlock, targetBlock);

                            if (!edgeWeights.ContainsKey(t))
                            {
                                edgeWeights.Add(t, 0);
                            }

                            edgeWeights[t] += w;
                        }
                    }
                }

                // Add edges
                foreach (var kvp in edgeWeights)
                {
                    var u = kvp.Key.Item1;
                    var v = kvp.Key.Item2;
                    var w = kvp.Value;

                    coarseGraph.AddEdge(u, v, w);
                }
            } while ((double)fineGraph.NumNodes / (double)coarseGraph.NumNodes > 1.0 + e && coarseGraph.NumNodes > M);

            return Tuple.Create(coarseGraph, projections);
        }

        /// <summary>
        /// Performs load balancing using a minimum load heuristic.
        /// </summary>
        /// <typeparam name="T">Type of items.</typeparam>
        /// <param name="ts">Collection of items.</param>
        /// <param name="size">Function which gives the size (weight) of an item.</param>
        /// <param name="m">Number of buckets to divide the items into.</param>
        /// <returns>A partition indicating which item was put into which bucket.</returns>
        public static Dictionary<T, int> Balance<T>(IEnumerable<T> ts, Func<T, int> size, int m)
        {
            var partition = new Dictionary<T, int>();
            var load = new int[m];

            // Sort items by size descendingly
            var ta = ts.ToArray();
            Array.Sort(ta, (t1, t2) => size(t2).CompareTo(size(t1)));

            foreach (var t in ta)
            {
                // Assign t to block with lowest load
                var j = Enumerable.Range(0, m).MinBy(i => load[i]);
                load[j] += size(t);
                partition.Add(t, j);
            }

            return partition;
        }

        /// <summary>
        /// KerninghanLin algorithm which refines a partition.
        /// Equalizes block sizes and swaps nodes between partition blocks to minimize the edge cut.
        /// </summary>
        /// <typeparam name="TNode">Type of node.</typeparam>
        /// <typeparam name="TLabel">Type of label.</typeparam>
        /// <param name="graph">The graph of the partition.</param>
        /// <param name="partition">The partition to refine.</param>
        /// <param name="K">Maximum number of iterations.</param>
        public static void KernighanLin<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, Dictionary<TNode, int> partition, int K)
        {
            // Compute ED and ID of each node
            var ED = new Dictionary<TNode, int>();
            var ID = new Dictionary<TNode, int>();
            Func<TNode, int> D = node => ED[node] - ID[node];

            foreach (var node in graph.Nodes)
            {
                ED.Add(node, 0);
                ID.Add(node, 0);
            }

            foreach (var edge in graph.Edges)
            {
                var s = graph.Source(edge);
                var t = graph.Target(edge);

                if (partition[s] == partition[t])
                {
                    ID[s] += 1;
                    ID[t] += 1;
                }
                else
                {
                    ED[s] += 1;
                    ED[t] += 1;
                }
            }

            // Stick nodes into sets of their partition block
            var inverted = Utils.Invert(partition);
            var AI = inverted.Keys.First();
            var BI = inverted.Keys.Last();

            // Swaps a node from its original partition block to the other
            Action<TNode> swap = node =>
            {
                foreach (var edge in graph.Out(node).Concat(graph.In(node)))
                {
                    var neighbor = graph.Target(edge);

                    if (node.Equals(neighbor))
                    {
                        continue;
                    }

                    if (partition[node] == partition[neighbor])
                    {
                        // Will be in other block now
                        ID[neighbor] -= 1;
                        ID[node] -= 1;
                        ED[neighbor] += 1;
                        ED[node] += 1;
                    }
                    else
                    {
                        // Will be in same block now
                        ID[neighbor] += 1;
                        ID[node] += 1;
                        ED[neighbor] -= 1;
                        ED[node] -= 1;
                    }
                }

                if (partition[node] == AI)
                {
                    partition[node] = BI;
                    inverted[AI].Remove(node);
                    inverted[BI].Add(node);
                }
                else
                {
                    partition[node] = AI;
                    inverted[AI].Add(node);
                    inverted[BI].Remove(node);
                }
            };

            // Equalize block sizes
            while (Math.Abs(inverted[AI].Count - inverted[BI].Count) > 1)
            {
                if (inverted[AI].Count > inverted[BI].Count)
                {
                    // Move from A to B
                    var a = inverted[AI].MaxBy(node => D(node));
                    swap(a);
                }
                else
                {
                    // Move from B to A
                    var b = inverted[BI].MaxBy(node => D(node));
                    swap(b);
                }
            }

            // Keep performing positive swaps
            int n = Math.Min(inverted[AI].Count, inverted[BI].Count);
            for (int i = 0; i < K; i++)
            {
                bool hasGained = false;
                var AA = new HashSet<TNode>(inverted[AI]);
                var BB = new HashSet<TNode>(inverted[BI]);

                for (int j = 0; j < n; j++)
                {
                    var a = AA.MaxBy(node => D(node));
                    var b = BB.MaxBy(node => D(node));
                    int gain = D(a) + D(b);

                    if (graph.HasEdge(a, b))
                    {
                        gain -= 2;
                    }

                    if (graph.HasEdge(b, a))
                    {
                        gain -= 2;
                    }

                    if (gain > 0)
                    {
                        hasGained = true;
                        swap(a);
                        swap(b);
                    }

                    AA.Remove(a);
                    BB.Remove(b);
                }

                if (!hasGained)
                {
                    break;
                }
            }
        }
    }
}
