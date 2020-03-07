using System;

namespace DatadogStatsD.Transport
{
    public interface ITransport : IDisposable
    {
        void Send(ArraySegment<byte> buffer);
    }
}