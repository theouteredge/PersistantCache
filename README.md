PersistentCache
===============

A C# library which gives you a Persistent Cache, if we run out of memory we will store the expired and evicted cache items to disk. Think Membase but as a simple C# project, which you can include in your project.

ABOUT
-----
A Persistent Cache. Once a key/value is stored in the cache its stored for the lifetime of the cache. If you want a standard cache with expiry then this is not the cache for you.

This is designed to be used in a very specific Use Case where you want to hold a large cache which wouldn't fit in the systems main memory and you dont want to lose any of the data stored in the cache. Once the process has completed the cache should be disposed of. This should be used for one off large data processing and disposed of once you have finished with the cache.
 
DEPENDANCIES
------------
ServiceStack.Text


NUGET
-----
Install-Package PersistentCache


LICENCE
-------
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.