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
using NUnit.Framework;

namespace Asynchrony.Collections
{
    [TestFixture]
    public class AsyncQueueTests
    {
        [Test]
        public async Task TestBlockingDequeue()
        {
            var queue = new AsyncQueue<int>();
            var get = queue.DequeueAsync();

            // Wait a moment
            await Task.Yield();

            await queue.EnqueueAsync(2);

            Assert.That(get.IsCompleted, Is.False); // getter was enqueued for execution

            await Task.Yield();

            Assert.That(get.Result, Is.EqualTo(2));
        }

        [Test]
        public async Task TestBlockingEnqueue()
        {
            var queue = new AsyncQueue<int>();
            await queue.EnqueueAsync(10);

            int result;
            Assert.That(queue.TryDequeue(out result), Is.True);
            Assert.That(result, Is.EqualTo(10));
        }

        [Test]
        public void TestBoundedQueue()
        {
            var queue = new AsyncQueue<int>(2);
            Assert.That(queue.TryEnqueue(1));
            Assert.That(queue.TryEnqueue(2));
            Assert.That(queue.TryEnqueue(3), Is.False);
        }

        [Test]
        public async Task TestIsEmpty()
        {
            var queue = new AsyncQueue<string>();
            Assert.That(queue.IsEmpty);
            
            await queue.EnqueueAsync("foo");
            Assert.That(queue.IsEmpty, Is.False);
            
            await queue.DequeueAsync();
            Assert.That(queue.IsEmpty);
        }

        [Test]
        public async Task TestIsFull()
        {
            var queue = new AsyncQueue<int>(1);
            Assert.That(queue.IsFull, Is.False);

            await queue.EnqueueAsync(5);
            Assert.That(queue.IsFull, Is.True);

            await queue.DequeueAsync();
            Assert.That(queue.IsFull, Is.False);
        }

        [Test]
        public async Task TestQueueOrder()
        {
            var queue = new AsyncQueue<int>();
            queue.TryEnqueue(1);
            queue.TryEnqueue(3);
            queue.TryEnqueue(2);

            Assert.That(await queue.DequeueAsync(), Is.EqualTo(1));
            Assert.That(await queue.DequeueAsync(), Is.EqualTo(3));
            Assert.That(await queue.DequeueAsync(), Is.EqualTo(2));
        }

        //
        // Dequeue tests
        //

        [Test]
        public async Task TestBlockingDequeueWithPendingEnqueues()
        {
            var queue = new AsyncQueue<int>(1);
            queue.TryEnqueue(1);

            var enqueueTask = Task.Run(() => queue.EnqueueAsync(2));

            var element = await queue.DequeueAsync();
            await Task.Yield();
            Assert.That(element, Is.EqualTo(1));
            Assert.That(enqueueTask.IsCompleted);
        }

        // TestBlockingDequeueWait -> AsyncQueueTestsThatRunOnMono

        [Test]
        public void TestTryDequeue()
        {
            var queue = new AsyncQueue<int>();
            queue.TryEnqueue(1);

            int result;
            Assert.That(queue.TryDequeue(out result), Is.True);
            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void TestTryDequeueWhenEmpty()
        {
            var queue = new AsyncQueue<int>();

            int result;
            Assert.That(queue.TryDequeue(out result), Is.False);
        }

        //
        // Enqueue tests
        //

        [Test]
        public async Task TestBlockingEnqueueWhenFull()
        {
            var queue = new AsyncQueue<int>(1);
            queue.TryEnqueue(1);
            var setter = queue.EnqueueAsync(2);

            // wait a moment
            await Task.Yield();

            Assert.That(setter.IsCompleted, Is.False);

            int result;
            queue.TryDequeue(out result);

            await Task.Yield();

            Assert.That(queue.TryDequeue(out result), Is.True);
            Assert.That(result, Is.EqualTo(2));
        }

        //TestBlockingEnqueueWait() -> AsyncQueueTestsThatRunOnMono

        [Test]
        public async Task TestTryEnqueue()
        {
            var queue = new AsyncQueue<int>();
            queue.TryEnqueue(3);
            Assert.That(await queue.DequeueAsync(), Is.EqualTo(3));
        }

        [Test]
        public void TestTryEnqueueWhenFull()
        {
            var queue = new AsyncQueue<int>(1);
            queue.TryEnqueue(2);
            Assert.That(queue.TryEnqueue(3), Is.False);
        }
    }
}
