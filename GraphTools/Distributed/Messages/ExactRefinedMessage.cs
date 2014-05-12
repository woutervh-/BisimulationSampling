using GraphTools.Distributed.Machines;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphTools.Distributed.Messages
{
    /// <summary>
    /// A message indicating that a worker machine has performed a refinement step.
    /// </summary>
    class ExactRefinedMessage<TLabel> : AbstractMessage
    {
        /// <summary>
        /// 
        /// </summary>
        private IEnumerable<KeyValuePair<Tuple<TLabel, HashSet<Tuple<TLabel, int>>>, int>> partitionMap;

        public IEnumerable<KeyValuePair<Tuple<TLabel, HashSet<Tuple<TLabel, int>>>, int>> PartitionMap
        {
            get
            {
                return partitionMap;
            }
        }

        public ExactRefinedMessage(AbstractMachine from, IEnumerable<KeyValuePair<Tuple<TLabel, HashSet<Tuple<TLabel, int>>>, int>> partitionMap)
            : base(from)
        {
            this.partitionMap = partitionMap.ToArray();
        }

        public override int Size
        {
            get
            {
                return partitionMap.Sum(kvp => 2 + 2 * kvp.Key.Item2.Count);
            }
        }
    }
}
