using System;
using System.Collections.Generic;
using System.Globalization;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources;

/// <summary>
/// Media-type selection over the registered record writers — the mechanism an HTTP endpoint uses to
/// turn an <c>Accept</c> header into a format.
/// </summary>
public abstract partial class RecordWriterTypes
{
    /// <summary>
    /// Gets the registered writer type serving the supplied media type.
    /// </summary>
    /// <param name="mediaType">A single media type, with or without parameters (<c>text/csv;charset=utf-8</c>).</param>
    /// <returns>The matching writer type, or <c>NotFound</c> when no registered format serves it.</returns>
    public static IRecordWriterType ByMediaType(string? mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
        {
            return NotFound;
        }

        foreach (var candidate in All())
        {
            // Why an empty media type is skipped rather than matched: a format declares an empty media
            // type to say it cannot be served over HTTP at all (see FixedWidth, which needs per-field
            // widths a record schema does not carry). Matching it would make it reachable and then fail.
            if (string.IsNullOrEmpty(candidate.MediaType))
            {
                continue;
            }

            if (string.Equals(candidate.MediaType, Bare(mediaType!), StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return NotFound;
    }

    /// <summary>
    /// Gets the registered writer type best matching an HTTP <c>Accept</c> header.
    /// </summary>
    /// <param name="acceptHeader">The raw header value, which may list several media types with q-values.</param>
    /// <returns>
    /// The writer type for the most-preferred media type that a registered format serves, or
    /// <c>NotFound</c>.
    /// </returns>
    /// <remarks>
    /// A wildcard (<c>*/*</c> or <c>text/*</c>) deliberately returns <c>NotFound</c>. It states no
    /// preference, so honouring it here would mean this method picking a format on the caller's behalf
    /// — a default invented by the framework. The endpoint declares the format it serves when the
    /// client expressed no preference; that is the endpoint's decision to state, not this one's to
    /// guess.
    /// </remarks>
    public static IRecordWriterType ByAcceptHeader(string? acceptHeader)
    {
        if (string.IsNullOrWhiteSpace(acceptHeader))
        {
            return NotFound;
        }

        var ranked = new List<KeyValuePair<double, string>>();
        foreach (var entry in acceptHeader!.Split(','))
        {
            var trimmed = entry.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            ranked.Add(new KeyValuePair<double, string>(Quality(trimmed), Bare(trimmed)));
        }

        ranked.Sort((left, right) => right.Key.CompareTo(left.Key));

        foreach (var entry in ranked)
        {
            var match = ByMediaType(entry.Value);
            if (!ReferenceEquals(match, NotFound))
            {
                return match;
            }
        }

        return NotFound;
    }

    private static string Bare(string mediaType)
    {
        var separator = mediaType.IndexOf(';');
        return (separator < 0 ? mediaType : mediaType.Substring(0, separator)).Trim();
    }

    // Why an unparsable q-value ranks lowest instead of defaulting to 1.0: a malformed parameter is the
    // client saying something the server cannot read, and treating it as the strongest preference would
    // let a typo outrank a well-formed one.
    private static double Quality(string entry)
    {
        var separator = entry.IndexOf(';');
        if (separator < 0)
        {
            return 1.0;
        }

        foreach (var parameter in entry.Substring(separator + 1).Split(';'))
        {
            var trimmed = parameter.Trim();
            if (!trimmed.StartsWith("q=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return double.TryParse(
                trimmed.Substring(2), NumberStyles.Float, CultureInfo.InvariantCulture, out var quality)
                ? quality
                : 0.0;
        }

        return 1.0;
    }
}
