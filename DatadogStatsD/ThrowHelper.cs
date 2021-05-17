using System;

namespace DatadogStatsD
{
    internal static class ThrowHelper
    {
        public static void ThrowIfNaN(double value)
        {
            if (double.IsNaN(value))
            {
                throw new ArgumentException("Function does not accept floating point Not-a-Number values");
            }
        }
    }
}
