using GraphTools.Distributed;
using GraphTools.Distributed.Machines;
using GraphTools.Graph;
using GraphTools.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphTools
{
    /// <summary>
    /// Dummy class: performs some arbitrary analyses on graphs.
    /// </summary>
    static class Experiments
    {
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
            var partitions = partitioner.MultilevelBisimulationReduction();
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

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TLabel"></typeparam>
        /// <param name="graph"></param>
        /// <param name="M">Maximum number of machines (at least 1).</param>
        /// <returns></returns>
        public static Experiment MeasureDistributedPerformance<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, int M)
        {
            var sequentialPartitioner = new GraphPartitioner<TNode, TLabel>(graph);
            var distributedPartitioner = new DistributedGraphPartitioner<TNode, TLabel>[M];

            for (int i = 0; i < M; i++)
            {
                distributedPartitioner[i] = new DistributedGraphPartitioner<TNode, TLabel>(i + 1, graph);
            }

            var experiment = new Experiment(3)
            {
                Labels = new string[] { "Number of machines", "Sequential (ms)", "Distributed (ms)" },
                Meta = new string[] { "Performance", "Estimated", graph.Name },
                F = i =>
                {
                    int m = Convert.ToInt32(i);
                    sequentialPartitioner.EstimateBisimulationReduction();
                    var sequentialTime = sequentialPartitioner.ElapsedMilliseconds;
                    distributedPartitioner[m].EstimateBisimulationReduction();
                    var distributedTime = distributedPartitioner[m].ElapsedMilliseconds;

                    return new double[] { m + 1, sequentialTime, distributedTime };
                },
            };

            experiment.Run(0, M - 1, 1, 10);
            return experiment;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TLabel"></typeparam>
        /// <param name="graph"></param>
        /// <param name="M"></param>
        /// <returns></returns>
        public static Experiment MeasureDistributedVisitTimes<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, int M)
        {
            var distributedPartitioner = new DistributedGraphPartitioner<TNode, TLabel>[M];

            for (int i = 0; i < M; i++)
            {
                distributedPartitioner[i] = new DistributedGraphPartitioner<TNode, TLabel>(i + 1, graph);
            }

            var experiment = new Experiment(2)
            {
                Labels = new string[] { "Number of machines", "Visit times" },
                Meta = new string[] { "VisitTimes", "Estimated", graph.Name },
                F = i =>
                {
                    int m = Convert.ToInt32(i);
                    distributedPartitioner[m].EstimateBisimulationReduction();
                    var visitTimes = distributedPartitioner[m].VisitTimes;

                    return new double[] { m + 1, visitTimes };
                },
            };

            experiment.Run(0, M - 1, 1, 10);
            return experiment;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TLabel"></typeparam>
        /// <param name="graph"></param>
        /// <param name="M"></param>
        /// <returns></returns>
        public static Experiment MeasureDistributedDataShipment<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, int M)
        {
            var distributedPartitioner = new DistributedGraphPartitioner<TNode, TLabel>[M];

            for (int i = 0; i < M; i++)
            {
                distributedPartitioner[i] = new DistributedGraphPartitioner<TNode, TLabel>(i + 1, graph);
            }

            var experiment = new Experiment(2)
            {
                Labels = new string[] { "Number of machines", "Data shipment" },
                Meta = new string[] { "DataShipment", "Estimated", graph.Name },
                F = i =>
                {
                    int m = Convert.ToInt32(i);
                    distributedPartitioner[m].EstimateBisimulationReduction();
                    var dataShipment = distributedPartitioner[m].DataShipment;

                    return new double[] { m + 1, dataShipment };
                },
            };

            experiment.Run(0, M - 1, 1, 10);
            return experiment;
        }

        /// <summary>
        /// Coverage and correctness vs sample fraction.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="labels"></param>
        /// <param name="samplerName"></param>
        /// <param name="sampler"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static Experiment StandardBisimulationMetrics<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, string samplerName, Func<double, MultiDirectedGraph<TNode, TLabel>> sampler, int k)
        {
            double[] percentages = new double[]
            {
                1.0, 2.0, 4.0, 8.0, 16.0, 32.0, 64.0
            };

            var experiment = new Experiment(3)
            {
                Labels = new string[] { "Sample fraction", "Coverage", "Correctness" },
                Meta = new string[] { "Standard", graph.Name, samplerName, k + "-bisimulation" },
                F = i =>
                {
                    double p = percentages[Convert.ToInt32(i)] / 100.0;
                    var sample = sampler(p);
                    var counts = GraphMetrics.BisimulationEquivalence(graph, sample, k);

                    // Graph block count
                    double N1 = counts.Item1;
                    // Sample block count
                    double N2 = counts.Item2;
                    // Shared block count
                    double NS = N1 + N2 - (double)counts.Item3;

                    double coverage = NS / N1;
                    double correctness = NS / N2;

                    return new double[] { p, coverage, correctness };
                },
            };

            experiment.Run(0, percentages.Length - 1, 1, 10);
            return experiment;
        }

        /// <summary>
        /// Weighted coverage and correctness vs sample fraction.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="labels"></param>
        /// <param name="samplerName"></param>
        /// <param name="sampler"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static Experiment WeightedBisimulationMetrics<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, string samplerName, Func<double, MultiDirectedGraph<TNode, TLabel>> sampler, int k)
        {
            double[] percentages = new double[]
            {
                1.0, 2.0, 4.0, 8.0, 16.0, 32.0, 64.0
            };

            var experiment = new Experiment(3)
            {
                Labels = new string[] { "Sample fraction", "Weighted coverage", "Weighted correctness" },
                Meta = new string[] { "Weighted", graph.Name, samplerName, k + "-bisimulation" },
                F = i =>
                {
                    double p = percentages[Convert.ToInt32(i)] / 100.0;
                    var sample = sampler(p);
                    var counts = GraphMetrics.WeightedBisimulationEquivalence(graph, sample, k);

                    // Weighted coverage
                    double wr = (double)counts.Item1 / (double)graph.NumNodes;
                    // Weighted correctness
                    double wp = (double)counts.Item2 / (double)sample.NumNodes;

                    return new double[] { p, wr, wp };
                },
            };

            experiment.Run(0, percentages.Length - 1, 1, 10);
            return experiment;
        }

        /// <summary>
        /// Compute distances.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <param name="graph"></param>
        /// <returns></returns>
        public static Experiment DistanceProbabilityMassFunction<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph)
        {
            var distribution = new Dictionary<int, int>();
            object @lock = new object();

            Parallel.ForEach(graph.Nodes, Program.ParallelOptions, node =>
            {
                var distances = Utils.SingleSourceDistances(graph, node).Values.Where(distance => distance > 0 && distance < int.MaxValue);

                lock (@lock)
                {
                    Utils.UpdateDistribution(distribution, distances);
                }
            });

            double count = distribution.Values.Sum();

            Experiment experiment = new Experiment(2)
            {
                Labels = new string[] { "Distance", "Probability" },
                Meta = new string[] { "Distance-PMF", graph.Name },
                F = d =>
                {
                    int distance = Convert.ToInt32(d);

                    if (distribution.ContainsKey(distance))
                    {
                        return new double[] { d, distribution[distance] / count };
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
            var partitions = partitioner.MultilevelBisimulationReduction();

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
    }
}
