using System.Diagnostics;
using System.Threading.Tasks;
using DatadogStatsD.Events;

namespace DatadogStatsD.Sample
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using var dog = new DogStatsD(new DogStatsDConfiguration
            {
                Namespace = "toto",
                ConstantTags = new[] { "env:dev" },
                Source = "evian",
            });
            await SendEvents(dog);
            // await SendMetrics(dog);
        }

        private static async Task SendEvents(DogStatsD dog)
        {
            for (int i = 0; i < 1000; i += 1)
            {
                dog.RaiseEvent(AlertType.Error, "Bad thing happened " + i, "The cloud transpilation to Rust failed",
                    Priority.Low, "rust_fail", new[] { "extratag" });
                await Task.Delay(1000);
            }
        }

        private static async Task SendMetrics(DogStatsD dog)
        {
            int i = 0;
            var sw = new Stopwatch();

            using var count = dog.CreateCount("test.incr");
            using var gauge = dog.CreateGauge("test.gauge", () => i);
            using var hist = dog.CreateHistogram("test.hist", 0.5);

            sw.Start();
            for (; i < 100_000_000; i += 1)
            {
                count.Increment();
                await Task.Delay(50);
                hist.Update(sw.ElapsedMilliseconds);
                sw.Restart();
            }
        }
    }
}