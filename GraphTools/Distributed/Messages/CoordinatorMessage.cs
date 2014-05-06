using GraphTools.Distributed.Machines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphTools.Distributed.Messages
{
    /// <summary>
    /// Instructs a coordinator to start the bisimulation partition computation.
    /// </summary>
    class CoordinatorMessage : AbstractMessage
    {
        /// <summary>
        /// The workers for this bisimulation job.
        /// </summary>
        private IEnumerable<AbstractMachine> workers;

        /// <summary>
        /// Gets the workers for this bisimulation job.
        /// </summary>
        public IEnumerable<AbstractMachine> Workers
        {
            get
            {
                return workers;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="workers"></param>
        public CoordinatorMessage(AbstractMachine from, IEnumerable<AbstractMachine> workers)
            : base(from)
        {
            this.workers = workers.ToArray();
        }

        public override int Size
        {
            get
            {
                return workers.Count();
            }
        }
    }
}
