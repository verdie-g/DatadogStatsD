using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DatadogStatsD.Events;
using DatadogStatsD.Metrics;

namespace DatadogStatsD
{
    internal static class DogStatsDSerializer
    {
        private const int MetricNameMaxLength = 200;
        private const int TagMaxLength = 200;
        private const int DecimalPrecision = 6;
        private static readonly byte[] MaxSampleRateBytes = SerializeSampleRate(1.0);

        private const int EventTitleMaxLength = 100;
        private static readonly int EventTitleMaxLengthWidth = IntegerWidth(EventTitleMaxLength);
        private static readonly string EventTitleLengthFormat = GenerateIntegerFormat(EventTitleMaxLengthWidth);

        private const int EventMessageMaxLength = 4000;
        private static readonly int EventMessageMaxLengthWidth = IntegerWidth(EventMessageMaxLength);
        private static readonly string EventMessageLengthFormat = GenerateIntegerFormat(EventMessageMaxLengthWidth);

        private const string AlertTypePrefix = "|t:";
        private const string PriorityPrefix = "|p:";
        private const string AggregationKeyPrefix = "|k:";
        private const string SourcePrefix = "|s:";
        private const string TagsPrefix  = "|#";
        private static readonly byte[] AggregationKeyPrefixBytes = Encoding.ASCII.GetBytes(AggregationKeyPrefix);
        private static readonly byte[] SourcePrefixBytes = Encoding.ASCII.GetBytes(SourcePrefix);
        private static readonly byte[] TagsPrefixBytes = Encoding.ASCII.GetBytes(TagsPrefix);

        private static readonly byte[][] MetricTypeBytes =
        {
            Encoding.ASCII.GetBytes("c"),
            Encoding.ASCII.GetBytes("d"),
            Encoding.ASCII.GetBytes("g"),
            Encoding.ASCII.GetBytes("h"),
            Encoding.ASCII.GetBytes("s"),
        };

        private static readonly byte[][] AlertTypeBytes =
        {
            Encoding.ASCII.GetBytes(AlertTypePrefix + "info"),
            Encoding.ASCII.GetBytes(AlertTypePrefix + "success"),
            Encoding.ASCII.GetBytes(AlertTypePrefix + "warning"),
            Encoding.ASCII.GetBytes(AlertTypePrefix + "error"),
        };

        private static readonly byte[][] PriorityBytes =
        {
            Encoding.ASCII.GetBytes(PriorityPrefix + "normal"),
            Encoding.ASCII.GetBytes(PriorityPrefix + "low"),
        };

