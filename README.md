Asynchrony
==========

[![Build Status](https://travis-ci.org/benjamin-bader/asynchrony.svg?branch=master)](https://travis-ci.org/benjamin-bader/asynchrony)

Asynchrony is a collection of tools for async-friendly coding in C#
on Xamarin, WinRT, native .NET, and Windows Phone Silverlight.

Asynchrony.Collections
----------------------

`IAsyncQueue<T>` represents an `await`-able FIFO queue.  It has three
implementations: the vanilla `AsyncQueue<T>`, `AsyncPriorityQueue<T>`,
and the LIFO `AsyncStack<T>`.

These queues are lightweight, efficient, and serve as effective building
blocks for asynchronous workflows where TPL Dataflow would be too heavy.

License
-------

    Copyright 2014 Benjamin Bader

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
