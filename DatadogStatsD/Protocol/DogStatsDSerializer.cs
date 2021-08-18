using System;
using System.Buffers;
#if NETSTANDARD2_1
using System.Buffers.Text;
#endif
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DatadogStatsD.Events;
using DatadogStatsD.Metrics;
using DatadogStatsD.ServiceChecks;

namespace DatadogStatsD.Protocol
{
    internal static class DogStatsDSerializer
    {
        private const int MetricNameMaxLength = 200;
        private const int TagMaxLength = 200;
        private static readonly int SerializedValueMaxLength = double.MinValue.ToString(CultureInfo.InvariantCulture).Length;

        private const int EventTitleMaxLength = 100;
        private static readonly int EventTitleMaxLengthWidth = IntegerWidth(EventTitleMaxLength);
        private static readonly string EventTitleLengthFormat = GenerateIntegerFormat(EventTitleMaxLengthWidth);

        private const int EventMessageMaxLength = 4000;
        private static readonly int EventMessageMaxLengthWidth = IntegerWidth(EventMessageMaxLength);
        private static readonly string EventMessageLengthFormat = GenerateIntegerFormat(EventMessageMaxLengthWidth);

        private const int EventAggregationKeyMaxLength = 100;

        private const string EventPrefix = "_e";
        private const string ServiceCheckPrefix = "_sc";
        private const string SampleRatePrefix = "|@";
        private const string AlertTypePrefix = "|t:";
        private const string PriorityPrefix = "|p:";
        private const string AggregationKeyPrefix = "|k:";
        private const string SourcePrefix = "|s:";
        private const string ServiceCheckMessagePrefix = "|m:";
        private const string TagsPrefix  = "|#";
        private static readonly byte[] EventPrefixBytes = Encoding.ASCII.GetBytes(EventPrefix);
        private static readonly byte[] ServiceCheckPrefixBytes = Encoding.ASCII.GetBytes(ServiceCheckPrefix);
        private static readonly byte[] SampleRatePrefixBytes = Encoding.ASCII.GetBytes(SampleRatePrefix);
        private static readonly byte[] AggregationKeyPrefixBytes = Encoding.ASCII.GetBytes(AggregationKeyPrefix);
        private static readonly byte[] SourcePrefixBytes = Encoding.ASCII.GetBytes(SourcePrefix);
        private static readonly byte[] ServiceCheckMessagePrefixBytes = Encoding.ASCII.GetBytes(ServiceCheckMessagePrefix);
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
        /// <param name="metricPrefixBytes">Serialized metric prefix built with <see cref="SerializeMetricPrefix"/>.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="metricSuffixBytes">Serialized metric suffix built with <see cref="SerializeMetricSuffix"/>.</param>
        /// <returns>A segment of a byte array containing the serialized metric. The array was loaned from
        /// <see cref="ArrayPool{T}.Shared"/> and must be returned once it's not used.</returns>
        /// <remarks>Documentation: https://docs.datadoghq.com/developers/dogstatsd/datagram_shell/?tab=metrics</remarks>
        public static ArraySegment<byte> SerializeMetric(byte[] metricPrefixBytes, double value, byte[] metricSuffixBytes)
        {
            int length = metricPrefixBytes.Length + SerializedValueMaxLength + metricSuffixBytes.Length;
            var stream = new DogStatsDStream(length);

            // <METRIC_NAME>:<VALUE>|<TYPE>|@<SAMPLE_RATE>|#<TAGS>
            stream.Write(metricPrefixBytes);
            WriteValue(value, ref stream);
            stream.Write(metricSuffixBytes);

            return stream.GetBuffer();
        }

        public static byte[] SerializeMetricPrefix(string metricName)
        {
            // <METRIC_NAME>:
            return SerializeMetricName(metricName).Append((byte)':').ToArray();
        }