        /// <summary>
        /// Serialize a metric with its value from pre-serialized parts.
        /// </summary>
        /// <param name="metricName">Serialized metric name built with <see cref="SerializeMetricName"/>.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="type">Serialized metric type built with <see cref="SerializeMetricType"/>.</param>
        /// <param name="sampleRate">Serialized sample rate built with <see cref="SerializeSampleRate"/>. Can be null.</param>
        /// <param name="tags">Serialized tags built with <see cref="ValidateAndSerializeTags"/>.</param>
        /// <returns>A segment of a byte array containing the serialized metric. The array was loaned from
        /// <see cref="ArrayPool{T}.Shared"/> and must be returned once it's not used.</returns>
        /// <remarks>Documentation: https://docs.datadoghq.com/developers/dogstatsd/datagram_shell/?tab=metrics</remarks>
        public static ArraySegment<byte> SerializeMetric(byte[] metricName, double value, byte[] type, byte[]? sampleRate, byte[] tags)
        {
            int length = SerializedMetricLength(metricName, value, type, sampleRate, tags);
            byte[] metricBytes = ArrayPool<byte>.Shared.Rent(length);
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

            // wrap array in a segment because ArrayPool can return a larger array than "length"
            return new ArraySegment<byte>(metricBytes, 0, length);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="alertType"></param>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="priority"></param>
        /// <param name="sourceBytes"></param>
        /// <param name="aggregationKey"></param>
        /// <param name="constantTagsBytes"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        /// <remarks>Documentation: https://docs.datadoghq.com/developers/dogstatsd/datagram_shell/?tab=events</remarks>
        public static ArraySegment<byte> SerializeEvent(AlertType alertType, string title, string message, Priority priority,
            byte[] sourceBytes, string? aggregationKey, byte[] constantTagsBytes, IList<string>? tags)
        {
            tags ??= Array.Empty<string>();

            int length = SerializedEventLength(alertType, title, message, priority, sourceBytes, aggregationKey, constantTagsBytes, tags);
            var eventBytes = ArrayPool<byte>.Shared.Rent(length);

            int writeIndex = 0;
            eventBytes[0] = (byte)'_';
            eventBytes[1] = (byte)'e';
            writeIndex += 2;

            // {<TITLE>.length,<TEXT>.length}:<TITLE>|<TEXT>
            eventBytes[writeIndex] = (byte)'{';
            writeIndex += 1;

            int lengthIndex = writeIndex;
            writeIndex += EventTitleMaxLengthWidth + 1 + EventMessageMaxLengthWidth; // skip length part

            eventBytes[writeIndex] = (byte)'}';
            eventBytes[writeIndex + 1] = (byte)':';
            writeIndex += 2;
            int titleLength = Encoding.UTF8.GetBytes(title, 0, Math.Min(title.Length, EventTitleMaxLength), eventBytes, writeIndex);
            writeIndex += titleLength;
            eventBytes[writeIndex] = (byte)'|';
            writeIndex += 1;
            int messageLength = Encoding.UTF8.GetBytes(message, 0, Math.Min(message.Length, EventMessageMaxLength), eventBytes, writeIndex);
            writeIndex += messageLength;

            // go back to lengthIndex to write the lengths
            Span<char> lengthString = stackalloc char[EventMessageMaxLengthWidth];
            titleLength.TryFormat(lengthString, out int lengthStringLength, EventTitleLengthFormat);
            Encoding.ASCII.GetBytes(lengthString.Slice(0, lengthStringLength),
                new Span<byte>(eventBytes, lengthIndex, EventTitleMaxLengthWidth));
            lengthIndex += EventTitleMaxLengthWidth;

            eventBytes[lengthIndex] = (byte)',';
            lengthIndex += 1;

            messageLength.TryFormat(lengthString, out lengthStringLength, EventMessageLengthFormat);
            Encoding.ASCII.GetBytes(lengthString.Slice(0, lengthStringLength),
                new Span<byte>(eventBytes, lengthIndex, EventMessageMaxLengthWidth));

            if (priority != Priority.Normal)
            {
                var priorityBytes = PriorityBytes[(int)priority];
                Array.Copy(priorityBytes, 0, eventBytes, writeIndex, priorityBytes.Length);
                writeIndex += priorityBytes.Length;
            }

            if (alertType != AlertType.Info)
            {
                var alertTypeBytes = AlertTypeBytes[(int)alertType];
                Array.Copy(alertTypeBytes, 0, eventBytes, writeIndex, alertTypeBytes.Length);
                writeIndex += alertTypeBytes.Length;
            }

            if (aggregationKey != null)
            {
                Array.Copy(AggregationKeyPrefixBytes, 0, eventBytes, writeIndex, AggregationKeyPrefixBytes.Length);
                writeIndex += AggregationKeyPrefix.Length;
                writeIndex += Encoding.ASCII.GetBytes(aggregationKey, 0, aggregationKey.Length, eventBytes, writeIndex);
            }

            if (sourceBytes.Length != 0)
            {
                Array.Copy(SourcePrefixBytes, 0, eventBytes, writeIndex, SourcePrefixBytes.Length);
                writeIndex += SourcePrefixBytes.Length;
                Array.Copy(sourceBytes, 0, eventBytes, writeIndex, sourceBytes.Length);
                writeIndex += sourceBytes.Length;
            }

            if (constantTagsBytes.Length != 0 || tags.Count != 0)
            {
                Array.Copy(TagsPrefixBytes, 0, eventBytes, writeIndex, TagsPrefixBytes.Length);
                writeIndex += TagsPrefixBytes.Length;
                Array.Copy(constantTagsBytes, 0, eventBytes, writeIndex, constantTagsBytes.Length);
                writeIndex += constantTagsBytes.Length;
                if (constantTagsBytes.Length != 0 && tags.Count != 0)
                {
                    eventBytes[writeIndex] = (byte)',';
                    writeIndex += 1;
                }

                writeIndex += SerializeTags(tags, eventBytes, writeIndex);
            }

            // wrap array in a segment because ArrayPool can return a larger array than "length"
            return new ArraySegment<byte>(eventBytes, 0, length);
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

            if (metricName.Length > MetricNameMaxLength)
            {
                throw new ArgumentException($"Metric name exceeds ${MetricNameMaxLength} characters", nameof(metricName));
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
            return MetricTypeBytes[(int)type];
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

        public static byte[] SerializeSource(string? source)
        {
            return source != null ? Encoding.ASCII.GetBytes(source) : Array.Empty<byte>();
        }

        public static byte[] ValidateAndSerializeTags(IList<string>? tags)
        {
            if (tags == null || tags.Count == 0)
            {
                return Array.Empty<byte>();
            }

            ValidateTags(tags);
            var tagsBytes = new byte[SerializedTagsLength(tags)];
            SerializeTags(tags, tagsBytes, 0);
            return tagsBytes;
        }

        private static void ValidateTags(IList<string> tags)
        {
            foreach (string tag in tags)
            {
                ValidateTag(tag);
            }
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

            if (tag.Length > TagMaxLength)
            {
                throw new ArgumentException($"Tag exceeds ${TagMaxLength} characters");
            }

            foreach (char c in tag)
            {
                if (c >= 128 || (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != ':' && c != '.' && c != '/'))
                {
                    throw new ArgumentException("Tag must only contain alphanumerics, underscores, minuses, colons, periods, slashes");
                }
            }
        }

        private static int SerializeTags(IList<string> tags, byte[] tagsBytes, int writeIndex)
        {
            int bytesWritten = 0;
            for (int i = 0; i < tags.Count; i += 1)
            {
                bytesWritten += Encoding.ASCII.GetBytes(tags[i], 0, tags[i].Length, tagsBytes, writeIndex + bytesWritten);
                if (i < tags.Count - 1)
                {
                    tagsBytes[writeIndex + bytesWritten] = (byte)',';
                    bytesWritten += 1;
                }
            }

            return bytesWritten;
        }

        private static int SerializeValue(double value, byte[] bytes, int byteIndex)
        {
            bool isValueWhole = value % 1 == 0;
            Span<char> valueChars = stackalloc char[20 + 1 + DecimalPrecision];
            int valueCharsLength;
            if (isValueWhole)
            {
                ((long)value).TryFormat(valueChars, out valueCharsLength);
            }
            else
            {
                value.TryFormat(valueChars, out valueCharsLength, "0.000000", CultureInfo.InvariantCulture);
            }

            valueChars = valueChars.Slice(0, valueCharsLength);
            var bytesSpan = new Span<byte>(bytes, byteIndex, bytes.Length - byteIndex);
            return Encoding.ASCII.GetBytes(valueChars, bytesSpan);
        }

        private static int SerializedMetricLength(byte[] metricName, double value, byte[] type, byte[]? sampleRate, byte[] tags)
        {
            int length = metricName.Length + 1 + SerializedValueLength(value) + 1 + type.Length; // ':' + '|'

            if (sampleRate != null && !sampleRate.SequenceEqual(MaxSampleRateBytes))
            {
                length += 2 + sampleRate.Length; // '|@'
            }

            if (tags != null && tags.Length != 0)
            {
                length += 2 + tags.Length; // '|#'
            }

            return length;
        }

        private static int SerializedEventLength(AlertType alertType, string title, string message, Priority priority,
            byte[] source, string? aggregationKey, byte[] constantTags, IList<string> tags)
        {
            // _e{<TITLE>.length,<TEXT>.length}:<TITLE>|<TEXT>
            int length = 3 + EventTitleMaxLengthWidth + 1 + EventMessageMaxLengthWidth + 2
                       + Encoding.UTF8.GetByteCount(title) + 1 + Encoding.UTF8.GetByteCount(message);

            if (priority != Priority.Normal)
            {
                length += PriorityBytes[(int)priority].Length;
            }

            if (alertType != AlertType.Info)
            {
                length += AlertTypeBytes[(int)alertType].Length;
            }

            if (aggregationKey != null)
            {
                length += AggregationKeyPrefixBytes.Length + Encoding.ASCII.GetByteCount(aggregationKey);
            }

            if (source.Length != 0)
            {
                length += SourcePrefixBytes.Length + source.Length;
            }

            if (constantTags.Length != 0 || tags.Count != 0)
            {
                length += TagsPrefixBytes.Length;
            }

            if (constantTags.Length != 0 && tags.Count != 0)
            {
                length += 1; // for comma
            }

            length += constantTags.Length + SerializedTagsLength(tags);

            return length;
        }

        private static int SerializedValueLength(double value)
        {
            int signLength = value < 0 ? 1 : 0;
            bool isWhole = value % 1 == 0;
            long wholePart = (long)value;

            return (wholePart == 0 ? signLength + 1 : IntegerWidth(wholePart)) + (isWhole ? 0 : 1 + DecimalPrecision); // '.000000'
        }

        private static int SerializedTagsLength(IList<string> tags)
        {
            if (tags.Count == 0)
            {
                return 0;
            }

            // 1 byte per character + comma between tags
            int length = tags.Count - 1; // commas
            foreach (string tag in tags)
            {
                length += tag.Length;
            }

            return length;
        }

        private static int IntegerWidth(long l) => (int)Math.Floor(Math.Log10(Math.Abs(l)) + 1) + (l < 0 ? 1 : 0);

        private static string GenerateIntegerFormat(int width)
        {
            return string.Create<object?>(width, null, (chars, _) => chars.Fill('0'));
        }
    }
}