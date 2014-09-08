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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Asynchrony.Collections
{
    /// <summary>
    /// Provides a foundation for building awaitable queues.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The core operations implemented are EnqueueAsync, TryEnqueue,
    /// DequeueAsync, and TryDequeue.  They are implemented in terms
    /// of synchronous Enqueue, Dequeue, and Count operations.
    /// </para>
    /// <para>
    /// Derived classes must implement these, as well as manage the
    /// backing queue storage.
    /// </para>
    /// </remarks>
    /// <typeparam name="TElement">
    /// The type of element contained in the queue.
    /// </typeparam>
    public abstract class AsyncQueueBase<TElement> : IAsyncQueue<TElement>
    {
        private const int True = 1;
        private const int False = 0;

        private readonly Queue<TaskCompletionSource<TElement>> getters
            = new Queue<TaskCompletionSource<TElement>>();

        private readonly Queue<Tuple<TaskCompletionSource<NonCastable>, TElement>> setters
            = new Queue<Tuple<TaskCompletionSource<NonCastable>, TElement>>();

        private readonly int? maxSize;
        private readonly bool isTokenSourceOwnedByUs;

        private int disposed;
        private CancellationTokenSource cts;

        protected object QueueLock
        {
            get { return getters; }
        }

        public int Count
        {
            get
            {
                lock (QueueLock)
                {
                    return GetSizeOfQueue();
                }
            }
        }

        public bool IsEmpty
        {
            get { return Count == 0; }
        }

        public bool IsBounded
        {
            get { return maxSize.HasValue; }
        }

        public bool IsFull
        {
            get { return maxSize.HasValue && maxSize.Value == Count; }
        }

        protected AsyncQueueBase(int? maxSize = null, CancellationTokenSource cts = null)
        {
            if (maxSize.HasValue && maxSize < 1)
            {
                throw new ArgumentOutOfRangeException("maxSize", maxSize.Value, "Must be greater than zero if specified");
            }

            this.maxSize = maxSize;
            this.cts = cts ?? new CancellationTokenSource();

            isTokenSourceOwnedByUs = cts == null;
        }

        public Task EnqueueAsync(TElement element)
        {
            Task result;

            TaskCompletionSource<TElement> getter = null;
            lock (QueueLock)
            {
                ThrowIfDisposed();

                ConsumeFinishedGetters();
                if (getters.Count > 0)
                {
                    AssertQueueEmpty();

                    getter = getters.Dequeue();

                    result = TaskExtensions.CompletedTask;
                }
                else if (IsFull)
                {
                    var setter = new TaskCompletionSource<NonCastable>();
                    setters.Enqueue(Tuple.Create(setter, element));
                    result = setter.Task.WithCancellation(cts.Token);
                }
                else
                {
                    EnqueueCore(element);
                    result = TaskExtensions.CompletedTask;
                }
            }

            if (getter != null)
            {
                SetResultAsync(getter, element);
            }

            return result;
        }

        public bool TryEnqueue(TElement element)
        {
            ThrowIfDisposed();

            TaskCompletionSource<TElement> getter = null;

            var didEnqueue = false;
            lock (QueueLock)
            {
                ConsumeFinishedGetters();
                if (getters.Count > 0)
                {
                    AssertQueueEmpty();
                    getter = getters.Dequeue();
                    didEnqueue = true;
                }
                else if (!IsFull)
                {
                    EnqueueCore(element);
                    didEnqueue = true;
                }
            }

            if (getter != null)
            {
                SetResultAsync(getter, element);
            }

            return didEnqueue;
        }

        public Task<TElement> DequeueAsync()
        {
            Task<TElement> result;
            TaskCompletionSource<NonCastable> setter;
            lock (QueueLock)
            {
                ThrowIfDisposed();

                ConsumeFinishedSetters();
                if (setters.Count > 0)
                {
                    AssertQueueFull();

                    var setterAndItem = setters.Dequeue();
                    setter = setterAndItem.Item1;

                    result = Task.FromResult(DequeueCore());
                    EnqueueCore(setterAndItem.Item2);

                    // Set the result outside of the lock
                }
                else if (IsEmpty)
                {
                    var getter = new TaskCompletionSource<TElement>();
                    getters.Enqueue(getter);
                    return getter.Task.WithCancellation(cts.Token);
                }
                else
                {
                    return Task.FromResult(DequeueCore());
                }
            }

            SetResultAsync(setter, NonCastable.NobodyCanDowncastSetterTasksWithThisCleverScheme);
            return result;
        }

        public bool TryDequeue(out TElement element)
        {
            TaskCompletionSource<NonCastable> setter;
            lock (QueueLock)
            {
                ThrowIfDisposed();

                ConsumeFinishedSetters();
                if (setters.Count > 0)
                {
                    AssertQueueFull();

                    var setterAndItem = setters.Dequeue();
                    setter = setterAndItem.Item1;

                    element = DequeueCore();
                    EnqueueCore(setterAndItem.Item2);

                    // Set the result outside of the lock
                }
                else if (IsEmpty)
                {
                    element = default(TElement);
                    return false;
                }
                else
                {
                    element = DequeueCore();
                    return true;
                }
            }

            SetResultAsync(setter, NonCastable.NobodyCanDowncastSetterTasksWithThisCleverScheme);
            return true;
        }

        public TElement DequeueNow()
        {
            TElement result;
            if (!TryDequeue(out result))
            {
                throw new InvalidOperationException("Cannot dequeue from an empty queue");
            }
            return result;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref disposed, True, False) != False)
            {
                return;
            }

            if (disposing)
            {
                if (cts != null)
                {
                    if (isTokenSourceOwnedByUs)
                    {
                        cts.Dispose();
                    }

                    cts = null;
                }

                lock (QueueLock)
                {
                    getters.Clear();
                    setters.Clear();
                }
            }
        }

        /// <summary>
        /// Starts a background task which will set the given result on the given
        /// task completion source.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the task's result.
        /// </typeparam>
        /// <param name="tcs">
        /// The task completion source whose result is to be set.
        /// </param>
        /// <param name="result">
        /// The result.
        /// </param>
        private void SetResultAsync<TResult>(TaskCompletionSource<TResult> tcs, TResult result)
        {
            Task.Factory.StartNew(
                s => ((TaskCompletionSource<TResult>)s).TrySetResult(result),
                tcs,
                cts.Token,
                TaskCreationOptions.None,
                TaskScheduler.Current);
        }

        private void ConsumeFinishedGetters()
        {
            ConsumeFinishedTasks(getters, t => t.Task);
        }

        private void ConsumeFinishedSetters()
        {
            ConsumeFinishedTasks(setters, t => t.Item1.Task);
        }

        private static void ConsumeFinishedTasks<T>(Queue<T> q, Func<T, Task> selector)
        {
            while (q.Count > 0)
            {
                var element = q.Peek();
                var task = selector(element);

                if (task.IsCompleted || task.IsCanceled || task.IsFaulted)
                {
                    q.Dequeue();
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// When implemented in a derived class, returns the number of elements
        /// in the queue.
        /// </summary>
        /// <remarks>
        /// This exists because <see cref="Queue&lt;TElement&gt;"/> does not
        /// implement <see cref="ICollection&lt;TElement&gt;"/>, and does not
        /// expose a generic-constraint-friendly Count member.
        /// </remarks>
        /// <returns>
        /// Returns the number of elements in the queue.
        /// </returns>
        protected abstract int GetSizeOfQueue();

        /// <summary>
        /// When implemented in a derived class, adds an element to the queue.
        /// </summary>
        /// <param name="element">
        /// The element to be enqueued.
        /// </param>
        protected abstract void EnqueueCore(TElement element);

        /// <summary>
        /// When implemented in a derived class, removes and returns the element
        /// at the head of the queue.
        /// </summary>
        /// <returns>
        /// Returns the element at the head of the queue.
        /// </returns>
        protected abstract TElement DequeueCore();

        /// <summary>
        /// Check if the queue has been disposed, and throw if it has.
        /// </summary>
        /// <exception cref="ObjectDisposedException"/>
        private void ThrowIfDisposed()
        {
            // Non-volatile read of an int is safe if it is only set via Interlocked.* methods,
            // which this one is.
            if (disposed == True)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        /// <summary>
        /// Asserts that the queue is empty, and throws an exception if it is not.
        /// </summary>
        /// <remarks>
        /// Disabled in release builds.
        /// </remarks>
        /// <exception cref="Exception"/>
        [Conditional("DEBUG")]
        private void AssertQueueEmpty()
        {
            if (!IsEmpty)
            {
                throw new Exception("The queue should be empty, but has count of: " + Count);
            }
        }

        /// <summary>
        /// Asserts that the queue has a max size and that it has that many elements,
        /// throwing an exception if either of those conditions does not hold.
        /// </summary>
        /// <remarks>
        /// Disabled in release builds.
        /// </remarks>
        /// <exception cref="Exception"/>
        [Conditional("DEBUG")]
        private void AssertQueueFull()
        {
            if (!IsFull)
            {
                throw new Exception("Queue is not full, why are setters waiting");
            }
        }

        [Conditional("DEBUG")]
        protected void AssertQueueBounded()
        {
            if (!IsBounded)
            {
                throw new Exception("Queue is unbounded!");
            }
        }

        [Conditional("DEBUG")]
        protected void AssertQueueNotBounded()
        {
            if (IsBounded)
            {
                throw new Exception("Queue is bounded!");
            }
        }
    }
}
