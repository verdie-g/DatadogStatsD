using System;
using System.Collections.Generic;
using System.Linq;
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
        private static readonly string[] ConstantTags = { "env:prod", "service:my_service", "instance:my_instance" };
        private static readonly string[] Tags = { "name:toto" };

        [MemoryDiagnoser]
        public class Count
        {
            private readonly Socket _agent = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            private Metrics.Count _datadogStatsD;
            private DatadogSharp.DogStatsd.DatadogStats _datadogSharp;
            private DogStatsdService _dogStatsDService;

            [GlobalSetup]
            public void GlobalSetup()
            {
                _agent.Bind(Endpoint);

                _datadogStatsD = new DogStatsD(new DogStatsDConfiguration
                {
                    EndPoint = Endpoint,
                    Namespace = Namespace,
                    ConstantTags = ParseTags(ConstantTags).ToArray(),
                }).CreateCount(MetricName, ParseTags(Tags).ToArray());

                _datadogSharp = new DatadogSharp.DogStatsd.DatadogStats(Endpoint.Address.ToString(), Endpoint.Port,
                    Namespace, ConstantTags);
                _dogStatsDService = new DogStatsdService();
                _dogStatsDService.Configure(new StatsdConfig
                {
                    Prefix = Namespace,
                    ConstantTags = ConstantTags,
                    StatsdServerName = Endpoint.Address.ToString(),
                    StatsdPort = Endpoint.Port
                });
            }

            [Benchmark(Description = "verdie-g/DatadogStatsD", OperationsPerInvoke = Operations)]
            public void DatadogStatsD()
            {
                for (int i = 0; i < Operations; i += 1)
                {
                    _datadogStatsD.Increment(i);
                }
            }

            [Benchmark(Description = "Datadog/dogstatsd-csharp-client", OperationsPerInvoke = Operations)]
            public void DogStatsDService()
            {
                for (int i = 0; i < Operations; i += 1)
                {
                    _dogStatsDService.Counter(MetricName, i, SamplingRate, Tags);
                }
            }

            [Benchmark(Description = "neuecc/DatadogSharp", OperationsPerInvoke = Operations)]
            public void DatadogSharp()
            {
                for (int i = 0; i < Operations; i += 1)
                {
                    _datadogSharp.Increment(MetricName, i, SamplingRate, Tags);
                }
            }
        }

        [MemoryDiagnoser]
        public class Histogram
        {
            private readonly Socket _agent = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            private Metrics.Histogram _datadogStatsD;
            private DatadogSharp.DogStatsd.DatadogStats _datadogSharp;
            private DogStatsdService _dogStatsDService;

            [GlobalSetup]
            public void GlobalSetup()
            {
                _agent.Bind(Endpoint);

                _datadogStatsD = new DogStatsD(new DogStatsDConfiguration
                {
                    EndPoint = Endpoint,
                    Namespace = Namespace,
                    ConstantTags = ParseTags(ConstantTags).ToArray(),
                }).CreateHistogram(MetricName, 1.0, ParseTags(Tags).ToArray());

                _datadogSharp = new DatadogSharp.DogStatsd.DatadogStats(Endpoint.Address.ToString(), Endpoint.Port,
                    Namespace, ConstantTags);
                _dogStatsDService = new DogStatsdService();
                _dogStatsDService.Configure(new StatsdConfig
                {
                    Prefix = Namespace,
                    ConstantTags = ConstantTags,
                    StatsdServerName = Endpoint.Address.ToString(),
                    StatsdPort = Endpoint.Port
                });
            }

            [Benchmark(Description = "verdie-g/DatadogStatsD", OperationsPerInvoke = Operations)]
            public void DatadogStatsD()
            {
                for (int i = 0; i < Operations; i += 1)
                {
                    _datadogStatsD.Sample(i);
                }
            }

            [Benchmark(Description = "Datadog/dogstatsd-csharp-client", OperationsPerInvoke = Operations)]
            public void DogStatsDService()
            {
                for (int i = 0; i < Operations; i += 1)
                {
                    _dogStatsDService.Histogram(MetricName, i, SamplingRate, Tags);
                }
            }

            [Benchmark(Description = "neuecc/DatadogSharp", OperationsPerInvoke = Operations)]
            public void DatadogSharp()
            {
                for (int i = 0; i < Operations; i += 1)
                {
                    _datadogSharp.Histogram(MetricName, i, SamplingRate, Tags);
                }
            }
        }

        public static IEnumerable<KeyValuePair<string, string>> ParseTags(IEnumerable<string> tags)
        {
            foreach (string tag in tags)
            {
                var parts = tag.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
                string key = parts[0];
                string value = parts.Length > 1 ? parts[1] : string.Empty;
                yield return KeyValuePair.Create(key, value);
            }
        }
    }
}
