# DatadogStatsD
Full featured [DogStatsD](https://docs.datadoghq.com/developers/dogstatsd) client:
- Count, Histogram, Timer
- [Telemetry](https://docs.datadoghq.com/developers/dogstatsd/high_throughput/?tab=go#client-side-telemetry)
- UDP or UDS transport
- Performance (see benchmark)
- Back pressure

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
and [DataDog/dogstatsd-csharp-client](https://github.com/DataDog/dogstatsd-csharp-client).

```
|        Method |     Op |       Mean |     Error |    StdDev |      Gen 0 | Gen 1 | Gen 2 |   Allocated |
|-------------- |------- |-----------:|----------:|----------:|-----------:|------:|------:|------------:|
| DatadogStatsD |   1000 |   1.028 ms | 0.0205 ms | 0.0236 ms |   117.1875 |     - |     - |    180.9 KB |
|  DatadogSharp |   1000 |   2.891 ms | 0.0156 ms | 0.0146 ms |   355.4688 |     - |     - |   545.78 KB |
|  StatsDClient |   1000 |   3.701 ms | 0.0366 ms | 0.0342 ms |   578.1250 |     - |     - |   890.64 KB |
| DatadogStatsD |  10000 |  10.221 ms | 0.1694 ms | 0.2483 ms |  1140.6250 |     - |     - |  1750.96 KB |
|  DatadogSharp |  10000 |  31.607 ms | 0.1655 ms | 0.1467 ms |  3562.5000 |     - |     - |  5467.67 KB |
|  StatsDClient |  10000 |  39.319 ms | 0.3287 ms | 0.3075 ms |  5846.1538 |     - |     - |  8976.56 KB |
| DatadogStatsD | 100000 | 101.220 ms | 2.2907 ms | 2.0306 ms |  8000.0000 |     - |     - | 12405.45 KB |
|  DatadogSharp | 100000 | 289.838 ms | 3.1539 ms | 2.9501 ms | 35000.0000 |     - |     - | 54686.41 KB |
|  StatsDClient | 100000 | 382.944 ms | 3.7201 ms | 3.4798 ms | 58000.0000 |     - |     - | 89835.94 KB |
```

Benchmark sources can be found in
[/DatadogStatsD.Benchmark](https://github.com/verdie-g/DatadogStatsD/blob/master/DatadogStatsD.Benchmark/Program.cs).
