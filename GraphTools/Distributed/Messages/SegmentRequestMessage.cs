using GraphTools.Distributed.Machines;

namespace GraphTools.Distributed.Messages
{
    /// <summary>
    /// A message instructing a worker machine to ship its local partition to the sender of this message.
    /// </summary>
    class SegmentRequestMessage : AbstractMessage
    {
        public SegmentRequestMessage(AbstractMachine from) : base(from) { }

        public override int Size
        {
            get
            {
                return 0;
            }
        }
    }
}
