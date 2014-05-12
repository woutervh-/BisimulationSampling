using GraphTools.Graph;
using GraphTools.Helpers;
using System;

namespace GraphTools
{
    static partial class Experiments
    {
        /// <summary>
        /// Coverage and correctness vs sample fraction.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="labels"></param>
        /// <param name="samplerName"></param>
        /// <param name="sampler"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static Experiment StandardBisimulationMetrics<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, string samplerName, Func<double, MultiDirectedGraph<TNode, TLabel>> sampler, int k)
        {
            double[] percentages = new double[]
            {
                1.0, 2.0, 4.0, 8.0, 16.0, 32.0, 64.0
            };

            var experiment = new Experiment(3)
            {
                Labels = new string[] { "Sample fraction", "Coverage", "Correctness" },
                Meta = new string[] { "Standard", graph.Name, samplerName, k + "-bisimulation" },
                F = i =>
                {
                    double p = percentages[Convert.ToInt32(i)] / 100.0;
                    var sample = sampler(p);
                    var counts = GraphMetrics.BisimulationEquivalence(graph, sample, k);

                    // Graph block count
                    double N1 = counts.Item1;
                    // Sample block count
                    double N2 = counts.Item2;
                    // Shared block count
                    double NS = N1 + N2 - (double)counts.Item3;

                    double coverage = NS / N1;
                    double correctness = NS / N2;

                    return new double[] { p, coverage, correctness };
                },
            };

            experiment.Run(0, percentages.Length - 1, 1, 10);
            return experiment;
        }

        /// <summary>
        /// Weighted coverage and correctness vs sample fraction.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="labels"></param>
        /// <param name="samplerName"></param>
        /// <param name="sampler"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static Experiment WeightedBisimulationMetrics<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, string samplerName, Func<double, MultiDirectedGraph<TNode, TLabel>> sampler, int k)
        {
            double[] percentages = new double[]
            {
                1.0, 2.0, 4.0, 8.0, 16.0, 32.0, 64.0
            };

            var experiment = new Experiment(3)
            {
                Labels = new string[] { "Sample fraction", "Weighted coverage", "Weighted correctness" },
                Meta = new string[] { "Weighted", graph.Name, samplerName, k + "-bisimulation" },
                F = i =>
                {
                    double p = percentages[Convert.ToInt32(i)] / 100.0;
                    var sample = sampler(p);
                    var counts = GraphMetrics.WeightedBisimulationEquivalence(graph, sample, k);

                    // Weighted coverage
                    double wr = (double)counts.Item1 / (double)graph.NumNodes;
                    // Weighted correctness
                    double wp = (double)counts.Item2 / (double)sample.NumNodes;

                    return new double[] { p, wr, wp };
                },
            };

            experiment.Run(0, percentages.Length - 1, 1, 10);
            return experiment;
        }
    }
}
