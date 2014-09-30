using GraphTools.Graph;
using GraphTools.Plot;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GraphTools
{
    static class Dummy
    {
        public static void Fix()
        {
            string path = Program.Input("Please enter the path to folder with graph files", string.Copy);
            string[] tsvFiles = Directory.GetFiles(path, "*approx.tsv");

            foreach (var filePath in tsvFiles)
            {
                var lines = File.ReadAllLines(filePath);
                var meta = lines.Take(4).Select(line => line.Substring(2));

                var fromSvg = filePath.Replace(".tsv", ".svg");
                var fromTsv = filePath;
                var toSvg = path + @"\" + string.Join("_", meta) + ".svg";
                var toTsv = path + @"\" + string.Join("_", meta) + ".tsv";

                File.Move(fromSvg, toSvg);
                File.Move(fromTsv, toTsv);
            }
        }

        public static void Bla()
        {
            // Merge graphs in a folder with a single source node and sink node

            string path = Program.Input("Please enter the path to folder with graph files", string.Copy);
            var outPath = Program.Input("Out path?", string.Copy);

            string[] filePaths = Directory.GetFiles(path, "*.xml");
            var finalGraph = new MultiDirectedGraph<int, int>();
            var finalSources = new List<int>();
            var finalSinks = new List<int>();
            int count = 0;

            foreach (var filePath in filePaths)
            {
                var graph = GraphLoader.LoadGraphML(filePath, int.Parse, int.Parse);

                foreach (var node in graph.Nodes)
                {
                    finalGraph.AddNode(node + count, graph.NodeLabel(node));
                }

                foreach (var edge in graph.Edges)
                {
                    var s = graph.Source(edge);
                    var t = graph.Target(edge);
                    var l = graph.EdgeLabel(edge);

                    finalGraph.AddEdge(s + count, t + count, l);
                }

                var sources = graph.Nodes.Where(u => graph.In(u).Count() == 0);
                var sinks = graph.Nodes.Where(u => graph.Out(u).Count() == 0);

                if (sources.Count() != 1 || sinks.Count() != 1)
                {
                    throw new Exception();
                }

                finalSources.Add(sources.First() + count);
                finalSinks.Add(sinks.First() + count);

                count += graph.NumNodes;
            }

            finalGraph.MergeNodes(finalSources);
            finalGraph.MergeNodes(finalSinks);

            GraphConverter.SaveToGraphML(finalGraph, outPath);
        }

        public static void Foo()
        {
            // Process log generator petrinet dot conversion

            var inPath = Program.Input("In path?", string.Copy);
            var outPath = Program.Input("Out path?", string.Copy);
            var graph = new MultiDirectedGraph<int, int>();

            var nodeMap = new Dictionary<string, int>();
            int counter = 0;

            var lines = File.ReadAllLines(inPath);
            foreach (var line in lines)
            {
                var tokens = line.Split(new char[] { ' ', '\t', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Length <= 1)
                {
                    continue;
                }

                if (tokens[0][0] == 't')
                {
                    if (!nodeMap.ContainsKey(tokens[0]))
                    {
                        nodeMap.Add(tokens[0], counter++);
                        graph.AddNode(nodeMap[tokens[0]], 0);
                    }
                }

                if (tokens[0][0] == 'p')
                {
                    if (!nodeMap.ContainsKey(tokens[0]))
                    {
                        nodeMap.Add(tokens[0], counter++);
                        graph.AddNode(nodeMap[tokens[0]], 1);
                    }
                }

                if (tokens[1] == "->")
                {
                    var u = nodeMap[tokens[0]];
                    var v = nodeMap[tokens[2]];

                    graph.AddEdge(u, v);
                }
            }

            GraphConverter.SaveToGraphML(graph, outPath);
        }

        public static void Bar()
        {
            // Stanford graphs conversion

            var inPath = Program.Input("In path?", string.Copy);
            var outPath = Program.Input("Out path?", string.Copy);
            var graph = new MultiDirectedGraph<int, int>();

            var lines = File.ReadAllLines(inPath);
            foreach (var line in lines)
            {
                var tokens = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (tokens[0] != "#")
                {
                    int source = int.Parse(tokens[0]);
                    int target = int.Parse(tokens[1]);

                    if (!graph.HasNode(source))
                    {
                        graph.AddNode(source);
                    }

                    if (!graph.HasNode(target))
                    {
                        graph.AddNode(target);
                    }

                    if (!graph.HasEdge(source, target) && !graph.HasEdge(target, source))
                    {
                        graph.AddEdge(source, target);
                    }
                }
            }

            GraphConverter.SaveToGraphML(graph, outPath);
        }
    }
}