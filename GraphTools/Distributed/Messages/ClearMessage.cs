using GraphTools.Distributed.Machines;

namespace GraphTools.Distributed.Messages
{
    /// <summary>
    /// A message which instructs a worker machine to reset its local partition to k = 0.
    /// </summary>
    class ClearMessage : AbstractMessage
    {
        public ClearMessage(AbstractMachine from) : base(from) { }

        public override int Size
        {
            get
            {
                return 0;
            }
        }
    }
}
