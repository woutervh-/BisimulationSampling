using GraphTools.Distributed.Machines;

namespace GraphTools.Distributed.Messages
{
    class StopMessage : AbstractMessage
    {
        public StopMessage(AbstractMachine from) : base(from) { }

        public override int Size
        {
            get
            {
                return 0;
            }
        }
    }
}
