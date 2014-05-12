using GraphTools.Graph;
using GraphTools.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphTools
{
    /// <summary>
    /// Generates synthetic graphs.
    /// </summary>
    static class GraphGenerator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static MultiDirectedGraph<int, int> ErdosRenyi(int n, double p)
        {
            // Create empty graph and label provider
            var graph = new MultiDirectedGraph<int, int>();
            graph.Name = "Synthetic_ErdosRenyi_" + n + "_" + p;

            // Add n nodes
            for (int i = 0; i < n; i++)
            {
                graph.AddNode(i, 0);
            }

            // Add edges with probability p
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (StaticRandom.NextDouble() <= p)
                    {
                        graph.AddEdge(i, j, 0);
                    }
                }
            }

            return graph;
        }

        /// <summary>
        /// Generates a graph with chains of nodes as close to the desired properties as possible.
        /// Under k-bisimulation it is guaranteed that there are p / (k + 1) * (k + 1) partition blocks.
        /// It is also guaranteed that the number of nodes is at least n.
        /// Hint: choose p as multiple of k + 1, and choose n as multiple of p.
        /// </summary>
        /// <param name="n">Desired number of nodes.</param>
        /// <param name="p">Desired number of partition blocks.</param>
        /// <param name="k">Depth parameter for bisimulation.</param>
        /// <returns></returns>
        public static MultiDirectedGraph<int, int> GenerateChains(int n, int p, int k)
        {
            // Create empty graph and label provider
            var graph = new MultiDirectedGraph<int, int>();
            graph.Name = "Synthetic_Chains_" + k + "_" + p + "_" + n;

            // Node counter for uniqueness
            int node = 0;
            // Number of chains of length k + 1 to satisfy p partition requirement
            int c = p / (k + 1);

            // Keep adding chains while we lack nodes
            while (graph.NumNodes < n)
            {
                // Add c chains
                for (int i = 0; i < c; i++)
                {
                    // Add initial node
                    graph.AddNode(node, i);
                    node += 1;

                    // Add subsequent nodes with edges
                    for (int j = 1; j <= k; j++)
                    {
                        graph.AddNode(node, i);
                        graph.AddEdge(node - 1, node, i);
                        node += 1;
                    }
                }
            }

            return graph;
        }

        /// <summary>
        /// Generates a graph with stars of nodes as close to the desired properties as possible.
        /// Under k-bisimulation it is guaranteed that there are p / (s + 1) * (s + 1) partition blocks.
        /// It is also guaranteed that the number of nodes is at least n.
        /// Hint: choose p as multiple of s + 1, and choose n as multiple of p.
        /// </summary>
        /// <param name="n">Desired number of nodes.</param>
        /// <param name="p">Desired number of partition blocks.</param>
        /// <param name="s">Degree of each star.</param>
        /// <returns></returns>
        public static MultiDirectedGraph<int, int> GenerateStars(int n, int p, int s)
        {
            // Create empty graph and label provider
            var graph = new MultiDirectedGraph<int, int>();
            graph.Name = "Synthetic_" + s + "_Stars_" + p + "_" + n;

            // Node counter for uniqueness
            int node = 0;
            // Number of stars of size s + 1 to satisfy p partition requirement
            int c = p / (s + 1);

            // Keep adding stars while we lack nodes
            while (graph.NumNodes < n)
            {
                // Add c stars
                for (int i = 0; i < c; i++)
                {
                    // Add center node
                    graph.AddNode(node, 0);
                    node += 1;

                    // Add child nodes
                    for (int j = 1; j <= s; j++)
                    {
                        graph.AddNode(node, i * (s + 1) + j);
                        graph.AddEdge(node - j, node, 0);
                        node += 1;
                    }
                }
            }

            return graph;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="p"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static MultiDirectedGraph<int, int> GenerateTrees(int n, int p, int k)
        {
            // Create empty graph and label provider
            var graph = new MultiDirectedGraph<int, int>();
            graph.Name = "Synthetic_Trees_" + k + "_" + p + "_" + n;

            // Find the degree necessary for a k-depth tree to achieve at least p number of nodes
            int degree = 1;
            while ((int)Math.Pow(degree, k + 1) - 1 < p)
            {
                degree += 1;
            }
            int branchSize = (int)Math.Pow(degree, k + 1) - 1;

            // Keep adding trees while we lack nodes
            for (int treeOffset = 0; treeOffset < n; treeOffset += branchSize)
            {
                int partitionSize = k + 1;

                // Add nodes
                for (int i = 0; i < branchSize; i++)
                {
                    graph.AddNode(treeOffset + i);

                    if (partitionSize < p && Math.Log(i + 1, degree) % 1 != 0)
                    {
                        graph.SetNodeLabel(treeOffset + i, i);
                        partitionSize += 1;
                    }
                    else
                    {
                        graph.SetNodeLabel(treeOffset + i, branchSize);
                    }
                }

                // Add edges
                for (int i = 0; i < branchSize / degree; i++)
                {
                    for (int j = 1; j <= degree; j++)
                    {
                        graph.AddEdge(treeOffset + i, treeOffset + degree * i + j, 0);
                    }
                }
            }

            return graph;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="D"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static MultiDirectedGraph<int, int> GenerateNiceDAG(int D, int b)
        {
            // Create empty graph and label provider
            var graph = new MultiDirectedGraph<int, int>();
            graph.Name = "Synthetic_DAG_" + D + "_" + b;

            // Define parent function
            Func<int, int> parent = node =>
            {
                // Assume node > 0 (not the root node)
                return graph.In(node).First();
            };

            // Define level function
            Func<int, int> level = Utils.Y<int, int>(fix => node =>
            {
                if (node == 0)
                {
                    // Root node
                    return 0;
                }
                else
                {
                    return 1 + fix(parent(node));
                }
            });

            // Create initial tree
            int counter = 0;
            graph.AddNode(counter, 0);
            counter += 1;

            while (graph.Nodes.Select(node => level(node)).Max() < D)
            {
                int max = graph.Nodes.Select(node => level(node)).Max();
                var lowest = graph.Nodes.Where(node => level(node) == max).ToArray();

                foreach (var node in lowest)
                {
                    int k = StaticRandom.Next(b + 1);

                    for (int i = 0; i < k; i++)
                    {
                        graph.AddNode(counter, 0);
                        graph.AddEdge(node, counter, i);
                        // graph.AddEdge(node, counter, 0);
                        counter += 1;
                    }
                }
            }

            // Transform tree to DAG with nicer partition block distribution
            var copy = graph.Clone();
            var partitioner = new GraphPartitioner<int, int>(graph);
            var partition = partitioner.BoundedExactBisimulationReduction(D);
            var partitionInverted = Utils.Invert(partition);
            var blocks = partition.Values.Distinct();
            var blockSizes = Utils.Distribution(partition.Values);
            int blockMax = blockSizes.Values.Max();

            foreach (var block in blocks)
            {
                int size = blockSizes[block];
                var nodes = new List<int>(partitionInverted[block]);

                for (int i = size; i < blockMax; i++)
                {
                    // Replicate a random node in this partition block
                    int k = StaticRandom.Next(size);
                    var v = nodes[k];

                    // Replicate the node
                    graph.AddNode(counter, graph.NodeLabel(v));

                    // Replicate its incoming edges
                    foreach (var ei in copy.In(v))
                    {
                        var u = graph.Source(ei);
                        graph.AddEdge(u, counter, graph.EdgeLabel(ei));
                    }

                    // Replicate its outgoing edges
                    foreach (var eo in copy.Out(v))
                    {
                        var w = graph.Target(eo);
                        graph.AddEdge(counter, w, graph.EdgeLabel(eo));
                    }

                    counter += 1;
                }
            }

            return graph;
        }

        /// <summary>
        /// Computes a reduced graph under some bisimulation equivalence relation.
        /// The input graph must be partitioned by the partitioner modulo said equivalence relation.
        /// </summary>
        /// <typeparam name="TNode">Node type.</typeparam>
        /// <typeparam name="TLabel">Label type.</typeparam>
        /// <param name="graph">Input graph.</param>
        /// <param name="labels">Labels of graph.</param>
        /// <param name="partitioner">Function which partitions the graph modulo some bisimulation equivalence relation.</param>
        /// <returns>A reduced graph where each partition block is a node and edges are reconstructued such that bisimulation equivalence is maintained.</returns>
        public static MultiDirectedGraph<int, TLabel> ReducedGraph<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, Func<IDictionary<TNode, int>> partitioner)
        {
            var reduced = new MultiDirectedGraph<int, TLabel>();
            var partition = partitioner();
            var inverted = Utils.Invert(partition);

            // Add a node for each partition block
            foreach (var kvp in inverted)
            {
                var block = kvp.Key;
                var nodes = kvp.Value.ToArray();
                // var someNode = Utils.Shuffled(nodes).First();
                var someNode = nodes.First();

                reduced.AddNode(block, graph.NodeLabel(someNode));
            }

            // Add the edge going from each partition block to another
            foreach (var kvp in inverted)
            {
                var block = kvp.Key;
                var nodes = kvp.Value.ToArray();
                // var someSource = Utils.Shuffled(nodes).First();
                var someSource = nodes.First();

                foreach (var eo in graph.Out(someSource))
                {
                    var someTarget = graph.Target(eo);
                    var label = graph.EdgeLabel(eo);

                    if (!reduced.HasEdge(block, partition[someTarget], label))
                    {
                        reduced.AddEdge(block, partition[someTarget], label);
                    }
                }
            }

            return reduced;
        }
    }
}
