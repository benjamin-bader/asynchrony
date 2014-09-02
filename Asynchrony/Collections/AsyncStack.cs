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

using System.Collections.Generic;

namespace Asynchrony.Collections
{
    public class AsyncStack<TElement> : AsyncQueueBase<TElement>
    {
        private readonly Stack<TElement> stack;

        public AsyncStack(int? maxSize = null)
            : base(maxSize)
        {
            stack = maxSize.HasValue
                ? new Stack<TElement>(maxSize.Value)
                : new Stack<TElement>();
        }

        protected override int GetSizeOfQueue()
        {
            return stack.Count;
        }

        protected override void EnqueueCore(TElement element)
        {
            stack.Push(element);
        }

        protected override TElement DequeueCore()
        {
            return stack.Pop();
        }
    }
}
