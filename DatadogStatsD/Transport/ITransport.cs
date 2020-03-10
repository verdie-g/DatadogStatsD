using System;

namespace DatadogStatsD.Transport
{
    public interface ITransport : IDisposable
    {
        public void Send(ArraySegment<byte> buffer);
    }
}