        public static byte[] SerializeMetricSuffix(MetricType metricType, double sampleRate, IList<KeyValuePair<string, string>>? tags)
        {
            // |<TYPE>|@<SAMPLE_RATE>|#<TAGS>
            IEnumerable<byte> bytes = Enumerable.Empty<byte>();

            bytes = bytes.Append((byte)'|');
            bytes = bytes.Concat(SerializeMetricType(metricType));

            if (sampleRate != 1.0)
            {
                bytes = bytes.Concat(SampleRatePrefixBytes);
                bytes = bytes.Concat(ValidateAndSerializeSampleRate(sampleRate));
            }

            if (tags != null && tags.Count != 0)
            {
                bytes = bytes.Concat(TagsPrefixBytes);
                bytes = bytes.Concat(ValidateAndSerializeTags(tags));
            }

            return bytes.ToArray();
        }

        /// <remarks>Documentation: https://docs.datadoghq.com/developers/dogstatsd/datagram_shell/?tab=events</remarks>
        public static ArraySegment<byte> SerializeEvent(AlertType alertType, string title, string message, EventPriority priority,
            byte[] sourceBytes, string? aggregationKey, byte[] constantTagsBytes, IList<KeyValuePair<string, string>>? tags)
        {
            tags ??= Array.Empty<KeyValuePair<string, string>>();
            title = EscapeNewLines(title);
            message = EscapeNewLines(message);

            int length = SerializedEventLength(alertType, title, message, priority, sourceBytes, aggregationKey, constantTagsBytes, tags);
            var stream = new DogStatsDStream(length);

            stream.Write(EventPrefixBytes);
            // {<TITLE>.length,<TEXT>.length}:<TITLE>|<TEXT>
            stream.Write((byte)'{');

            int lengthStartPosition = stream.Position;
            stream.Seek(EventTitleMaxLengthWidth + 1 + EventMessageMaxLengthWidth, SeekOrigin.Current);

            stream.Write((byte)'}');
            stream.Write((byte)':');
            int titleLength = stream.WriteUTF8(title, Math.Min(title.Length, EventTitleMaxLength));
            stream.Write((byte)'|');
            int messageLength = stream.WriteUTF8(message, Math.Min(message.Length, EventMessageMaxLength));

            // go back to lengthIndex to write the lengths
            int lengthEndPosition = stream.Position;
            stream.Seek(lengthStartPosition, SeekOrigin.Begin);
#if NETSTANDARD2_0
            stream.WriteASCII(titleLength.ToString(EventTitleLengthFormat));
            stream.Write((byte)',');
            stream.WriteASCII(messageLength.ToString(EventMessageLengthFormat));
#else
            Span<char> lengthString = stackalloc char[EventMessageMaxLengthWidth];
            titleLength.TryFormat(lengthString, out int lengthStringLength, EventTitleLengthFormat);
            stream.WriteASCII(lengthString.Slice(0, lengthStringLength));
            stream.Write((byte)',');
            messageLength.TryFormat(lengthString, out lengthStringLength, EventMessageLengthFormat);
            stream.WriteASCII(lengthString.Slice(0, lengthStringLength));
#endif
            stream.Seek(lengthEndPosition, SeekOrigin.Begin);

            if (priority != EventPriority.Normal)
            {
                stream.Write(PriorityBytes[(int)priority]);
            }

            if (alertType != AlertType.Info)
            {
                stream.Write(AlertTypeBytes[(int)alertType]);
            }

            if (aggregationKey != null)
            {
                stream.Write(AggregationKeyPrefixBytes);
                stream.WriteASCII(aggregationKey, Math.Min(aggregationKey.Length, EventAggregationKeyMaxLength));
            }

            if (sourceBytes.Length != 0)
            {
                stream.Write(SourcePrefixBytes);
                stream.Write(sourceBytes);
            }

            WriteConstantAndExtraTags(constantTagsBytes, tags, ref stream);

            return stream.GetBuffer();
        }

