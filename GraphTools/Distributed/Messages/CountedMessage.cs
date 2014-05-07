using GraphTools.Distributed.Machines;
using System.Collections.Generic;
using System.Linq;

namespace GraphTools.Distributed.Messages
{
    class CountedMessage<TSignature> : AbstractMessage
    {
        private IEnumerable<TSignature> blocks;

        public IEnumerable<TSignature> Blocks
        {
            get
            {
                return blocks;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="count"></param>
        public CountedMessage(AbstractMachine from, IEnumerable<TSignature> blocks)
            : base(from)
        {
            this.blocks = blocks.ToArray();
        }

        public override int Size
        {
            get
            {
                return blocks.Count();
            }
        }
    }
}
