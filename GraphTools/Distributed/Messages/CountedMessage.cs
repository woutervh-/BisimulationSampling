using GraphTools.Distributed.Machines;
using System.Collections.Generic;
using System.Linq;

namespace GraphTools.Distributed.Messages
{
    class CountedMessage : AbstractMessage
    {
        private IEnumerable<int> blocks;

        public IEnumerable<int> Blocks
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
        public CountedMessage(AbstractMachine from, IEnumerable<int> blocks)
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
