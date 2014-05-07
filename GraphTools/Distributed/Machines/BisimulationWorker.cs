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
    abstract class BisimulationWorker<TNode, TLabel, TSignature> : AbstractMachine
    {
        /// <summary>
        /// The graph.
        /// </summary>
        protected MultiDirectedGraph<TNode, TLabel> Graph;

        /// <summary>
        /// Owner function.
        /// </summary>
        protected IDictionary<TNode, BisimulationWorker<TNode, TLabel, TSignature>> Owner;

        /// <summary>
        /// Inverted owner function.
        /// </summary>
        protected IDictionary<BisimulationWorker<TNode, TLabel, TSignature>, HashSet<TNode>> OwnerInverted;

        /// <summary>
        /// Interested-in function.
        /// </summary>
        protected Dictionary<BisimulationWorker<TNode, TLabel, TSignature>, HashSet<TNode>> InterestedIn;

        /// <summary>
        /// Partition segment.
        /// </summary>
        protected Dictionary<TNode, TSignature> Partition;

        /// <summary>
        /// Function indicating which nodes have changed block since the previous partition.
        /// </summary>
        protected Dictionary<TNode, bool> Changed;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="partition"></param>
        /// <returns></returns>
        public static BisimulationWorker<TNode, TLabel, TSignature>[] CreateWorkers<TWorker>(MultiDirectedGraph<TNode, TLabel> graph, IDictionary<TNode, int> partition)
            where TWorker : BisimulationWorker<TNode, TLabel, TSignature>, new()
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

            var workers = new BisimulationWorker<TNode, TLabel, TSignature>[counter];
            for (int i = 0; i < counter; i++)
            {
                workers[i] = new TWorker();
                workers[i].setGraph(graph);
            }

            var owner = new Dictionary<TNode, BisimulationWorker<TNode, TLabel, TSignature>>();
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
        /// Constructor.
        /// </summary>
        protected BisimulationWorker()
        {
            //
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graph"></param>
        private void setGraph(MultiDirectedGraph<TNode, TLabel> graph)
        {
            this.Graph = graph;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="owner"></param>
        private void setOwner(IDictionary<TNode, BisimulationWorker<TNode, TLabel, TSignature>> owner)
        {
            this.Owner = owner;
            this.OwnerInverted = Utils.Invert(owner);
            this.InterestedIn = new Dictionary<BisimulationWorker<TNode, TLabel, TSignature>, HashSet<TNode>>();
            this.Changed = new Dictionary<TNode, bool>();

            // Initialize interested-in function
            foreach (var worker in OwnerInverted.Keys)
            {
                InterestedIn.Add(worker, new HashSet<TNode>());
            }
            foreach (var target in OwnerInverted[this])
            {
                foreach (var ei in Graph.In(target))
                {
                    var source = Graph.Source(ei);

                    if (!InterestedIn[owner[source]].Contains(target))
                    {
                        InterestedIn[owner[source]].Add(target);
                    }
                }
            }
            InterestedIn.Remove(this);

            // Initialize changed function
            foreach (var node in OwnerInverted[this])
            {
                Changed.Add(node, default(bool));
            }
        }

        protected override void OnReceive(AbstractMessage message)
        {
            TypeSwitch.On(message)
                .Case((ClearMessage clearMessage) =>
                {
                    // Reset partition and send sync message
                    Clear();
                    clearMessage.Sender.SendMe(new RefinedMessage(this));
                })
                .Case((RefineMessage refineMessage) =>
                {
                    // Perform local refine step
                    Refine();
                    refineMessage.Sender.SendMe(new RefinedMessage(this));
                })
                .Case((CountMessage countMessage) =>
                {
                    var blocks = new HashSet<TSignature>();
                    foreach (var node in OwnerInverted[this])
                    {
                        if (!blocks.Contains(Partition[node]))
                        {
                            blocks.Add(Partition[node]);
                        }
                    }

                    countMessage.Sender.SendMe(new CountedMessage<TSignature>(this, blocks));
                })
                .Case((ShareMessage shareMessage) =>
                {
                    // Share changes
                    foreach (var worker in OwnerInverted.Keys)
                    {
                        if (worker != this)
                        {
                            var changedNodesOfInterest = Partition.Where(kvp => InterestedIn[worker].Contains(kvp.Key) && Changed[kvp.Key]);

                            if (changedNodesOfInterest.Any())
                            {
                                worker.SendMe(new UpdatePartitionMessage<TNode, TSignature>(this, changedNodesOfInterest, SignatureSize));
                            }
                        }
                    }

                    shareMessage.Sender.SendMe(new SharedMessage(this));
                })
                .Case((UpdatePartitionMessage<TNode, TSignature> updatePartitionMessage) =>
                {
                    foreach (var change in updatePartitionMessage.Changes)
                    {
                        Partition[change.Key] = change.Value;
                    }
                })
                .Case((SegmentRequestMessage segmentRequestMessage) =>
                {
                    segmentRequestMessage.Sender.SendMe(new SegmentResponseMessage<TNode, TSignature>(this, Partition.Where(kvp => Owner[kvp.Key] == this)));
                });
        }

        /// <summary>
        /// Gets the size of a signature.
        /// </summary>
        /// <param name="signature"></param>
        /// <returns></returns>
        protected abstract int SignatureSize(TSignature signature);

        /// <summary>
        /// Resets the partition to k = 0.
        /// </summary>
        protected abstract void Clear();
        
        /// <summary>
        /// Refines the partition by performing a refinement step.
        /// </summary>
        protected abstract void Refine();
    }
}
