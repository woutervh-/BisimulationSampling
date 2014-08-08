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

                    if (!graph.HasEdge(source, target))
                    {
                        graph.AddEdge(source, target);
                    }
                }
            }

            GraphConverter.SaveToGraphML(graph, outPath);
        }
    }
}