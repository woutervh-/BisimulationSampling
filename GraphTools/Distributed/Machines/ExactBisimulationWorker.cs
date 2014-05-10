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
        /// Function indicating which nodes have changed block since the previous partition.
        /// </summary>
        private Dictionary<TNode, bool> changed;

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
            this.changed = new Dictionary<TNode, bool>();

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

            // Initialize changed function
            foreach (var node in ownerInverted[this])
            {
                changed.Add(node, default(bool));
            }
        }

        protected override void OnReceive(AbstractMessage message)
        {
            TypeSwitch.On(message)
                .Case((ClearMessage clearMessage) =>
                {
                    // Reset partition and send sync message
                    clear();
                    clearMessage.Sender.SendMe(new ExactRefinedMessage(this));
                })
                .Case((RefineMessage refineMessage) =>
                {
                    // Perform local refine step
                    refine();
                    refineMessage.Sender.SendMe(new ExactRefinedMessage(this));
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
                            var changedNodesOfInterest = partition.Where(kvp => interestedIn[worker].Contains(kvp.Key) && changed[kvp.Key]);

                            if (changedNodesOfInterest.Any())
                            {
                                worker.SendMe(new UpdatePartitionMessage<TNode>(this, changedNodesOfInterest));
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
            partition = new Dictionary<TNode, int>();

            foreach (var node in graph.Nodes)
            {
                if (owner[node] == this)
                {
                    partition.Add(node, Hash(graph.NodeLabel(node)));
                    changed[node] = true;
                }
                else
                {
                    partition.Add(node, default(int));
                }
            }
        }

        private void refine()
        {
            var newPartition = new Dictionary<TNode, int>();

            foreach (var u in graph.Nodes)
            {
                if (owner[u] == this)
                {
                    var H = new HashSet<int>();
                    H.Add(Hash(graph.NodeLabel(u)));

                    // Compute the hashes for pairs (label[u, v], pId_k-1(v))
                    foreach (var eo in graph.Out(u))
                    {
                        var v = graph.Target(eo);
                        var edgeHash = Hash(graph.EdgeLabel(eo), partition[v]);
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
                    newPartition.Add(u, hash);

                    // Determine if u has changed
                    changed[u] = partition[u] != hash;
                }
                else
                {
                    newPartition.Add(u, partition[u]);
                }
            }

            partition = newPartition;
        }
    }
}
