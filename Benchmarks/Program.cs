﻿// Copyright (c) Vladimir Sadov. All rights reserved.
//
// This file is distributed under the MIT License. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using NonBlocking;
using System.Threading;
using Concurrent = System.Collections.Concurrent;

namespace NonBlockingTests
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Stopwatch.IsHighResolution)
            {
                System.Console.WriteLine("Timer: High Resolution");
            }
            else
            {
                System.Console.WriteLine("Timer: Low Resolution");
            }

            EmptyAction();
            InterlockedIncrement();

            Counter32Perf();
            Counter64Perf();

            Counter32GetEstimatedPerf();

            GetBenchNB();
            GetBenchCD();
            GetBenchRndNB();
            GetBenchRndCD();

            AddBenchRndNB();
            AddBenchRndCD();

            GetOrAddFuncBenchRndNB();
            GetOrAddFuncBenchRndCD();

            WriteBenchRndNB();
            WriteBenchRndCD();

            //并发性能
            GetOrAddFuncBenchConcurrentNB();
            GetOrAddFuncBenchConcurrentCD();

            System.Console.ReadLine();
        }

        private static void EmptyAction()
        {
            var benchmarkName = "======== EmptyAction 1M Ops/sec:";
            Action<int, int> act = (_, __) => { };
            RunBench(benchmarkName, act);
        }

        private static void InterlockedIncrement()
        {
            var benchmarkName = "======== InterlockedIncrement 1M Ops/sec:";
            int cnt = 0;
            Action<int, int> act = (_, __) => { Interlocked.Increment(ref cnt); };

            RunBench(benchmarkName, act);
        }

        private static void Counter32Perf()
        {
            var benchmarkName = "======== Counter32 1M Ops/sec:";

            Counter32 cnt = new Counter32();
            Action<int, int> act = (_, __) => { cnt.Increment(); };

            RunBench(benchmarkName, act);
        }

        private static void Counter32GetEstimatedPerf()
        {
            var benchmarkName = "======== Counter32 Estimated Get 1M Ops/sec:";

            Counter32 cnt = new Counter32();
            Action<int, int> act = (_, __) => { cnt.Increment(); var dummy = cnt.EstimatedValue; };

            RunBench(benchmarkName, act);
        }

        private static void Counter64Perf()
        {
            var benchmarkName = "======== Counter64 1M Ops/sec:";

            Counter64 cnt = new Counter64();
            Action<int, int> act = (_, __) => { cnt.Increment(); };

            RunBench(benchmarkName, act);
        }

        private static void GetBenchNB()
        {
            var dict = new NonBlocking.ConcurrentDictionary<int, string>();

            Parallel.For(0, 100000, (i) => dict[i] = i.ToString());
            Parallel.For(0, 100000, (i) => { var dummy = dict[i]; });

            var benchmarkName = "======== Get NonBlocking 1M Ops/sec:";

            Action<int, int> act = (i, threadBias) => { var dummy = dict[i % 100000]; };

            RunBench(benchmarkName, act);
        }

        private static void GetBenchCD()
        {
            var dict = new Concurrent.ConcurrentDictionary<int, string>();

            Parallel.For(0, 100000, (i) => dict[i] = i.ToString());
            Parallel.For(0, 100000, (i) => { var dummy = dict[i]; });

            var benchmarkName = "======== Get Concurrent 1M Ops/sec:";

            Action<int, int> act = (i, threadBias) => { var dummy = dict[i % 100000]; };

            RunBench(benchmarkName, act);
        }

        private static void GetBenchRndNB()
        {
            var dict = new NonBlocking.ConcurrentDictionary<int, string>();

            Parallel.For(0, 100000, (i) => dict[i] = "qq");
            Parallel.For(0, 100000, (i) => { var dummy = dict[i]; });

            var benchmarkName = "======== Random Get NonBlocking 1M Ops/sec:";

            Action<int, int> act = (i, threadBias) =>
            {
                string dummy;
                int randomIndex = GetRandomIndex(i, threadBias, 100000);
                dict.TryGetValue(randomIndex, out dummy);
            };

            RunBench(benchmarkName, act);
        }

        private static void GetBenchRndCD()
        {
            var dict = new Concurrent.ConcurrentDictionary<int, string>();

            Parallel.For(0, 100000, (i) => dict[i] = "qq");
            Parallel.For(0, 100000, (i) => { var dummy = dict[i]; });

            var benchmarkName = "======== Random Get Concurrent 1M Ops/sec:";

            Action<int, int> act = (i, threadBias) =>
            {
                string dummy;
                int randomIndex = GetRandomIndex(i, threadBias, 100000);
                dict.TryGetValue(randomIndex, out dummy);
            };

            RunBench(benchmarkName, act);
        }

        private static int GetRandomIndex(int i, int threadBias, uint limit)
        {
            return (int)((uint)RandomizeBits(i + threadBias) % limit);
        }

        private static bool Every8K(int i)
        {
            const int mask8K = 8192 - 1;
            return (i & mask8K) == mask8K;
        }

        private static void AddBenchRndNB()
        {
            var dict = new NonBlocking.ConcurrentDictionary<int, string>();
            var cnt = new Counter32();

            var benchmarkName = "======== Random Add NonBlocking 1M Ops/sec:";

            Action<int, int> act = (i, threadBias) =>
            {
                // get some random index in [0, 1000000]
                int randomIndex = GetRandomIndex(i, threadBias, 1000000);
                dict.TryAdd(randomIndex, "qq");

                // after making about 1000000 adds, start with a new table
                var c = cnt;
                c.Increment();
                if (Every8K(i) && c.Value > 1000000)
                {
                    if (Interlocked.CompareExchange(ref cnt, new Counter32(), c) == c)
                    {
                        dict = new NonBlocking.ConcurrentDictionary<int, string>();
                    }
                }
            };

            RunBench(benchmarkName, act);
        }

        private static void AddBenchRndCD()
        {
            var dict = new Concurrent.ConcurrentDictionary<int, string>();
            var cnt = new Counter32();

            var benchmarkName = "======== Random Add Concurrent 1M Ops/sec:";

            Action<int, int> act = (i, threadBias) =>
            {
                // get some random index in [0, 1000000]
                int randomIndex = GetRandomIndex(i, threadBias, 1000000);
                dict.TryAdd(randomIndex, "qq");

                // after making about 1000000 adds, start with a new table
                var c = cnt;
                c.Increment();
                if (Every8K(i) && c.Value > 1000000)
                {
                    if (Interlocked.CompareExchange(ref cnt, new Counter32(), c) == c)
                    {
                        dict = new Concurrent.ConcurrentDictionary<int, string>();
                    }
                }
            };

            RunBench(benchmarkName, act);
        }

        private static void GetOrAddFuncBenchRndNB()
        {
            var dict = new NonBlocking.ConcurrentDictionary<int, string>();
            var cnt = new Counter32();

            var benchmarkName = "======== Random GetOrAdd Func NonBlocking 1M Ops/sec:";

            Action<int, int> act = (i, threadBias) =>
            {
                // get some random index in [0, 1000000]
                int randomIndex = GetRandomIndex(i, threadBias, 1000000);
                dict.GetOrAdd(randomIndex, (_) => "qq");

                // after making about 1000000 adds, start with a new table
                var c = cnt;
                c.Increment();
                if (Every8K(i) && c.Value > 1000000)
                {
                    if (Interlocked.CompareExchange(ref cnt, new Counter32(), c) == c)
                    {
                        dict = new NonBlocking.ConcurrentDictionary<int, string>();
                    }
                }
            };

            RunBench(benchmarkName, act);
        }

        private static void GetOrAddFuncBenchConcurrentNB()
        {
            var dict = new NonBlocking.ConcurrentDictionary<string, string>();
            var cnt = new Counter64();

            var benchmarkName = "======== Concurrency GetOrAdd Func NonBlocking 1M Ops/sec:";

            Action<int, int> act = (i, threadBias) =>
            {
                dict.GetOrAdd("Test", (_) => "qq");

                // after making about 1000000 adds, start with a new table
                var c = cnt;
                c.Increment();
                if (Every8K(i) && c.Value > 1000000)
                {
                    if (Interlocked.CompareExchange(ref cnt, new Counter64(), c) == c)
                    {
                        dict = new NonBlocking.ConcurrentDictionary<string, string>();
                    }
                }
            };

            RunBenchConcurrent(benchmarkName, act);
        }

        private static void GetOrAddFuncBenchRndCD()
        {
            var dict = new Concurrent.ConcurrentDictionary<int, string>();
            var cnt = new Counter32();

            var benchmarkName = "======== Random GetOrAdd Func Concurrent 1M Ops/sec:";

            Action<int, int> act = (i, threadBias) =>
            {
                // get some random index in [0, 1000000]
                int randomIndex = GetRandomIndex(i, threadBias, 1000000);
                dict.GetOrAdd(randomIndex, (_) => "qq");

                // after making about 1000000 adds, start with a new table
                var c = cnt;
                c.Increment();
                if (Every8K(i) && c.Value > 1000000)
                {
                    if (Interlocked.CompareExchange(ref cnt, new Counter32(), c) == c)
                    {
                        dict = new Concurrent.ConcurrentDictionary<int, string>();
                    }
                }
            };

            RunBench(benchmarkName, act);
        }

        private static void GetOrAddFuncBenchConcurrentCD()
        {
            var dict = new Concurrent.ConcurrentDictionary<string, string>();
            var cnt = new Counter64();

            var benchmarkName = "======== Concurrency GetOrAdd Func Concurrent 1M Ops/sec:";

            Action<int, int> act = (i, threadBias) =>
            {
                dict.GetOrAdd("Test", (_) => "qq");

                // after making about 1000000 adds, start with a new table
                var c = cnt;
                c.Increment();
                if (Every8K(i) && c.Value > 1000000)
                {
                    if (Interlocked.CompareExchange(ref cnt, new Counter64(), c) == c)
                    {
                        dict = new Concurrent.ConcurrentDictionary<string, string>();
                    }
                }
            };

            RunBenchConcurrent(benchmarkName, act);
        }

        private static void WriteBenchRndNB()
        {
            var dict = new NonBlocking.ConcurrentDictionary<int, string>();

            var benchmarkName = "======== Random Write NonBlocking 1M Ops/sec:";

            Action<int, int> act = (i, threadBias) =>
            {
                // get some random index in [0, 1000000]
                int randomIndex = GetRandomIndex(i, threadBias, 1000000);
                dict[randomIndex] = "qq";
                dict[randomIndex] = "aa";
            };

            RunBench(benchmarkName, act);
        }

        private static void WriteBenchRndCD()
        {
            var dict = new Concurrent.ConcurrentDictionary<int, string>();

            var benchmarkName = "======== Random Write Concurrent 1M Ops/sec:";

            Action<int, int> act = (i, threadBias) =>
            {
                // get some random index in [0, 1000000]
                int randomIndex = GetRandomIndex(i, threadBias, 1000000);
                dict[randomIndex] = "qq";
                dict[randomIndex] = "aa";
            };

            RunBench(benchmarkName, act);
        }

        private static long RunBenchmark(Action<int, int> action, int threads, int time)
        {
            Counter64 cnt = new Counter64();
            Task[] workers = new Task[threads];
            Stopwatch sw = Stopwatch.StartNew();
            ManualResetEventSlim e = new ManualResetEventSlim();
            long stop_time = 0;

            Action body = () =>
            {
                int iteration = 0;
                int threadBias = RandomizeBits(Environment.CurrentManagedThreadId);
                e.Wait();
                while (sw.ElapsedMilliseconds < stop_time)
                {
                    const int batch = 10000;
                    for (int i = 0; i < batch; i++)
                    {
                        action(iteration++, threadBias);
                    }
                    cnt.Add(batch);
                }
            };

            for (int i = 0; i < workers.Length; i++)
            {
                workers[i] = Task.Factory.StartNew(body, TaskCreationOptions.None);
            }

            stop_time = sw.ElapsedMilliseconds + time;
            e.Set();

            Task.WaitAll(workers);
            return cnt.Value;
        }

        private static void RunBench(string benchmarkName, Action<int, int> action)
        {
            System.Console.WriteLine(benchmarkName);
            var max_threads = Environment.ProcessorCount;
            for (int i = 1; i <= max_threads; i++)
            {
                var MOps = RunBenchmark(action, i, 3000) / 3000000.0;
                if (MOps > 1000)
                {
                    System.Console.Write("{0:f0} ", MOps);
                }
                else if (MOps > 100)
                {
                    System.Console.Write("{0:f1} ", MOps);
                }
                else if (MOps > 10)
                {
                    System.Console.Write("{0:f2} ", MOps);
                }
                else if (MOps > 1)
                {
                    System.Console.Write("{0:f3} ", MOps);
                }
                else
                {
                    System.Console.Write("{0:f4} ", MOps);
                }
            }
            System.Console.WriteLine();
            GC.Collect();
            GC.Collect();
        }

        private static void RunBenchConcurrent(string benchmarkName, Action<int, int> action)
        {
            System.Console.WriteLine(benchmarkName);
            var threadCnt = 10000;
            
            var MOps = RunBenchmark(action, threadCnt, 3000) / 3000000.0;
            if (MOps > 1000)
            {
                System.Console.Write("{0:f0} ", MOps);
            }
            else if (MOps > 100)
            {
                System.Console.Write("{0:f1} ", MOps);
            }
            else if (MOps > 10)
            {
                System.Console.Write("{0:f2} ", MOps);
            }
            else if (MOps > 1)
            {
                System.Console.Write("{0:f3} ", MOps);
            }
            else
            {
                System.Console.Write("{0:f4} ", MOps);
            }
           
            System.Console.WriteLine();
            GC.Collect();
            GC.Collect();
        }

        private static int RandomizeBits(int i)
        {
            uint h = (uint)i;
            // 32-bit finalizer for MurmurHash3.
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;

            return (int)h;
        }


        private static void ChurnSequential()
        {
            var dict = new NonBlocking.ConcurrentDictionary<int, string>();

            for (int i = 0; i < 1000000; i++)
            {
                dict.Add(i, "dummy");
                dict.Remove(i);
                //Thread.Sleep(10);
            }

            for (int i = 0; i < 100000; i++)
            {
                dict.Add(i, "dummy");
                dict.Remove(i);
                Thread.Sleep(5);
            }

        }

        private static void ChurnConcurrent()
        {
            var dict = new NonBlocking.ConcurrentDictionary<int, string>();
            //var dict = new ConcurrentDictionary<int, string>();

            var threadCnt = 200;
            List<Task> tasks = new List<Task>(threadCnt);

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < threadCnt; i++)
            {
                var task = new Task(() =>
                {
                    for (int j = i * 1000000, l = j + 1000000; j < l; j++)
                    {
                        string dummy;
                        dict.TryAdd(j, "dummy");
                        dict.TryRemove(j, out dummy);
                        //Thread.Sleep(10);
                    }
                    System.Console.Write('.');
                }, TaskCreationOptions.LongRunning);

                tasks.Add(task);
                task.Start();
            }

            Task.WaitAll(tasks.ToArray());

            System.Console.WriteLine(sw.ElapsedMilliseconds);
        }
    }

}
