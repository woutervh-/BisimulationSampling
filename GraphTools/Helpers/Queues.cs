using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphTools.Helpers
{
    /// <summary>
    /// Generic order queue.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    interface GenericQueue<T>
    {
        /// <summary>
        /// Take an item from the queue.
        /// </summary>
        /// <returns></returns>
        T Take();

        /// <summary>
        /// Put an item in the queue.
        /// </summary>
        /// <param name="item"></param>
        void Put(T item);

        /// <summary>
        /// The number of items in the queue.
        /// </summary>
        int Count { get; }
    }

    /// <summary>
    /// Max heap.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class MaxHeap<T> : GenericQueue<T>
    {
        private SortedSet<T> heap;

        public MaxHeap(Comparer<T> comparer)
        {
            heap = new SortedSet<T>(comparer);
        }

        public MaxHeap(Comparison<T> comparison)
            : this(Comparer<T>.Create(comparison))
        {
            //
        }

        public T Take()
        {
            T max = heap.Max;
            heap.Remove(max);
            return max;
        }

        public void Put(T item)
        {
            heap.Add(item);
        }

        public int Count
        {
            get
            {
                return heap.Count;
            }
        }

        public void UpdateAll()
        {
            var items = heap.ToArray();
            heap = new SortedSet<T>(items);
        }

        public bool Contains(T item)
        {
            return heap.Contains(item);
        }
    }

    /// <summary>
    /// Last-in-first-out queue.
    /// Stack-like queue.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class LifoQueue<T> : GenericQueue<T>
    {
        private Stack<T> stack = new Stack<T>();

        public T Take()
        {
            return stack.Pop();
        }

        public void Put(T item)
        {
            stack.Push(item);
        }

        public int Count
        {
            get { return stack.Count; }
        }
    }

    /// <summary>
    /// First-in-first-out queue.
    /// Proper queue.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class FifoQueue<T> : GenericQueue<T>
    {
        private Queue<T> queue = new Queue<T>();

        public T Take()
        {
            return queue.Dequeue();
        }

        public void Put(T item)
        {
            queue.Enqueue(item);
        }

        public int Count
        {
            get { return queue.Count; }
        }
    }

    /// <summary>
    /// Anything-in-random-out queue.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class AiroQueue<T> : GenericQueue<T>
    {
        private List<T> items = new List<T>();

        public T Take()
        {
            int index = StaticRandom.Next(items.Count);
            T item = items[index];
            items.RemoveAt(index);

            return item;
        }

        public void Put(T item)
        {
            items.Add(item);
        }

        public int Count
        {
            get { return items.Count; }
        }
    }
}
