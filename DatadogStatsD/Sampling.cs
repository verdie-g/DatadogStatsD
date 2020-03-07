using System;
using System.Security.Cryptography;
using System.Threading;

namespace DatadogStatsD
{
    /// <remarks>Documentation: https://docs.datadoghq.com/developers/metrics/dogstatsd_metrics_submission?tab=net#sample-rates</remarks>
    internal static class Sampling
    {
        // https://devblogs.microsoft.com/pfxteam/getting-random-numbers-in-a-thread-safe-way/
        private static readonly RNGCryptoServiceProvider StrongRng = new RNGCryptoServiceProvider();
        private static readonly ThreadLocal<Random> Random = new ThreadLocal<Random>(() =>
        {
            byte[] buffer = new byte[4];
            StrongRng.GetBytes(buffer);
            return new Random(BitConverter.ToInt32(buffer, 0));
        });

        public static bool Sample(double sampleRate)
        {
            return sampleRate == 1.0 || Random.Value.NextDouble() < sampleRate;
        }
    }
}