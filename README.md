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
|        Method |     Op |         Mean |       Error |      StdDev |      Gen 0 | Gen 1 | Gen 2 |   Allocated |
|-------------- |------- |-------------:|------------:|------------:|-----------:|------:|------:|------------:|
| DatadogStatsD |   1000 |     947.4 us |    17.25 us |    14.41 us |    82.0313 |     - |     - |   126.19 KB |
|  DatadogSharp |   1000 |   2,877.0 us |    19.73 us |    18.46 us |   355.4688 |     - |     - |   545.79 KB |
|  StatsDClient |   1000 |   4,114.0 us |    54.89 us |    51.35 us |   578.1250 |     - |     - |   890.63 KB |
| DatadogStatsD |  10000 |   9,317.6 us |    56.12 us |    49.75 us |   828.1250 |     - |     - |   1281.8 KB |
|  DatadogSharp |  10000 |  29,524.1 us |   167.88 us |   148.82 us |  3562.5000 |     - |     - |  5467.66 KB |
|  StatsDClient |  10000 |  39,811.0 us |   656.84 us |   548.49 us |  5846.1538 |     - |     - |  8976.56 KB |
| DatadogStatsD | 100000 |  95,300.2 us | 1,963.51 us | 2,182.43 us |  8500.0000 |     - |     - | 13073.19 KB |
|  DatadogSharp | 100000 | 306,468.1 us | 1,771.37 us | 1,570.28 us | 35000.0000 |     - |     - | 54686.41 KB |
|  StatsDClient | 100000 | 362,094.1 us | 2,985.94 us | 2,793.05 us | 58000.0000 |     - |     - | 89835.94 KB |
```

Benchmark source can be found in
[/DatadogStatsD.Benchmark](https://github.com/verdie-g/DatadogStatsD/blob/master/DatadogStatsD.Benchmark/Program.cs).
