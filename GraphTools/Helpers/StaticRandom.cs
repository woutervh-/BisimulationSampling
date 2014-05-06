using System;

namespace GraphTools.Helpers
{
    /// <summary>
    /// A thread-safe static variant of the Random class.
    /// </summary>
    static class StaticRandom
    {
        private static Random random = new Random();
        private static object @lock = new object();

        /// <summary>
        /// Returns a nonnegative random number.
        /// </summary>
        /// <returns></returns>
        public static int Next()
        {
            lock (@lock)
            {
                return random.Next();
            }
        }

        /// <summary>
        /// Returns a random number between 0.0 and 1.0.
        /// </summary>
        /// <returns></returns>
        public static double NextDouble()
        {
            lock (@lock)
            {
                return random.NextDouble();
            }
        }

        /// <summary>
        /// Returns a nonnegative random number less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. maxValue must be greater than or equal to zero.</param>
        /// <returns></returns>
        public static int Next(int maxValue)
        {
            lock (@lock)
            {
                return random.Next(maxValue);
            }
        }

        /// <summary>
        /// Returns a random number within a specified range.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. maxValue must be greater than or equal to minValue.</param>
        /// <returns></returns>
        public static int Next(int minValue, int maxValue)
        {
            lock (@lock)
            {
                return random.Next(minValue, maxValue);
            }
        }
    }
}
