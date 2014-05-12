using GraphTools.Distributed.Messages;
using GraphTools.Graph;
using GraphTools.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphTools.Distributed.Machines
{
    class ExactBisimulationWorker<TNode, TLabel> : AbstractMachine
    {
        /// <summary>
        /// The graph.
        /// </summary>
        private MultiDirectedGraph<TNode, TLabel> graph;

        /// <summary>
        /// Owner function.
        /// </summary>
        private IDictionary<TNode, ExactBisimulationWorker<TNode, TLabel>> owner;

        /// <summary>
        /// Inverted owner function.
        /// </summary>
        private IDictionary<ExactBisimulationWorker<TNode, TLabel>, HashSet<TNode>> ownerInverted;

        /// <summary>
        /// Interested-in function.
        /// </summary>
        private Dictionary<ExactBisimulationWorker<TNode, TLabel>, HashSet<TNode>> interestedIn;

        /// <summary>
        /// Partition segment.
        /// </summary>
        private Dictionary<TNode, int> partition;

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<Tuple<TLabel, HashSet<Tuple<TLabel, int>>>, int> partitionMap;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="partition"></param>
        /// <returns></returns>
        public static ExactBisimulationWorker<TNode, TLabel>[] CreateWorkers(MultiDirectedGraph<TNode, TLabel> graph, IDictionary<TNode, int> partition)
        {
            var mapping = new Dictionary<int, int>();
            int counter = 0;

            foreach (var kvp in partition)
            {
                if (!mapping.ContainsKey(kvp.Value))
                {
                    mapping.Add(kvp.Value, counter);
                    counter += 1;
                }
            }

            var workers = new ExactBisimulationWorker<TNode, TLabel>[counter];
            for (int i = 0; i < counter; i++)
            {
                workers[i] = new ExactBisimulationWorker<TNode, TLabel>();
                workers[i].setGraph(graph);
            }

            var owner = new Dictionary<TNode, ExactBisimulationWorker<TNode, TLabel>>();
            foreach (var node in graph.Nodes)
            {
                owner.Add(node, workers[mapping[partition[node]]]);
            }

            for (int i = 0; i < counter; i++)
            {
                workers[i].setOwner(owner);
            }

            return workers;
        }

        /// <summary>
        /// Private constructor.
        /// </summary>
        private ExactBisimulationWorker()
        {
            //
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graph"></param>
        private void setGraph(MultiDirectedGraph<TNode, TLabel> graph)
        {
            this.graph = graph;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="owner"></param>
        private void setOwner(IDictionary<TNode, ExactBisimulationWorker<TNode, TLabel>> owner)
        {
            this.owner = owner;
            this.ownerInverted = Utils.Invert(owner);
            this.interestedIn = new Dictionary<ExactBisimulationWorker<TNode, TLabel>, HashSet<TNode>>();

            // Initialize interested-in function
            foreach (var worker in ownerInverted.Keys)
            {
                interestedIn.Add(worker, new HashSet<TNode>());
            }
            foreach (var target in ownerInverted[this])
            {
                foreach (var ei in graph.In(target))
                {
                    var source = graph.Source(ei);

                    if (!interestedIn[owner[source]].Contains(target))
                    {
                        interestedIn[owner[source]].Add(target);
                    }
                }
            }
            interestedIn.Remove(this);
        }

        protected override void OnReceive(AbstractMessage message)
        {
            TypeSwitch.On(message)
                .Case((ClearMessage clearMessage) =>
                {
                    // Reset partition and send sync message
                    clear();
                    clearMessage.Sender.SendMe(new ExactRefinedMessage<TLabel>(this, partitionMap));
                })
                .Case((RefineMessage refineMessage) =>
                {
                    // Perform local refine step
                    refine();
                    refineMessage.Sender.SendMe(new ExactRefinedMessage<TLabel>(this, partitionMap));
                })
                .Case((RemapPartitionMessage remapPartitionMessage) =>
                {
                    var remap = new Dictionary<int, int>();

                    foreach (var kvp in remapPartitionMessage.Map)
                    {
                        remap.Add(kvp.Key, kvp.Value);
                    }

                    foreach (var node in ownerInverted[this])
                    {
                        partition[node] = remap[partition[node]];
                    }
                })
                .Case((CountMessage countMessage) =>
                {
                    var blocks = new HashSet<int>();
                    foreach (var node in ownerInverted[this])
                    {
                        if (!blocks.Contains(partition[node]))
                        {
                            blocks.Add(partition[node]);
                        }
                    }

                    countMessage.Sender.SendMe(new CountedMessage(this, blocks));
                })
                .Case((ShareMessage shareMessage) =>
                {
                    // Share changes
                    foreach (var worker in ownerInverted.Keys)
                    {
                        if (worker != this)
                        {
                            var nodesOfInterest = partition.Where(kvp => interestedIn[worker].Contains(kvp.Key));

                            if (nodesOfInterest.Any())
                            {
                                worker.SendMe(new UpdatePartitionMessage<TNode>(this, nodesOfInterest));
                            }
                        }
                    }

                    shareMessage.Sender.SendMe(new SharedMessage(this));
                })
                .Case((UpdatePartitionMessage<TNode> updatePartitionMessage) =>
                {
                    foreach (var change in updatePartitionMessage.Changes)
                    {
                        partition[change.Key] = change.Value;
                    }
                })
                .Case((SegmentRequestMessage segmentRequestMessage) =>
                {
                    segmentRequestMessage.Sender.SendMe(new SegmentResponseMessage<TNode>(this, partition.Where(kvp => owner[kvp.Key] == this)));
                });
        }

        private void clear()
        {
            var comparer1 = EqualityComparer<TLabel>.Default;
            var comparer2 = new Utils.HashSetEqualityComparer<Tuple<TLabel, int>>();
            var comparer = new Utils.PairEqualityComparer<TLabel, HashSet<Tuple<TLabel, int>>>(comparer1, comparer2);

            partition = new Dictionary<TNode, int>();
            partitionMap = new Dictionary<Tuple<TLabel, HashSet<Tuple<TLabel, int>>>, int>(comparer);
            int counter = 0;

            foreach (var node in graph.Nodes)
            {
                if (owner[node] == this)
                {
                    var signature = Tuple.Create(graph.NodeLabel(node), new HashSet<Tuple<TLabel, int>>());

                    if (!partitionMap.ContainsKey(signature))
                    {
                        partitionMap.Add(signature, counter++);
                    }

                    partition.Add(node, partitionMap[signature]);
                }
            }
        }

        private void refine()
        {
            var newPartition = new Dictionary<TNode, int>();
            partitionMap.Clear();
            int counter = 0;

            foreach (var u in graph.Nodes)
            {
                if (owner[u] == this)
                {
                    var S = new HashSet<Tuple<TLabel, int>>();
                    foreach (var eo in graph.Out(u))
                    {
                        var v = graph.Target(eo);
                        var t = Tuple.Create(graph.EdgeLabel(eo), partition[v]);
                        if (!S.Contains(t))
                        {
                            S.Add(t);
                        }
                    }

                    var signature = Tuple.Create(graph.NodeLabel(u), S);

                    if (!partitionMap.ContainsKey(signature))
                    {
                        partitionMap.Add(signature, counter++);
                    }

                    // Assign partition block to node
                    newPartition.Add(u, partitionMap[signature]);
                }
            }

            partition = newPartition;
        }
    }
}
