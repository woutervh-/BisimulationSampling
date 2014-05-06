using GraphTools.Distributed.Machines;
using GraphTools.Distributed.Messages;
using GraphTools.Graph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphTools.Distributed
{
    class DistributedGraphPartitioner<TNode, TLabel>
    {
        /// <summary>
        /// Graph to partition.
        /// </summary>
        private MultiDirectedGraph<TNode, TLabel> graph;

        /// <summary>
        /// Number of machines.
        /// </summary>
        private int m;

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
        /// Number of messages sent across all machines.
        /// </summary>
        private int visitTimes = 0;

        /// <summary>
        /// Gets the number of messages sent across all machines.
        /// </summary>
        public int VisitTimes
        {
            get
            {
                return visitTimes;
            }
        }

        /// <summary>
        /// Total size of all the messages sent.
        /// </summary>
        private int dataShipment = 0;

        /// <summary>
        /// Gets the total size of all the messages sent.
        /// </summary>
        public int DataShipment
        {
            get
            {
                return dataShipment;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="m">Number of machines.</param>
        /// <param name="graph"></param>
        public DistributedGraphPartitioner(int m, MultiDirectedGraph<TNode, TLabel> graph)
        {
            this.graph = graph;
            this.m = m;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IDictionary<TNode, int> EstimateBisimulationReduction()
        {
            IDictionary<TNode, int> distributedPartition = null;
            var segments = DistributedUtils.ExploreSplit(graph, m);
            var workers = BisimulationWorker<TNode, TLabel>.CreateWorkers(graph, segments);
            BisimulationCoordinator<TNode> coordinator = null;
            coordinator = new BisimulationCoordinator<TNode>((k_max, foundPartition) =>
            {
                // Console.WriteLine("k_max=" + k_max);
                distributedPartition = foundPartition;

                coordinator.Stop();
                foreach (var worker in workers)
                {
                    worker.Stop();
                }
            });

            // Start bisimulation partition computation
            coordinator.SendMe(new CoordinatorMessage(null, workers));

            // Create tasks
            var tasks = new Task[m + 1];
            for (int i = 0; i < m; i++)
            {
                tasks[i] = new Task(workers[i].Run);
            }
            tasks[m] = new Task(coordinator.Run);

            stopwatch.Reset();
            stopwatch.Start();
            {
                // Run each task
                foreach (var task in tasks)
                {
                    task.Start();
                }

                // Wait for each task to finish
                foreach (var task in tasks)
                {
                    task.Wait();
                }
            }
            stopwatch.Stop();

            visitTimes = workers.Sum(worker => worker.VisitTimes);
            dataShipment = workers.Sum(worker => worker.DataShipment);

            return distributedPartition;
        }
    }
}
