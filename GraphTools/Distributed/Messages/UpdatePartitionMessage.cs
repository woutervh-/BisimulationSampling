using GraphTools.Distributed.Machines;
using System.Collections.Generic;
using System.Linq;

namespace GraphTools.Distributed.Messages
{
    /// <summary>
    /// A message indicating local changes to the partition of a worker machine.
    /// </summary>
    /// <typeparam name="TNode"></typeparam>
    class UpdatePartitionMessage<TNode> : AbstractMessage
    {
        /// <summary>
        /// Changes since the last partition.
        /// </summary>
        private IEnumerable<KeyValuePair<TNode, int>> changes;

        /// <summary>
        /// Gets the changes since the last partition.
        /// </summary>
        public IEnumerable<KeyValuePair<TNode, int>> Changes
        {
            get
            {
                return changes;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="changes"></param>
        public UpdatePartitionMessage(AbstractMachine from, IEnumerable<KeyValuePair<TNode, int>> changes)
            : base(from)
        {
            this.changes = changes.ToArray();
        }

        public override int Size
        {
            get
            {
                return 2 * changes.Count();
            }
        }
    }
}
