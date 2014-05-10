using GraphTools.Distributed.Machines;

namespace GraphTools.Distributed.Messages
{
    /// <summary>
    /// A message indicating that a worker machine has performed a refinement step.
    /// </summary>
    class ExactRefinedMessage : AbstractMessage
    {
        public ExactRefinedMessage(AbstractMachine from) : base(from) { }

        public override int Size
        {
            get
            {
                return 0;
            }
        }
    }
}
