﻿using GraphTools.Distributed;
using GraphTools.Distributed.Machines;
using GraphTools.Distributed.Messages;
using GraphTools.Graph;
using GraphTools.Helpers;
using GraphTools.Plot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GraphTools
{
    /// <summary>
    /// Main program.
    /// Serves as a playground for setting up experiments and converting data.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Run all performance experiments sequentially.
        /// </summary>
        public static void AllPerformanceExperiments(string filter = "*")
        {
            string path = Input("Please enter the path to folder with graph files", string.Copy);
            int M = Input("Maximum number of machines?", int.Parse);
            int algorithm = Select("Algorithm?", new string[] { "Exact (explore)", "Estimate (explore)", "Exact (random)", "Estimate (random)", "All of the above" });

            string outPath = Path.GetDirectoryName(path) + @"\Performance";
            if (!Directory.Exists(outPath))
            {
                Directory.CreateDirectory(outPath);
            }

            string[] filePaths = Directory.GetFiles(path, filter + ".xml");
            foreach (var filePath in filePaths)
            {
                var graph = GraphLoader.LoadGraphML(filePath, int.Parse, int.Parse);

                Experiment[] experiments = null;
                switch (algorithm)
                {
                    case 0:
                        experiments = Experiments.MeasureDistributedPerformanceExact(graph, M, DistributedUtils.ExploreSplit, "Explore");
                        break;
                    case 1:
                        experiments = Experiments.MeasureDistributedPerformanceEstimate(graph, M, DistributedUtils.ExploreSplit, "Explore");
                        break;
                    case 2:
                        experiments = Experiments.MeasureDistributedPerformanceExact(graph, M, DistributedUtils.RandomSplit, "Random");
                        break;
                    case 3:
                        experiments = Experiments.MeasureDistributedPerformanceEstimate(graph, M, DistributedUtils.RandomSplit, "Random");
                        break;
                    case 4:
                        var exps1 = Experiments.MeasureDistributedPerformanceExact(graph, M, DistributedUtils.ExploreSplit, "Explore");
                        var exps2 = Experiments.MeasureDistributedPerformanceEstimate(graph, M, DistributedUtils.ExploreSplit, "Explore");
                        var exps3 = Experiments.MeasureDistributedPerformanceExact(graph, M, DistributedUtils.RandomSplit, "Random");
                        var exps4 = Experiments.MeasureDistributedPerformanceEstimate(graph, M, DistributedUtils.RandomSplit, "Random");
                        experiments = exps1.Concat(exps2).Concat(exps3).Concat(exps4).ToArray();
                        break;
                }

                foreach (var experiment in experiments)
                {
                    var plot = experiment.Plot(0, double.NaN);
                    experiment.GetHorizontalAxis(plot).MinorTickSize = 0;
                    experiment.GetHorizontalAxis(plot).MajorStep = 1;
                    Experiment.SaveSVG(outPath + @"\" + string.Join("_", experiment.Meta) + ".svg", plot);
                    experiment.SaveTSV(outPath + @"\" + string.Join("_", experiment.Meta) + ".tsv");
                }
            }
        }

        /// <summary>
        /// Run all analytics experiments.
        /// </summary>
        public static void AllAnalyticsExperiments(string filter = "*")
        {
            string path = Input("Please enter the path to folder with graph files", string.Copy);
            string[] filePaths = Directory.GetFiles(path, filter + ".xml");

            string outPath = Path.GetDirectoryName(path) + @"\Analytics";
            if (!Directory.Exists(outPath))
            {
                Directory.CreateDirectory(outPath);
            }

            foreach (var filePath in filePaths)
            {
                var graph = GraphLoader.LoadGraphML(filePath, int.Parse, int.Parse);

                var exp1 = Experiments.DistanceProbabilityMassFunction(graph);
                Experiment.SaveSVG(outPath + @"\" + string.Join("_", exp1.Meta) + ".svg", exp1.Plot(0, double.NaN));
                exp1.SaveTSV(outPath + @"\" + string.Join("_", exp1.Meta) + ".tsv");

                var exp2 = Experiments.BisimulationPartitionSize(graph);
                Experiment.SaveSVG(outPath + @"\" + string.Join("_", exp2.Meta) + ".svg", exp2.Plot(0, double.NaN));
                exp2.SaveTSV(outPath + @"\" + string.Join("_", exp2.Meta) + ".tsv");

                var exp3 = Experiments.PartitionBlockDistribution(graph);
                Experiment.SaveSVG(outPath + @"\" + string.Join("_", exp3.Meta) + ".svg", exp3.Plot(0, double.NaN));
                exp3.SaveTSV(outPath + @"\" + string.Join("_", exp3.Meta) + ".tsv");
            }
        }

        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            //*
            Dummy.Fix();
            return;
            //*/

            /*
            AllPerformanceExperiments();
            return;
            //*/

            //*
            AllAnalyticsExperiments("Petrinet_no_labels");
            return;
            //*/

            // Ask for graph file
            string path = Input("Please enter the path to the graph file", string.Copy);
            string outPath = Path.GetDirectoryName(path) + @"\..\Results";

            // Load graph and labels
            var graph = GraphLoader.LoadGraphML(path, int.Parse, int.Parse);

            /*
            var partitioner = new GraphPartitioner<int, int>(graph);
            var distributedPartitioner = new DistributedGraphPartitioner<int, int>(1, graph);
            //*/

            /*
            var estim = GraphGenerator.ReducedGraph(graph, distributedPartitioner.ExactBisimulationReduction);
            var exact = GraphGenerator.ReducedGraph(graph, partitioner.EstimateBisimulationReduction);
            //*/

            //*
            // GraphConverter.SaveToGraphML(estim, Path.GetDirectoryName(path) + @"\" + graph.Name + "_estim.xml");
            // GraphConverter.SaveToGraphML(exact, Path.GetDirectoryName(path) + @"\" + graph.Name + "_exact.xml");
            // GraphConverter.SaveToGraphML(coarse, Path.GetDirectoryName(path) + @"\" + graph.Name + "_coarse.xml");
            //*/

            // Samplers
            var samplers = new Func<double, MultiDirectedGraph<int, int>>[]
            {
                //* Normal samplers
                p => graph.Induce(graph.RN((int)(p * graph.NumNodes))),
                p => graph.Induce(graph.RE((int)(p * graph.NumEdges))),
                // p => graph.Induce(graph.LowDegreeFirst((int)(p * graph.NumNodes))),
                // p => graph.Induce(graph.GreedyLabels((int)(p * graph.NumNodes))),
                p => graph.Induce(graph.DistinctLabelsSB((int)(p * graph.NumNodes))),
                p => graph.Induce(graph.QueuedSampler<int, int, FifoQueue<int>>((int)(p * graph.NumNodes))),
                // p => graph.Induce(graph.QueuedSampler<int, int, LifoQueue<int>>((int)(p * graph.NumNodes))),
                // p => graph.Induce(graph.QueuedSampler<int, int, AiroQueue<int>>((int)(p * graph.NumNodes))),
                p => graph.Induce(graph.RandomWalkTeleport((int)(p * graph.NumNodes), 0.1)),
                //*/
                /* Approximation samplers
                p => estim.Induce(estim.RN((int)(p * graph.NumNodes))),
                p => estim.Induce(estim.RE((int)(p * graph.NumEdges))),
                // p => estim.Induce(estim.LowDegreeFirst((int)(p * graph.NumNodes))),
                // p => estim.Induce(estim.GreedyLabels((int)(p * graph.NumNodes))),
                p => estim.Induce(estim.DistinctLabelsSB((int)(p * graph.NumNodes))),
                p => estim.Induce(estim.QueuedSampler<int, int, FifoQueue<int>>((int)(p * graph.NumNodes))),
                // p => estim.Induce(estim.QueuedSampler<int, int, LifoQueue<int>>((int)(p * graph.NumNodes))),
                // p => estim.Induce(estim.QueuedSampler<int, int, AiroQueue<int>>((int)(p * graph.NumNodes))),
                p => estim.Induce(estim.RandomWalkTeleport((int)(p * graph.NumNodes), 0.1)),
                //*/
            };

            // Sampler names
            var samplerNames = new string[]
            {
                "RN",
                "RE",
                // "LDF",
                // "GL",
                "DLSB",
                "BFS",
                // "DFS",
                // "RFS",
                "RWT",
            };
            //*/

            //* Run many bisimulation experiments in batch
            var partitioner = new GraphPartitioner<int, int>(graph);
            var k_max = partitioner.MultilevelExactBisimulationReduction().Count - 1;
            for (int k = 0; k <= k_max; k++)
            {
                for (int i = 0; i < samplers.Length; i++)
                {
                    var sampler = samplers[i];
                    var samplerName = samplerNames[i];

                    // var experiment = Experiments.StandardBisimulationMetrics(graph, samplerName, sampler, k);
                    // Experiment.SaveSVG(outPath + @"\" + string.Join("_", experiment.Meta) + ".svg", experiment.Plot(0.0, 1.0));
                    // experiment.SaveTSV(outPath + @"\" + string.Join("_", experiment.Meta) + ".tsv");

                    var experiment = Experiments.WeightedBisimulationMetrics(graph, samplerName, sampler, k);
                    Experiment.SaveSVG(outPath + @"\" + string.Join("_", experiment.Meta) + ".svg", experiment.Plot(0.0, 1.0));
                    experiment.SaveTSV(outPath + @"\" + string.Join("_", experiment.Meta) + ".tsv");

                    Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] k=" + k + " sampler=" + samplerName);
                };
            }
            //*/

            /*
            // var experiment = Analytics.ReachabilityProbabilityMassFunction(graph);
            // var experiment = Experiments.BisimulationPartitionBlockCounts(graph, labels);
            // var experiment = Experiments.FindKMax(graph, labels);
            // var experiment = Experiments.TreeGeneratorPartitionSize(1, 20);
            PlotForm plotForm = new PlotForm();
            plotForm.Display(experiment.Plot(double.NaN, double.NaN));
            plotForm.ShowDialog();
            //*/

            /* Get k
            int k = Input("Please enter a value for k in k-bisimulation", int.Parse);
            //*/

            /*
            var experiment = Experiments.DistanceProbabilityMassFunction(graph);
            Experiment.SaveSVG(outPath + @"\" + string.Join("_", experiment.Meta) + ".svg", experiment.Plot(0, double.NaN));
            experiment.SaveTSV(outPath + @"\" + string.Join("_", experiment.Meta) + ".tsv");
            //*/

            /* Run bisimulation experiment
            var experiment = Experiments.StandardBisimulationMetrics(graph, labels, () => GraphSampler.Funny(graph, labels));
            Experiment.SaveSVG(outPath + @"\" + string.Join("_", experiment.Meta) + ".svg", experiment.Plot(0.0, 1.0));
            experiment.SaveTSV(outPath + @"\" + string.Join("_", experiment.Meta) + ".tsv");
            //*/

            /*
            var experiment = Experiments.BisimulationPartitionSize(graph, labels);
            // var experiment = Experiments.PartitionBlockDistribution(graph, labels);
            Experiment.SaveSVG(outPath + @"\" + string.Join("_", experiment.Meta) + ".svg", experiment.Plot(double.NaN, double.NaN));
            experiment.SaveTSV(outPath + @"\" + string.Join("_", experiment.Meta) + ".tsv");
            //*/

            /*
            PlotForm plotForm = new PlotForm();
            plotForm.Display(experiment.Plot(0, double.NaN));
            plotForm.ShowDialog();
            //*/
        }

        /// <summary>
        /// Read input from the console and convert it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="question"></param>
        /// <param name="parser"></param>
        /// <returns></returns>
        public static T Input<T>(string question, Func<string, T> parser)
        {
            Console.WriteLine(question);

            while (true)
            {
                try
                {
                    return parser(Console.ReadLine());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// Ask the user to select an option through the console.
        /// </summary>
        /// <param name="question"></param>
        /// <param name="answers"></param>
        /// <returns></returns>
        public static int Select(string question, string[] answers)
        {
            int selected = 0;
            Console.WriteLine(question);

            for (int i = 0; i < answers.Length; i++)
            {
                Console.WriteLine("(" + (i + 1) + ") " + answers[i]);
            }

            while (selected <= 0 || selected > answers.Length)
            {
                int.TryParse(Console.ReadLine(), out selected);
            }

            return selected - 1;
        }
    }
}
