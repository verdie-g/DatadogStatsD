using NUnit.Framework;

namespace DatadogStatsD.Test
{
    public class SamplingTest
    {
        [Test]
        public void SampleMaxRateAlwaysReturnTrue()
        {
            for (int i = 0; i < 1000; i += 1)
            {
                Assert.True(Sampling.Sample(1.0));
            }
        }

        [Test]
        public void SamplingIsWellDistributed()
        {
            const int samples = 10_000;
            double sampleRate = 0.5;

            int trues = 0;
            for (int i = 0; i < samples; i += 1)
            {
                trues += Sampling.Sample(sampleRate) ? 1 : 0;
            }

            double ratio = (double) trues / samples;
            Assert.AreEqual(sampleRate, ratio, 0.1);
        }
    }
}