using GraphTools.Graph;
using GraphTools.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphTools
{
    /// <summary>
    /// Computes some graph metrics.
    /// </summary>
    static class GraphMetrics
    {
        /// <summary>
        /// Measures the cut of a partition of a graph.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TLabel"></typeparam>
        /// <param name="graph"></param>
        /// <param name="partition"></param>
        /// <returns></returns>
        public static int Cut<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, IDictionary<TNode, int> partition)
        {
            int cut = 0;
            foreach (var edge in graph.Edges)
            {
                var s = graph.Source(edge);
                var t = graph.Target(edge);

                if (partition[s] != partition[t])
                {
                    cut += 1;
                }
            }

            return cut;
        }

        /// <summary>
        /// Measures the weighted k-bisimulation partition equivalence between two graphs.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TLabel"></typeparam>
        /// <param name="G1"></param>
        /// <param name="G2"></param>
        /// <param name="L1"></param>
        /// <param name="L2"></param>
        /// <param name="k">A tuple indicating how many nodes of G1 and G2 are in partition blocks that are shared between G1 and G2.</param>
        /// <returns></returns>
        public static Tuple<int, int> WeightedBisimulationEquivalence<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> G1, MultiDirectedGraph<TNode, TLabel> G2, int k)
        {
            // Create new empty graph and label provider
            var G = new MultiDirectedGraph<Tuple<int, TNode>, TLabel>();

            // Add nodes of G1
            foreach (var node in G1.Nodes.Select(node => Tuple.Create(1, node)))
            {
                G.AddNode(node, G1.NodeLabel(node.Item2));
            }

            // Add nodes of G2
            foreach (var node in G2.Nodes.Select(node => Tuple.Create(2, node)))
            {
                G.AddNode(node, G2.NodeLabel(node.Item2));
            }

            // Add edges of G1
            foreach (var edge in G1.Edges)
            {
                var s = Tuple.Create(1, G1.Source(edge));
                var t = Tuple.Create(1, G1.Target(edge));
                G.AddEdge(s, t, G1.EdgeLabel(edge));
            }

            // Add edges of G2
            foreach (var edge in G2.Edges)
            {
                var s = Tuple.Create(2, G2.Source(edge));
                var t = Tuple.Create(2, G2.Target(edge));
                G.AddEdge(s, t, G2.EdgeLabel(edge));
            }

            // Perform bisimulation reduction
            var partitioner = new GraphPartitioner<Tuple<int, TNode>, TLabel>(G);
            var partition = partitioner.BoundedBisimulationReduction(k);

            // Partition blocks of G1 and G2
            HashSet<int> P1 = new HashSet<int>();
            HashSet<int> P2 = new HashSet<int>();

            foreach (var node in G.Nodes)
            {
                int block = partition[node];

                switch (node.Item1)
                {
                    case 1:
                        if (!P1.Contains(block))
                        {
                            P1.Add(block);
                        }
                        break;
                    case 2:
                        if (!P2.Contains(block))
                        {
                            P2.Add(block);
                        }
                        break;
                }
            }

            int s1 = 0;
            int s2 = 0;
            foreach (var node in G.Nodes)
            {
                if (P1.Contains(partition[node]) && P2.Contains(partition[node]))
                {
                    switch (node.Item1)
                    {
                        case 1:
                            s1 += 1;
                            break;
                        case 2:
                            s2 += 1;
                            break;
                    }
                }
            }

            return Tuple.Create(s1, s2);
        }

        /// <summary>
        /// Measures the k-bisimulation partition equivalence between two graphs.
        /// </summary>
        /// <typeparam name="TNode">Node type.</typeparam>
        /// <typeparam name="TLabel">Label type.</typeparam>
        /// <param name="G1">Graph one.</param>
        /// <param name="G2">Graph two.</param>
        /// <param name="L1">Labels belonging to graph one.</param>
        /// <param name="L2">Labels belonging to graph two.</param>
        /// <param name="k">Depth parameter for bisimulation equivalence.</param>
        /// <returns>A triple with the number of partition blocks in Graph one, Graph two, and the total number of partition blocks among them.</returns>
        public static Tuple<int, int, int> BisimulationEquivalence<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> G1, MultiDirectedGraph<TNode, TLabel> G2, int k)
        {
            // Create new empty graph and label provider
            var G = new MultiDirectedGraph<Tuple<int, TNode>, TLabel>();

            // Add nodes of G1
            foreach (var node in G1.Nodes.Select(node => Tuple.Create(1, node)))
            {
                G.AddNode(node, G1.NodeLabel(node.Item2));
            }

            // Add nodes of G2
            foreach (var node in G2.Nodes.Select(node => Tuple.Create(2, node)))
            {
                G.AddNode(node, G2.NodeLabel(node.Item2));
            }

            // Add edges of G1
            foreach (var edge in G1.Edges)
            {
                var s = Tuple.Create(1, G1.Source(edge));
                var t = Tuple.Create(1, G1.Target(edge));
                G.AddEdge(s, t, G1.EdgeLabel(edge));
            }

            // Add edges of G2
            foreach (var edge in G2.Edges)
            {
                var s = Tuple.Create(2, G2.Source(edge));
                var t = Tuple.Create(2, G2.Target(edge));
                G.AddEdge(s, t, G2.EdgeLabel(edge));
            }

            // Perform bisimulation reduction
            var partitioner = new GraphPartitioner<Tuple<int, TNode>, TLabel>(G);
            var partition = partitioner.BoundedBisimulationReduction(k);

            // Partition blocks of G1, G2 and G1 and G2 total
            HashSet<int> P1 = new HashSet<int>();
            HashSet<int> P2 = new HashSet<int>();
            HashSet<int> PT = new HashSet<int>();

            foreach (var node in G.Nodes)
            {
                int block = partition[node];

                switch (node.Item1)
                {
                    case 1:
                        if (!P1.Contains(block))
                        {
                            P1.Add(block);
                        }
                        goto default;
                    case 2:
                        if (!P2.Contains(block))
                        {
                            P2.Add(block);
                        }
                        goto default;
                    default:
                        if (!PT.Contains(block))
                        {
                            PT.Add(block);
                        }
                        break;
                }
            }

            return Tuple.Create(P1.Count, P2.Count, PT.Count);
        }
    }
}
