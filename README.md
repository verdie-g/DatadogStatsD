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
| DatadogStatsD |   1000 |   1.040 ms | 0.0201 ms | 0.0215 ms |   103.5156 |     - |     - |   160.88 KB |
|  DatadogSharp |   1000 |   2.971 ms | 0.0183 ms | 0.0163 ms |   355.4688 |     - |     - |   545.79 KB |
|  StatsDClient |   1000 |   3.924 ms | 0.0274 ms | 0.0243 ms |   578.1250 |     - |     - |   890.63 KB |
| DatadogStatsD |  10000 |  10.415 ms | 0.2049 ms | 0.3190 ms |  1312.5000 |     - |     - |  2033.09 KB |
|  DatadogSharp |  10000 |  28.727 ms | 0.2411 ms | 0.2137 ms |  3562.5000 |     - |     - |  5467.66 KB |
|  StatsDClient |  10000 |  39.407 ms | 0.2569 ms | 0.2277 ms |  5846.1538 |     - |     - |  8976.58 KB |
| DatadogStatsD | 100000 | 104.598 ms | 2.0777 ms | 2.6277 ms | 12400.0000 |     - |     - |  19032.2 KB |
|  DatadogSharp | 100000 | 299.014 ms | 1.6739 ms | 1.4838 ms | 35000.0000 |     - |     - | 54686.41 KB |
|  StatsDClient | 100000 | 390.388 ms | 5.0534 ms | 4.7270 ms | 58000.0000 |     - |     - | 89835.94 KB |
```

Benchmark sources can be found in
[/DatadogStatsD.Benchmark](https://github.com/verdie-g/DatadogStatsD/blob/master/DatadogStatsD.Benchmark/Program.cs).
