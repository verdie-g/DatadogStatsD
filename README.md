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
[DatadogStatsD](https://www.nuget.org/packages/DatadogStatsD) targets both .NET Standard 2.0 & 2.1.

`dotnet add package DatadogStatsD`

## Examples

```csharp
await using var dogStatsD = new DogStatsD();

using var requests = dogStatsD.CreateCount("requests", new[] { "environment:dev" });
requests.Increment();
requests.Decrement();

var latency = dogStatsD.CreateHistogram("latency", sampleRate: 0.5);
latency.Sample(5.423);
latency.Sample(1.27);

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

### Count, Gauge, Set

|        Method |         Mean |      Error |     StdDev |     Gen 0 |  Gen 1 | Gen 2 | Allocated |
|-------------- |-------------:|-----------:|-----------:|----------:|-------:|------:|----------:|
| DatadogStatsD |     80.90 us |   0.099 us |   0.093 us |         - |      - |     - |         - |
|  DatadogSharp | 85,855.51 us | 406.704 us | 380.431 us | 3000.0000 |      - |     - | 4879261 B |
|  StatsDClient |  2,577.34 us |  43.403 us |  36.244 us |  574.2188 | 3.9063 |     - |  902065 B |

This library aggregates for 10 seconds ([DogStatsD flush interval](https://docs.datadoghq.com/developers/dogstatsd/data_aggregation/#how-is-aggregation-performed-with-the-dogstatsd-server))
counts, gauges and sets. So for 10000 increments, one packet is sent, hence the ~0 bytes allocated.

### Histogram, Distribution

|        Method |      Mean |     Error |    StdDev |     Gen 0 |  Gen 1 | Gen 2 | Allocated |
|-------------- |----------:|----------:|----------:|----------:|-------:|------:|----------:|
| DatadogStatsD |  2.464 ms | 0.0485 ms | 0.0696 ms |         - |      - |     - |     750 B |
|  DatadogSharp | 86.875 ms | 0.4703 ms | 0.4399 ms | 3000.0000 |      - |     - | 4879381 B |
|  StatsDClient |  2.558 ms | 0.0478 ms | 0.0423 ms |  574.2188 | 3.9063 |     - |  901430 B |

For those metrics, the library lets DogStatsD agent do the aggregation, so with a sample rate of 1.0, each call to
Histogram.Update will be sent to the agent.
