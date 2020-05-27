using System.Net;
using System.Net.Sockets;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace DatadogStatsD.Benchmark
{
    class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<Benchmark.Histogram>();
        }
    }

    public class Benchmark
    {
        private static readonly IPEndPoint Endpoint = IPEndPoint.Parse("127.0.0.1:2020");
        private static readonly string Namespace = "ns";
        private static readonly string MetricName = "example_metric.increment";
        private static readonly double SamplingRate = 1.0;
        private static readonly string[] ConstantTags = { "host:myhost" };
        private static readonly string[] Tags = { "environment:dev" };

        [MemoryDiagnoser]
        public class Count
        {
            private readonly Socket _agent = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            private Metrics.Count _datadogStatsD;
            private DatadogSharp.DogStatsd.DatadogStats _datadogSharp;
            private StatsdClient.Statsd _statsDClient;

            [Params(1_000, 10_000, 100_000)]
            public int Op { get; set; }

            [GlobalSetup]
            public void GlobalSetup()
            {
                _agent.Bind(Endpoint);

                _datadogStatsD = new DogStatsD(new DogStatsDConfiguration
                {
                    EndPoint = Endpoint,
                    Namespace = Namespace,
                    ConstantTags = ConstantTags,
                }).CreateCount(MetricName, Tags);

                _datadogSharp = new DatadogSharp.DogStatsd.DatadogStats(Endpoint.Address.ToString(), Endpoint.Port,
                    Namespace, ConstantTags);
                _statsDClient = new StatsdClient.Statsd(
                    new StatsdClient.StatsdUDP(Endpoint.Address.ToString(), Endpoint.Port),
                    new StatsdClient.RandomGenerator(), new StatsdClient.StopWatchFactory(), Namespace, ConstantTags);
            }

            [Benchmark]
            public void DatadogStatsD()
            {
                for (int i = 0; i < Op; i += 1)
                {
                    _datadogStatsD.Increment(i);
                }
            }

            [Benchmark]
            public void DatadogSharp()
            {
                for (int i = 0; i < Op; i += 1)
                {
                    _datadogSharp.Increment(MetricName, i, SamplingRate, Tags);
                }
            }

            [Benchmark]
            public void StatsDClient()
            {
                for (int i = 0; i < Op; i += 1)
                {
                    _statsDClient.Send<StatsdClient.Statsd.Counting, long>(MetricName, i, SamplingRate, Tags);
                }
            }
        }

        [MemoryDiagnoser]
        public class Histogram
        {
            private readonly Socket _agent = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            private Metrics.Histogram _datadogStatsD;
            private DatadogSharp.DogStatsd.DatadogStats _datadogSharp;
            private StatsdClient.Statsd _statsDClient;

            [Params(1_000, 10_000, 100_000)]
            public int Op { get; set; }

            [GlobalSetup]
            public void GlobalSetup()
            {
                _agent.Bind(Endpoint);

                _datadogStatsD = new DogStatsD(new DogStatsDConfiguration
                {
                    EndPoint = Endpoint,
                    Namespace = Namespace,
                    ConstantTags = ConstantTags,
                }).CreateHistogram(MetricName, 1.0, Tags);

                _datadogSharp = new DatadogSharp.DogStatsd.DatadogStats(Endpoint.Address.ToString(), Endpoint.Port,
                    Namespace, ConstantTags);
                _statsDClient = new StatsdClient.Statsd(
                    new StatsdClient.StatsdUDP(Endpoint.Address.ToString(), Endpoint.Port),
                    new StatsdClient.RandomGenerator(), new StatsdClient.StopWatchFactory(), Namespace, ConstantTags);
            }

            [Benchmark]
            public void DatadogStatsD()
            {
                for (int i = 0; i < Op; i += 1)
                {
                    _datadogStatsD.Record(i);
                }
            }

            [Benchmark]
            public void DatadogSharp()
            {
                for (int i = 0; i < Op; i += 1)
                {
                    _datadogSharp.Histogram(MetricName, i, SamplingRate, Tags);
                }
            }

            [Benchmark]
            public void StatsDClient()
            {
                for (int i = 0; i < Op; i += 1)
                {
                    _statsDClient.Send<StatsdClient.Statsd.Histogram, long>(MetricName, i, SamplingRate, Tags);
                }
            }
        }
    }
}