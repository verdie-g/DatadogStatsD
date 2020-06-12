using System.Timers;
using DatadogStatsD.Ticking;

namespace DatadogStatsD.Test
{
    public class ManualTimer : ITimer
    {
        public event ElapsedEventHandler Elapsed = (_, __) => {};
        public void TriggerElapsed() => Elapsed(null, null);
        public void Dispose() {}
    }
}