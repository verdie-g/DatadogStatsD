using System.Net;
using System.Net.Sockets;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using StatsdClient;

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
        private const int Operations = 10_000;
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
            private DogStatsdService _statsDClient;

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
                _statsDClient = new DogStatsdService();
                _statsDClient.Configure(new StatsdConfig
                {
                    Prefix = Namespace,
                    ConstantTags = ConstantTags,
                    StatsdServerName = Endpoint.Address.ToString(),
                    StatsdPort = Endpoint.Port
                });
            }

            [Benchmark]
            public void DatadogStatsD()
            {
                for (int i = 0; i < Operations; i += 1)
                {
                    _datadogStatsD.Increment(i);
                }
            }

            [Benchmark]
            public void DatadogSharp()
            {
                for (int i = 0; i < Operations; i += 1)
                {
                    _datadogSharp.Increment(MetricName, i, SamplingRate, Tags);
                }
            }

            [Benchmark]
            public void StatsDClient()
            {
                for (int i = 0; i < Operations; i += 1)
                {
                    _statsDClient.Counter(MetricName, i, SamplingRate, Tags);
                }
            }
        }

        [MemoryDiagnoser]
        public class Histogram
        {
            private readonly Socket _agent = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            private Metrics.Histogram _datadogStatsD;
            private DatadogSharp.DogStatsd.DatadogStats _datadogSharp;
            private DogStatsdService _statsDClient;

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
                _statsDClient = new DogStatsdService();
                _statsDClient.Configure(new StatsdConfig
                {
                    Prefix = Namespace,
                    ConstantTags = ConstantTags,
                    StatsdServerName = Endpoint.Address.ToString(),
                    StatsdPort = Endpoint.Port
                });
            }

            [Benchmark]
            public void DatadogStatsD()
            {
                for (int i = 0; i < Operations; i += 1)
                {
                    _datadogStatsD.Sample(i);
                }
            }

            [Benchmark]
            public void DatadogSharp()
            {
                for (int i = 0; i < Operations; i += 1)
                {
                    _datadogSharp.Histogram(MetricName, i, SamplingRate, Tags);
                }
            }

            [Benchmark]
            public void StatsDClient()
            {
                for (int i = 0; i < Operations; i += 1)
                {
                    _statsDClient.Histogram(MetricName, i, SamplingRate, Tags);
                }
            }
        }
    }
}