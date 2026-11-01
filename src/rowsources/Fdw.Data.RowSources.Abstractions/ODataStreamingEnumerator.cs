using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Conventions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Http.Abstractions.Results;
using Fdw.Data.RowSources.Json.Abstractions;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.RowSources.Http.Abstractions;

/// <summary>
/// OData streaming enumerator that handles $skip/$top pagination.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ODataStreamingEnumerator : HttpRowEnumeratorBase
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly ODataStreamingOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ODataStreamingEnumerator"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for requests.</param>
    /// <param name="baseUrl">The base URL for the OData endpoint.</param>
    /// <param name="options">Streaming options.</param>
    public ODataStreamingEnumerator(HttpClient httpClient, string baseUrl, ODataStreamingOptions? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        _options = options ?? new ODataStreamingOptions();

        // Ensure JSON array path points to OData value property
        if (string.IsNullOrEmpty(_options.JsonOptions.RowArrayPath))
        {
            _options.JsonOptions.RowArrayPath = "value";
        }
    }

    /// <inheritdoc />
    [ConventionOverride(MaxCyclomaticComplexity = 15)]  // Streaming pagination algorithm with state management (page limits, row limits, empty page detection)
    public override async IAsyncEnumerable<IGenericResult<IDictionary<string, object?>>> EnumerateRows(
        IRowMapper mapper,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int skip = 0;
        int pagesProcessed = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Check page limit
            if (_options.MaxPages > 0 && pagesProcessed >= _options.MaxPages)
            {
                break;
            }

            var url = BuildODataUrl(skip);

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

#if NETSTANDARD2_0
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#else
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#endif
            using var jsonSource = new JsonStreamRowSource(stream, _options.JsonOptions);

            var rowsInPage = 0;

            while (jsonSource.Read())
            {
                rowsInPage++;
                IncrementRowsRead();

                // Check row limit
                if (_options.MaxRows > 0 && RowsRead >= _options.MaxRows)
                {
                    yield break;
                }

                yield return GenericResult<IDictionary<string, object?>>.Success(mapper.MapRow(jsonSource));
            }

            // If page returned no rows or fewer than page size, we're done
            if (rowsInPage == 0 || rowsInPage < _options.PageSize)
            {
                break;
            }

            skip += _options.PageSize;
            pagesProcessed++;
        }
    }

    private string BuildODataUrl(int skip)
    {
        var sb = new StringBuilder(_baseUrl);
        var separator = _baseUrl.Contains("?") ? "&" : "?";

        // Add $skip and $top
        sb.Append(separator).Append("$skip=").Append(skip);
        sb.Append("&$top=").Append(_options.PageSize);

        // Add $count if requested
        if (_options.RequestCount)
        {
            sb.Append("&$count=true");
        }

        // Add $select if specified
        if (!string.IsNullOrEmpty(_options.Select))
        {
            sb.Append("&$select=").Append(Uri.EscapeDataString(_options.Select));
        }

        // Add $filter if specified
        if (!string.IsNullOrEmpty(_options.Filter))
        {
            sb.Append("&$filter=").Append(Uri.EscapeDataString(_options.Filter));
        }

        // Add $orderby if specified
        if (!string.IsNullOrEmpty(_options.OrderBy))
        {
            sb.Append("&$orderby=").Append(Uri.EscapeDataString(_options.OrderBy));
        }

        // Add $expand if specified
        if (!string.IsNullOrEmpty(_options.Expand))
        {
            sb.Append("&$expand=").Append(Uri.EscapeDataString(_options.Expand));
        }

        return sb.ToString();
    }
}
