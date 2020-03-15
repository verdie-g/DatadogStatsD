# DatadogStatsD
Full featured [DogStatsD](https://docs.datadoghq.com/developers/dogstatsd) client:
- Count, Histogram, Gauge, Distribution, Set
- **UDP** or **UDS** transport
- **Performance** - Metrics are aggregated and the submissions are batched
- **Back pressure** - Transport drops new metrics when it's falling behind
- [**Telemetry**](https://docs.datadoghq.com/developers/dogstatsd/high_throughput/?tab=go#client-side-telemetry) -
  Metrics are sent to troubleshoot dropped metrics

# Examples

```csharp
using var dogStatsD = new DogStatsD();

var exampleMetric = dogStatsD.CreateCount("example_metric", 1.0, new[] { "environment:dev" });
exampleMetric.Increment();
exampleMetric.Decrement();

var exampleMetric2 = dogStatsD.CreateHistogram("example_metric2");
exampleMetric2.Update(5.423);
exampleMetric2.Update(1.27);
```

# Benchmark

Benchmark comparing performance of this library with [neuecc/DatadogSharp](https://github.com/neuecc/DatadogSharp)
and [DataDog/dogstatsd-csharp-client](https://github.com/DataDog/dogstatsd-csharp-client). Sources can be found in
[DatadogStatsD.Benchmark](https://github.com/verdie-g/DatadogStatsD/blob/master/DatadogStatsD.Benchmark/Program.cs).

# Count & Gauge

```
|        Method |     Op |           Mean |         Error |        StdDev |      Gen 0 | Gen 1 | Gen 2 |  Allocated |
|-------------- |------- |---------------:|--------------:|--------------:|-----------:|------:|------:|-----------:|
| DatadogStatsD |   1000 |       8.053 us |     0.0139 us |     0.0123 us |          - |     - |     - |          - |
|  DatadogSharp |   1000 |   2,897.898 us |    19.9191 us |    18.6323 us |   355.4688 |     - |     - |   558890 B |
|  StatsDClient |   1000 |   3,573.429 us |    26.6741 us |    22.2740 us |   578.1250 |     - |     - |   912030 B |
| DatadogStatsD |  10000 |      80.623 us |     0.1401 us |     0.1310 us |          - |     - |     - |        1 B |
|  DatadogSharp |  10000 |  28,500.629 us |   175.8789 us |   164.5173 us |  3562.5000 |     - |     - |  5598889 B |
|  StatsDClient |  10000 |  39,972.683 us |   533.0033 us |   498.5716 us |  5846.1538 |     - |     - |  9192022 B |
| DatadogStatsD | 100000 |     806.619 us |     1.5047 us |     1.3339 us |          - |     - |     - |        4 B |
|  DatadogSharp | 100000 | 299,220.753 us | 1,611.8824 us | 1,345.9948 us | 35500.0000 |     - |     - | 55999024 B |
|  StatsDClient | 100000 | 371,823.942 us | 2,313.3733 us | 2,163.9308 us | 58000.0000 |     - |     - | 91992000 B |
```

This library aggregates for 10 seconds ([DogStatsD flush interval](https://docs.datadoghq.com/developers/dogstatsd/data_aggregation/#how-is-aggregation-performed-with-the-dogstatsd-server))
counts and gauges, so for 10000 increments, one packet is sent, hence the ~0 bytes allocated.

# Histogram, Set, Distribution

```
|        Method |     Op |       Mean |     Error |     StdDev |      Gen 0 | Gen 1 | Gen 2 |   Allocated |
|-------------- |------- |-----------:|----------:|-----------:|-----------:|------:|------:|------------:|
| DatadogStatsD |   1000 |   1.396 ms | 0.0279 ms |  0.0700 ms |    82.0313 |     - |     - |   126.19 KB |
|  DatadogSharp |   1000 |   3.782 ms | 0.0706 ms |  0.0661 ms |   355.4688 |     - |     - |   545.79 KB |
|  StatsDClient |   1000 |   4.897 ms | 0.0829 ms |  0.0692 ms |   578.1250 |     - |     - |   890.64 KB |
| DatadogStatsD |  10000 |  14.555 ms | 0.2901 ms |  0.7278 ms |   984.3750 |     - |     - |  1526.26 KB |
|  DatadogSharp |  10000 |  38.852 ms | 0.7309 ms |  0.7179 ms |  3500.0000 |     - |     - |  5467.66 KB |
|  StatsDClient |  10000 |  50.762 ms | 0.5816 ms |  0.5156 ms |  5800.0000 |     - |     - |   8976.6 KB |
| DatadogStatsD | 100000 | 141.765 ms | 3.0557 ms |  9.0097 ms |  7750.0000 |     - |     - | 12078.77 KB |
|  DatadogSharp | 100000 | 395.236 ms | 3.4644 ms |  3.0711 ms | 35000.0000 |     - |     - |  54686.9 KB |
|  StatsDClient | 100000 | 495.184 ms | 9.6524 ms | 11.4905 ms | 58000.0000 |     - |     - | 89836.03 KB |
```

For those metrics, the library lets DogStatsD agent do the aggregation, so with a sample rate of 1.0, each call to
Histogram.Update will be sent to the agent. The allocations are mostly due to [BlockingCollection.TryTake](https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.blockingcollection-1.trytake)
inefficiencies (see [#1](https://github.com/verdie-g/DatadogStatsD/issues/1)).
