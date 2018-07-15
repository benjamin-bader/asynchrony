// Copyright 2014 Benjamin Bader
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Asynchrony.Collections
{
    /// <summary>
    /// Represents an awaitable priority queue.
    /// </summary>
    /// <remarks>
    /// The queue is implemented with min-heap semantics; this means that
    /// elements that are <strong>less-than</strong> will be prioritized over
    /// elements that are greater-than.
    /// </remarks>
    /// <typeparam name="TElement">
    /// The type of element contained in the queue.
    /// </typeparam>
    public class AsyncPriorityQueue<TElement> : AsyncQueueBase<TElement>
    {
        private const int DefaultCapacity = 8;

        private readonly IComparer<TElement> comparer;

        private TElement[] queue;

        // Points to the next empty slot in the queue
        private int tail = 0;

        // Visible for testing
        internal int Capacity
        {
            get { return queue.Length; }
        }

        /// <summary>
        /// Creates a new instance of <see cref="AsyncPriorityQueue&lt;TElement&gt;"/>.
        /// </summary>
        /// <remarks>
        /// If <paramref name="comparer"/> is <see langword="null"/>, then it is
        /// required that <typeparamref name="TElement"/> implement
        /// <see cref="IComparable&lt;TElement&gt;"/>.
        /// </remarks>
        /// <param name="maxSize"></param>
        /// <param name="comparer"></param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="comparer"/> is <see langword="null"/> and
        /// <typeparamref name="TElement"/> does not implement
        /// <see cref="IComparable&lt;TElement&gt;"/>.
        /// </exception>
        public AsyncPriorityQueue(int? maxSize = null, IComparer<TElement> comparer = null)
            : base(maxSize)
        {
            if (comparer == null)
            {
                if (!typeof(IComparable<TElement>).GetTypeInfo().IsAssignableFrom(typeof(TElement).GetTypeInfo()))
                {
                    throw new ArgumentException("A comparer is required when TElement does not implement IComparable<TElement>");
                }

                comparer = Comparer<TElement>.Default;
            }

            this.comparer = comparer;
            queue = new TElement[Math.Max(maxSize ?? DefaultCapacity, DefaultCapacity)];
        }

        protected override int GetSizeOfQueue()
        {
            return tail;
        }

        protected override void EnqueueCore(TElement element)
        {
            if (tail == queue.Length)
            {
                AssertQueueNotBounded();

                checked
                {
                    var size = queue.Length * 2;
                    Array.Resize(ref queue, queue.Length * 2);
                }
            }

            queue[tail] = element;
            SiftUp(tail);
            ++tail;
        }

        protected override TElement DequeueCore()
        {
            if (tail == 0)
            {
                throw new InvalidOperationException("Cannot dequeue an empty queue");
            }

            var element = queue[0];

            --tail;
            queue[0] = queue[tail];
            queue[tail] = default(TElement);

            SiftDown(0);
            ShrinkIfNeeded();

            return element;
        }

        private void ShrinkIfNeeded()
        {
            var halfLength = queue.Length / 2;
            if (queue.Length == DefaultCapacity || tail >= halfLength)
            {
                return;
            }

            Array.Resize(ref queue, halfLength);
        }

        private void SiftUp(int index)
        {
            while (index != 0)
            {
                var parent = Parent(index);
                var comparison = Compare(index, parent);

                if (comparison >= 0)
                {
                    break;
                }

                Swap(index, parent);
                index = parent;
            }
        }

        private void SiftDown(int index)
        {
            while (true)
            {
                var smallest = index;
                var left = LeftChild(index);
                var right = RightChild(index);

                if (Exists(left) && Compare(left, smallest) < 0)
                {
                    smallest = left;
                }

                if (Exists(right) && Compare(right, smallest) < 0)
                {
                    smallest = right;
                }

                if (smallest == index)
                {
                    break;
                }

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int x, int y)
        {
            var temp = queue[x];
            queue[x] = queue[y];
            queue[y] = temp;
        }

        private static int Parent(int child)
        {
            return (child - 1) >> 1;
        }

        private static int LeftChild(int parent)
        {
            return (parent << 1) + 1;
        }

        private static int RightChild(int parent)
        {
            return (parent << 1) + 2;
        }

        private bool Exists(int element)
        {
            return element >= 0 && element < tail;
        }

        private int Compare(int x, int y)
        {
            return comparer.Compare(queue[x], queue[y]);
        }
    }
}
