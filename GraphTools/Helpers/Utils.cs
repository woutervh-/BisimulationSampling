using GraphTools.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace GraphTools.Helpers
{
    /// <summary>
    /// Provides some arbitrary utilities.
    /// </summary>
    static class Utils
    {
        /// <summary>
        /// Static Crc32 instance.
        /// </summary>
        private static Crc32 crc32 = new Crc32();

        /// <summary>
        /// Hashes a list of objects.
        /// The hash is dependent on the order of the items in the list.
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static int Hash(params object[] list)
        {
            byte[] buffer = new byte[4 * list.Length];

            for (int i = 0; i < list.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(list[i].GetHashCode()), 0, buffer, 4 * i, 4);
            }

            byte[] hash = crc32.ComputeHash(buffer);

            return BitConverter.ToInt32(hash, 0);
        }

        /// <summary>
        /// Shuffles an array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static void Shuffle<T>(this T[] source)
        {
            int n = source.Length;

            for (int i = n - 1; i >= 1; i--)
            {
                int j = StaticRandom.Next(i + 1);
                T temp = source[i];
                source[i] = source[j];
                source[j] = temp;
            }
        }

        /// <summary>
        /// Yields a shuffled enumerable of the source.
        /// Beware: lazily evaluated and will mess up the source array!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<T> Shuffled<T>(T[] source)
        {
            int n = source.Length;

            for (int i = n - 1; i >= 1; i--)
            {
                int j = StaticRandom.Next(i + 1);
                yield return source[j];
                source[j] = source[i];
            }

            yield return source[0];
        }

        /// <summary>
        /// Extract the power law exponent from a sequence of numbers.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static double PowerLawExponent(IEnumerable<double> sequence)
        {
            int n = sequence.Count();
            double min = sequence.Min();
            double sum = sequence.Sum(x => Math.Log(x / min));
            return 1.0 + n / sum;
        }

        /// <summary>
        /// Computes the discrete distribution of a sequence of items.
        /// Key-value pairs indicate how often (value) the item (key) occurs.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static Dictionary<T, int> Distribution<T>(IEnumerable<T> sequence)
        {
            Dictionary<T, int> distribution = new Dictionary<T, int>();

            foreach (var item in sequence)
            {
                if (!distribution.ContainsKey(item))
                {
                    distribution.Add(item, 0);
                }

                distribution[item] += 1;
            }

            return distribution;
        }

        /// <summary>
        /// Updates a discrete distribution with a new sequence of items.
        /// Key-value pairs indicate how often (value) the key occurs in the sequence + the distribution.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="distribution"></param>
        /// <param name="sequence"></param>
        public static void UpdateDistribution<T>(Dictionary<T, int> distribution, IEnumerable<T> sequence)
        {
            foreach (var item in sequence)
            {
                if (!distribution.ContainsKey(item))
                {
                    distribution.Add(item, 0);
                }

                distribution[item] += 1;
            }
        }

        /// <summary>
        /// Updates a discrete distribution with a new sequence of items.
        /// Key-value pairs indicate how often (value) the key occurs in the sequence + the distribution.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="distribution"></param>
        /// <param name="sequence"></param>
        public static void UpdateDistribution<T>(Dictionary<T, BigInteger> distribution, IEnumerable<T> sequence)
        {
            foreach (var item in sequence)
            {
                if (!distribution.ContainsKey(item))
                {
                    distribution.Add(item, 0);
                }

                distribution[item] += 1;
            }
        }

        /// <summary>
        /// Equality comparer for hash sets.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class HashSetEqualityComparer<T> : IEqualityComparer<HashSet<T>>
        {
            /// <summary>
            /// Checks if two hash sets are equal by checking if both sets are subsets of the other.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public bool Equals(HashSet<T> x, HashSet<T> y)
            {
                return x.SetEquals(y);
            }

            /// <summary>
            /// Generate a hash code based on the size of the set.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public int GetHashCode(HashSet<T> obj)
            {
                return obj.Count;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        public class PairEqualityComparer<T1, T2> : IEqualityComparer<Tuple<T1, T2>>
        {
            private IEqualityComparer<T1> comparer1;
            private IEqualityComparer<T2> comparer2;

            public PairEqualityComparer(IEqualityComparer<T1> comparer1, IEqualityComparer<T2> comparer2)
            {
                this.comparer1 = comparer1;
                this.comparer2 = comparer2;
            }

            public bool Equals(Tuple<T1, T2> x, Tuple<T1, T2> y)
            {
                return comparer1.Equals(x.Item1, y.Item1) && comparer2.Equals(x.Item2, y.Item2);
            }

            public int GetHashCode(Tuple<T1, T2> obj)
            {
                int hash = 1;
                hash = hash * 17 + comparer1.GetHashCode(obj.Item1);
                hash = hash * 31 + comparer2.GetHashCode(obj.Item2);
                return hash;
            }
        }

        /// <summary>
        /// Returns the element in the sequence which has the lowest key value.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) where TKey : IComparable<TKey>
        {
            using (IEnumerator<TSource> iterator = source.GetEnumerator())
            {
                if (!iterator.MoveNext())
                {
                    throw new ArgumentException("Source was empty.");
                }

                TSource minItem = iterator.Current;
                TKey minKey = selector(minItem);

                while (iterator.MoveNext())
                {
                    TSource item = iterator.Current;
                    TKey key = selector(item);

                    if (key.CompareTo(minKey) < 0)
                    {
                        minItem = item;
                        minKey = key;
                    }
                }

                return minItem;
            }
        }

        /// <summary>
        /// Returns the element in the sequence which has the highest key value.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) where TKey : IComparable<TKey>
        {
            using (IEnumerator<TSource> iterator = source.GetEnumerator())
            {
                if (!iterator.MoveNext())
                {
                    throw new ArgumentException("Source was empty.");
                }

                TSource maxItem = iterator.Current;
                TKey maxKey = selector(maxItem);

                while (iterator.MoveNext())
                {
                    TSource item = iterator.Current;
                    TKey key = selector(item);

                    if (key.CompareTo(maxKey) > 0)
                    {
                        maxItem = item;
                        maxKey = key;
                    }
                }

                return maxItem;
            }
        }

        /// <summary>
        /// Compute distances to all target nodes from a single source node.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <param name="graph"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Dictionary<TNode, int> SingleSourceDistances<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph, TNode source)
        {
            var distance = new Dictionary<TNode, int>();

            foreach (var v in graph.Nodes)
            {
                distance.Add(v, int.MaxValue);
            }

            var Q = new Queue<TNode>();
            var V = new HashSet<TNode>();
            Q.Enqueue(source);
            V.Add(source);
            distance[source] = 0;

            // Breadth-first walk
            while (Q.Count > 0)
            {
                var s = Q.Dequeue();

                foreach (var eo in graph.Out(s))
                {
                    var t = graph.Target(eo);
                    if (!V.Contains(t))
                    {
                        Q.Enqueue(t);
                        V.Add(t);
                        distance[t] = distance[s] + 1;
                    }
                }
            }

            return distance;
        }

        /// <summary>
        /// Compute distances to all target nodes from all source nodes.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <param name="graph"></param>
        /// <returns></returns>
        public static Dictionary<TNode, Dictionary<TNode, int>> AllPairsDistances<TNode, TLabel>(MultiDirectedGraph<TNode, TLabel> graph)
        {
            var distance = new Dictionary<TNode, Dictionary<TNode, int>>();

            foreach (var source in graph.Nodes)
            {
                distance.Add(source, SingleSourceDistances(graph, source));
            }

            return distance;
        }

        /// <summary>
        /// Lazily yields the power set of an array.
        /// Only use this for small initial sets because of exponential size.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> PowerSet<T>(T[] items)
        {
            int n = items.Length;
            int count = 1 << n;

            for (int i = 0; i < count; i++)
            {
                yield return Subset(items, i);
            }
        }

        /// <summary>
        /// Lazily yields the subset of an array where mask is a bitmask dictating which elements should be in the subset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static IEnumerable<T> Subset<T>(T[] items, int mask)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    yield return items[i];
                }
            }
        }

        /// <summary>
        /// Factorize n into primes.
        /// </summary>
        /// <param name="n">Number to be factorized. Must be at least 0.</param>
        /// <returns>Dictionary where key-value pairs (p, x) indicate that p ^ x divides n.</returns>
        public static IDictionary<int, int> Factorize(int n)
        {
            Dictionary<int, int> factors = new Dictionary<int, int>();

            for (int p = 2; n > 1; p++)
            {
                if (n % p == 0)
                {
                    int x = 0;
                    while (n % p == 0)
                    {
                        n /= p;
                        x += 1;
                    }

                    // p^x divides n
                    factors.Add(p, x);
                }
            }

            return factors;
        }

        /// <summary>
        /// Compute the divisors of n, including 1 and n itself.
        /// </summary>
        /// <param name="n">Must be at least 1.</param>
        /// <returns></returns>
        public static int[] Divisors(int n)
        {
            var primes = Factorize(n);
            var numDivisors = primes.Values.Aggregate(1, (total, next) => total * (next + 1));
            var divisors = new int[numDivisors];
            int k = 1;
            divisors[0] = 1;

            foreach (var kvp in primes)
            {
                var prime = kvp.Key;
                var count = kvp.Value;

                for (int i = 0; i < k; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        int prev = i + k * j;
                        int curr = i + k * (j + 1);
                        divisors[curr] = divisors[prev] * prime;
                    }
                }

                k *= count + 1;
            }

            return divisors;
        }

        /// <summary>
        /// Y combinator. Fixed-point combinator for recursive lambda functions.
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Func<A, R> Y<A, R>(Func<Func<A, R>, Func<A, R>> f)
        {
            Func<A, R> g = null;
            g = f(a => g(a));
            return g;
        }

        /// <summary>
        /// Invert a dictionary.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Dictionary<TValue, HashSet<TKey>> Invert<TKey, TValue>(IDictionary<TKey, TValue> f)
        {
            var result = new Dictionary<TValue, HashSet<TKey>>();

            foreach (var kvp in f)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                if (!result.ContainsKey(value))
                {
                    result.Add(value, new HashSet<TKey>());
                }

                result[value].Add(key);
            }

            return result;
        }
    }
}
