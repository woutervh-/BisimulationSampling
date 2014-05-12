using GraphTools.Distributed.Messages;
using GraphTools.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphTools.Distributed.Machines
{
    class ExactBisimulationCoordinator<TNode, TLabel> : AbstractMachine
    {
        /// <summary>
        /// The workers running the bisimulation.
        /// </summary>
        private IEnumerable<AbstractMachine> workers;

        /// <summary>
        /// 
        /// </summary>
        private int k_max;

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<TNode, int> partition;

        /// <summary>
        /// 
        /// </summary>
        private int oldNumBlocks;

        /// <summary>
        /// 
        /// </summary>
        private HashSet<int> blocks;

        /// <summary>
        /// States of workers.
        /// </summary>
        private Dictionary<AbstractMachine, WorkerState> state;

        /// <summary>
        /// 
        /// </summary>
        private Action<int, IDictionary<TNode, int>> onComplete;

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<Tuple<TLabel, HashSet<Tuple<TLabel, int>>>, int> signatures;

        /// <summary>
        /// 
        /// </summary>
        private int counter;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="onComplete"></param>
        public ExactBisimulationCoordinator(Action<int, IDictionary<TNode, int>> onComplete)
        {
            this.onComplete = onComplete;
        }

        protected override void OnReceive(AbstractMessage message)
        {
            TypeSwitch.On(message)
                .Case((CoordinatorMessage coordinatorMessage) =>
                {
                    k_max = 0;
                    partition = new Dictionary<TNode, int>();
                    oldNumBlocks = 0;
                    blocks = new HashSet<int>();
                    workers = coordinatorMessage.Workers;
                    state = new Dictionary<AbstractMachine, WorkerState>();

                    var comparer1 = EqualityComparer<TLabel>.Default;
                    var comparer2 = new Utils.HashSetEqualityComparer<Tuple<TLabel, int>>();
                    var comparer = new Utils.PairEqualityComparer<TLabel, HashSet<Tuple<TLabel, int>>>(comparer1, comparer2);
                    signatures = new Dictionary<Tuple<TLabel, HashSet<Tuple<TLabel, int>>>, int>(comparer);
                    counter = 0;

                    foreach (var worker in workers)
                    {
                        state.Add(worker, WorkerState.Refining);
                        worker.SendMe(new ClearMessage(this));
                    }
                })
                .Case((ExactRefinedMessage<TLabel> refinedMessage) =>
                {
                    state[refinedMessage.Sender] = WorkerState.Waiting;

                    var remap = new Dictionary<int, int>();
                    foreach (var kvp in refinedMessage.PartitionMap)
                    {
                        if (!signatures.ContainsKey(kvp.Key))
                        {
                            signatures.Add(kvp.Key, counter++);
                        }

                        remap.Add(kvp.Value, signatures[kvp.Key]);
                    }

                    refinedMessage.Sender.SendMe(new RemapPartitionMessage(this, remap));

                    if (workers.All(worker => state[worker] == WorkerState.Waiting))
                    {
                        signatures.Clear();

                        // All workers have refined, now perform a share step
                        foreach (var worker in workers)
                        {
                            state[worker] = WorkerState.Sharing;
                            worker.SendMe(new ShareMessage(this));
                        }
                    }
                })
                .Case((SharedMessage sharedMessage) =>
                {
                    state[sharedMessage.Sender] = WorkerState.Waiting;

                    if (workers.All(worker => state[worker] == WorkerState.Waiting))
                    {
                        // All workers have shared, now count the number of unqiue blocks
                        oldNumBlocks = blocks.Count;
                        blocks.Clear();
                        foreach (var worker in workers)
                        {
                            state[worker] = WorkerState.Counting;
                            worker.SendMe(new CountMessage(this));
                        }
                    }
                })
                .Case((CountedMessage countedMessage) =>
                {
                    state[countedMessage.Sender] = WorkerState.Waiting;
                    blocks.UnionWith(countedMessage.Blocks);

                    if (workers.All(worker => state[worker] == WorkerState.Waiting))
                    {
                        if (blocks.Count > oldNumBlocks)
                        {
                            // There was a change, continue refining
                            k_max += 1;
                            foreach (var worker in workers)
                            {
                                state[worker] = WorkerState.Refining;
                                worker.SendMe(new RefineMessage(this));
                            }
                        }
                        else
                        {
                            // We're done, collect global partition
                            k_max -= 1;
                            foreach (var worker in workers)
                            {
                                state[worker] = WorkerState.Collecting;
                                worker.SendMe(new SegmentRequestMessage(this));
                            }
                        }
                    }
                })
                .Case((SegmentResponseMessage<TNode> segmentResponseMessage) =>
                {
                    state[segmentResponseMessage.Sender] = WorkerState.Waiting;

                    foreach (var pair in segmentResponseMessage.Pairs)
                    {
                        partition.Add(pair.Key, pair.Value);
                    }

                    if (workers.All(worker => state[worker] == WorkerState.Waiting))
                    {
                        onComplete(k_max, partition);
                    }
                });
        }

        /// <summary>
        /// Worker states.
        /// </summary>
        private enum WorkerState
        {
            Collecting,
            Counting,
            Refining,
            Sharing,
            Waiting,
        }
    }
}
