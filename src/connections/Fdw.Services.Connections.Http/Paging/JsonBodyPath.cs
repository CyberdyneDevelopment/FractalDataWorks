using System;
using System.Text.Json;
using Fdw.Results;
using Fdw.Services.Connections.Http.Paging.PageText;
using Fdw.Services.Connections.Http.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Http.Paging;

/// <summary>Reads a text value at a path in a page's JSON body: a next link or a continuation token (FDW-4938).</summary>
/// <remarks>
/// The path is written as a container's RecordSelector is: an optional <c>$</c> or <c>$.</c>, then names separated by dots. Why a name is
/// matched longest first: some remotes put a dot inside a name (<c>@odata.nextLink</c>), so at each level the longest run of the remaining
/// names that is a property is taken before a shorter one.
/// </remarks>
public static class JsonBodyPath
{
    /// <summary>Reads the text at <paramref name="path"/> in <paramref name="body"/>.</summary>
    /// <param name="connectionName">The connection read, for the message.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="page">The page's number, for the message.</param>
    /// <param name="body">The page's body.</param>
    /// <param name="path">The path.</param>
    /// <returns>The text; null when the body holds nothing, or null, there (the remote has no more); or why the body cannot be read.</returns>
    public static IGenericResult<string?> Text(string connectionName, ILogger logger, int page, ReadOnlyMemory<byte> body, string path)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (Find(document.RootElement, Segments(path), 0) is not { } found)
                return GenericResult<string?>.Success(null);

            var kind = PageTextKinds.ByValueKind(found.ValueKind);
            return ReferenceEquals(kind, PageTextKinds.NotFound)
                ? GenericResult<string?>.Failure(HttpPagingLog.PageBodyUnreadable(
                    logger, connectionName, page, path, $"it holds a {found.ValueKind}, not text"))
                : GenericResult<string?>.Success(kind.Read(found));
        }
        catch (JsonException ex)
        {
            return GenericResult<string?>.Failure(HttpPagingLog.PageBodyUnreadable(logger, connectionName, page, path, ex.Message));
        }
    }

    private static string[] Segments(string path)
    {
        var clean = path.StartsWith("$.", StringComparison.Ordinal) ? path[2..]
            : path.StartsWith('$') ? path[1..]
            : path;
        return clean.Split('.', StringSplitOptions.RemoveEmptyEntries);
    }

    private static JsonElement? Find(JsonElement element, string[] segments, int from)
    {
        if (from == segments.Length)
            return element;

        if (element.ValueKind == JsonValueKind.Array)
        {
            return int.TryParse(segments[from], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var index)
                   && index < element.GetArrayLength()
                ? Find(element[index], segments, from + 1)
                : null;
        }

        if (element.ValueKind != JsonValueKind.Object)
            return null;

        for (var end = segments.Length; end > from; end--)
        {
            if (element.TryGetProperty(string.Join(".", segments, from, end - from), out var child))
                return Find(child, segments, end);
        }

        return null;
    }
}
