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
            BenchmarkRunner.Run<Benchmark>();
        }
    }

    [MemoryDiagnoser]
    public class Benchmark
    {
        private static readonly IPEndPoint Endpoint = IPEndPoint.Parse("127.0.0.1:2020");
        private static readonly string MetricName = "example_metric.increment";
        private static readonly double SamplingRate = 1.0;
        private static readonly string[] Tags = { "environment:dev" };
        private readonly Socket _agent = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        private Metrics.Count _datadogStatsD;
        private DatadogSharp.DogStatsd.DatadogStats _datadogSharp;
        private StatsdClient.Statsd _statsDClient;

        [Params(1_000, 10_000, 100_000)]
        public int Operations { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _agent.Bind(Endpoint);

            _datadogStatsD = new DogStatsD(new DogStatsDConfiguration
            {
                Host = Endpoint.Address.ToString(),
                Port = Endpoint.Port,
            }).CreateCount(MetricName, SamplingRate, Tags);

            _datadogSharp = new DatadogSharp.DogStatsd.DatadogStats(Endpoint.Address.ToString(), Endpoint.Port);
            _statsDClient = new StatsdClient.Statsd(new StatsdClient.StatsdUDP(Endpoint.Address.ToString(), Endpoint.Port));
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
                _statsDClient.Send<StatsdClient.Statsd.Counting, long>(MetricName, i, SamplingRate, Tags);
            }
        }
    }
}