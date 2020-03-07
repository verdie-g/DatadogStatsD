# DatadogStatsD
High Performance [DogStatsD](https://docs.datadoghq.com/developers/dogstatsd) Client.

# Examples

```csharp
var dogStatsD = new DogStatsD(new DogStatsDConfiguration
{
    Host = "localhost",
    Port = Endpoint.8125,
});

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
| DatadogStatsD |   1000 |     643.5 us |     3.61 us |     3.37 us |          - |     - |     - |        1 B |
|  DatadogSharp |   1000 |   2,959.0 us |     8.82 us |     7.82 us |   355.4688 |     - |     - |   558883 B |
|  StatsDClient |   1000 |   3,649.3 us |    55.58 us |    51.99 us |   578.1250 |     - |     - |   912003 B |
| DatadogStatsD |  10000 |   6,404.3 us |    25.08 us |    22.23 us |          - |     - |     - |          - |
|  DatadogSharp |  10000 |  29,899.1 us |    65.44 us |    51.09 us |  3562.5000 |     - |     - |  5598888 B |
|  StatsDClient |  10000 |  35,761.9 us |   220.41 us |   184.06 us |  5818.1818 |     - |     - |  9192000 B |
| DatadogStatsD | 100000 |  64,551.2 us | 1,243.10 us | 1,101.98 us |          - |     - |     - |          - |
|  DatadogSharp | 100000 | 287,262.7 us | 1,609.85 us | 1,344.30 us | 35000.0000 |     - |     - | 55998880 B |
|  StatsDClient | 100000 | 357,629.9 us | 2,680.93 us | 2,507.75 us | 58000.0000 |     - |     - | 91994736 B |
```

Benchmark source can be found in
[/DatadogStatsD.Benchmark](https://github.com/verdie-g/DatadogStatsD/blob/master/DatadogStatsD.Benchmark/Program.cs).
