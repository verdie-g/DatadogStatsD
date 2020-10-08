using System;

namespace DatadogStatsD.Transport
{
    internal interface ITransport
#if NETSTANDARD2_0
        : IDisposable
#else
        : IAsyncDisposable
#endif
    {
        void Send(ArraySegment<byte> buffer);
        event Action<int> OnPacketSent;
        event Action<int, bool> OnPacketDropped;
    }
}
