using System;

namespace DatadogStatsD.Transport
{
    internal interface ISocket : IDisposable
    {
        void Send(ArraySegment<byte> buffer);
    }
}