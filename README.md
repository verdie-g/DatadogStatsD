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
|        Method |     Op |             Mean |          Error |         StdDev |      Gen 0 | Gen 1 | Gen 2 |  Allocated |
|-------------- |------- |-----------------:|---------------:|---------------:|-----------:|------:|------:|-----------:|
| DatadogStatsD |   1000 |         8.480 us |      0.1635 us |      0.3554 us |          - |     - |     - |          - |
|  DatadogSharp |   1000 |     8,640.205 us |    171.6157 us |    390.8558 us |   343.7500 |     - |     - |   558880 B |
|  StatsDClient |   1000 |     9,928.695 us |    168.0484 us |    206.3785 us |   578.1250 |     - |     - |   912021 B |
| DatadogStatsD |  10000 |        86.608 us |      1.7011 us |      2.6982 us |          - |     - |     - |          - |
|  DatadogSharp |  10000 |    84,551.941 us |  1,679.0799 us |  3,112.2822 us |  3428.5714 |     - |     - |  5598880 B |
|  StatsDClient |  10000 |   102,578.512 us |  1,974.8266 us |  4,416.9827 us |  5800.0000 |     - |     - |  9192000 B |
| DatadogStatsD | 100000 |       862.448 us |     17.0070 us |     35.5000 us |          - |     - |     - |          - |
|  DatadogSharp | 100000 |   852,460.918 us | 16,706.6878 us | 29,260.4160 us | 35000.0000 |     - |     - | 56000000 B |
|  StatsDClient | 100000 | 1,001,979.915 us | 20,012.3158 us | 28,054.4718 us | 58000.0000 |     - |     - | 91992000 B |
```

This library aggregates for 10 seconds ([DogStatsD flush interval](https://docs.datadoghq.com/developers/dogstatsd/data_aggregation/#how-is-aggregation-performed-with-the-dogstatsd-server))
counts and gauges, so for 10000 increments, one packet is sent, hence the ~0 bytes allocated.

### Histogram, Set, Distribution

```
|        Method |     Op |         Mean |        Error |       StdDev |      Gen 0 | Gen 1 | Gen 2 |  Allocated |
|-------------- |------- |-------------:|-------------:|-------------:|-----------:|------:|------:|-----------:|
| DatadogStatsD |   1000 |     607.0 us |     11.48 us |     18.87 us |          - |     - |     - |      384 B |
|  DatadogSharp |   1000 |   8,485.5 us |    162.87 us |    167.26 us |   343.7500 |     - |     - |   559013 B |
|  StatsDClient |   1000 |  10,053.5 us |    200.77 us |    396.29 us |   578.1250 |     - |     - |   912000 B |
| DatadogStatsD |  10000 |   5,955.4 us |    118.61 us |    170.11 us |          - |     - |     - |     3196 B |
|  DatadogSharp |  10000 |  87,439.0 us |  1,722.20 us |  3,670.14 us |  3500.0000 |     - |     - |  5599101 B |
|  StatsDClient |  10000 | 104,099.7 us |  2,063.75 us |  3,504.41 us |  5800.0000 |     - |     - |  9192000 B |
| DatadogStatsD | 100000 |  60,332.3 us |  1,202.28 us |  3,291.21 us |          - |     - |     - |    23940 B |
|  DatadogSharp | 100000 | 867,484.8 us | 17,075.68 us | 22,203.21 us | 35000.0000 |     - |     - | 56000000 B |
|  StatsDClient | 100000 | 972,766.1 us | 19,108.68 us | 28,600.97 us | 58000.0000 |     - |     - | 91993400 B |
```

For those metrics, the library lets DogStatsD agent do the aggregation, so with a sample rate of 1.0, each call to
Histogram.Update will be sent to the agent.
