using System;

namespace GraphTools.Helpers
{
    /// <summary>
    /// Used to compute statistics of a sample set.
    /// The statistics are computed incrementally.
    /// All methods and accessors in this class are thread-safe.
    /// </summary>
    class Statistic
    {
        private double min = double.PositiveInfinity;
        private double max = double.NegativeInfinity;
        private int count = 0;
        private double mean = 0.0;
        private double variance = 0.0;

        public double Min { get { lock (@lock) { return min; } } }
        public double Max { get { lock (@lock) { return max; } } }
        public int Count { get { lock (@lock) { return count; } } }
        public double Mean { get { lock (@lock) { return mean; } } }
        public double Variance { get { lock (@lock) { return variance; } } }
        public double StdDev { get { lock (@lock) { return Math.Sqrt(variance); } } }

        /// <summary>
        /// Mutex lock for thread-safety.
        /// </summary>
        private object @lock = new object();

        /// <summary>
        /// Update the statistics with a new value.
        /// </summary>
        /// <param name="s"></param>
        public void Update(double s)
        {
            lock (@lock)
            {
                count += 1;
                min = Math.Min(min, s);
                max = Math.Max(max, s);

                double delta = s - mean;
                mean += delta / count;

                if (count > 1)
                {
                    variance += (delta * (s - mean) - variance) / (count - 1);
                }
            }
        }

        /// <summary>
        /// Returns a string representation of the current statistic.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            lock (@lock)
            {
                return "Min = " + Min + ", Max = " + Max + ", N = " + Count + ", u = " + Mean + ", V = " + Variance;
            }
        }
    }
}
