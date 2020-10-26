using System;
using System.Collections.Generic;

namespace DatadogStatsD.Test
{
    internal class TestHelper
    {
        public static KeyValuePair<string, string>[]? ParseTags(string? tagsStr)
        {
            if (tagsStr == null)
            {
                return null;
            }

            var tags = new List<KeyValuePair<string, string>>();
            foreach (string tagStr in tagsStr.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = tagStr.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
                string key = parts[0];
                string value = parts.Length > 1 ? parts[1] : string.Empty;
                tags.Add(KeyValuePair.Create(key, value));
            }

            return tags.ToArray();
        }
    }
}
