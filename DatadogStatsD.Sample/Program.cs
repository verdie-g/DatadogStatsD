using System.Diagnostics;
using System.Threading.Tasks;

namespace DatadogStatsD.Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // socket.Bind(IPEndPoint.Parse("127.0.0.1:2020"));
            int i = 0;
            var sw = new Stopwatch();

            var dog = new DogStatsD(new DogStatsDConfiguration { Namespace = "toto", ConstantTags = new[] { "env:dev" }});
            var count = dog.CreateCount("test.incr");
            var gauge = dog.CreateGauge("test.gauge", () => i);
            var hist = dog.CreateHistogram("test.hist", 0.5);

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