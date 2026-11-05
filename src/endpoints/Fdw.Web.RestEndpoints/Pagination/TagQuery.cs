using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Fdw.Web.RestEndpoints.Pagination;

/// <summary>
/// The tags a list request asks for: <c>?tag=a&amp;tag=b</c> or <c>?tag=a,b</c>, the way <c>?id=</c> is written (FDW-3816).
/// </summary>
/// <remarks>
/// A list keeps the members that carry every tag named. A blank value names no tag, as a blank <c>q</c> searches for nothing. Only the names are read
/// here; which things carry them is the Tags service's to say, and a list reads the names from its page base (<c>TaggedWith</c>) the way it reads
/// <c>Search</c>, so a test that calls a list's load method directly states them without a running host.
/// </remarks>
public static class TagQuery
{
    /// <summary>The query parameter that names the tags.</summary>
    public const string QueryName = "tag";

    /// <summary>Reads the tags a request names.</summary>
    /// <param name="context">The current request.</param>
    /// <returns>The tag names, each once (case is not significant), in the order first written; none when the request names none.</returns>
    public static IReadOnlyList<string> Read(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // StringValues joins a repeated parameter with commas, so ?tag=a&tag=b and ?tag=a,b read the same.
        return [.. context.Request.Query[QueryName].ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }
}
