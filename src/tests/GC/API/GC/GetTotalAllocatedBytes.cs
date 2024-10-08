// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Tests GC.Collect()

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class Test_GetTotalAllocatedBytes 
{
    struct Counts
    {
        public Counts(long precise, long imprecise)
        {
            this.precise = precise;
            this.imprecise = imprecise;
        }
        public readonly long precise;
        public readonly long imprecise;
    }

    static Random Rand = new Random();
    static volatile object s_stash; // static volatile variable to keep the jit from eliding allocations or anything.

    delegate long GetTotalAllocatedBytesDelegate(bool precise);
    static GetTotalAllocatedBytesDelegate GetTotalAllocatedBytes = Get_GetTotalAllocatedBytesDelegate();

    private static GetTotalAllocatedBytesDelegate Get_GetTotalAllocatedBytesDelegate()
    {
        const string name = "GetTotalAllocatedBytes";
        var typeInfo = typeof(GC).GetTypeInfo();
        var method = typeInfo.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        GetTotalAllocatedBytesDelegate del = (GetTotalAllocatedBytesDelegate)method.CreateDelegate(typeof(GetTotalAllocatedBytesDelegate));
        // Prime the delegate to ensure its been called some.
        del(true);
        del(false);

        return del;
    }

    private static Counts CallGetTotalAllocatedBytes(Counts previous, out long differenceBetweenPreciseAndImprecise)
    {
        long precise = GetTotalAllocatedBytes(true);
        long imprecise = GetTotalAllocatedBytes(false);

        if (precise <= 0)
        {
            throw new Exception($"Bytes allocated is not positive, this is unlikely. precise = {precise}");
        }

        if (imprecise < precise)
        {
            throw new Exception($"Imprecise total bytes allocated less than precise, imprecise is required to be a conservative estimate (that estimates high). imprecise = {imprecise}, precise = {precise}");
        }

        if (previous.precise > precise)
        {
            throw new Exception($"Expected more memory to be allocated. previous.precise = {previous.precise}, precise = {precise}, difference = {previous.precise - precise}");
        }

        if (previous.imprecise > imprecise)
        {
            throw new Exception($"Expected more memory to be allocated. previous.imprecise = {previous.imprecise}, imprecise = {imprecise}, difference = {previous.imprecise - imprecise}");
        }

        differenceBetweenPreciseAndImprecise = imprecise - precise;
        return new Counts(precise, imprecise);
    }

    private static Counts CallGetTotalAllocatedBytes(Counts previous)
    {
        long differenceBetweenPreciseAndImprecise;
        previous = CallGetTotalAllocatedBytes(previous, out differenceBetweenPreciseAndImprecise);
        s_stash = new byte[differenceBetweenPreciseAndImprecise];
        previous = CallGetTotalAllocatedBytes(previous, out differenceBetweenPreciseAndImprecise);
        return previous;
    }

    public static void TestSingleThreaded()
    {
        Counts previous = default(Counts);
        for (int i = 0; i < 1000; ++i)
        {
            s_stash = new byte[1234];
            previous = CallGetTotalAllocatedBytes(previous);
        }
    }

    public static void TestSingleThreadedLOH()
    {
        Counts previous = default(Counts);
        for (int i = 0; i < 1000; ++i)
        {
            s_stash = new byte[123456];
            previous = CallGetTotalAllocatedBytes(previous);
        }
    }

    public static void TestAnotherThread()
    {
        bool running = true;
        Task tsk = null;

        try
        {
            object lck = new object();

            tsk = Task.Run(() => {
                while (running)
                {
                    Thread thd = new Thread(() => {
                        lock (lck)
                        {
                            s_stash = new byte[1234];
                        }
                    });

                    thd.Start();
                    thd.Join();
                }
            });

            Counts previous = default(Counts);
            for (int i = 0; i < 1000; ++i)
            {
                lock (lck)
                {
                    previous = CallGetTotalAllocatedBytes(previous);
                }

                Thread.Sleep(1);
            }
        }
        finally
        {
            running = false;
            tsk?.Wait(1000);
        }
    }

    public static void TestLohSohConcurrently()
    {
        List<Thread> threads = new List<Thread>();
        ManualResetEventSlim me = new ManualResetEventSlim();
        int threadNum = Environment.ProcessorCount + Environment.ProcessorCount / 2;
        for (int i = 0; i < threadNum; i++)
        {
            Thread thr = new Thread(() =>
            {
                me.Wait();
                Counts previous = default(Counts);
                for (int i = 0; i < 2; ++i)
                {
                    s_stash = new byte[123456];
                    previous = CallGetTotalAllocatedBytes(previous);
                    s_stash = new byte[1234];
                    previous = CallGetTotalAllocatedBytes(previous);
                }
            });

            thr.Start();
            threads.Add(thr);
        }

        me.Set();

        foreach (var thr in threads)
            thr.Join();
    }

    [Fact]
    public static void TestEntryPoint() 
    {
        TestSingleThreaded();
        TestSingleThreadedLOH();
        TestAnotherThread();
        TestLohSohConcurrently();
    }
}
