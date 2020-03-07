using System;
using DatadogStatsD.Transport;

namespace DatadogStatsD
{
    public class DogStatsD : IDisposable
    {
        private readonly DogStatsDConfiguration _conf;
        private readonly ITransport _transport;

        public DogStatsD(DogStatsDConfiguration conf)
        {
            _conf = conf;
            _transport = new UdpTransport(_conf.Host, _conf.Port);
        }

        public void Dispose()
        {
            _transport.Dispose();
        }
    }
}