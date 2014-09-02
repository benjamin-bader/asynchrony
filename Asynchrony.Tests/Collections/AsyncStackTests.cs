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
using System.Threading.Tasks;
using NUnit.Framework;

namespace Asynchrony.Collections
{
    [TestFixture]
    public class AsyncStackTests
    {
        [Test]
        public async Task TestStackOrder()
        {
            var stack = new AsyncStack<int>();
            stack.TryEnqueue(1);
            stack.TryEnqueue(2);
            stack.TryEnqueue(3);

            Assert.That(await stack.DequeueAsync(), Is.EqualTo(3));
            Assert.That(await stack.DequeueAsync(), Is.EqualTo(2));
            Assert.That(await stack.DequeueAsync(), Is.EqualTo(1));
        }
    }
}
