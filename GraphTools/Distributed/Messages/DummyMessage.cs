using GraphTools.Distributed.Machines;

namespace GraphTools.Distributed.Messages
{
    class DummyMessage : AbstractMessage
    {
        public DummyMessage(AbstractMachine from) : base(from) { }

        public override int Size
        {
            get
            {
                return 0;
            }
        }
    }
}
