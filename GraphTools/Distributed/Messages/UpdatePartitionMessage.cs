using GraphTools.Distributed.Machines;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphTools.Distributed.Messages
{
    /// <summary>
    /// A message indicating local changes to the partition of a worker machine.
    /// </summary>
    /// <typeparam name="TNode"></typeparam>
    class UpdatePartitionMessage<TNode, TSignature> : AbstractMessage
    {
        /// <summary>
        /// Changes since the last partition.
        /// </summary>
        private IEnumerable<KeyValuePair<TNode, TSignature>> changes;

        /// <summary>
        /// Gets the changes since the last partition.
        /// </summary>
        public IEnumerable<KeyValuePair<TNode, TSignature>> Changes
        {
            get
            {
                return changes;
            }
        }

        /// <summary>
        /// Size of a the signature.
        /// </summary>
        private Func<TSignature, int> signatureSize;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="changes"></param>
        /// <param name="signatureSize"></param>
        public UpdatePartitionMessage(AbstractMachine from, IEnumerable<KeyValuePair<TNode, TSignature>> changes, Func<TSignature, int> signatureSize)
            : base(from)
        {
            this.changes = changes.ToArray();
            this.signatureSize = signatureSize;
        }

        public override int Size
        {
            get
            {
                // Sum of number of nodes and their signature sizes
                return changes.Sum(kvp => 1 + signatureSize(kvp.Value));
            }
        }
    }
}
