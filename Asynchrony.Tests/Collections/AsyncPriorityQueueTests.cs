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
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace Asynchrony.Collections
{
    [TestFixture]
    public class AsyncPriorityQueueTests
    {
        [Test]
        public async Task TestPriorityOrder()
        {
            var queue = new AsyncPriorityQueue<int>();
            queue.TryEnqueue(10);
            queue.TryEnqueue(8);
            queue.TryEnqueue(9);
            queue.TryEnqueue(7);

            Assert.That(await queue.DequeueAsync(), Is.EqualTo(7));
            Assert.That(await queue.DequeueAsync(), Is.EqualTo(8));
            Assert.That(await queue.DequeueAsync(), Is.EqualTo(9));
            Assert.That(await queue.DequeueAsync(), Is.EqualTo(10));
        }

        [Test]
        public void TestQueueGrowth()
        {
            var queue = new AsyncPriorityQueue<int>();
            var initialCapacity = queue.Capacity;

            for (var i = 0; i <= initialCapacity; ++i)
            {
                queue.TryEnqueue(i);
            }

            Assert.That(queue.Capacity, Is.GreaterThan(initialCapacity));
        }

        [Test]
        public void TestQueueShrinking()
        {
            var queue = new AsyncPriorityQueue<int>();
            var initialCapacity = queue.Capacity;

            for (var i = 0; i <= initialCapacity; ++i)
            {
                queue.TryEnqueue(i);
            }

            Assert.That(queue.Capacity, Is.GreaterThan(initialCapacity));

            while (!queue.IsEmpty)
            {
                queue.DequeueNow();
            }

            Assert.That(queue.Capacity, Is.EqualTo(initialCapacity));
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestComparableDetection()
        {
            new AsyncPriorityQueue<object>();
        }

        [Test]
        public void TestNonComparableWithComparer()
        {
            var comparer = new Mock<IComparer<object>>().Object;
            new AsyncPriorityQueue<object>(comparer: comparer);
        }

        [Test]
        public async Task TestComparerUsage()
        {
            var comparer = new ReverseComparer<int>(Comparer<int>.Default);
            var queue = new AsyncPriorityQueue<int>(comparer: comparer);

            queue.TryEnqueue(7);
            queue.TryEnqueue(8);
            queue.TryEnqueue(9);
            queue.TryEnqueue(10);

            Assert.That(await queue.DequeueAsync(), Is.EqualTo(10));
            Assert.That(await queue.DequeueAsync(), Is.EqualTo(9));
            Assert.That(await queue.DequeueAsync(), Is.EqualTo(8));
            Assert.That(await queue.DequeueAsync(), Is.EqualTo(7));
        }

        [Test]
        public async Task TestBoundedQueueDoesNotGrow()
        {
            var queue = new AsyncPriorityQueue<int>(3);

            queue.TryEnqueue(3);
            queue.TryEnqueue(2);
            queue.TryEnqueue(1);

            var task = queue.EnqueueAsync(0);

            Assert.That(task.IsCompleted, Is.False);
            Assert.That(await queue.DequeueAsync(), Is.EqualTo(1));

            await task;

            Assert.That(await queue.DequeueAsync(), Is.EqualTo(0));
        }

        private class ReverseComparer<T> : IComparer<T>
        {
            private readonly IComparer<T> inner;

            public ReverseComparer(IComparer<T> inner)
            {
                this.inner = inner;
            }

            public int Compare(T x, T y)
            {
                return inner.Compare(x, y)*-1;
            }
        }
    }
}
