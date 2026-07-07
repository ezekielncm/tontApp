using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Performance Benchmark: N+1 Query vs Cached Dictionary");
            Console.WriteLine("-----------------------------------------------------");

            int itemCount = 1000;
            int distinctPlansCount = 3;
            var random = new Random(42);

            // Generate mock data
            var abonnementPlanIds = new List<Guid>();
            var availablePlanIds = Enumerable.Range(0, distinctPlansCount).Select(_ => Guid.NewGuid()).ToList();

            for (int i = 0; i < itemCount; i++)
            {
                abonnementPlanIds.Add(availablePlanIds[random.Next(distinctPlansCount)]);
            }

            Console.WriteLine($"Items to process: {itemCount}");
            Console.WriteLine($"Distinct plans: {distinctPlansCount}");
            Console.WriteLine();

            // 1. Unoptimized (N+1 Query)
            var swUnoptimized = Stopwatch.StartNew();
            int unoptimizedQueries = 0;
            foreach (var planId in abonnementPlanIds)
            {
                // Simulate GetByIdAsync with 2ms DB latency
                await MockGetByIdAsync(planId);
                unoptimizedQueries++;
            }
            swUnoptimized.Stop();

            Console.WriteLine($"[Baseline - Unoptimized]");
            Console.WriteLine($"Time elapsed: {swUnoptimized.ElapsedMilliseconds} ms");
            Console.WriteLine($"Simulated queries executed: {unoptimizedQueries}");
            Console.WriteLine();

            // 2. Optimized (Cached Dictionary)
            var swOptimized = Stopwatch.StartNew();
            int optimizedQueries = 0;

            // Pre-fetch unique plans
            var uniquePlanIds = abonnementPlanIds.Distinct().ToList();
            var plansCache = new Dictionary<Guid, string>();

            foreach (var uniqueId in uniquePlanIds)
            {
                var plan = await MockGetByIdAsync(uniqueId);
                optimizedQueries++;
                plansCache[uniqueId] = plan;
            }

            // Process loop using cache
            foreach (var planId in abonnementPlanIds)
            {
                plansCache.TryGetValue(planId, out var plan);
                // Do nothing, just simulate dictionary lookup
            }
            swOptimized.Stop();

            Console.WriteLine($"[Optimized - Cached]");
            Console.WriteLine($"Time elapsed: {swOptimized.ElapsedMilliseconds} ms");
            Console.WriteLine($"Simulated queries executed: {optimizedQueries}");
            Console.WriteLine();

            // Comparison
            double speedup = (double)swUnoptimized.ElapsedMilliseconds / Math.Max(1, swOptimized.ElapsedMilliseconds);
            Console.WriteLine($"Improvement: Optimized is {speedup:F2}x faster");
        }

        static async Task<string> MockGetByIdAsync(Guid id)
        {
            // Simulate 2ms database latency per query
            await Task.Delay(2);
            return $"Plan-{id}";
        }
    }
}
