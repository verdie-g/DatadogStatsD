using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
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
        private readonly int _maxQueueSize;
        private readonly BlockingCollection<ArraySegment<byte>> _queue;
        private readonly CancellationTokenSource _cancellationTokenSource;

        // eventually consistent size of _queue
        private int _queueSize;

        public NonBlockingBufferedTransport(ISocket socket, int maxBufferingSize, TimeSpan maxBufferingTime,
            int maxQueueSize)
            : this(socket, maxBufferingSize, maxBufferingTime, maxQueueSize, new CancellationTokenSource())
        {
        }

        public event Action<int> OnPacketSent = size => {};
        public event Action<int, bool> OnPacketDropped = (size, queue) => {};

        // internal for testing
        internal NonBlockingBufferedTransport(ISocket socket, int maxBufferingSize, TimeSpan maxBufferingTime,
            int maxQueueSize, CancellationTokenSource cancellationTokenSource)
        {
            _socket = socket;
            _maxBufferingSize = maxBufferingSize;
            _maxBufferingTime = maxBufferingTime;
            _maxQueueSize = maxQueueSize;
            _queue = new BlockingCollection<ArraySegment<byte>>(new ConcurrentQueue<ArraySegment<byte>>());
            _cancellationTokenSource = cancellationTokenSource;

            Task.Run(SendBuffers, _cancellationTokenSource.Token);
        }

        public void Send(ArraySegment<byte> buffer)
        {
            if (_queueSize >= _maxQueueSize)
            {
                OnPacketDropped(buffer.Count, true);
                ArrayPool<byte>.Shared.Return(buffer.Array);
                return;
            }

            _queue.Add(buffer);
            Interlocked.Increment(ref _queueSize);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _socket.Dispose();
        }

        private async Task SendBuffers()
        {
            var bufferingCtx = new BufferingContext(_maxBufferingSize, _maxBufferingTime);

            while (true)
            {
                bool taken = _queue.TryTake(out var buffer,
                    (int)bufferingCtx.RemainingBufferingTime.TotalMilliseconds,
                    _cancellationTokenSource.Token);

                if (!taken) // timeout reached
                {
                    if (!bufferingCtx.Empty)
                    {
                        await Flush(bufferingCtx);
                    }
                    else
                    {
                        bufferingCtx.Reset();
                    }

                    continue; // buffer is outdated so don't go any further
                }

                Interlocked.Decrement(ref _queueSize);

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

            public BufferingContext(int maxBufferingSize, TimeSpan maxBufferingTime)
            {
                _maxBufferingSize = maxBufferingSize;
                _maxBufferingTime = maxBufferingTime;
                _buffer = new byte[maxBufferingSize + 1]; // +1 for extra '\n'
                _size = 0;
                _stopwatch = new Stopwatch();
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