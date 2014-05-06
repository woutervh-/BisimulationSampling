using GraphTools.Distributed.Machines;

namespace GraphTools.Distributed.Messages
{
    /// <summary>
    /// A message for instructing a worker machine to perform a share step.
    /// </summary>
    class ShareMessage : AbstractMessage
    {
        public ShareMessage(AbstractMachine from) : base(from) { }

        public override int Size
        {
            get
            {
                return 0;
            }
        }
    }
}
