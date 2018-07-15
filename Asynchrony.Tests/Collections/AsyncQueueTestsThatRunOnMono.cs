using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

using Asynchrony.Collections;

namespace Asynchrony
{
    /// <summary>
    /// As of the Mono 3.6, async methods with async delegates defined
    /// inside are compiled incorrectly; here, we have tests whose
    /// delegates are broken out into separate methods.
    /// </summary>
    public class AsyncQueueTestsThatRunOnMono
    {
        private AsyncQueue<int> queue;
        private TaskCompletionSource<bool> started;
        private bool finished;

        public AsyncQueueTestsThatRunOnMono()
        {
            queue = new AsyncQueue<int>();
            started = new TaskCompletionSource<bool>();
            finished = false;
        }

        [Fact]
        public async Task TestBlockingDequeueWait()
        {
            var dequeuedResult = await Task.Run((Func<Task<int>>) DequeueTest_StartEnqueueing, CancellationToken.None);
            dequeuedResult.Should().Be(1);
        }

        private async Task<int> DequeueTest_StartDequeueing()
        {
            started.SetResult(true);
            var element = await queue.DequeueAsync();
            finished = true;
            return element;
        }

        private async Task<int> DequeueTest_StartEnqueueing()
        {
            var task = Task.Run((Func<Task<int>>) DequeueTest_StartDequeueing, CancellationToken.None);
            await started.Task;
            Assert.False(finished);
            Task.Run(() => queue.TryEnqueue(1));
            var result = await task;
            Assert.True(finished);
            return result;
        }

        [Fact]
        public async Task TestBlockingEnqueueWait()
        {
            await EnqueueTest_StartDequeueing();
        }

        private async Task EnqueTest_StartEnqueueing()
        {
            started.SetResult(true);
            await queue.EnqueueAsync(2);
            await queue.EnqueueAsync(3);
            finished = true;
        }

        private async Task EnqueueTest_StartDequeueing()
        {
            var enqueueTask = Task.Run((Func<Task>) EnqueTest_StartEnqueueing);
            await started.Task;
            Assert.False(finished);
            Task.Run((Func<Task<int>>) EnqueueTest_DequeueAsync);
            await enqueueTask;
            Assert.True(finished);
        }

        private async Task<int> EnqueueTest_DequeueAsync()
        {
            return await queue.DequeueAsync();
        }
    }
}
