using GraphTools.Distributed;
using GraphTools.Graph;
using GraphTools.Helpers;
using System;

namespace GraphTools
{
    static partial class Experiments
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TLabel"></typeparam>
        /// <param name="graph"></param>
        /// <param name="M">Maximum number of machines (at least 1).</param>
        /// <returns></returns>
        public static Experiment MeasureDistributedMakespanEstimate<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, int M)
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
                Meta = new string[] { "Makespan", "Estimated", graph.Name },
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
        public static Experiment MeasureDistributedVisitTimesEstimate<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, int M)
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
        public static Experiment MeasureDistributedDataShipmentEstimate<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, int M)
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
        /// 
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TLabel"></typeparam>
        /// <param name="graph"></param>
        /// <param name="M">Maximum number of machines (at least 1).</param>
        /// <returns></returns>
        public static Experiment MeasureDistributedMakespanExact<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, int M)
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
                Meta = new string[] { "Makespan", "Exact", graph.Name },
                F = i =>
                {
                    int m = Convert.ToInt32(i);
                    sequentialPartitioner.ExactBisimulationReduction();
                    var sequentialTime = sequentialPartitioner.ElapsedMilliseconds;
                    distributedPartitioner[m].ExactBisimulationReduction();
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
        public static Experiment MeasureDistributedVisitTimesExact<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, int M)
        {
            var distributedPartitioner = new DistributedGraphPartitioner<TNode, TLabel>[M];

            for (int i = 0; i < M; i++)
            {
                distributedPartitioner[i] = new DistributedGraphPartitioner<TNode, TLabel>(i + 1, graph);
            }

            var experiment = new Experiment(2)
            {
                Labels = new string[] { "Number of machines", "Visit times" },
                Meta = new string[] { "VisitTimes", "Exact", graph.Name },
                F = i =>
                {
                    int m = Convert.ToInt32(i);
                    distributedPartitioner[m].ExactBisimulationReduction();
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
        public static Experiment MeasureDistributedDataShipmentExact<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, int M)
        {
            var distributedPartitioner = new DistributedGraphPartitioner<TNode, TLabel>[M];

            for (int i = 0; i < M; i++)
            {
                distributedPartitioner[i] = new DistributedGraphPartitioner<TNode, TLabel>(i + 1, graph);
            }

            var experiment = new Experiment(2)
            {
                Labels = new string[] { "Number of machines", "Data shipment" },
                Meta = new string[] { "DataShipment", "Exact", graph.Name },
                F = i =>
                {
                    int m = Convert.ToInt32(i);
                    distributedPartitioner[m].ExactBisimulationReduction();
                    var dataShipment = distributedPartitioner[m].DataShipment;

                    return new double[] { m + 1, dataShipment };
                },
            };

            experiment.Run(0, M - 1, 1, 10);
            return experiment;
        }
    }
}
