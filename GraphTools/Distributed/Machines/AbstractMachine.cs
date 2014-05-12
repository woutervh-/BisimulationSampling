using GraphTools.Distributed.Messages;
using System;
using System.Collections.Concurrent;

namespace GraphTools.Distributed.Machines
{
    abstract class AbstractMachine
    {
        /// <summary>
        /// Messages sent to this machine.
        /// </summary>
        private BlockingCollection<AbstractMessage> messages = new BlockingCollection<AbstractMessage>();

        /// <summary>
        /// Mutex lock.
        /// </summary>
        private object @lock = new object();

        /// <summary>
        /// Flag indicating whether this machine is running or not.
        /// </summary>
        private bool running = false;

        /// <summary>
        /// Number of messages which have been sent to this machine.
        /// </summary>
        private int messageCount = 0;

        /// <summary>
        /// Total size of all messages which have been sent to this machine.
        /// </summary>
        private int cumulativeSize = 0;

        /// <summary>
        /// Gets the visit times for this machine (number of messages sent to this machine).
        /// </summary>
        public int VisitTimes
        {
            get
            {
                return messageCount;
            }
        }

        /// <summary>
        /// Gets the data shipment for this machine (total size of messages sent to this machine).
        /// </summary>
        public int DataShipment
        {
            get
            {
                return cumulativeSize;
            }
        }

        /// <summary>
        /// Asynchronously send a message to this worker.
        /// </summary>
        /// <param name="message">Message to be sent.</param>
        public void SendMe(AbstractMessage message)
        {
            messages.Add(message);
        }

        /// <summary>
        /// Run this machine in the current thread.
        /// </summary>
        public void Run()
        {
            lock (@lock)
            {
                if (running)
                {
                    throw new InvalidOperationException();
                }

                running = true;
                // Console.WriteLine("Starting machine " + this);
            }

            while (running)
            {
                AbstractMessage message = messages.Take();
                messageCount += 1;
                cumulativeSize += message.Size;
                OnReceive(message);
            }
        }

        /// <summary>
        /// Asynchronously stop this machine.
        /// </summary>
        public void Stop()
        {
            lock (@lock)
            {
                if (!running)
                {
                    throw new InvalidOperationException();
                }

                running = false;
                SendMe(new StopMessage(this));
                // Console.WriteLine("Stopping machine " + this);
            }
        }

        /// <summary>
        /// Action to take upon receiving a message.
        /// </summary>
        /// <param name="message"></param>
        protected abstract void OnReceive(AbstractMessage message);
    }
}
