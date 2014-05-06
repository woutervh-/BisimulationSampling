using GraphTools.Distributed.Machines;
using System.Collections.Generic;
using System.Linq;

namespace GraphTools.Distributed.Messages
{
    /// <summary>
    /// A message containing the local partition of a worker machine.
    /// </summary>
    /// <typeparam name="TNode"></typeparam>
    class SegmentResponseMessage<TNode> : AbstractMessage
    {
        /// <summary>
        /// The local partition pairs of the worker (sender).
        /// </summary>
        private IEnumerable<KeyValuePair<TNode, int>> pairs;

        /// <summary>
        /// Gets the local partition pairs of the worker (sender).
        /// </summary>
        public IEnumerable<KeyValuePair<TNode, int>> Pairs
        {
            get
            {
                return pairs;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="pairs"></param>
        public SegmentResponseMessage(AbstractMachine from, IEnumerable<KeyValuePair<TNode, int>> pairs)
            : base(from)
        {
            this.pairs = pairs.ToArray();
        }

        public override int Size
        {
            get
            {
                return 2 * pairs.Count();
            }
        }
    }
}
