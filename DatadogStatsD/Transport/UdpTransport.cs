using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DatadogStatsD.Transport
{
    internal class UdpTransport : ITransport
    {
        // https://stackoverflow.com/a/35697810/5407910
        private const int MaxBatchingSize = 508;
        private static readonly TimeSpan MaxBatchingTime = TimeSpan.FromSeconds(2);

        private readonly ISocket _socket;
        private readonly BlockingCollection<ArraySegment<byte>> _buffers;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _sendBuffersTask;

        public UdpTransport(string host, int port)
        {
            _socket = new UdpSocket();
            _buffers = new BlockingCollection<ArraySegment<byte>>(new ConcurrentQueue<ArraySegment<byte>>());
            _cancellationTokenSource = new CancellationTokenSource();

            var ipAddress = Dns.GetHostEntry(host).AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork);
            _socket.Connect(ipAddress, port);

            // send two packets to fail fast if there is no listening socket
            _socket.Send(new ArraySegment<byte>(Array.Empty<byte>()), SocketFlags.None); // the first one get the potential ICMP error
            _socket.Send(new ArraySegment<byte>(Array.Empty<byte>()), SocketFlags.None); // the second throws if there was an error
            // passing this test doesn't mean a socket is listening https://serverfault.com/a/416269

            _sendBuffersTask = Task.Factory.StartNew(
                () => SendBuffers(_socket, _buffers, _cancellationTokenSource.Token),
                _cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public void Send(ArraySegment<byte> buffer)
        {
            _buffers.Add(buffer);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _sendBuffersTask.Dispose();
            _socket.Dispose();
        }

        // internal for testing
        internal static void SendBuffers(ISocket socket, BlockingCollection<ArraySegment<byte>> buffers, CancellationToken cancellationToken)
        {
            var sendBuffer =  new byte[MaxBatchingSize + 1]; // +1 for extra '\n'
            int sendBufferSize = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool taken = buffers.TryTake(out var buffer, (int)MaxBatchingTime.TotalMilliseconds, cancellationToken);
                if (!taken || sendBufferSize + buffer.Count > MaxBatchingSize)
                {
                    if (sendBufferSize > 0) // in case !taken and send buffer is empty
                    {
                        // -1 for extra '\n'
                        socket.Send(new ArraySegment<byte>(sendBuffer, 0, sendBufferSize - 1), SocketFlags.None);
                        sendBufferSize = 0;
                    }

                    if (!taken) // buffer is outdated so don't try to go any further
                    {
                        continue;
                    }
                }

                Array.Copy(buffer.Array, 0, sendBuffer, sendBufferSize, buffer.Count);
                sendBufferSize += buffer.Count;

                if (sendBufferSize <= MaxBatchingSize)
                {
                    sendBuffer[sendBufferSize] = (byte)'\n';
                    sendBufferSize += 1;
                }

                ArrayPool<byte>.Shared.Return(buffer.Array);
            }
        }
    }
}