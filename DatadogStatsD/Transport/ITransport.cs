using System;

namespace DatadogStatsD.Transport
{
    internal interface ITransport : IDisposable
    {
        void Send(ArraySegment<byte> buffer);
    }
}