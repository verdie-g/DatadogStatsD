# DatadogStatsD
Full featured [DogStatsD](https://docs.datadoghq.com/developers/dogstatsd) client:
- [Count](https://docs.datadoghq.com/developers/metrics/types/?tab=count#metric-types),
  [Histogram](https://docs.datadoghq.com/developers/metrics/types/?tab=count#metric-types),
  [Gauge](https://docs.datadoghq.com/developers/metrics/types/?tab=gauge#metric-types),
  [Distribution](https://docs.datadoghq.com/developers/metrics/types/?tab=distribution#metric-types),
  [Set](https://statsd.readthedocs.io/en/v3.2.1/types.html#sets)
- [Events](https://docs.datadoghq.com/events)
- [Service Checks](https://docs.datadoghq.com/developers/service_checks)
- [UDP](https://docs.datadoghq.com/developers/dogstatsd/?tab=hostagent#how-it-works) or
  [UDS](https://docs.datadoghq.com/developers/dogstatsd/unix_socket) transport
- Performance (**up to 40x faster than the official library**) - Metrics are aggregated and the submissions are batched
- Back pressure - Transport drops new metrics when it's falling behind
- [Telemetry](https://docs.datadoghq.com/developers/dogstatsd/high_throughput/?tab=go#client-side-telemetry) -
  Metrics to monitor communication between the agent and this client

## Installation
[DatadogStatsD](https://www.nuget.org/packages/DatadogStatsD) targets both .NET Standard 2.0 & 2.1.

`dotnet add package DatadogStatsD`

## Examples

```csharp
// Create a DogStatsD client with the default configuration, that is, UDP on port 8125.
await using var dogStatsD = new DogStatsD();

// Pass a DogStatsDConfiguration instance to configure the client. For example, to
// use a unix socket, a common prefix and common tags to all your metrics:
await using var dogStatsD = new DogStatsD(new DogStatsDConfiguration
{
    EndPoint = new UnixDomainSocketEndPoint("/path/to/unix.socket"),
    Namespace = "foo",
    ConstantTags = new[] { KeyValuePair.Create("service", "service_foo") },
});

// Create a COUNT metric named "requests" with the tag "environment:dev". The method
// throws if the metric name or tags are invalid (e.g. too long, invalid characters)
// to avoid using metrics that won't be accepted by the agent.
using var requests = dogStatsD.CreateCount("requests", new[] { KeyValuePair.Create("env", "dev") });
requests.Increment(); // requests++
requests.Decrement(); // requests--
// Because counters are aggregated client-side, nothing is sent here since the metric
// was incremented once then decremented once which results in zero.

// No client-side aggregation is possible for histograms. In performance sensitive
// scenario, a sample rate can be used to only send metrics a percentage of the time
// and a correction is applied server-side. Note that the library is very fast. In the
// benchmarks, Histogram.Sample takes 130 ns to execute.
var latency = dogStatsD.CreateHistogram("latency", sampleRate: 0.5);
latency.Sample(5.423);
latency.Sample(1.27);

// Gauges use a function that is periodically evaluated and send to the agent. Here,
// until you dispose the object. you will get a graph of the number of threads in
// your process.
using var threads = dogStatsD.CreateGauge("threads", () => Process.GetCurrentProcess().Threads.Count);
// You can also manually update the gauge.
threads.Update(25);

dogStatsD.RaiseEvent(AlertType.Info, title: "Bad thing happened", message: "This happened");
dogStatsD.SendServiceCheck("is_connected", CheckStatus.Ok);
```

## Benchmark

Benchmark comparing performance of this library
with [DataDog/dogstatsd-csharp-client](https://github.com/DataDog/dogstatsd-csharp-client)
and [neuecc/DatadogSharp](https://github.com/neuecc/DatadogSharp).
Sources can be found in [DatadogStatsD.Benchmark](https://github.com/verdie-g/DatadogStatsD/blob/master/DatadogStatsD.Benchmark/Program.cs).

### Count, Gauge, Set

|                          Method |         Mean |     Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------- |-------------:|----------:|----------:|-------:|------:|------:|----------:|
|          verdie-g/DatadogStatsD |     1.347 ns | 0.0160 ns | 0.0150 ns |      - |     - |     - |         - |
| Datadog/dogstatsd-csharp-client |    56.548 ns | 1.0741 ns | 1.3584 ns | 0.0039 |     - |     - |      66 B |
|             neuecc/DatadogSharp | 3,191.921 ns | 5.2765 ns | 4.6775 ns | 0.0313 |     - |     - |     599 B |

This library aggregates for 10 seconds ([DogStatsD flush interval](https://docs.datadoghq.com/developers/dogstatsd/data_aggregation/#how-is-aggregation-performed-with-the-dogstatsd-server))
counts, gauges and sets. So for 1000 increments, one packet is sent, hence the ~0 bytes allocated.

### Histogram, Distribution

|                          Method |        Mean |    Error |   StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------- |------------:|---------:|---------:|-------:|------:|------:|----------:|
|          verdie-g/DatadogStatsD |   130.18 ns | 2.545 ns | 3.126 ns |      - |     - |     - |         - |
| Datadog/dogstatsd-csharp-client |    55.34 ns | 1.081 ns | 1.287 ns | 0.0039 |     - |     - |      66 B |
|             neuecc/DatadogSharp | 3,196.62 ns | 3.358 ns | 2.804 ns | 0.0313 |     - |     - |     599 B |

For those metrics, the library lets DogStatsD agent do the aggregation, so with
a sample rate of 1.0, each call to Histogram.Update will be sent to the agent.

Even though execution times might seem similar between this library (DatadogStatsD)
and the official one (DogStatsDService), during the 130ns, the former serializes
the metric and enqueue it, ready to be sent to the agent, when the latter only
enqueues the values passed to the `DogStatsDService.Histogram` method and the
serialization is done in a dedicated thread. Also, since this library doesn't allocate
any memory, it doesn't add any pressure on the GC.
