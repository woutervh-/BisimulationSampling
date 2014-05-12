using GraphTools.Graph;
using GraphTools.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GraphTools
{
    /// <summary>
    /// Partitions a graph.
    /// </summary>
    class GraphPartitioner<TNode, TLabel>
    {
        /// <summary>
        /// Graph to partition.
        /// </summary>
        private MultiDirectedGraph<TNode, TLabel> graph;

        /// <summary>
        /// Measures total running time of simulation.
        /// </summary>
        private Stopwatch stopwatch = new Stopwatch();

        /// <summary>
        /// Gets the number of milliseconds it took for the partition computation to complete.
        /// </summary>
        public long ElapsedMilliseconds
        {
            get
            {
                return stopwatch.ElapsedMilliseconds;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="graph"></param>
        public GraphPartitioner(MultiDirectedGraph<TNode, TLabel> graph)
        {
            this.graph = graph;
        }

        /// <summary>
        /// Estimate the (unbounded) bisimulation partition of a graph.
        /// Uses a hash function to determine partition block signature equivalence.
        /// </summary>
        /// <returns></returns>
        public IDictionary<TNode, int> EstimateBisimulationReduction()
        {
            // List of partitions; the k+1-th entry in the list corresponds to the k-th bisimulation partition
            var partitions = new List<Dictionary<TNode, int>>();

            stopwatch.Reset();
            stopwatch.Start();
            {
                // Base (k = 0); add empty partition
                partitions.Add(new Dictionary<TNode, int>());

                // Assign block to each node; depending on the node's label
                foreach (var node in graph.Nodes)
                {
                    var label = graph.NodeLabel(node);
                    partitions[0].Add(node, Utils.Hash(label));
                }

                // Step (k > 0); repeat until the previous partition is no longer refined
                int k = 0;
                do
                {
                    // Add empty partition
                    k += 1;
                    partitions.Add(new Dictionary<TNode, int>());

                    foreach (var u in graph.Nodes)
                    {
                        var H = new HashSet<int>();
                        H.Add(Utils.Hash(graph.NodeLabel(u)));

                        // Compute the hashes for pairs (label[u, v], pId_k-1(v))
                        foreach (var eo in graph.Out(u))
                        {
                            var v = graph.Target(eo);
                            var edgeHash = Utils.Hash(graph.EdgeLabel(eo), partitions[k - 1][v]);
                            if (!H.Contains(edgeHash))
                            {
                                H.Add(edgeHash);
                            }
                        }

                        // Combine the hashes using some associative-commutative operator
                        int hash = 0;
                        foreach (var edgeHash in H)
                        {
                            hash += edgeHash;
                        }

                        // Assign partition block to node
                        partitions[k].Add(u, hash);
                    }
                } while (partitions[k].Values.Distinct().Count() > partitions[k - 1].Values.Distinct().Count());

                // Remove last partition because it is equivalent to the second last partition
                partitions.RemoveAt(partitions.Count - 1);
            }
            stopwatch.Stop();

            return partitions[partitions.Count - 1];
        }

        /// <summary>
        /// Compute the (unbounded) bisimulation partition of a graph.
        /// </summary>
        /// <returns></returns>
        public IDictionary<TNode, int> ExactBisimulationReduction()
        {
            // Compute partitions from 0 to k_max
            var partitions = MultilevelExactBisimulationReduction();

            // Return the k_max partition
            return partitions[partitions.Count - 1];
        }

        /// <summary>
        /// Partition a directed graph's nodes based on (unbounded) bisimulation equivalence.
        /// Return all intermediate 0..k_max depth bisimulation partitions.
        /// </summary>
        /// <returns></returns>
        public List<IDictionary<TNode, int>> MultilevelExactBisimulationReduction()
        {
            var partitions = new List<IDictionary<TNode, int>>();

            stopwatch.Reset();
            stopwatch.Start();
            {
                // Partition block counter (for uniqueness)
                int counter = 0;
                var comparer = new Utils.HashSetEqualityComparer<Tuple<TLabel, int>>();

                // Base (k = 0)
                // Create a partition based solely on the labels of the nodes
                partitions.Add(new Dictionary<TNode, int>());
                var labelIds = new Dictionary<TLabel, int>();
                foreach (var label in graph.NodeLabels)
                {
                    labelIds.Add(label, counter++);
                }

                // Assign block to each node
                foreach (var node in graph.Nodes)
                {
                    partitions[0].Add(node, labelIds[graph.NodeLabel(node)]);
                }

                // Store the signatures and the block identifier associated with them
                var signatures = new Dictionary<TLabel, Dictionary<HashSet<Tuple<TLabel, int>>, int>>();
                foreach (var label in graph.NodeLabels)
                {
                    signatures.Add(label, null);
                }

                // Step (k > 0)
                int i = 0;
                do
                {
                    // Initialize partition
                    i += 1;
                    partitions.Add(new Dictionary<TNode, int>());

                    // Initialize each entry in the signature storage
                    foreach (var label in graph.NodeLabels)
                    {
                        // Use custom hash set for faster equivalence determination
                        signatures[label] = new Dictionary<HashSet<Tuple<TLabel, int>>, int>(comparer);
                    }

                    foreach (var u in graph.Nodes)
                    {
                        // Label of node
                        var label = graph.NodeLabel(u);

                        // Compute signature (without label) of node u
                        // This is the set of tuples (label(u, v), partition_i-1(v)) for every edge (u, v)
                        var S = new HashSet<Tuple<TLabel, int>>();
                        foreach (var eo in graph.Out(u))
                        {
                            var v = graph.Target(eo);
                            var t = Tuple.Create(graph.EdgeLabel(eo), partitions[i - 1][v]);
                            if (!S.Contains(t))
                            {
                                S.Add(t);
                            }
                        }

                        // Check if signature exists and create a new block if it does not
                        if (!signatures[label].ContainsKey(S))
                        {
                            signatures[label].Add(S, counter++);
                        }

                        // Set block of node u
                        partitions[i].Add(u, signatures[label][S]);
                    }
                } while (partitions[i].Values.Distinct().Count() > partitions[i - 1].Values.Distinct().Count());

                // Remove last partition because it is equivalent to the second last partition
                partitions.RemoveAt(partitions.Count - 1);
            }
            stopwatch.Stop();

            return partitions;
        }

        /// <summary>
        /// Partition a directed graph's nodes based on (bounded) k-bisimulation equivalence.
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        public IDictionary<TNode, int> BoundedExactBisimulationReduction(int k)
        {
            var partitions = new Dictionary<TNode, int>[k + 1];

            stopwatch.Reset();
            stopwatch.Start();
            {
                // Partition block counter (for uniqueness)
                int counter = 0;
                var comparer = new Utils.HashSetEqualityComparer<Tuple<TLabel, int>>();

                // Base (k = 0)
                // Create a partition based solely on the labels of the nodes
                partitions[0] = new Dictionary<TNode, int>();
                var labelIds = new Dictionary<TLabel, int>();
                foreach (var label in graph.NodeLabels)
                {
                    labelIds.Add(label, counter++);
                }

                // Assign block to each node
                foreach (var node in graph.Nodes)
                {
                    partitions[0].Add(node, labelIds[graph.NodeLabel(node)]);
                }

                // Store the signatures and the block identifier associated with them
                var signatures = new Dictionary<TLabel, Dictionary<HashSet<Tuple<TLabel, int>>, int>>();
                foreach (var label in graph.NodeLabels)
                {
                    signatures.Add(label, null);
                }

                // Step (k > 0)
                for (int i = 1; i <= k; i++)
                {
                    // Initialize partition
                    partitions[i] = new Dictionary<TNode, int>();

                    // Initialize each entry in the signature storage
                    foreach (var label in graph.NodeLabels)
                    {
                        // Use custom hash set for faster equivalence determination
                        signatures[label] = new Dictionary<HashSet<Tuple<TLabel, int>>, int>(comparer);
                    }

                    foreach (var u in graph.Nodes)
                    {
                        // Label of node
                        var label = graph.NodeLabel(u);

                        // Compute signature (without label) of node u
                        // This is the set of tuples (label(u, v), partition_i-1(v)) for every edge (u, v)
                        var S = new HashSet<Tuple<TLabel, int>>();
                        foreach (var eo in graph.Out(u))
                        {
                            var v = graph.Target(eo);
                            var t = Tuple.Create(graph.EdgeLabel(eo), partitions[i - 1][v]);
                            if (!S.Contains(t))
                            {
                                S.Add(t);
                            }
                        }

                        // Check if signature exists and create a new block if it does not
                        if (!signatures[label].ContainsKey(S))
                        {
                            signatures[label].Add(S, counter++);
                        }

                        // Set block of node u
                        partitions[i].Add(u, signatures[label][S]);
                    }
                }
            }
            stopwatch.Stop();

            return partitions[k];
        }
    }
}
