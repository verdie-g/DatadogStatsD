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
|        Method |     Op |         Mean |       Error |      StdDev |      Gen 0 | Gen 1 | Gen 2 |  Allocated |
|-------------- |------- |-------------:|------------:|------------:|-----------:|------:|------:|-----------:|
| DatadogStatsD |   1000 |     940.2 us |    16.07 us |    15.03 us |          - |     - |     - |      247 B |
|  DatadogSharp |   1000 |   2,833.4 us |    14.24 us |    13.32 us |   355.4688 |     - |     - |   558888 B |
|  StatsDClient |   1000 |   3,836.0 us |    30.79 us |    28.80 us |   578.1250 |     - |     - |   912003 B |
| DatadogStatsD |  10000 |   9,262.4 us |   183.28 us |   162.48 us |          - |     - |     - |      903 B |
|  DatadogSharp |  10000 |  27,975.8 us |   125.71 us |   111.44 us |  3562.5000 |     - |     - |  5598880 B |
|  StatsDClient |  10000 |  37,297.5 us |   321.04 us |   300.30 us |  5857.1429 |     - |     - |  9192000 B |
| DatadogStatsD | 100000 |  92,738.0 us | 1,740.60 us | 2,137.61 us |          - |     - |     - |     3535 B |
|  DatadogSharp | 100000 | 282,354.0 us |   966.75 us |   857.00 us | 35000.0000 |     - |     - | 55998880 B |
|  StatsDClient | 100000 | 377,613.8 us | 1,673.49 us | 1,565.38 us | 58000.0000 |     - |     - | 91992000 B |
```

Benchmark source can be found in
[/DatadogStatsD.Benchmark](https://github.com/verdie-g/DatadogStatsD/blob/master/DatadogStatsD.Benchmark/Program.cs).
