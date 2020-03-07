using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DatadogStatsD.Transport
{
    internal class UdpTransport : ITransport
    {
        // https://stackoverflow.com/a/35697810/5407910
        private const int SafeUdpPayloadSize = 508;
        private const int SafeUdpPayloadWithNewLineSize = 509;

        private readonly Socket _socket;
        private readonly byte[] _sendBuffer;
        private readonly BlockingCollection<ArraySegment<byte>> _buffers;

        public UdpTransport(string host, int port)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _sendBuffer =  new byte[SafeUdpPayloadWithNewLineSize];
            _buffers = new BlockingCollection<ArraySegment<byte>>(new ConcurrentQueue<ArraySegment<byte>>());

            var ipAddress = Dns.GetHostEntry(host).AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork);
            _socket.Connect(ipAddress, port);

            // send two packets to fail fast if there is no listening socket
            _socket.Send(Array.Empty<byte>()); // the first one get the potential ICMP error
            _socket.Send(Array.Empty<byte>()); // the second throws if there was an error
            // passing this test doesn't mean a socket is listening https://serverfault.com/a/416269

            Task.Factory.StartNew(SendBuffers, TaskCreationOptions.LongRunning);
        }

        public void Send(ArraySegment<byte> buffer)
        {
            _buffers.Add(buffer);
        }

        public void Dispose()
        {
            _socket.Dispose(); // makes SendBuffers to throw
        }

        // internal for testing
        internal void SendBuffers()
        {
            int sendBufferSize = 0;
            foreach (var buffer in _buffers.GetConsumingEnumerable())
            {
                if (sendBufferSize + buffer.Count > SafeUdpPayloadSize)
                {
                    // send sendBufferSize - 1 to remove the extra new line
                    _socket.Send(new ArraySegment<byte>(_sendBuffer, 0, sendBufferSize - 1), SocketFlags.None);
                    sendBufferSize = 0;
                }

                Array.Copy(buffer.Array, 0, _sendBuffer, sendBufferSize, buffer.Count);
                sendBufferSize += buffer.Count;

                if (sendBufferSize + 1 <= SafeUdpPayloadWithNewLineSize)
                {
                    _sendBuffer[sendBufferSize] = (byte)'\n';
                    sendBufferSize += 1;
                }

                ArrayPool<byte>.Shared.Return(buffer.Array);
            }
        }
    }
}