using System;

namespace Fdw.Mcp.Bus;

/// <summary>
/// Matches MCP topic strings against glob-style patterns. <c>*</c> matches a single segment;
/// <c>**</c> matches zero or more segments. Topic segments are split on <c>/</c>.
/// </summary>
public static class McpTopicPattern
{
    /// <summary>True when <paramref name="topic"/> matches <paramref name="pattern"/>.</summary>
    public static bool Matches(string pattern, string topic)
    {
        if (pattern is null) throw new ArgumentNullException(nameof(pattern));
        if (topic is null) throw new ArgumentNullException(nameof(topic));

        var patternSegments = pattern.Split('/');
        var topicSegments = topic.Split('/');
        return MatchSegments(patternSegments, 0, topicSegments, 0);
    }

    private static bool MatchSegments(string[] pattern, int pi, string[] topic, int ti)
    {
        while (pi < pattern.Length)
        {
            var seg = pattern[pi];
            if (string.Equals(seg, "**", StringComparison.Ordinal))
            {
                if (pi == pattern.Length - 1) return true;
                for (var k = ti; k <= topic.Length; k++)
                {
                    if (MatchSegments(pattern, pi + 1, topic, k)) return true;
                }
                return false;
            }

            if (ti >= topic.Length) return false;

            if (!string.Equals(seg, "*", StringComparison.Ordinal) && !string.Equals(seg, topic[ti], StringComparison.Ordinal))
                return false;

            pi++;
            ti++;
        }

        return ti == topic.Length;
    }
}
