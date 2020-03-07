# DatadogStatsD
High Performance [DogStatsD](https://docs.datadoghq.com/developers/dogstatsd) Client.

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
|        Method |     Op |       Mean |     Error |    StdDev |      Median |      Gen 0 | Gen 1 | Gen 2 |   Allocated |
|-------------- |------- |-----------:|----------:|----------:|------------:|-----------:|------:|------:|------------:|
| DatadogStatsD |   1000 |   1.005 ms | 0.0206 ms | 0.0578 ms |   0.9816 ms |    82.0313 |     - |     - |   126.41 KB |
|  DatadogSharp |   1000 |   2.803 ms | 0.0196 ms | 0.0183 ms |   2.7963 ms |   355.4688 |     - |     - |   545.78 KB |
|  StatsDClient |   1000 |   3.564 ms | 0.0265 ms | 0.0248 ms |   3.5581 ms |   578.1250 |     - |     - |   890.63 KB |
| DatadogStatsD |  10000 |   9.387 ms | 0.1623 ms | 0.1439 ms |   9.3740 ms |   828.1250 |     - |     - |  1282.08 KB |
|  DatadogSharp |  10000 |  28.837 ms | 0.3450 ms | 0.3227 ms |  28.8204 ms |  3562.5000 |     - |     - |  5467.66 KB |
|  StatsDClient |  10000 |  36.194 ms | 0.2229 ms | 0.2085 ms |  36.1422 ms |  5857.1429 |     - |     - |  8976.56 KB |
| DatadogStatsD | 100000 |  91.979 ms | 1.8285 ms | 1.7103 ms |  92.8387 ms |  8400.0000 |     - |     - | 13043.32 KB |
|  DatadogSharp | 100000 | 281.143 ms | 1.2151 ms | 1.0772 ms | 280.9410 ms | 35500.0000 |     - |     - | 54686.41 KB |
|  StatsDClient | 100000 | 365.255 ms | 3.2131 ms | 3.0055 ms | 364.8525 ms | 58000.0000 |     - |     - | 89835.94 KB |
```

Benchmark source can be found in
[/DatadogStatsD.Benchmark](https://github.com/verdie-g/DatadogStatsD/blob/master/DatadogStatsD.Benchmark/Program.cs).
