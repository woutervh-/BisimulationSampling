using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphTools.Helpers
{
    /// <summary>
    /// Used to compute statistics of a set of numbers.
    /// </summary>
    class Statistic
    {
        private List<double> items = new List<double>();

        /// <summary>
        /// Gets the minimum value of the set of numbers.
        /// </summary>
        public double Min
        {
            get
            {
                return items.Min();
            }
        }

        /// <summary>
        /// Gets the maximum value of the set of numbers.
        /// </summary>
        public double Max
        {
            get
            {
                return items.Max();
            }
        }

        /// <summary>
        /// Gets the size of the set of numbers.
        /// </summary>
        public int Count
        {
            get
            {
                return items.Count;
            }
        }

        /// <summary>
        /// Gets the average (mean) value of the set of numbers.
        /// </summary>
        public double Mean
        {
            get
            {
                return items.Average();
            }
        }

        /// <summary>
        /// Gets the variance of the set of numbers.
        /// </summary>
        public double Variance
        {
            get
            {
                double mean = Mean;
                double sumSquares = items.Sum(item => (item - mean) * (item - mean));
                return sumSquares / Count;
            }
        }

        /// <summary>
        /// Gets the standard deviation of the set of numbers.
        /// </summary>
        public double StdDev
        {
            get
            {
                return Math.Sqrt(Variance);
            }
        }

        /// <summary>
        /// Update the statistics with a new value.
        /// </summary>
        /// <param name="s"></param>
        public void Update(double s)
        {
            items.Add(s);
        }

        /// <summary>
        /// Returns a string representation of the current statistic.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Min = " + Min + ", Max = " + Max + ", N = " + Count + ", u = " + Mean + ", V = " + Variance;
        }
    }
}
