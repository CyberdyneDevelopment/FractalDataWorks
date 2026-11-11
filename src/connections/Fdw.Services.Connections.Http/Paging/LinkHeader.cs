using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;

namespace Fdw.Services.Connections.Http.Paging;

/// <summary>Reads the target of a relation from an HTTP <c>Link</c> header (RFC 8288), such as the next page's address (FDW-4938).</summary>
/// <remarks>
/// Why a parser and not the first <c>&lt;...&gt;</c>: a header lists several links (<c>&lt;a&gt;; rel="prev", &lt;b&gt;; rel="next"</c>), and the
/// one wanted is the one whose rel names it. A rel may list several relations (<c>rel="next last"</c>), quoted or not, in any case.
/// </remarks>
public static class LinkHeader
{
    /// <summary>Finds the target of the link whose rel names <paramref name="relation"/>.</summary>
    /// <param name="headers">The response headers.</param>
    /// <param name="relation">The relation, such as <c>next</c>.</param>
    /// <returns>The link's target as the header gives it; null when no link names the relation.</returns>
    public static string? Target(HttpResponseHeaders headers, string relation)
    {
        return headers.TryGetValues("Link", out var values) ? Target(string.Join(",", values), relation) : null;
    }

    /// <summary>Finds the target of the link whose rel names <paramref name="relation"/> in a Link header's value.</summary>
    /// <param name="header">The header's value; several values joined by commas.</param>
    /// <param name="relation">The relation, such as <c>next</c>.</param>
    /// <returns>The link's target; null when no link names the relation.</returns>
    public static string? Target(string header, string relation)
    {
        var position = 0;
        while (position < header.Length)
        {
            var open = header.IndexOf('<', position);
            if (open < 0) return null;
            var close = header.IndexOf('>', open + 1);
            if (close < 0) return null;

            var target = header.Substring(open + 1, close - open - 1).Trim();
            var end = ParametersEnd(header, close + 1);
            if (Relations(header[(close + 1)..end]).Contains(relation, StringComparer.OrdinalIgnoreCase))
                return target;

            position = end + 1;
        }

        return null;
    }

    // A link's parameters end before the comma that separates it from the next link, one not inside a quoted value; the comma is not part
    // of them (FDW-5009: it was, so `rel="next",` read as `next",` and only the last link in a header could match).
    private static int ParametersEnd(string header, int from)
    {
        var quoted = false;
        for (var i = from; i < header.Length; i++)
        {
            if (header[i] == '"') quoted = !quoted;
            else if (header[i] == ',' && !quoted) return i;
        }

        return header.Length;
    }

    private static IEnumerable<string> Relations(string parameters)
    {
        foreach (var parameter in parameters.Split(';'))
        {
            var equals = parameter.IndexOf('=', StringComparison.Ordinal);
            if (equals < 0 || !string.Equals(parameter[..equals].Trim(), "rel", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var relation in parameter[(equals + 1)..].Trim().Trim('"').Split(' ', StringSplitOptions.RemoveEmptyEntries))
                yield return relation;
        }
    }
}
