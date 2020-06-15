# DatadogStatsD
Full featured [DogStatsD](https://docs.datadoghq.com/developers/dogstatsd) client:
- [Count](https://docs.datadoghq.com/developers/metrics/types/?tab=count#metric-types),
  [Histogram](https://docs.datadoghq.com/developers/metrics/types/?tab=count#metric-types),
  [Gauge](https://docs.datadoghq.com/developers/metrics/types/?tab=gauge#metric-types),
  [Distribution](https://docs.datadoghq.com/developers/metrics/types/?tab=distribution#metric-types),
  [Set](https://statsd.readthedocs.io/en/v3.2.1/types.html#sets)
- [Events](https://docs.datadoghq.com/events)
- [Service Checks](https://docs.datadoghq.com/developers/service_checks)
- [**UDP**](https://docs.datadoghq.com/developers/dogstatsd/?tab=hostagent#how-it-works) or
  [**UDS**](https://docs.datadoghq.com/developers/dogstatsd/unix_socket) transport
- **Performance** - Metrics are aggregated and the submissions are batched
- **Back pressure** - Transport drops new metrics when it's falling behind
- [**Telemetry**](https://docs.datadoghq.com/developers/dogstatsd/high_throughput/?tab=go#client-side-telemetry) -
  Metrics to monitor communication between the agent and this client

## Installation
[DatadogStatsD](https://www.nuget.org/packages/DatadogStatsD) targets .NET Standard 2.1.

`dotnet add package DatadogStatsD`

## Examples

```csharp
using var dogStatsD = new DogStatsD();

using var requests = dogStatsD.CreateCount("requests", new[] { "environment:dev" });
exampleMetric.Increment();
exampleMetric.Decrement();

using var latency = dogStatsD.CreateHistogram("latency", sampleRate: 0.5);
exampleMetric2.Sample(5.423);
exampleMetric2.Sample(1.27);

using var threads = dogStatsD.CreateGauge("threads", () => Process.GetCurrentProcess().Threads.Count);

dogStasD.RaiseEvent(AlertType.Info, "Bad thing happened", "This happened");
dogStasD.SendServiceCheck("is_connected", CheckStatus.Ok);
```

See [DogStatsDConfiguration.cs](https://github.com/verdie-g/DatadogStatsD/blob/master/DatadogStatsD/DogStatsDConfiguration.cs)
to configure the client.

## Benchmark

Benchmark comparing performance of this library (DatadogStatsD), when sending 10000 metrics, with [neuecc/DatadogSharp](https://github.com/neuecc/DatadogSharp)
(DatadogSharp) and [DataDog/dogstatsd-csharp-client](https://github.com/DataDog/dogstatsd-csharp-client) (StatsDClient).
Sources can be found in [DatadogStatsD.Benchmark](https://github.com/verdie-g/DatadogStatsD/blob/master/DatadogStatsD.Benchmark/Program.cs).

### Count & Gauge

|        Method |          Mean |        Error |       StdDev |     Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|-------------- |--------------:|-------------:|-------------:|----------:|---------:|---------:|----------:|
| DatadogStatsD |      55.41 us |     0.273 us |     0.242 us |         - |        - |        - |         - |
|  DatadogSharp | 140,563.34 us | 1,540.997 us | 1,441.449 us | 1750.0000 |        - |        - | 5599762 B |
|  StatsDClient |   9,535.01 us |   187.826 us |   348.147 us | 1671.8750 | 531.2500 | 109.3750 | 6089620 B |

This library aggregates for 10 seconds ([DogStatsD flush interval](https://docs.datadoghq.com/developers/dogstatsd/data_aggregation/#how-is-aggregation-performed-with-the-dogstatsd-server))
counts and gauges, so for 10000 increments, one packet is sent, hence the ~0 bytes allocated.

### Histogram, Set, Distribution

|        Method |       Mean |     Error |    StdDev |     Gen 0 |    Gen 1 |    Gen 2 |  Allocated |
|-------------- |-----------:|----------:|----------:|----------:|---------:|---------:|-----------:|
| DatadogStatsD |   5.024 ms | 0.0657 ms | 0.0615 ms |         - |        - |        - |    1.18 KB |
|  DatadogSharp | 140.095 ms | 0.9445 ms | 0.8835 ms | 1750.0000 |        - |        - | 5468.24 KB |
|  StatsDClient |   9.951 ms | 0.1976 ms | 0.4658 ms | 1671.8750 | 515.6250 | 109.3750 | 5945.63 KB |

For those metrics, the library lets DogStatsD agent do the aggregation, so with a sample rate of 1.0, each call to
Histogram.Update will be sent to the agent.
