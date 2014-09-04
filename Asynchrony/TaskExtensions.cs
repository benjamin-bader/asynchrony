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
using System.Threading;
using System.Threading.Tasks;

namespace Asynchrony
{
    /// <summary>
    /// There is no non-generic TaskCompletionSource, so we have to use the generic
    /// version even when there is no real result to provide.
    ///
    /// We could just use TaskCompletionSource&lt;bool&gt;, but callers could downcast
    /// the result to Task&lt;bool&gt; and make assumptions about the value therein.
    ///
    /// By using an internal type as the task result, we prevent any but the most
    /// determined of reflectors from trying to do stupid things.
    /// </summary>
    internal enum NonCastable : byte
    {
        NobodyCanDowncastSetterTasksWithThisCleverScheme
    }

    internal static class TaskExtensions
    {
        /// <summary>
        /// A singleton <see cref="Task"/> that is already completed.
        /// </summary>
        /// <remarks>
        /// Intended for use when an operation is completed without yielding
        /// and a non-value-bearing result is needed.
        /// </remarks>
        internal static readonly Task CompletedTask;

        static TaskExtensions()
        {
            var tcs = new TaskCompletionSource<NonCastable>();
            tcs.SetResult(NonCastable.NobodyCanDowncastSetterTasksWithThisCleverScheme);
            CompletedTask = tcs.Task;
        }

        public static async Task WithCancellation(this Task task, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (token.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task))
                {
                    throw new OperationCanceledException();
                }

                await task;
            }
        }

        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (token.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task))
                {
                    throw new OperationCanceledException();
                }

                return await task;
            }
        }
    }
}
