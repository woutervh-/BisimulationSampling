using GraphTools.Graph;
using GraphTools.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace GraphTools
{
    static partial class Experiments
    {
        /// <summary>
        /// Compute distances.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <param name="graph"></param>
        /// <returns></returns>
        public static Experiment DistanceProbabilityMassFunction<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph)
        {
            var distribution = new Dictionary<int, BigInteger>();
            object @lock = new object();

            Parallel.ForEach(graph.Nodes, node =>
            {
                var distances = Utils.SingleSourceDistances(graph, node).Values.Where(distance => distance > 0 && distance < int.MaxValue);

                lock (@lock)
                {
                    Utils.UpdateDistribution(distribution, distances);
                }
            });

            double count = 0.0;
            foreach (var value in distribution.Values)
            {
                count += (double)value;
            }

            Experiment experiment = new Experiment(2)
            {
                Labels = new string[] { "Distance", "Probability" },
                Meta = new string[] { "Distance-PMF", graph.Name },
                F = d =>
                {
                    int distance = Convert.ToInt32(d);

                    if (distribution.ContainsKey(distance))
                    {
                        return new double[] { d, (double)distribution[distance] / count };
                    }
                    else
                    {
                        return new double[] { d, 0.0 };
                    }
                },
            };

            experiment.Run(distribution.Keys.Min(), distribution.Keys.Max(), 1, 1);
            return experiment;
        }

        /// <summary>
        /// Count the number of partition blocks in a graph.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TLabel"></typeparam>
        /// <param name="graph"></param>
        /// <param name="labels"></param>
        /// <returns></returns>
        public static Experiment BisimulationPartitionSize<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph)
        {
            var partitioner = new GraphPartitioner<TNode, TLabel>(graph);
            var partitions = partitioner.MultilevelExactBisimulationReduction();

            Experiment experiment = new Experiment(2)
            {
                Labels = new string[] { "k", "Number of blocks" },
                Meta = new string[] { "Partition-size", graph.Name },
                F = i =>
                {
                    var k = Convert.ToInt32(i);
                    var partitionCount = partitions[k].Values.Distinct().Count();

                    return new double[] { k, partitionCount };
                },
            };

            experiment.Run(0, partitions.Count - 1, 1, 1);
            return experiment;
            // experiment.Save(@"..\..\..\..\Analytics\BisimBlocks-" + graph.Name.ToLower() + ".tsv");
        }

        /// <summary>
        /// Compute the partition block size distribution.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="labels"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static Experiment PartitionBlockDistribution<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph)
        {
            var partitioner = new GraphPartitioner<TNode, TLabel>(graph);
            var partitions = partitioner.MultilevelExactBisimulationReduction();
            int k_max = partitions.Count - 1;

            var sizes = Utils.Distribution(partitions[k_max].Values).Values.ToArray();
            Array.Sort(sizes);
            Array.Reverse(sizes);

            var experiment = new Experiment(2)
            {
                Labels = new string[] { "Partition block", "Number of nodes" },
                Meta = new string[] { "Blocks", graph.Name, },
                F = i =>
                {
                    int index = Convert.ToInt32(i);
                    int size = sizes[index];

                    return new double[] { index + 1, size };
                },
            };

            experiment.Run(0, sizes.Length - 1, 1, 1);
            return experiment;
        }
    }
}
