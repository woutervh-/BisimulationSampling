using GraphTools.Graph;
using System.Collections.Generic;
using System.IO;

namespace GraphTools
{
    /// <summary>
    /// Converts graphs from one format to another.
    /// </summary>
    static class GraphConverter
    {
        /// <summary>
        /// Saves a graph to a file in the GraphML format.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TLabel"></typeparam>
        /// <param name="graph"></param>
        /// <param name="labels"></param>
        public static void SaveToGraphML<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, string path)
        {
            List<string> lines = new List<string>();

            lines.Add("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            lines.Add("<graphml xmlns=\"http://graphml.graphdrawing.org/xmlns\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://graphml.graphdrawing.org/xmlns http://graphml.graphdrawing.org/xmlns/1.0/graphml.xsd\">");

            lines.Add("\t<key id=\"label\" for=\"all\" attr.name=\"label\" attr.type=\"string\">");
            lines.Add("\t\t<default>0</default>");
            lines.Add("\t</key>");

            lines.Add("\t<graph id=\"" + graph.Name + "\" edgedefault=\"" + (graph.IsDirected ? "directed" : "undirected") + "\">");

            foreach (var node in graph.Nodes)
            {
                lines.Add("\t\t<node id=\"" + node + "\">");
                lines.Add("\t\t\t<data key=\"label\">" + graph.NodeLabel(node) + "</data>");
                lines.Add("\t\t</node>");
            }

            foreach (var edge in graph.Edges)
            {
                var s = graph.Source(edge);
                var t = graph.Target(edge);
                lines.Add("\t\t<edge source=\"" + s + "\" target=\"" + t + "\">");
                lines.Add("\t\t\t<data key=\"label\">" + graph.EdgeLabel(edge) + "</data>");
                lines.Add("\t\t</edge>");
            }

            lines.Add("\t</graph>");
            lines.Add("</graphml>");

            File.WriteAllLines(path, lines);
        }
    }
}
