using GraphTools.Distributed.Machines;

namespace GraphTools.Distributed.Messages
{
    /// <summary>
    /// A message indicating that a worker machine has performed a share step.
    /// </summary>
    class SharedMessage : AbstractMessage
    {
        public SharedMessage(AbstractMachine from) : base(from) { }

        public override int Size
        {
            get
            {
                return 0;
            }
        }
    }
}
