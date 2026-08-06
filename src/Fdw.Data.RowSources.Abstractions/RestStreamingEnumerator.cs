using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Http.Abstractions.Results;
using Fdw.Data.RowSources.Json.Abstractions;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.RowSources.Http.Abstractions;

/// <summary>
/// REST streaming enumerator that handles pagination-based streaming.
/// Supports offset/limit, page number, cursor, and Link header pagination styles.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class RestStreamingEnumerator : HttpRowEnumeratorBase
{
    private static readonly Regex LinkHeaderRegex = new(
        @"<(?<url>[^>]+)>;\s*rel=""next""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(1));

    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly RestStreamingOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RestStreamingEnumerator"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for requests.</param>
    /// <param name="baseUrl">The base URL for the REST endpoint.</param>
    /// <param name="options">Streaming options.</param>
    public RestStreamingEnumerator(HttpClient httpClient, string baseUrl, RestStreamingOptions? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        _options = options ?? new RestStreamingOptions();
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<IGenericResult<IDictionary<string, object?>>> EnumerateRows(
        IRowMapper mapper,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var state = new PaginationState();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ShouldStopPagination(state))
            {
                break;
            }

            var url = BuildUrl(state.NextUrl, state.Cursor, state.Offset, state.PageNumber);

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                yield return GenericResult<IDictionary<string, object?>>.Failure(
                    HttpRowSourceResultCodes.ByName("HttpRequestFailed"),
                    ResultDetails.Create("StatusCode", (int)response.StatusCode, "ReasonPhrase", response.ReasonPhrase ?? "Unknown"));
                IncrementRowErrors();
                break;
            }

            await foreach (var row in EnumeratePageRows(response, mapper, cancellationToken).ConfigureAwait(false))
            {
                if (_options.MaxRows > 0 && RowsRead >= _options.MaxRows)
                {
                    yield break;
                }

                yield return row;
            }

            state.PagesProcessed++;

            if (!TryGetNextPage(response, state))
            {
                break;
            }
        }
    }

    private bool ShouldStopPagination(PaginationState state)
    {
        return _options.MaxPages > 0 && state.PagesProcessed >= _options.MaxPages;
    }

    private async IAsyncEnumerable<IGenericResult<IDictionary<string, object?>>> EnumeratePageRows(
        HttpResponseMessage response,
        IRowMapper mapper,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
#if NETSTANDARD2_0
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#else
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#endif
        using var jsonSource = new JsonStreamRowSource(stream, _options.JsonOptions);

        while (jsonSource.Read())
        {
            IncrementRowsRead();
            yield return GenericResult<IDictionary<string, object?>>.Success(mapper.MapRow(jsonSource));
        }
    }

    private bool TryGetNextPage(HttpResponseMessage response, PaginationState state)
    {
        state.NextUrl = null;

        switch (_options.PaginationStyle.Name)
        {
            case "LinkHeader" when _options.ParseLinkHeader:
                state.NextUrl = ExtractLinkHeaderNext(response);
                return !string.IsNullOrEmpty(state.NextUrl);

            case "Cursor":
                // Would need to extract cursor from response body
                return false;

            case "OffsetLimit":
                state.Offset += _options.PageSize;
                return true; // Will break when page returns no rows

            case "PageNumber":
                state.PageNumber++;
                return true;

            default:
                return false;
        }
    }

    private string BuildUrl(string? nextUrl, string? cursor, long offset, int pageNumber)
    {
        if (!string.IsNullOrEmpty(nextUrl))
        {
            return nextUrl!;
        }

        var separator = _baseUrl.Contains("?") ? "&" : "?";

        return _options.PaginationStyle.Name switch
        {
            "OffsetLimit" =>
                $"{_baseUrl}{separator}{_options.OffsetParameter}={offset}&{_options.LimitParameter}={_options.PageSize}",

            "PageNumber" =>
                $"{_baseUrl}{separator}{_options.PageParameter}={pageNumber}&{_options.LimitParameter}={_options.PageSize}",

            "Cursor" when !string.IsNullOrEmpty(cursor) =>
                $"{_baseUrl}{separator}{_options.CursorParameter}={Uri.EscapeDataString(cursor)}&{_options.LimitParameter}={_options.PageSize}",

            "Cursor" =>
                $"{_baseUrl}{separator}{_options.LimitParameter}={_options.PageSize}",

            _ => _baseUrl
        };
    }

    private static string? ExtractLinkHeaderNext(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Link", out var linkValues))
        {
            return null;
        }

        foreach (var linkValue in linkValues)
        {
            var match = LinkHeaderRegex.Match(linkValue);
            if (match.Success)
            {
                return match.Groups["url"].Value;
            }
        }

        return null;
    }
}
