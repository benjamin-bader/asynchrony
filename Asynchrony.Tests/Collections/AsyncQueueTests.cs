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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Asynchrony.Collections
{
    public class AsyncQueueTests
    {
        [Fact]
        public async Task TestBlockingDequeue()
        {
            var queue = new AsyncQueue<int>();
            var get = queue.DequeueAsync();

            // Wait a moment
            await Task.Yield();

            await queue.EnqueueAsync(2);

            get.IsCompleted.Should().BeFalse(); // getter was enqueued for execution

            await Task.Yield();

            get.Result.Should().Be(2);
        }

        [Fact]
        public async Task TestBlockingEnqueue()
        {
            var queue = new AsyncQueue<int>();
            await queue.EnqueueAsync(10);

            int result;
            queue.TryDequeue(out result).Should().BeTrue();
            result.Should().Be(10);
        }

        [Fact]
        public void TestBoundedQueue()
        {
            var queue = new AsyncQueue<int>(2);
            Assert.True(queue.TryEnqueue(1));
            Assert.True(queue.TryEnqueue(2));
            Assert.False(queue.TryEnqueue(3));
        }

        [Fact]
        public async Task TestIsEmpty()
        {
            var queue = new AsyncQueue<string>();
            Assert.True(queue.IsEmpty);
            
            await queue.EnqueueAsync("foo");
            Assert.False(queue.IsEmpty);
            
            await queue.DequeueAsync();
            Assert.True(queue.IsEmpty);
        }

        [Fact]
        public async Task TestIsFull()
        {
            var queue = new AsyncQueue<int>(1);
            Assert.False(queue.IsFull);

            await queue.EnqueueAsync(5);
            Assert.True(queue.IsFull);

            await queue.DequeueAsync();
            Assert.False(queue.IsFull);
        }

        [Fact]
        public async Task TestQueueOrder()
        {
            var queue = new AsyncQueue<int>();
            queue.TryEnqueue(1);
            queue.TryEnqueue(3);
            queue.TryEnqueue(2);

            (await queue.DequeueAsync()).Should().Be(1);
            (await queue.DequeueAsync()).Should().Be(3);
            (await queue.DequeueAsync()).Should().Be(2);
        }

        //
        // Dequeue tests
        //

        [Fact]
        public async Task TestBlockingDequeueWithPendingEnqueues()
        {
            var queue = new AsyncQueue<int>(1);
            queue.TryEnqueue(1);

            var enqueueTask = Task.Run(() => queue.EnqueueAsync(2));

            var element = await queue.DequeueAsync();
            await Task.Yield();
            element.Should().Be(1);
            enqueueTask.IsCompleted.Should().BeTrue();
        }

        // TestBlockingDequeueWait -> AsyncQueueTestsThatRunOnMono

        [Fact]
        public void TestTryDequeue()
        {
            var queue = new AsyncQueue<int>();
            queue.TryEnqueue(1);

            int result;
            queue.TryDequeue(out result).Should().BeTrue();
            result.Should().Be(1);
        }

        [Fact]
        public void TestTryDequeueWhenEmpty()
        {
            var queue = new AsyncQueue<int>();

            int result;
            Assert.False(queue.TryDequeue(out result));
        }

        [Fact]
        public void TestDequeueNowWhenQueueHasItems()
        {
            var queue = new AsyncQueue<int>();
            queue.TryEnqueue(2);
            queue.DequeueNow().Should().Be(2);
        }

        [Fact]
        public void TestDequeueNowWhenQueueIsEmpty()
        {
            new AsyncQueue<int>()
                .Invoking(it => it.DequeueNow())
                .Should()
                .Throw<InvalidOperationException>();
        }

        //
        // Enqueue tests
        //

        [Fact]
        public async Task TestBlockingEnqueueWhenFull()
        {
            var queue = new AsyncQueue<int>(1);
            queue.TryEnqueue(1);
            var setter = queue.EnqueueAsync(2);

            // wait a moment
            await Task.Yield();

            Assert.False(setter.IsCompleted);

            int result;
            queue.TryDequeue(out result);

            await Task.Yield();

            Assert.True(queue.TryDequeue(out result));
            result.Should().Be(2);
        }

        //TestBlockingEnqueueWait() -> AsyncQueueTestsThatRunOnMono

        [Fact]
        public async Task TestTryEnqueue()
        {
            var queue = new AsyncQueue<int>();
            queue.TryEnqueue(3);
            (await queue.DequeueAsync()).Should().Be(3);
        }

        [Fact]
        public void TestTryEnqueueWhenFull()
        {
            var queue = new AsyncQueue<int>(1);
            queue.TryEnqueue(2);
            Assert.False(queue.TryEnqueue(3));
        }
    }
}
