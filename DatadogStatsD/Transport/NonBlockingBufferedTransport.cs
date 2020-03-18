using System;
using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DatadogStatsD.Transport
{
    /// <summary>
    /// Wraps an <see cref="ISocket"/> to buffer messages sent and join them with a newline.
    /// </summary>
    internal class NonBlockingBufferedTransport : ITransport
    {
        private readonly ISocket _socket;
        private readonly int _maxBufferingSize;
        private readonly TimeSpan _maxBufferingTime;
        private readonly Channel<ArraySegment<byte>> _chan;
        private readonly Task _sendBuffersTask;
        private readonly CancellationTokenSource _sendBuffersCancellation;

        public NonBlockingBufferedTransport(ISocket socket, int maxBufferingSize, TimeSpan maxBufferingTime,
            int maxQueueSize)
            : this(socket, maxBufferingSize, maxBufferingTime, maxQueueSize, new CancellationTokenSource())
        {
        }

        public event Action<int> OnPacketSent = size => {};
        public event Action<int, bool> OnPacketDropped = (size, queue) => {};

        // internal for testing
        internal NonBlockingBufferedTransport(ISocket socket, int maxBufferingSize, TimeSpan maxBufferingTime,
            int maxQueueSize, CancellationTokenSource sendBuffersCancellation)
        {
            _socket = socket;
            _maxBufferingSize = maxBufferingSize;
            _maxBufferingTime = maxBufferingTime;
            _chan = Channel.CreateBounded<ArraySegment<byte>>(new BoundedChannelOptions(maxQueueSize)
            {
                SingleReader = true,
                SingleWriter = false,
            });
            _sendBuffersCancellation = sendBuffersCancellation;
            _sendBuffersTask = Task.Run(SendBuffers, _sendBuffersCancellation.Token);
        }

        public void Send(ArraySegment<byte> buffer)
        {
            if (!_sendBuffersCancellation.IsCancellationRequested && _chan.Writer.TryWrite(buffer))
            {
                return;
            }

            OnPacketDropped(buffer.Count, true);
            ArrayPool<byte>.Shared.Return(buffer.Array);
        }

        public void Dispose()
        {
            _sendBuffersCancellation.Cancel();
            try
            {
                _sendBuffersTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }
            _sendBuffersTask.Dispose();
            _sendBuffersCancellation.Dispose();
            _socket.Dispose();
        }

        private async Task SendBuffers()
        {
            var bufferingCtx = new BufferingContext(_maxBufferingSize, _maxBufferingTime);

            while (true)
            {
                if (bufferingCtx.BufferingCancellation.IsCancellationRequested)
                {
                    bufferingCtx.BufferingCancellation.Dispose();
                    bufferingCtx.BufferingCancellation = CancellationTokenSource.CreateLinkedTokenSource(_sendBuffersCancellation.Token);
                }

                // the try block was not extract in its own method to avoid the cost of a new async state machine
                ArraySegment<byte> buffer;
                try
                {
                    bufferingCtx.BufferingCancellation.CancelAfter(bufferingCtx.RemainingBufferingTime);
                    buffer = await _chan.Reader.ReadAsync(bufferingCtx.BufferingCancellation.Token);

                    // make sure it doesn't get cancelled so it can be reused in the next iteration
                    // Timeout.Infinite won't work because it would delete the underlying timer
                    bufferingCtx.BufferingCancellation.CancelAfter(int.MaxValue);
                }
                catch (OperationCanceledException) when (!_sendBuffersCancellation.IsCancellationRequested) // timeout reached
                {
                    if (!bufferingCtx.Empty)
                    {
                        await Flush(bufferingCtx);
                    }
                    else
                    {
                        bufferingCtx.Reset();
                    }

                    continue;
                }

                if (buffer.Count > _maxBufferingSize)
                {
                    await SendBuffer(buffer);
                    ArrayPool<byte>.Shared.Return(buffer.Array);
                    continue;
                }

                if (!bufferingCtx.Fits(buffer))
                {
                    await Flush(bufferingCtx);
                }

                bufferingCtx.Append(buffer);
                ArrayPool<byte>.Shared.Return(buffer.Array);
            }
        }

        private async Task Flush(BufferingContext bufferingCtx)
        {
            await SendBuffer(bufferingCtx.Segment);
            bufferingCtx.Reset();
        }

        private async Task SendBuffer(ArraySegment<byte> buffer)
        {
            try
            {
                await _socket.SendAsync(buffer);
                OnPacketSent(buffer.Count);
            }
            catch // an error occured. Try resuming after 1 second
            {
                OnPacketDropped(buffer.Count, false);
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private class BufferingContext
        {
            private readonly int _maxBufferingSize;
            private readonly TimeSpan _maxBufferingTime;
            private readonly byte[] _buffer;
            private readonly Stopwatch _stopwatch;
            private int _size;

            public bool Empty => _size == 0;
            public TimeSpan RemainingBufferingTime => TimeSpan.FromMilliseconds(Math.Max(0, _maxBufferingTime.TotalMilliseconds - _stopwatch.Elapsed.TotalMilliseconds));
            public ArraySegment<byte> Segment => new ArraySegment<byte>(_buffer, 0, _size - 1); // -1 for extra '\n'
            public CancellationTokenSource BufferingCancellation { get; set; } = new CancellationTokenSource();

            public BufferingContext(int maxBufferingSize, TimeSpan maxBufferingTime)
            {
                _maxBufferingSize = maxBufferingSize;
                _maxBufferingTime = maxBufferingTime;
                _buffer = new byte[maxBufferingSize + 1]; // +1 for extra '\n'
                _stopwatch = new Stopwatch();
                _size = 0;

                _stopwatch.Start();
            }

            public void Append(ArraySegment<byte> buffer)
            {
                Array.Copy(buffer.Array, 0, _buffer, _size, buffer.Count);
                _size += buffer.Count;

                if (_size <= _maxBufferingSize)
                {
                    _buffer[_size] = (byte)'\n';
                    _size += 1;
                }
            }

            public bool Fits(ArraySegment<byte> buffer)
            {
                return _size + buffer.Count <= _maxBufferingSize;
            }

            public void Reset()
            {
                _size = 0;
                _stopwatch.Restart();
            }
        }
    }
}