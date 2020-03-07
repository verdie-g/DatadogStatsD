using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DatadogStatsD.Metrics;

namespace DatadogStatsD
{
    internal static class DogStatsDSerializer
    {
        private const int MetricNameMaxSize = 200;
        private const int TagMaxSize = 200;
        private const int DecimalPrecision = 6;
        private static readonly byte[] MaxSampleRateBytes = SerializeSampleRate(1.0);

        /// <summary>
        /// Serialize a metric with its value from pre-serialized parts.
        /// </summary>
        /// <param name="metricName">Serialized metric name built with <see cref="SerializeMetricName"/>.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="type">Serialized metric type built with <see cref="SerializeMetricType"/>.</param>
        /// <param name="sampleRate">Serialized sample rate built with <see cref="SerializeSampleRate"/>. Can be null.</param>
        /// <param name="tags">Serialized tags built with <see cref="SerializeTags"/>.</param>
        /// <returns>A segment of a byte array containing the serialized metric. The array was loaned from
        /// <see cref="ArrayPool{T}.Shared"/> and must be returned once it's not used.</returns>
        /// <remarks>Documentation: https://docs.datadoghq.com/developers/dogstatsd/datagram_shell?tab=metrics</remarks>
        public static ArraySegment<byte> SerializeMetric(byte[] metricName, double value, byte[] type, byte[]? sampleRate, byte[] tags)
        {
            int size = SerializedMetricSize(metricName, value, type, sampleRate, tags);
            byte[] metricBytes = ArrayPool<byte>.Shared.Rent(size);
            int index = 0;

            // <METRIC_NAME>:<VALUE>|<TYPE>|@<SAMPLE_RATE>|#<TAGS>

            // METRIC_NAME
            Array.Copy(metricName, 0, metricBytes, index, metricName.Length);
            index += metricName.Length;
            metricBytes[index] = (byte)':';
            index += 1;

            // VALUE
            index += SerializeValue(value, metricBytes, index);
            metricBytes[index] = (byte)'|';
            index += 1;

            // TYPE
            Array.Copy(type, 0, metricBytes, index, type.Length);
            index += type.Length;

            // SAMPLE_RATE
            if (sampleRate != null && !sampleRate.SequenceEqual(MaxSampleRateBytes))
            {
                metricBytes[index] = (byte)'|';
                metricBytes[index + 1] = (byte)'@';
                index += 2;

                Array.Copy(sampleRate, 0, metricBytes, index, sampleRate.Length);
                index += sampleRate.Length;
            }

            // TAGS
            if (tags != null && tags.Length != 0)
            {
                metricBytes[index] = (byte)'|';
                metricBytes[index + 1] = (byte)'#';
                index += 2;

                Array.Copy(tags, 0, metricBytes, index, tags.Length);
                index += tags.Length;
            }

            // wrap array in a segment because ArrayPool can return a larger array than "size"
            return new ArraySegment<byte>(metricBytes, 0, size);
        }

        /// <remarks>Documentation: https://docs.datadoghq.com/developers/metrics</remarks>
        public static byte[] SerializeMetricName(string metricName)
        {
            if (string.IsNullOrEmpty(metricName))
            {
                throw new ArgumentException("Metric name can't be empty", nameof(metricName));
            }

            if (!char.IsLetter(metricName[0]))
            {
                throw new ArgumentException("Metric name should start with a letter", nameof(metricName));
            }

            if (metricName.Length > MetricNameMaxSize)
            {
                throw new ArgumentException($"Metric name exceeds ${MetricNameMaxSize} characters", nameof(metricName));
            }

            foreach (char c in metricName)
            {
                if (c >= 128 || (!char.IsLetterOrDigit(c) && c != '_' && c != '.'))
                {
                    throw new ArgumentException("Metric name must only contain ASCII alphanumerics, underscores, and periods", nameof(metricName));
                }
            }

            return Encoding.ASCII.GetBytes(metricName);
        }

