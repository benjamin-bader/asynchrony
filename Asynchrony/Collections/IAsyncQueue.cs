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
using System.Threading.Tasks;

namespace Asynchrony.Collections
{
    /// <summary>
    /// Represents queue whose operations are awaitable.
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    public interface IAsyncQueue<TElement> : IDisposable
    {
        /// <summary>
        /// Gets the number of elements in the queue.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets a value indicating whether the queue is at its maximum capacity.
        /// </summary>
        bool IsFull { get; }

        /// <summary>
        /// Gets a value indicating whether the queue is empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Gets a value indicating whether the queue has a maximum capacity.
        /// </summary>
        bool IsBounded { get; }

        /// <summary>
        /// Adds an element to the queue, possibly waiting if the queue has a
        /// max size and is full.
        /// </summary>
        /// <param name="element">
        /// The element to enqueue
        /// </param>
        /// <returns>
        /// An awaitiable task which will complete when the element is successfully
        /// enqueued.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the queue has been disposed.
        /// </exception>
        Task EnqueueAsync(TElement element);

        /// <summary>
        /// Attempts to enqueue an element immediately, returning a value
        /// indicating whether the operation succeeded.
        /// </summary>
        /// <remarks>
        /// This operation is guaranteed to complete before returning.  It will
        /// fail in the event that the queue is full.  It will never fail on an
        /// unbounded queue.
        /// </remarks>
        /// <param name="element">
        /// The element to enqueue.
        /// </param>
        /// <returns>
        /// Returns <see langword="true"/> if the enqueue succeeded, and
        /// <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ObjectDisposedException"/>
        bool TryEnqueue(TElement element);

        /// <summary>
        /// Dequeues an element, if necessary waiting until an element is added
        /// to the queue.
        /// </summary>
        /// <returns>
        /// Returns an awaitable task eventually containing the dequeued element.
        /// </returns>
        /// <exception cref="System.ObjectDisposedException">
        /// Thrown when the queue has been disposed.
        /// </exception>
        Task<TElement> DequeueAsync();

        /// <summary>
        /// Attempts to dequeue an element immediately, without waiting.
        /// </summary>
        /// <param name="element">
        /// When the method returns successfully, this will contain the dequeued
        /// element.  When unsuccessful, this will be initialized to a default value.
        /// </param>
        /// <returns>
        /// Returns <see langword="true"/> when an element was dequeued, and
        /// <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the queue has been disposed.
        /// </exception>
        bool TryDequeue(out TElement element);
    }
}
