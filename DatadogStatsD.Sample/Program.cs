using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DatadogStatsD.Events;
using DatadogStatsD.ServiceChecks;

namespace DatadogStatsD.Sample
{
    internal class Program
    {
        static async Task Main()
        {
            using var dog = new DogStatsD(new DogStatsDConfiguration
            {
                Namespace = "test",
                ConstantTags = new[] { "env:dev" },
            });

            const int delay = 50;
            int i = 0;
            var sw = new Stopwatch();
            var rdn = new Random();

            using var count = dog.CreateCount("count");
            using var gauge = dog.CreateGauge("gauge", () => i);
            using var hist = dog.CreateHistogram("histogram", 0.5);
            using var set = dog.CreateSet("set");
            using var distribution = dog.CreateDistribution("distribution");

            for (; i < 100_000_000; i += 1)
            {
                sw.Restart();
                count.Increment();

                if (i % (2000 / delay) == 0)
                {
                    dog.RaiseEvent((AlertType)rdn.Next(3), "Bad thing happened " + i, "The cloud transpilation to Rust failed",
                        (EventPriority)rdn.Next(1), "rust_fail", new[] { "extratag" });
                }

                if (i % (5000 / delay) == 0)
                {
                    dog.SendServiceCheck("is_connected", (CheckStatus)rdn.Next(3), "A message", new[] { "extratag" });
                }

                await Task.Delay(delay);
                hist.Record(sw.ElapsedMilliseconds);
                set.Add(i % 2);
                distribution.Record(i % 10);
            }
        }
    }
}