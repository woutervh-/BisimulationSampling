using GraphTools.Distributed;
using GraphTools.Graph;
using GraphTools.Helpers;
using System;

namespace GraphTools
{
    static partial class Experiments
    {
        /// <summary>
        /// Creates three experiments for measuring the makespan, visit times and data shipment of the distributed estimated bisimulation algorithm.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TLabel"></typeparam>
        /// <param name="graph"></param>
        /// <param name="M"></param>
        /// <returns></returns>
        public static Experiment[] MeasureDistributedPerformanceEstimate<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, int M)
        {
            var sequentialPartitioner = new GraphPartitioner<TNode, TLabel>(graph);
            var distributedPartitioner = new DistributedGraphPartitioner<TNode, TLabel>[M];

            for (int i = 0; i < M; i++)
            {
                distributedPartitioner[i] = new DistributedGraphPartitioner<TNode, TLabel>(i + 1, graph);
            }

            var experiment = new Experiment(5)
            {
                Labels = new string[] { "Number of machines", "Sequential (ms)", "Distributed (ms)", "Visit times", "Data shipment" },
                Meta = new string[] { "Performance", "Estimated", graph.Name },
                F = i =>
                {
                    int m = Convert.ToInt32(i);
                    sequentialPartitioner.EstimateBisimulationReduction();
                    var sequentialTime = sequentialPartitioner.ElapsedMilliseconds;
                    distributedPartitioner[m].EstimateBisimulationReduction();
                    var distributedTime = distributedPartitioner[m].ElapsedMilliseconds;
                    var visitTimes = distributedPartitioner[m].VisitTimes;
                    var dataShipment = distributedPartitioner[m].DataShipment;

                    return new double[] { m + 1, sequentialTime, distributedTime, visitTimes, dataShipment };
                },
            };

            experiment.Run(0, M - 1, 1, 10);

            var splits = new int[][]
            {
                new int[] { 0, 1, 2 },
                new int[] { 0, 3 },
                new int[] { 0, 4 }
            };

            var experiments = experiment.Split(splits);
            experiments[0].Meta = new string[] { "Makespan", "Estimated", graph.Name };
            experiments[1].Meta = new string[] { "VisitTimes", "Estimated", graph.Name };
            experiments[2].Meta = new string[] { "DataShipment", "Estimated", graph.Name };

            return experiments;
        }

        /// <summary>
        /// Creates three experiments for measuring the makespan, visit times and data shipment of the distributed exact bisimulation algorithm.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TLabel"></typeparam>
        /// <param name="graph"></param>
        /// <param name="M"></param>
        /// <returns></returns>
        public static Experiment[] MeasureDistributedPerformanceExact<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, int M)
        {
            var sequentialPartitioner = new GraphPartitioner<TNode, TLabel>(graph);
            var distributedPartitioner = new DistributedGraphPartitioner<TNode, TLabel>[M];

            for (int i = 0; i < M; i++)
            {
                distributedPartitioner[i] = new DistributedGraphPartitioner<TNode, TLabel>(i + 1, graph);
            }

            var experiment = new Experiment(5)
            {
                Labels = new string[] { "Number of machines", "Sequential (ms)", "Distributed (ms)", "Visit times", "Data shipment" },
                Meta = new string[] { "Performance", "Exact", graph.Name },
                F = i =>
                {
                    int m = Convert.ToInt32(i);
                    sequentialPartitioner.ExactBisimulationReduction();
                    var sequentialTime = sequentialPartitioner.ElapsedMilliseconds;
                    distributedPartitioner[m].ExactBisimulationReduction();
                    var distributedTime = distributedPartitioner[m].ElapsedMilliseconds;
                    var visitTimes = distributedPartitioner[m].VisitTimes;
                    var dataShipment = distributedPartitioner[m].DataShipment;

                    return new double[] { m + 1, sequentialTime, distributedTime, visitTimes, dataShipment };
                },
            };

            experiment.Run(0, M - 1, 1, 10);

            var splits = new int[][]
            {
                new int[] { 0, 1, 2 },
                new int[] { 0, 3 },
                new int[] { 0, 4 }
            };

            var experiments = experiment.Split(splits);
            experiments[0].Meta = new string[] { "Makespan", "Exact", graph.Name };
            experiments[1].Meta = new string[] { "VisitTimes", "Exact", graph.Name };
            experiments[2].Meta = new string[] { "DataShipment", "Exact", graph.Name };

            return experiments;
        }
    }
}
