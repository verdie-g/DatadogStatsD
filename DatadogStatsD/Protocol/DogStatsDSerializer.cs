using System;
using System.Buffers;
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
        private static readonly byte[] MaxSampleRateBytes = SerializeSampleRate(1.0);

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
        /// <param name="metricNameBytes">Serialized metric name built with <see cref="SerializeMetricName"/>.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="metricType">Metric type.</param>
        /// <param name="sampleRate">Serialized sample rate built with <see cref="SerializeSampleRate"/>.</param>
        /// <param name="tagsBytes">Serialized tags built with <see cref="ValidateAndSerializeTags"/>.</param>
        /// <returns>A segment of a byte array containing the serialized metric. The array was loaned from
        /// <see cref="ArrayPool{T}.Shared"/> and must be returned once it's not used.</returns>
        /// <remarks>Documentation: https://docs.datadoghq.com/developers/dogstatsd/datagram_shell/?tab=metrics</remarks>
        public static ArraySegment<byte> SerializeMetric(byte[] metricNameBytes, double value, MetricType metricType, byte[] sampleRate, byte[] tagsBytes)
        {
            byte[] metricTypeBytes = SerializeMetricType(metricType);
            int length = SerializedMetricLength(metricNameBytes, metricTypeBytes, sampleRate, tagsBytes);
            var stream = new DogStatsDStream(length);

            // <METRIC_NAME>:<VALUE>|<TYPE>|@<SAMPLE_RATE>|#<TAGS>

            stream.Write(metricNameBytes);
            stream.Write((byte)':');
            WriteValue(value, ref stream);
            stream.Write((byte)'|');
            stream.Write(metricTypeBytes);

            if (!sampleRate.SequenceEqual(MaxSampleRateBytes))
            {
                stream.Write(SampleRatePrefixBytes);
                stream.Write(sampleRate);
            }

            if (tagsBytes != null && tagsBytes.Length != 0)
            {
                stream.Write(TagsPrefixBytes);
                stream.Write(tagsBytes);
            }

            // wrap array in a segment because ArrayPool can return a larger array than "length"
            return stream.GetBuffer();
        }

        /// <remarks>Documentation: https://docs.datadoghq.com/developers/dogstatsd/datagram_shell/?tab=events</remarks>
        public static ArraySegment<byte> SerializeEvent(AlertType alertType, string title, string message, EventPriority priority,
            byte[] sourceBytes, string? aggregationKey, byte[] constantTagsBytes, IList<string>? tags)
        {
            tags ??= Array.Empty<string>();
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
            Span<char> lengthString = stackalloc char[EventMessageMaxLengthWidth];
            titleLength.TryFormat(lengthString, out int lengthStringLength, EventTitleLengthFormat);
            stream.WriteASCII(lengthString.Slice(0, lengthStringLength));
            stream.Write((byte)',');
            messageLength.TryFormat(lengthString, out lengthStringLength, EventMessageLengthFormat);
            stream.WriteASCII(lengthString.Slice(0, lengthStringLength));
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
            byte[] constantTagsBytes, IList<string>? extraTags)
        {
            message = EscapeNewLines(message).Replace("m:", "m\\:");
            extraTags ??= Array.Empty<string>();

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

        public static byte[] SerializeSampleRate(double sampleRate)
        {
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

        public static byte[] ValidateAndSerializeTags(IList<string>? tags)
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

        private static void WriteConstantAndExtraTags(byte[] constantTagsBytes, IList<string> tags,
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

        private static void WriteTags(IList<string> tags, ref DogStatsDStream stream)
        {
            for (int i = 0; i < tags.Count; i += 1)
            {
                stream.WriteASCII(tags[i]);
                if (i < tags.Count - 1)
                {
                    stream.Write((byte)',');
                }
            }
        }

        private static void WriteValue(double value, ref DogStatsDStream stream)
        {
            bool isValueWhole = Math.Round(value) == value;
            Span<char> valueChars = stackalloc char[SerializedValueMaxLength];
            int valueCharsLength;
            if (isValueWhole)
            {
                ((long)value).TryFormat(valueChars, out valueCharsLength);
            }
            else
            {
                value.TryFormat(valueChars, out valueCharsLength, "G", CultureInfo.InvariantCulture);
            }

            valueChars = valueChars.Slice(0, valueCharsLength);
            stream.WriteASCII(valueChars);
        }

        private static byte[] SerializeMetricType(MetricType type)
        {
            return MetricTypeBytes[(int)type];
        }

        private static int SerializedMetricLength(byte[] metricName, byte[] type, byte[] sampleRate, byte[] tags)
        {
            int length = metricName.Length + 1 + SerializedValueMaxLength + 1 + type.Length; // ':' + '|'

            if (!sampleRate.SequenceEqual(MaxSampleRateBytes))
            {
                length += SampleRatePrefixBytes.Length + sampleRate.Length;
            }

            if (tags != null && tags.Length != 0)
            {
                length += TagsPrefixBytes.Length + tags.Length;
            }

            return length;
        }

        private static int SerializedEventLength(AlertType alertType, string title, string message, EventPriority priority,
            byte[] source, string? aggregationKey, byte[] constantTagsBytes, IList<string> extraTags)
        {
            // _e{<TITLE>.length,<TEXT>.length}:<TITLE>|<TEXT>
            int length = EventPrefixBytes.Length + 1 + EventTitleMaxLengthWidth + 1 + EventMessageMaxLengthWidth + 2
                         + Encoding.UTF8.GetByteCount(title, 0, Math.Min(title.Length, EventTitleMaxLength)) + 1
                         + Encoding.UTF8.GetByteCount(message, 0, Math.Min(message.Length, EventMessageMaxLength));

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
                length += Encoding.ASCII.GetByteCount(aggregationKey, 0, Math.Min(aggregationKey.Length, EventAggregationKeyMaxLength));
            }

            if (source.Length != 0)
            {
                length += SourcePrefixBytes.Length + source.Length;
            }

            length += SerializedConstantAndExtraTagsLength(constantTagsBytes, extraTags);

            return length;
        }

        private static int SerializedServiceCheckLength(byte[] namespaceBytes, string name, string message,
            byte[] constantTagsBytes, IList<string> extraTags)
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

        private static int SerializedConstantAndExtraTagsLength(byte[] constantTagsBytes, IList<string> extraTags)
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

            public int WriteASCII(Span<char> chars)
            {
                var bufferSpan = new Span<byte>(_buffer, Position, _buffer.Length - Position);
                int written = Encoding.ASCII.GetBytes(chars, bufferSpan);
                Position += written;
                return written;
            }

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