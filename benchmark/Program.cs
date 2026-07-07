using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Mock setup
public class Benchmark
{
    public static async Task Main()
    {
        Console.WriteLine("Starting benchmark...");

        // Setup
        int numMembers = 1000;
        var members = Enumerable.Range(1, numMembers).Select(i => i.ToString()).ToList();

        // Simulating N+1
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < members.Count; i++)
        {
            await Task.Delay(2); // Simulate DB trip + network
        }
        sw.Stop();
        Console.WriteLine($"N+1 execution time for {numMembers} members: {sw.ElapsedMilliseconds} ms");

        // Simulating Batch
        sw.Restart();
        await Task.Delay(2); // One DB query for rate limits
        await Task.Delay(2); // One DB query for insert
        sw.Stop();
        Console.WriteLine($"Batch execution time for {numMembers} members: {sw.ElapsedMilliseconds} ms");

        // Improvement
        Console.WriteLine("Drastic improvement expected.");
    }
}
