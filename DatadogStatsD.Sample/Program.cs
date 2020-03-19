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
                Namespace = "toto",
                ConstantTags = new[] { "env:dev" },
                Source = "evian",
            });

            const int delay = 50;
            int i = 0;
            var sw = new Stopwatch();
            var rdn = new Random();

            using var count = dog.CreateCount("test.incr");
            using var gauge = dog.CreateGauge("test.gauge", () => i);
            using var hist = dog.CreateHistogram("test.hist", 0.5);

            for (; i < 100_000_000; i += 1)
            {
                sw.Restart();
                count.Increment();

                if (i % (2000 / delay) == 0)
                {
                    dog.RaiseEvent((AlertType)rdn.Next(3), "Bad thing happened " + i, "The cloud transpilation to Rust failed",
                        (Priority)rdn.Next(1), "rust_fail", new[] { "extratag" });
                }

                if (i % (5000 / delay) == 0)
                {
                    dog.SendServiceCheck("is_connected", (CheckStatus)rdn.Next(3), "A message", new[] { "extratag" });
                }

                await Task.Delay(delay);
                hist.Update(sw.ElapsedMilliseconds);
            }
        }
    }
}