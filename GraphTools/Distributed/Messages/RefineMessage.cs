using GraphTools.Distributed.Machines;

namespace GraphTools.Distributed.Messages
{
    /// <summary>
    /// A message for instructing a worker machine to perform a local refinement step.
    /// </summary>
    class RefineMessage : AbstractMessage
    {
        public RefineMessage(AbstractMachine from) : base(from) { }

        public override int Size
        {
            get
            {
                return 0;
            }
        }
    }
}
