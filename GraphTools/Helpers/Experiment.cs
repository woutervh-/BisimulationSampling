﻿using GraphTools.Plot;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace GraphTools.Helpers
{
    /// <summary>
    /// Represents an experiment which can be run to evaluated functions.
    /// </summary>
    class Experiment
    {
        /// <summary>
        /// Results from running the experiment.
        /// </summary>
        private List<List<Statistic>> results = null;

        /// <summary>
        /// Function to evaluate experiment on.
        /// </summary>
        public Func<double, double[]> F = null;

        /// <summary>
        /// Labels.
        /// </summary>
        public string[] Labels { get; set; }

        /// <summary>
        /// Horitonzal axis index.
        /// </summary>
        public int HorizontalAxis { get; set; }

        /// <summary>
        /// Meta-information about the experiment.
        /// </summary>
        public string[] Meta { get; set; }

        /// <summary>
        /// Number of variables (including horizontal axis variable.)
        /// </summary>
        public int N { get { return n; } }

        /// <summary>
        /// Number of variables (including horizontal axis variable.)
        /// </summary>
        private int n;

        /// <summary>
        /// Create new experiment.
        /// </summary>
        /// <param name="n">Number of variables (including horizontal axis variable.)</param>
        public Experiment(int n)
        {
            this.n = n;
            Meta = new string[] { };
            Labels = new string[n];
            HorizontalAxis = 0;
        }

        /// <summary>
        /// Run experiment.
        /// </summary>
        /// <param name="from">Horizontal axis variable lower bound (inclusive).</param>
        /// <param name="to">Horizontal axis variable upper bound (inclusive).</param>
        /// <param name="step">Step size.</param>
        /// <param name="iterations">Number of iterations per step.</param>
        public void Run(double from, double to, double step, int iterations)
        {
            if (results != null)
            {
                throw new InvalidOperationException("Experiment has already run.");
            }

            if (F == null)
            {
                throw new InvalidOperationException("Function has not been set.");
            }

            results = new List<List<Statistic>>();

            for (int k = 0; k < n; k++)
            {
                results.Add(new List<Statistic>());
            }

            for (double i = from; i <= to; i += step)
            {
                List<Statistic> statistics = new List<Statistic>();

                for (int k = 0; k < n; k++)
                {
                    statistics.Add(new Statistic());
                }

                for (int j = 0; j < iterations; j++)
                {
                    double[] values = F(i);

                    if (values.Length != n)
                    {
                        throw new InvalidDataException();
                    }

                    for (int k = 0; k < n; k++)
                    {
                        statistics[k].Update(values[k]);
                    }
                }

                for (int k = 0; k < n; k++)
                {
                    results[k].Add(statistics[k]);
                }
            }
        }

        /// <summary>
        /// Returns the statistics of the variable with the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IList<Statistic> GetStatistics(int index)
        {
            return results[index];
        }

        /// <summary>
        /// Generates an OxyPlot plot model.
        /// </summary>
        /// <param name="yMin">Minimum value for the vertical axis. If NaN will find the lowest value.</param>
        /// <param name="yMax">Maximum value for the vertical axis. If NaN will find the highest value.</param>
        /// <returns></returns>
        public PlotModel Plot(double yMin, double yMax)
        {
            // Check if experiment has run yet
            if (results == null)
            {
                throw new InvalidOperationException("Experiment has not been run yet.");
            }

            // Check if yMin is valid
            if (double.IsNaN(yMin))
            {
                yMin = double.PositiveInfinity;
            }

            // Check if yMax is valid
            if (double.IsNaN(yMax))
            {
                yMax = double.NegativeInfinity;
            }

            // Expand yMin and yMax so all values fit
            for (int i = 0; i < N; i++)
            {
                if (i != HorizontalAxis)
                {
                    double min = GetStatistics(i).Select(statistic => statistic.Min).Min();
                    if (yMin > min)
                    {
                        yMin = min;
                    }

                    double max = GetStatistics(i).Select(statistic => statistic.Max).Max();
                    if (yMax < max)
                    {
                        yMax = max;
                    }
                }
            }

            // Arbitrary small value to prevent clipping
            yMin -= 0.001;
            yMax += 0.001;

            // Create OxyPlot series
            List<Series> series = new List<Series>();
            for (int i = 0; i < N; i++)
            {
                if (i != HorizontalAxis)
                {
                    var color = OxyPalettes.Rainbow(N).Colors[i];

                    var mySeries = new ErrorLineSeries()
                    {
                        Color = color,
                        MarkerFill = color,
                        MarkerSize = 5,
                        MarkerStroke = OxyColors.White,
                        MarkerType = MarkerType.Diamond,
                        Title = Labels[i],
                        Points = GetStatistics(i).Select((statistic, j) => (IDataPoint)new DataPoint(GetStatistics(HorizontalAxis)[j].Mean, statistic.Mean)).ToList(),
                        ErrorValues = GetStatistics(i).Select(statistic => statistic.StdDev).ToList(),
                        ErrorWidth = 0.01,
                    };

                    series.Add(mySeries);
                }
            }

            // Create OxyPlot model
            var plotModel = new PlotModel()
            {
                Title = string.Join(", ", Meta),
                LegendSymbolLength = 16,
                LegendPlacement = OxyPlot.LegendPlacement.Outside,

                Axes = new Collection<Axis>()
                {
                    new LinearAxis()
                    {
                        Minimum = yMin,
                        Maximum = yMax,
                        Position = AxisPosition.Left,
                        MajorGridlineStyle = LineStyle.Solid,
                    },

                    new LinearAxis()
                    {
                        Minimum = GetStatistics(HorizontalAxis).Select(statistic => statistic.Min).Min(),
                        Maximum = GetStatistics(HorizontalAxis).Select(statistic => statistic.Max).Max(),
                        Position = AxisPosition.Bottom,
                        MajorGridlineStyle = LineStyle.Solid,
                        Title = Labels[HorizontalAxis],
                    }
                },

                Series = new Collection<Series>(series)
            };

            return plotModel;
        }

        /// <summary>
        /// Save plot model to SVG file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="plotModel"></param>
        public static void SaveSVG(string path, PlotModel plotModel, double width = double.NaN, double height = double.NaN)
        {
            using (var stream = File.Create(path))
            {
                var exporter = new SvgExporter();

                if (!double.IsNaN(width))
                {
                    exporter.Width = width;
                }
                
                if (!double.IsNaN(height))
                {
                    exporter.Height = height;
                }

                exporter.Export(plotModel, stream);
            }
        }

        /// <summary>
        /// Save experiment to TSV file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="experiment"></param>
        public void SaveTSV(string path)
        {
            if (results == null)
            {
                throw new InvalidOperationException("Experiment has not been run yet.");
            }

            List<string> lines = new List<string>();

            if (Meta.Length > 0)
            {
                lines.AddRange(Meta.Select(meta => "# " + meta));
            }

            lines.Add(string.Join("\t\t", Labels));
            lines.Add(string.Join("\t", Labels.SelectMany(label => new string[] { "Average", "SD" })));

            int m = results[0].Count;
            for (int i = 0; i < m; i++)
            {
                lines.Add(string.Join("\t", results.Select(result => result[i].Mean + "\t" + result[i].StdDev)));
            }

            File.WriteAllLines(path, lines);
        }

        /*
        /// <summary>
        /// Save results to file in TSV format.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            if (results == null)
            {
                throw new InvalidOperationException("Experiment has not been run yet.");
            }

            string content = Meta.Length > 0 ? (string.Join(Environment.NewLine, Meta.Select(meta => "# " + meta)) + Environment.NewLine) : string.Empty;
            content += XLabel + "\t";
            foreach (var YLabel in YLabels)
            {
                // content += YLabel + " (" + results.First().Value[0].Count + " runs)\t\t";
                content += YLabel + "\t\t";
            }
            content += Environment.NewLine;
            foreach (var YLabel in YLabels)
            {
                content += "\tAverage\tSD";
            }
            content += Environment.NewLine;

            foreach (var kvp in results.OrderBy(kvp => kvp.Key))
            {
                var statistics = kvp.Value;
                content += kvp.Key;
                foreach (var statistic in statistics)
                {
                    content += "\t" + statistic.Mean + "\t" + statistic.StdDev;
                }
                content += Environment.NewLine;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
        }
        //*/
    }
}
