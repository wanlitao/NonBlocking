using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace NonBlockingTests
{
    public class ConcurrencyTests
    {
        [Fact]
        private void TestGetOrAdd_NonBlocking()
        {
            ValueFactory(null);
            var nonBlocking_dict = new NonBlocking.ConcurrentDictionary<string, string>();

            var tasks = new List<Task>();
            for (int i = 0; i < 1000; i++)
            {
                Func<int, Task> task = async (j) =>
                {
                    await Task.Yield();
                    var value = nonBlocking_dict.GetOrAdd("Test", (key) => _value);                    
                    Assert.Equal(_value, value);
                };

                tasks.Add(task(i));
            }

            Task.WaitAll(tasks.ToArray());
        }

        [Fact]
        private void TestGetOrAdd_System()
        {
            ValueFactory(null);
            var dict = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

            var tasks = new List<Task>();
            for (int i = 0; i < 1000; i++)
            {
                Func<int, Task> task = async (j) =>
                {
                    await Task.Yield();
                    var value = dict.GetOrAdd("Test", (key) => _value);
                    Assert.Equal(_value, value);
                };

                tasks.Add(task(i));
            }

            Task.WaitAll(tasks.ToArray());
        }

        private volatile string _value = null;

        private string ValueFactory(string key)
        {
            return _value = Path.GetRandomFileName();
        }
    }
}
