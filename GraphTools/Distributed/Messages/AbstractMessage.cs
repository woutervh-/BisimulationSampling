using GraphTools.Distributed.Machines;

namespace GraphTools.Distributed.Messages
{
    /// <summary>
    /// Represents an abstract message which can be sent to machines.
    /// </summary>
    abstract class AbstractMessage
    {
        /// <summary>
        /// The machine which sent this message.
        /// </summary>
        private AbstractMachine sender;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sender"></param>
        public AbstractMessage(AbstractMachine sender)
        {
            this.sender = sender;
        }

        /// <summary>
        /// Gets the machine which sent this message.
        /// </summary>
        public AbstractMachine Sender
        {
            get
            {
                return sender;
            }
        }

        /// <summary>
        /// Gets the size of the message data.
        /// </summary>
        public abstract int Size
        {
            get;
        }
    }
}
