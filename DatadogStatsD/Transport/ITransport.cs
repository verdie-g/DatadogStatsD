using System;

namespace DatadogStatsD.Transport
{
    internal interface ITransport : IAsyncDisposable
    {
        void Send(ArraySegment<byte> buffer);
        event Action<int> OnPacketSent;
        event Action<int, bool> OnPacketDropped;
    }
}