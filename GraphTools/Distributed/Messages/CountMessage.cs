using GraphTools.Distributed.Machines;

namespace GraphTools.Distributed.Messages
{
    class CountMessage : AbstractMessage
    {
        public CountMessage(AbstractMachine from) : base(from) { }

        public override int Size
        {
            get
            {
                return 0;
            }
        }
    }
}
