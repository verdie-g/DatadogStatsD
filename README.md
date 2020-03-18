# DatadogStatsD
Full featured [DogStatsD](https://docs.datadoghq.com/developers/dogstatsd) client:
- Count, Histogram, Gauge, Distribution, Set, Event
- **UDP** or **UDS** transport
- **Performance** - Metrics are aggregated and the submissions are batched
- **Back pressure** - Transport drops new metrics when it's falling behind
- [**Telemetry**](https://docs.datadoghq.com/developers/dogstatsd/high_throughput/?tab=go#client-side-telemetry) -
  Metrics are sent to troubleshoot dropped metrics

# Examples

```csharp
using var dogStatsD = new DogStatsD();

using var exampleMetric = dogStatsD.CreateCount("example_metric", 1.0, new[] { "environment:dev" });
exampleMetric.Increment();
exampleMetric.Decrement();

using var exampleMetric2 = dogStatsD.CreateHistogram("example_metric2");
exampleMetric2.Update(5.423);
exampleMetric2.Update(1.27);

dogStasD.RaiseEvent(AlertType.Info, "Bad thing happened", "This happened");
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
|        Method |     Op |       Mean |     Error |    StdDev |     Median |      Gen 0 | Gen 1 | Gen 2 |   Allocated |
|-------------- |------- |-----------:|----------:|----------:|-----------:|-----------:|------:|------:|------------:|
| DatadogStatsD |   1000 |   1.444 ms | 0.0284 ms | 0.0540 ms |   1.447 ms |    23.4375 |     - |     - |    35.38 KB |
|  DatadogSharp |   1000 |   2.851 ms | 0.0162 ms | 0.0143 ms |   2.851 ms |   355.4688 |     - |     - |   545.79 KB |
|  StatsDClient |   1000 |   3.691 ms | 0.0247 ms | 0.0231 ms |   3.691 ms |   578.1250 |     - |     - |   890.64 KB |
| DatadogStatsD |  10000 |  14.238 ms | 0.2806 ms | 0.6502 ms |  14.373 ms |   203.1250 |     - |     - |    310.2 KB |
|  DatadogSharp |  10000 |  27.880 ms | 0.1069 ms | 0.0947 ms |  27.896 ms |  3562.5000 |     - |     - |  5467.69 KB |
|  StatsDClient |  10000 |  37.435 ms | 0.3046 ms | 0.2700 ms |  37.400 ms |  5857.1429 |     - |     - |  8976.56 KB |
| DatadogStatsD | 100000 | 144.595 ms | 2.8810 ms | 8.1729 ms | 147.057 ms |  2000.0000 |     - |     - |  3232.81 KB |
|  DatadogSharp | 100000 | 314.129 ms | 3.0197 ms | 2.8246 ms | 313.519 ms | 35000.0000 |     - |     - | 54686.41 KB |
|  StatsDClient | 100000 | 360.540 ms | 2.7272 ms | 2.5510 ms | 360.405 ms | 58000.0000 |     - |     - | 89835.94 KB |
```

For those metrics, the library lets DogStatsD agent do the aggregation, so with a sample rate of 1.0, each call to
Histogram.Update will be sent to the agent.
