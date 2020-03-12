using System;
using System.Threading.Tasks;

namespace DatadogStatsD.Transport
{
    internal interface ISocket : IDisposable
    {
        Task SendAsync(ArraySegment<byte> buffer);
    }
}