        public static byte[] SerializeMetricType(MetricType type)
        {
            string typeStr = type switch
            {
                MetricType.Count => "c",
                // MetricType.Rate => "",
                MetricType.Gauge => "g",
                MetricType.Histogram => "h",
                // MetricType.Distribution => ,
                _ => throw new ArgumentOutOfRangeException(nameof(type)),
            };

            return Encoding.ASCII.GetBytes(typeStr);
        }

        public static byte[] SerializeSampleRate(double sampleRate)
        {
            double min = Math.Pow(10, -DecimalPrecision);
            if (sampleRate < min || sampleRate > 1.0)
            {
                throw new ArgumentException($"Sample rate must be included between {min} and {1.0}", nameof(sampleRate));
            }

            return Encoding.ASCII.GetBytes($"{sampleRate:0.000000}");
        }

        public static byte[] SerializeTags(IList<string>? tags)
        {
            if (tags == null || tags.Count == 0)
            {
                return Array.Empty<byte>();
            }

            int tagsSizeSum = 0;
            foreach (string tag in tags)
            {
                ValidateTag(tag);
                tagsSizeSum += tag.Length;
            }

            // 1 byte per character + comma between tags
            var tagsBytes = new byte[tagsSizeSum + tags.Count - 1];
            int writeIndex = 0;
            foreach (string tag in tags)
            {
                writeIndex += Encoding.ASCII.GetBytes(tag, 0, tag.Length, tagsBytes, writeIndex);
                if (writeIndex < tagsBytes.Length)
                {
                    tagsBytes[writeIndex] = (byte)',';
                    writeIndex += 1;
                }
            }

            return tagsBytes;
        }

        /// <remarks>Documentation: https://docs.datadoghq.com/tagging</remarks>
        private static void ValidateTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                throw new ArgumentException("Tag can't be empty");
            }

            if (!char.IsLetter(tag[0]))
            {
                throw new ArgumentException("Tag should start with a letter");
            }

            if (tag[^1] == ':')
            {
                throw new ArgumentException("Tag cannot end with a colon");
            }

            if (tag.Length > TagMaxSize)
            {
                throw new ArgumentException($"Tag exceeds ${TagMaxSize} characters");
            }

            foreach (char c in tag)
            {
                if (c >= 128 || (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != ':' && c != '.' && c != '/'))
                {
                    throw new ArgumentException("Tag must only contain alphanumerics, underscores, minuses, colons, periods, slashes");
                }
            }
        }

        private static int SerializeValue(double value, byte[] bytes, int byteIndex)
        {
            bool isValueWhole = value % 1 == 0;
            Span<char> valueChars = stackalloc char[20 + 1 + DecimalPrecision];
            int valueCharsSize;
            if (isValueWhole)
            {
                ((long)value).TryFormat(valueChars, out valueCharsSize);
            }
            else
            {
                value.TryFormat(valueChars, out valueCharsSize, "0.000000");
            }

            valueChars = valueChars.Slice(0, valueCharsSize);
            var bytesSpan = new Span<byte>(bytes, byteIndex, bytes.Length - byteIndex);
            return Encoding.ASCII.GetBytes(valueChars, bytesSpan);
        }

        private static int SerializedMetricSize(byte[] metricName, double value, byte[] type, byte[]? sampleRate, byte[] tags)
        {
            int size = metricName.Length + 1 + SerializedValueSize(value) + 1 + type.Length; // ':' + '|'

            if (sampleRate != null && !sampleRate.SequenceEqual(MaxSampleRateBytes))
            {
                size += 2 + sampleRate.Length; // '|@'
            }

            if (tags != null && tags.Length != 0)
            {
                size += 2 + tags.Length; // '|#'
            }

            return size;
        }

        private static int SerializedValueSize(double value)
        {
            bool isNegative = value < 0;
            bool isWhole = value % 1 == 0;
            long wholePart = (long)value;

            return (wholePart == 0 ? 1 : (int)Math.Floor(Math.Log10(Math.Abs(wholePart)) + 1))
                + (isNegative ? 1 : 0) // '-'
                + (isWhole ? 0 : 1 + DecimalPrecision); // '.000000'
        }
    }
}