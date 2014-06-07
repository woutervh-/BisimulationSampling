BisimulationSampling
====================

Q: Where do I find the sampling algorithms?
A: All the sampling algorithms can be found in the file GraphTools/GraphSampler.cs; the induced subgraph can be obtained by passing the sampled node set through to the Induce function of the graph class.

Q: Where do I find the sequential bisimulation partition algorithms?
A: The sequential partition algorithms can be found in the file GraphTools/GraphPartitioner.cs; a reduced graph can be obtained by passing the partition to the ReducedGraph function in GraphTools/GraphGenerator.cs.

Q: Where do I find the distributed bisimulation partition algorithms?
A: The distributed partition algorithms can be found GraphTools/Distributed/Machines; there you can find classes for the signature-based (exact) algorithm's coordinator and worker, as well as for the hash-based (estimate) algorithm. Once again, a reduced graph can be obtained by passing the partition to the ReducedGraph function in GraphTools/GraphGenerator.cs.
