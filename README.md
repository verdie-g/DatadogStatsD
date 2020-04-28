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
exampleMetric2.Record(5.423);
exampleMetric2.Record(1.27);

using var threads = dogStatsD.CreateGauge("threads", () => Process.GetCurrentProcess().Threads.Count);

dogStasD.RaiseEvent(AlertType.Info, "Bad thing happened", "This happened");
dogStasD.SendServiceCheck("is_connected", CheckStatus.Ok);
```

See [DogStatsDConfiguration.cs](https://github.com/verdie-g/DatadogStatsD/blob/master/DatadogStatsD/DogStatsDConfiguration.cs)
to configure the client.

## Benchmark

Benchmark comparing performance of this library with [neuecc/DatadogSharp](https://github.com/neuecc/DatadogSharp)
and [DataDog/dogstatsd-csharp-client](https://github.com/DataDog/dogstatsd-csharp-client). Sources can be found in
[DatadogStatsD.Benchmark](https://github.com/verdie-g/DatadogStatsD/blob/master/DatadogStatsD.Benchmark/Program.cs).

### Count & Gauge

```
|        Method |     Op |           Mean |         Error |        StdDev |      Gen 0 | Gen 1 | Gen 2 |  Allocated |
|-------------- |------- |---------------:|--------------:|--------------:|-----------:|------:|------:|-----------:|
| DatadogStatsD |   1000 |       5.837 us |     0.0078 us |     0.0069 us |          - |     - |     - |          - |
|  DatadogSharp |   1000 |   2,514.247 us |    13.9417 us |    13.0410 us |   355.4688 |     - |     - |   558888 B |
|  StatsDClient |   1000 |   3,060.752 us |    18.6742 us |    16.5542 us |   578.1250 |     - |     - |   912003 B |
| DatadogStatsD |  10000 |      58.381 us |     0.0399 us |     0.0354 us |          - |     - |     - |          - |
|  DatadogSharp |  10000 |  26,261.458 us |   143.5175 us |   134.2464 us |  3562.5000 |     - |     - |  5598880 B |
|  StatsDClient |  10000 |  32,056.996 us |   242.7546 us |   215.1956 us |  5800.0000 |     - |     - |  9192000 B |
| DatadogStatsD | 100000 |     806.360 us |     3.1682 us |     2.8085 us |          - |     - |     - |        3 B |
|  DatadogSharp | 100000 | 254,610.103 us | 1,050.1342 us |   930.9166 us | 35500.0000 |     - |     - | 55998880 B |
|  StatsDClient | 100000 | 325,899.359 us | 1,442.2961 us | 1,349.1247 us | 58000.0000 |     - |     - | 91994112 B |
```

This library aggregates for 10 seconds ([DogStatsD flush interval](https://docs.datadoghq.com/developers/dogstatsd/data_aggregation/#how-is-aggregation-performed-with-the-dogstatsd-server))
counts and gauges, so for 10000 increments, one packet is sent, hence the ~0 bytes allocated.

### Histogram, Set, Distribution

```
|        Method |     Op |         Mean |       Error |      StdDev |      Gen 0 | Gen 1 | Gen 2 |   Allocated |
|-------------- |------- |-------------:|------------:|------------:|-----------:|------:|------:|------------:|
| DatadogStatsD |   1000 |     754.4 us |    15.01 us |    42.82 us |     0.9766 |     - |     - |     1.49 KB |
|  DatadogSharp |   1000 |   2,456.9 us |     7.23 us |     6.41 us |   355.4688 |     - |     - |   545.79 KB |
|  StatsDClient |   1000 |   3,104.0 us |    16.35 us |    14.49 us |   578.1250 |     - |     - |   890.63 KB |
| DatadogStatsD |  10000 |   7,326.7 us |   149.88 us |   200.08 us |          - |     - |     - |     3.88 KB |
|  DatadogSharp |  10000 |  24,669.0 us |   107.04 us |   100.12 us |  3562.5000 |     - |     - |  5467.66 KB |
|  StatsDClient |  10000 |  31,343.0 us |    83.92 us |    70.08 us |  5812.5000 |     - |     - |  8976.56 KB |
| DatadogStatsD | 100000 |  71,366.4 us |   783.04 us |   732.46 us |          - |     - |     - |    34.68 KB |
|  DatadogSharp | 100000 | 255,144.0 us | 1,767.41 us | 1,653.24 us | 35500.0000 |     - |     - | 54687.38 KB |
|  StatsDClient | 100000 | 332,697.0 us | 6,509.56 us | 8,690.08 us | 58000.0000 |     - |     - | 89835.94 KB |

```

For those metrics, the library lets DogStatsD agent do the aggregation, so with a sample rate of 1.0, each call to
Histogram.Update will be sent to the agent.
