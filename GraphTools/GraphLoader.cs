using GraphTools.Graph;
using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace GraphTools
{
    /// <summary>
    /// Loads graphs from files.
    /// </summary>
    static class GraphLoader
    {
        /// <summary>
        /// Load a GraphML document.
        /// </summary>
        /// <typeparam name="TNode">Type of the nodes.</typeparam>
        /// <typeparam name="TLabel">Type of the labels.</typeparam>
        /// <param name="path">Path to the GraphML file.</param>
        /// <param name="nodeParser">Function which converts a string to TNode.</param>
        /// <param name="labelParser">Function which converts a string to TLabel.</param>
        /// <returns>The graph and label provider representing the loaded GraphML document.</returns>
        public static MultiDirectedGraph<TNode, TLabel> LoadGraphML<TNode, TLabel>(string path, Func<string, TNode> nodeParser, Func<string, TLabel> labelParser)
        {
            // Read GraphML document
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;

            using (XmlReader reader = XmlReader.Create(path, settings))
            {
                var document = XDocument.Load(reader);
                var keys = document.Root.Elements().Where(element => element.Name.LocalName == "key");
                var graphs = document.Root.Elements().Where(element => element.Name.LocalName == "graph");
                var xmlGraph = graphs.First();
                var defaultLabel = default(TLabel);

                // Go through keys
                foreach (var key in keys)
                {
                    // Id of the key
                    var id = (string)key.Attribute("id");

                    switch (id)
                    {
                        case "label":
                            defaultLabel = labelParser(key.Elements().Where(element => element.Name.LocalName == "default").First().Value);
                            break;
                    }
                }

                // Read graph attributes
                string graphName = (string)xmlGraph.Attribute("id");
                bool isDirected = (string)xmlGraph.Attribute("edgedefault") == "directed";

                // Construct empty graph and label provider
                // TODO: undirected graph if isDirected is false (for now only use directed graphs)
                var graph = new MultiDirectedGraph<TNode, TLabel>();
                graph.Name = graphName;

                var nodes = xmlGraph.Elements().Where(element => element.Name.LocalName == "node");
                var edges = xmlGraph.Elements().Where(element => element.Name.LocalName == "edge");

                // Go through each node
                foreach (var xmlNode in nodes)
                {
                    string id = (string)xmlNode.Attribute("id");
                    var node = nodeParser(id);
                    var label = defaultLabel;

                    var data = xmlNode.Elements().Where(element => element.Name.LocalName == "data");
                    foreach (var datum in data)
                    {
                        string key = (string)datum.Attribute("key");

                        switch (key)
                        {
                            case "label":
                                label = labelParser(datum.Value);
                                break;
                        }
                    }

                    graph.AddNode(node, label);
                }

                // Go through each edge
                foreach (var xmlEdge in edges)
                {
                    string xmlSource = (string)xmlEdge.Attribute("source");
                    string xmlTarget = (string)xmlEdge.Attribute("target");
                    var source = nodeParser(xmlSource);
                    var target = nodeParser(xmlTarget);
                    var label = defaultLabel;

                    var data = xmlEdge.Elements().Where(element => element.Name.LocalName == "data");
                    foreach (var datum in data)
                    {
                        string key = (string)datum.Attribute("key");

                        switch (key)
                        {
                            case "label":
                                label = labelParser(datum.Value);
                                break;
                        }
                    }

                    graph.AddEdge(source, target, label);

                    // TODO: replace temporary fix for undirected graphs
                    if (!isDirected)
                    {
                        graph.AddEdge(target, source, label);
                    }
                }

                return graph;
            }
        }

        /*
        /// <summary>
        /// Copies a graph.
        /// </summary>
        /// <typeparam name="TGraph"></typeparam>
        /// <typeparam name="TNode"></typeparam>
        /// <param name="graph"></param>
        /// <returns></returns>
        public static TGraph Copy<TGraph, TNode>(this TGraph graph) where TGraph : IGraph<TNode>, new()
        {
            TGraph copy = new TGraph();

            foreach (var node in graph.Nodes)
            {
                copy.AddNode(node);
            }

            graph.ForEachEdge((s, t) =>
            {
                copy.AddEdge(s, t);
            });

            return copy;
        }
        //*/

        /*
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TLabel"></typeparam>
        /// <typeparam name="TGraph"></typeparam>
        /// <param name="path"></param>
        /// <param name="nodeParser"></param>
        /// <param name="labelParser"></param>
        /// <returns></returns>
        public static Tuple<TGraph, ILabelProvider<TNode, TLabel>> LoadGraphML<TNode, TLabel, TGraph>(string path, Func<string, TNode> nodeParser, Func<string, TLabel> labelParser) where TGraph : IGraph<TNode>, new()
        {
            TGraph graph = new TGraph();
            ExplicitLabelProvider<TNode, TLabel> labels = new ExplicitLabelProvider<TNode, TLabel>();

            string content = File.ReadAllText(path);
            string[] tokens = content.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int i = 0;

            while (tokens[i++] != "graph") ;

            if (tokens[i++] != "[")
            {
                throw new InvalidDataException();
            }

            if (tokens[i++] != "directed")
            {
                throw new InvalidDataException();
            }

            if (tokens[i++] != "1")
            {
                throw new InvalidDataException();
            }

            while (tokens[i++] == "node")
            {
                if (tokens[i++] != "[")
                {
                    throw new InvalidDataException();
                }

                if (tokens[i++] != "id")
                {
                    throw new InvalidDataException();
                }

                TNode node = nodeParser(tokens[i++]);

                if (tokens[i++] != "label")
                {
                    throw new InvalidDataException();
                }

                TLabel label = labelParser(tokens[i++].Replace("\"", ""));

                if (tokens[i++] != "]")
                {
                    throw new InvalidDataException();
                }

                graph.AddNode(node);
                // labels.SetLabel(node, label);
                labels.SetLabel(node, default(TLabel));
            }

            i -= 1;

            while (tokens[i++] == "edge")
            {
                if (tokens[i++] != "[")
                {
                    throw new InvalidDataException();
                }

                if (tokens[i++] != "source")
                {
                    throw new InvalidDataException();
                }

                TNode source = nodeParser(tokens[i++]);

                if (tokens[i++] != "target")
                {
                    throw new InvalidDataException();
                }

                TNode target = nodeParser(tokens[i++]);

                if (tokens[i++] != "value")
                {
                    throw new InvalidDataException();
                }

                TLabel label = labelParser(tokens[i++]);

                if (tokens[i++] != "]")
                {
                    throw new InvalidDataException();
                }

                if (!graph.HasEdge(source, target))
                {
                    graph.AddEdge(source, target);
                    labels.SetLabel(source, target, label);
                }
            }

            i -= 1;

            if (tokens[i++] != "]")
            {
                throw new InvalidDataException();
            }

            return new Tuple<TGraph, ILabelProvider<TNode, TLabel>>(graph, labels);
        }
        //*/

        /*
        /// <summary>
        /// Loads annotated graph.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TLabel"></typeparam>
        /// <typeparam name="TGraph"></typeparam>
        /// <param name="path"></param>
        /// <param name="nodeParser"></param>
        /// <param name="labelParser"></param>
        /// <returns></returns>
        public static Tuple<TGraph, ILabelProvider<TNode, TLabel>> LoadAnnotatedGraph<TNode, TLabel, TGraph>(string path, Func<string, TNode> nodeParser, Func<string, TLabel> labelParser) where TGraph : IGraph<TNode>, new()
        {
            TGraph graph = new TGraph();
            ExplicitLabelProvider<TNode, TLabel> labels = new ExplicitLabelProvider<TNode, TLabel>();

            foreach (string line in File.ReadAllLines(path))
            {
                string[] tokens = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                switch (tokens[0].Trim().ToLower()[0])
                {
                    case 'n':
                        TNode node = nodeParser(tokens[1]);

                        if (!graph.HasNode(node))
                        {
                            graph.AddNode(node);
                        }

                        if (tokens.Length > 2)
                        {
                            labels.SetLabel(node, labelParser(tokens[2]));
                        }
                        else
                        {
                            labels.SetLabel(node, default(TLabel));
                        }
                        break;
                    case 'e':
                        TNode source = nodeParser(tokens[1]);
                        TNode target = nodeParser(tokens[2]);

                        if (!graph.HasNode(source))
                        {
                            graph.AddNode(source);
                        }

                        if (!graph.HasNode(target))
                        {
                            graph.AddNode(target);
                        }

                        graph.AddEdge(source, target);

                        if (tokens.Length > 3)
                        {
                            labels.SetLabel(source, target, labelParser(tokens[3]));
                        }
                        else
                        {
                            labels.SetLabel(source, target, default(TLabel));
                        }
                        break;
                }
            }

            return new Tuple<TGraph, ILabelProvider<TNode, TLabel>>(graph, labels);
        }
        //*/

        /*
        /// <summary>
        /// Loads a graph from a file.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TLabel"></typeparam>
        /// <typeparam name="TGraph"></typeparam>
        /// <param name="path"></param>
        /// <param name="nodeParser"></param>
        /// <param name="labelParser"></param>
        /// <param name="hasNodeLabels"></param>
        /// <param name="hasEdgeLabels"></param>
        /// <returns></returns>
        public static Tuple<TGraph, ILabelProvider<TNode, TLabel>> LoadGraph<TNode, TLabel, TGraph>(string path, Func<string, TNode> nodeParser, Func<string, TLabel> labelParser, bool hasNodeLabels, bool hasEdgeLabels) where TGraph : IGraph<TNode>, new()
        {
            if (hasNodeLabels && !hasEdgeLabels)
            {
                throw new ArgumentException("Cannot decide whether input contains nodes with labels or edges when hasNodeLabels=TRUE and hasEdgeLabels=FALSE.");
            }

            TGraph graph = new TGraph();
            ExplicitLabelProvider<TNode, TLabel> labels = new ExplicitLabelProvider<TNode, TLabel>();

            foreach (string line in File.ReadAllLines(path))
            {
                string[] tokens = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (tokens[0].Trim()[0] != '#')
                {
                    switch (tokens.Length)
                    {
                        case 1:
                            {
                                if (!hasNodeLabels)
                                {
                                    TNode node = nodeParser(tokens[0]);

                                    if (!graph.HasNode(node))
                                    {
                                        graph.AddNode(node);
                                        labels.SetLabel(node, default(TLabel));
                                    }
                                }
                                else
                                {
                                    throw new InvalidDataException("Node without label encountered.");
                                }
                            }
                            break;
                        case 2:
                            {
                                if (hasNodeLabels)
                                {
                                    TNode node = nodeParser(tokens[0]);
                                    TLabel label = labelParser(tokens[1]);

                                    if (!graph.HasNode(node))
                                    {
                                        graph.AddNode(node);
                                    }

                                    labels.SetLabel(node, label);
                                }
                                else if (!hasEdgeLabels)
                                {
                                    TNode source = nodeParser(tokens[0]);
                                    TNode target = nodeParser(tokens[1]);

                                    if (!graph.HasNode(source))
                                    {
                                        graph.AddNode(source);
                                        labels.SetLabel(source, default(TLabel));
                                    }

                                    if (!graph.HasNode(target))
                                    {
                                        graph.AddNode(target);
                                        labels.SetLabel(target, default(TLabel));
                                    }

                                    if (!graph.HasEdge(source, target))
                                    {
                                        graph.AddEdge(source, target);
                                        labels.SetLabel(source, target, default(TLabel));
                                    }
                                }
                                else
                                {
                                    throw new InvalidDataException("Invalid data.");
                                }
                            }
                            break;
                        case 3:
                            {
                                if (hasEdgeLabels)
                                {
                                    TNode source = nodeParser(tokens[0]);
                                    TNode target = nodeParser(tokens[1]);
                                    TLabel label = labelParser(tokens[2]);

                                    if (!graph.HasNode(source))
                                    {
                                        graph.AddNode(source);
                                        labels.SetLabel(source, default(TLabel));
                                    }

                                    if (!graph.HasNode(target))
                                    {
                                        graph.AddNode(target);
                                        labels.SetLabel(target, default(TLabel));
                                    }

                                    if (!graph.HasEdge(source, target))
                                    {
                                        graph.AddEdge(source, target);
                                    }

                                    labels.SetLabel(source, target, label);
                                }
                                else
                                {
                                    throw new InvalidDataException("Edge with label encountered.");
                                }
                            }
                            break;
                    }
                }
            }

            return new Tuple<TGraph, ILabelProvider<TNode, TLabel>>(graph, labels);
        }
        //*/
    }
}
