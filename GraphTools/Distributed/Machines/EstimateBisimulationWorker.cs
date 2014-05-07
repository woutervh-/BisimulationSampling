using GraphTools.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphTools.Distributed.Machines
{
    class EstimateBisimulationWorker<TNode, TLabel> : BisimulationWorker<TNode, TLabel, int>
    {
        /// <summary>
        /// Local Crc32 instance to avoid mutexing.
        /// </summary>
        private Crc32 crc32 = new Crc32();

        /// <summary>
        /// Hashes a list of objects.
        /// The hash is dependent on the order of the items in the list.
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public int Hash(params object[] list)
        {
            byte[] buffer = new byte[4 * list.Length];

            for (int i = 0; i < list.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(list[i].GetHashCode()), 0, buffer, 4 * i, 4);
            }

            byte[] hash = crc32.ComputeHash(buffer);

            return BitConverter.ToInt32(hash, 0);
        }

        protected override int SignatureSize(int signature)
        {
            return 1;
        }

        protected override void Clear()
        {
            Partition = new Dictionary<TNode, int>();

            foreach (var node in Graph.Nodes)
            {
                if (Owner[node] == this)
                {
                    Partition.Add(node, Hash(Graph.NodeLabel(node)));
                    Changed[node] = true;
                }
                else
                {
                    Partition.Add(node, default(int));
                }
            }
        }

        protected override void Refine()
        {
            var newPartition = new Dictionary<TNode, int>();

            foreach (var u in Graph.Nodes)
            {
                if (Owner[u] == this)
                {
                    var H = new HashSet<int>();
                    H.Add(Hash(Graph.NodeLabel(u)));

                    // Compute the hashes for pairs (label[u, v], pId_k-1(v))
                    foreach (var eo in Graph.Out(u))
                    {
                        var v = Graph.Target(eo);
                        var edgeHash = Hash(Graph.EdgeLabel(eo), Partition[v]);
                        if (!H.Contains(edgeHash))
                        {
                            H.Add(edgeHash);
                        }
                    }

                    // Combine the hashes using some associative-commutative operator
                    int hash = 0;
                    foreach (var edgeHash in H)
                    {
                        hash += edgeHash;
                    }

                    // Assign partition block to node
                    newPartition.Add(u, hash);

                    // Determine if u has changed
                    Changed[u] = Partition[u] != hash;
                }
                else
                {
                    newPartition.Add(u, Partition[u]);
                }
            }

            Partition = newPartition;
        }
    }
}
