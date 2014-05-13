/*
using GraphTools.Graph;
using GraphTools.Plot;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace GraphTools
{
    static class Dummy
    {
        /// <summary>
        /// Read RDF/XML graph file and convert to an edge-labeled GraphML file.
        /// </summary>
        public static void Foo()
        {
            var mapping = new Dictionary<INode, int>();
            var inPath = Program.Input("In path?", string.Copy);
            var outPath = Program.Input("Out path?", string.Copy);

            IGraph g = new VDS.RDF.Graph();
            RdfXmlParser parser = new RdfXmlParser();
            parser.Load(g, inPath);

            int counter = 0;
            var graph = new MultiDirectedGraph<int, int>();
            foreach (var triple in g.Triples)
            {
                if (!mapping.ContainsKey(triple.Subject))
                {
                    mapping.Add(triple.Subject, counter++);
                }

                if (!mapping.ContainsKey(triple.Predicate))
                {
                    mapping.Add(triple.Predicate, counter++);
                }

                if (!mapping.ContainsKey(triple.Object))
                {
                    mapping.Add(triple.Object, counter++);
                }

                int subjectId = mapping[triple.Subject];
                int predicateId = mapping[triple.Predicate];
                int objectId = mapping[triple.Object];

                if (!graph.HasNode(subjectId))
                {
                    graph.AddNode(subjectId, 0);
                }

                if (!graph.HasNode(objectId))
                {
                    graph.AddNode(objectId, 0);
                }

                if (!graph.HasEdge(subjectId, objectId))
                {
                    graph.AddEdge(subjectId, objectId, predicateId);
                }
            }

            GraphConverter.SaveToGraphML(graph, outPath);
        }
    }
}
//*/
