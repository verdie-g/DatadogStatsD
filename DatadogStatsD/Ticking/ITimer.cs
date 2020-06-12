using System;
using System.Timers;

namespace DatadogStatsD.Ticking
{
    internal interface ITimer : IDisposable
    {
        event ElapsedEventHandler Elapsed;
    }
}