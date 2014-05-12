using GraphTools.Distributed.Machines;
using System.Collections.Generic;
using System.Linq;

namespace GraphTools.Distributed.Messages
{
    class RemapPartitionMessage : AbstractMessage
    {
        private IEnumerable<KeyValuePair<int, int>> map;

        public IEnumerable<KeyValuePair<int, int>> Map
        {
            get
            {
                return map;
            }
        }

        public RemapPartitionMessage(AbstractMachine from, IEnumerable<KeyValuePair<int, int>> map)
            : base(from)
        {
            this.map = map.ToArray();
        }

        public override int Size
        {
            get
            {
                return 2 * map.Count();
            }
        }
    }
}
