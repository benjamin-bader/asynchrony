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
using FluentAssertions;
using Moq;
using Xunit;

namespace Asynchrony.Collections
{
    public class AsyncPriorityQueueTests
    {
        [Fact]
        public async Task TestPriorityOrder()
        {
            var queue = new AsyncPriorityQueue<int>();
            queue.TryEnqueue(10);
            queue.TryEnqueue(8);
            queue.TryEnqueue(9);
            queue.TryEnqueue(7);

            (await queue.DequeueAsync()).Should().Be(7);
            (await queue.DequeueAsync()).Should().Be(8);
            (await queue.DequeueAsync()).Should().Be(9);
            (await queue.DequeueAsync()).Should().Be(10);
        }

        [Fact]
        public void TestQueueGrowth()
        {
            var queue = new AsyncPriorityQueue<int>();
            var initialCapacity = queue.Capacity;

            for (var i = 0; i <= initialCapacity; ++i)
            {
                queue.TryEnqueue(i);
            }

            queue.Capacity.Should().BeGreaterThan(initialCapacity);
        }

        [Fact]
        public void TestQueueShrinking()
        {
            var queue = new AsyncPriorityQueue<int>();
            var initialCapacity = queue.Capacity;

            for (var i = 0; i <= initialCapacity; ++i)
            {
                queue.TryEnqueue(i);
            }

            queue.Capacity.Should().BeGreaterThan(initialCapacity);

            while (!queue.IsEmpty)
            {
                queue.DequeueNow();
            }

            queue.Capacity.Should().Be(initialCapacity);
        }

        [Fact]
        public void TestComparableDetection()
        {
            Assert.Throws<ArgumentException>(() => new AsyncPriorityQueue<object>());
        }

        [Fact]
        public void TestNonComparableWithComparer()
        {
            var comparer = new Mock<IComparer<object>>().Object;
            new AsyncPriorityQueue<object>(comparer: comparer);
        }

        [Fact]
        public async Task TestComparerUsage()
        {
            var comparer = new ReverseComparer<int>(Comparer<int>.Default);
            var queue = new AsyncPriorityQueue<int>(comparer: comparer);

            queue.TryEnqueue(7);
            queue.TryEnqueue(8);
            queue.TryEnqueue(9);
            queue.TryEnqueue(10);

            (await queue.DequeueAsync()).Should().Be(10);
            (await queue.DequeueAsync()).Should().Be(9);
            (await queue.DequeueAsync()).Should().Be(8);
            (await queue.DequeueAsync()).Should().Be(7);
        }

        [Fact]
        public async Task TestBoundedQueueDoesNotGrow()
        {
            var queue = new AsyncPriorityQueue<int>(3);

            queue.TryEnqueue(3);
            queue.TryEnqueue(2);
            queue.TryEnqueue(1);

            var task = queue.EnqueueAsync(0);

            task.IsCompleted.Should().BeFalse();
            (await queue.DequeueAsync()).Should().Be(1);

            await task;

            (await queue.DequeueAsync()).Should().Be(0);
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