        public static ArraySegment<byte> SerializeServiceCheck(byte[] namespaceBytes, string name, CheckStatus checkStatus, string message,
            byte[] constantTagsBytes, IList<KeyValuePair<string, string>>? extraTags)
        {
            message = EscapeNewLines(message).Replace("m:", "m\\:");
            extraTags ??= Array.Empty<KeyValuePair<string, string>>();

            int length = SerializedServiceCheckLength(namespaceBytes, name, message, constantTagsBytes, extraTags);
            var stream = new DogStatsDStream(length);

            // _sc|<NAMESPACE>.<NAME>|<STATUS>|#<TAG_KEY_1>:<TAG_VALUE_1>,<TAG_2>|m:<SERVICE_CHECK_MESSAGE>
            stream.Write(ServiceCheckPrefixBytes);
            stream.Write((byte)'|');
            if (namespaceBytes.Length != 0)
            {
                stream.Write(namespaceBytes);
                stream.Write((byte)'.');
            }

            stream.WriteASCII(name);
            stream.Write((byte)'|');
            stream.Write((byte)((byte)checkStatus + (byte)'0'));
            WriteConstantAndExtraTags(constantTagsBytes, extraTags, ref stream);
            if (message.Length != 0)
            {
                stream.Write(ServiceCheckMessagePrefixBytes);
                stream.WriteUTF8(message);
            }

            return stream.GetBuffer();
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

        public static byte[] ValidateAndSerializeSampleRate(double sampleRate)
        {
            ThrowHelper.ThrowIfNaN(sampleRate);
            if (sampleRate < 0.0 || sampleRate > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be included between 0 and 1");
            }

            return Encoding.ASCII.GetBytes(sampleRate.ToString(CultureInfo.InvariantCulture));
        }

        public static byte[] SerializeSource(string? source)
        {
            return source != null ? Encoding.ASCII.GetBytes(source) : Array.Empty<byte>();
        }

        public static byte[] ValidateAndSerializeTags(IList<KeyValuePair<string, string>>? tags)
        {
            if (tags == null || tags.Count == 0)
            {
                return Array.Empty<byte>();
            }

            ValidateTags(tags);
            var stream = new DogStatsDStream(new byte[SerializedTagsLength(tags)]);
            WriteTags(tags, ref stream);
            return stream.GetBuffer().Array;
        }

        private static void ValidateTags(IList<KeyValuePair<string, string>> tags)
        {
            foreach (var tag in tags)
            {
                ValidateTag(tag);
            }
        }

        /// <remarks>Documentation: https://docs.datadoghq.com/tagging</remarks>
        private static void ValidateTag(KeyValuePair<string, string> tag)
        {
            if (string.IsNullOrEmpty(tag.Key))
            {
                throw new ArgumentException("Tag key can't be null nor empty");
            }

            if (!char.IsLetter(tag.Key[0]))
            {
                throw new ArgumentException("Tag key should start with a letter");
            }

            if (tag.Key.Length + (string.IsNullOrEmpty(tag.Value) ? 0 : 1 + tag.Value.Length) > TagMaxLength)
            {
                throw new ArgumentException($"Tag exceeds {TagMaxLength} characters");
            }

            ValidateTagPart(tag.Key);
            if (!string.IsNullOrEmpty(tag.Value))
            {
                ValidateTagPart(tag.Value!);
            }
        }

        private static void ValidateTagPart(string part)
        {
            foreach (char c in part)
            {
                if (c >= 128 || (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != ':' && c != '.' && c != '/'))
                {
                    throw new ArgumentException("Tag must only contain alphanumerics, underscores, minuses, colons, periods, slashes");
                }
            }
        }

        private static void WriteConstantAndExtraTags(byte[] constantTagsBytes, IList<KeyValuePair<string, string>> tags,
            ref DogStatsDStream stream)
        {
             if (constantTagsBytes.Length == 0 && tags.Count == 0)
             {
                 return;
             }

             stream.Write(TagsPrefixBytes);
             stream.Write(constantTagsBytes);
             if (constantTagsBytes.Length != 0 && tags.Count != 0)
             {
                 stream.Write((byte)',');
             }

             WriteTags(tags, ref stream);
        }

        private static void WriteTags(IList<KeyValuePair<string, string>> tags, ref DogStatsDStream stream)
        {
            for (int i = 0; i < tags.Count; i += 1)
            {
                stream.WriteASCII(tags[i].Key);
                if (!string.IsNullOrEmpty(tags[i].Value))
                {
                    stream.Write((byte)':');
                    stream.WriteASCII(tags[i].Value);
                }

                if (i < tags.Count - 1)
                {
                    stream.Write((byte)',');
                }
            }
        }

        private static void WriteValue(double value, ref DogStatsDStream stream)
        {
#if NETSTANDARD2_0
            bool isValueWhole = Math.IEEERemainder(value, 1.0) == 0;
            string valueStr = isValueWhole
                ? ((long) value).ToString(CultureInfo.InvariantCulture)
                : value.ToString("G", CultureInfo.InvariantCulture);
            stream.WriteASCII(valueStr);
#else
            stream.Write(value);
#endif
        }

        private static byte[] SerializeMetricType(MetricType type)
        {
            return MetricTypeBytes[(int)type];
        }

        private static int SerializedEventLength(AlertType alertType, string title, string message, EventPriority priority,
            byte[] source, string? aggregationKey, byte[] constantTagsBytes, IList<KeyValuePair<string, string>> extraTags)
        {
            // _e{<TITLE>.length,<TEXT>.length}:<TITLE>|<TEXT>
            int length = EventPrefixBytes.Length + 1 + EventTitleMaxLengthWidth + 1 + EventMessageMaxLengthWidth + 2
#if NETSTANDARD2_0 // No GetByteCount(string, int, int) in .NET Standard 2.0
                         + Encoding.UTF8.GetByteCount(title.Substring(0, Math.Min(title.Length, EventTitleMaxLength))) + 1
                         + Encoding.UTF8.GetByteCount(message.Substring(0, Math.Min(message.Length, EventMessageMaxLength)));
#else
                         + Encoding.UTF8.GetByteCount(title, 0, Math.Min(title.Length, EventTitleMaxLength)) + 1
                         + Encoding.UTF8.GetByteCount(message, 0, Math.Min(message.Length, EventMessageMaxLength));
#endif

            if (priority != EventPriority.Normal)
            {
                length += PriorityBytes[(int)priority].Length;
            }

            if (alertType != AlertType.Info)
            {
                length += AlertTypeBytes[(int)alertType].Length;
            }

            if (aggregationKey != null)
            {
                length += AggregationKeyPrefixBytes.Length;
#if NETSTANDARD2_0 // No GetByteCount(string, int, int) in .NET Standard 2.0
                length += Encoding.ASCII.GetByteCount(aggregationKey.Substring(0, Math.Min(aggregationKey.Length, EventAggregationKeyMaxLength)));
#else
                length += Encoding.ASCII.GetByteCount(aggregationKey, 0, Math.Min(aggregationKey.Length, EventAggregationKeyMaxLength));
#endif
            }

            if (source.Length != 0)
            {
                length += SourcePrefixBytes.Length + source.Length;
            }

            length += SerializedConstantAndExtraTagsLength(constantTagsBytes, extraTags);

            return length;
        }

        private static int SerializedServiceCheckLength(byte[] namespaceBytes, string name, string message,
            byte[] constantTagsBytes, IList<KeyValuePair<string, string>> extraTags)
        {
            // _sc|<NAMESPACE>.<NAME>|<STATUS>|#<TAG_KEY_1>:<TAG_VALUE_1>,<TAG_2>|m:<SERVICE_CHECK_MESSAGE>
            int length = ServiceCheckPrefixBytes.Length + 1;
            if (namespaceBytes.Length != 0)
            {
                length += namespaceBytes.Length + 1;
            }

            length += Encoding.ASCII.GetByteCount(name) + TagsPrefixBytes.Length +
                      SerializedConstantAndExtraTagsLength(constantTagsBytes, extraTags);

            if (message.Length != 0)
            {
                length += ServiceCheckMessagePrefix.Length + Encoding.UTF8.GetByteCount(message);
            }

            return length;
        }

        private static int SerializedConstantAndExtraTagsLength(byte[] constantTagsBytes, IList<KeyValuePair<string, string>> extraTags)
        {
            int length = 0;
            if (constantTagsBytes.Length != 0 || extraTags.Count != 0)
            {
                length += TagsPrefixBytes.Length;
            }

            if (constantTagsBytes.Length != 0 && extraTags.Count != 0)
            {
                length += 1; // for comma
            }

            return length + constantTagsBytes.Length + SerializedTagsLength(extraTags);
        }

        private static int SerializedTagsLength(IList<KeyValuePair<string, string>> tags)
        {
            if (tags.Count == 0)
            {
                return 0;
            }

            // 1 byte per character + comma between tags
            int length = tags.Count - 1; // commas
            foreach (var tag in tags)
            {
                length += tag.Key.Length + (string.IsNullOrEmpty(tag.Value) ? 0 : 1 + tag.Value.Length);
            }

            return length;
        }

        private static int IntegerWidth(long l) => (int)Math.Floor(Math.Log10(Math.Abs(l)) + 1) + (l < 0 ? 1 : 0);
        private static string GenerateIntegerFormat(int width) => new string('0', width);
        private static string EscapeNewLines(string s) => s.Replace("\n", "\\n");

        private struct DogStatsDStream
        {
            private readonly byte[] _buffer;
            private readonly int _length;

            public DogStatsDStream(int length)
            {
                _buffer = ArrayPool<byte>.Shared.Rent(length);
                _length = length;
                Position = 0;
            }

            public DogStatsDStream(byte[] buffer)
            {
                _buffer = buffer;
                _length = buffer.Length;
                Position = 0;
            }

            public int Position { get; private set; }

            public int Write(byte b)
            {
                _buffer[Position] = b;
                Position += 1;
                return 1;
            }

#if NETSTANDARD2_1
            public int Write(double value)
            {
                bool isValueWhole = Math.IEEERemainder(value, 1.0) == 0;
                var valueBytes = new Span<byte>(_buffer, Position, SerializedValueMaxLength);
                int valuesBytesLength;
                if (isValueWhole)
                {
                    Utf8Formatter.TryFormat((long)value, valueBytes, out valuesBytesLength);
                }
                else
                {
                    Utf8Formatter.TryFormat(value, valueBytes, out valuesBytesLength);
                }

                Position += valuesBytesLength;
                return valuesBytesLength;
            }
#endif

            public int Write(byte[] buffer)
            {
                Array.Copy(buffer, 0, _buffer, Position, buffer.Length);
                Position += buffer.Length;
                return buffer.Length;
            }

            public int WriteASCII(string s)
            {
                return WriteASCII(s, s.Length);
            }

            public int WriteASCII(string s, int count)
            {
                int written = Encoding.ASCII.GetBytes(s, 0, count, _buffer, Position);
                Position += written;
                return written;
            }

#if !NETSTANDARD2_0
            public int WriteASCII(Span<char> chars)
            {
                var bufferSpan = new Span<byte>(_buffer, Position, _buffer.Length - Position);
                int written = Encoding.ASCII.GetBytes(chars, bufferSpan);
                Position += written;
                return written;
            }
#endif

            public int WriteUTF8(string s)
            {
                return WriteUTF8(s, s.Length);
            }

            public int WriteUTF8(string s, int count)
            {
                int written = Encoding.UTF8.GetBytes(s, 0, count, _buffer, Position);
                Position += written;
                return written;
            }

            public void Seek(int offset, SeekOrigin loc)
            {
                switch (loc)
                {
                    case SeekOrigin.Begin:
                        Position = offset;
                        break;
                    case SeekOrigin.Current:
                        Position += offset;
                        break;
                    case SeekOrigin.End:
                        Position = _length - offset;
                        break;
                    default:
                        throw new ArgumentException();
                }
            }

            public ArraySegment<byte> GetBuffer()
            {
                return new ArraySegment<byte>(_buffer, 0, Position);
            }
        }
    }
}
