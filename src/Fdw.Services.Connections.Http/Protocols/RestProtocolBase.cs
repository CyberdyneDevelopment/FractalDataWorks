using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Data.RowSources;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Delimited.Abstractions;
using Fdw.Data.RowSources.FixedWidth.Abstractions;
using Fdw.Data.RowSources.Json.Abstractions;
using Fdw.Data.RowSources.Xml.Abstractions;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;
using Fdw.Services.Connections.Http.Abstractions.Results;
using Fdw.Services.Connections.Http.Security;
using Fdw.Services.Connections.RowQuery;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Http.Protocols;

/// <summary>
/// Base class for REST protocol implementations.
/// </summary>
/// <remarks>
/// <para>
/// This base class handles common REST concerns:
/// <list type="bullet">
/// <item><description>HTTP method mapping from command types</description></item>
/// <item><description>Query string building with filter expressions</description></item>
/// <item><description>Request body serialization</description></item>
/// <item><description>Response parsing and error handling</description></item>
/// <item><description>Pagination support (offset, cursor, page-based)</description></item>
/// </list>
/// </para>
/// <para>
/// Derived classes override virtual methods to customize:
/// <list type="bullet">
/// <item><description><see cref="BuildFilterQueryString"/> - Customize filter-to-querystring translation</description></item>
/// <item><description><see cref="BuildPaginationQueryString"/> - Customize pagination parameters</description></item>
/// <item><description><see cref="BuildOrderingQueryString"/> - Customize ordering parameters</description></item>
/// <item><description><see cref="ParseErrorResponse"/> - Handle API-specific error formats</description></item>
/// <item><description><see cref="ConfigureAuthenticationHeaders"/> - Add authentication headers</description></item>
/// <item><description><see cref="ExtractPaginationInfo"/> - Extract pagination from response</description></item>
/// </list>
/// </para>
/// </remarks>
public abstract class RestProtocolBase : HttpProtocolBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestProtocolBase"/> class.
    /// </summary>
    /// <param name="name">The name of the protocol.</param>
    /// <param name="description">The description of the protocol.</param>
    /// <param name="contentType">The content type for requests (default: application/json).</param>
    protected RestProtocolBase(string name, string description, string contentType = "application/json")
        : base(name, description, contentType)
    {
    }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage(Justification = "Orchestrating method - tested via integration tests")]
    public override async Task<IGenericResult<HttpRequestMessage>> Translate(
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var method = GetHttpMethod(command, container, context);
            var path = GetRequestPath(command, container, context);

            // Build query string for GET requests
            if (method == HttpMethod.Get && command is IQueryCommand queryCommand)
            {
                if (queryCommand.Filter?.Root is { } filterRoot
                    && FirstUnsupportedNode(filterRoot) is { } unsupported)
                {
                    return GenericResult<HttpRequestMessage>.Failure(
                        HttpResultCodes.ByName("UnsupportedFilterOperator"),
                        unsupported switch
                        {
                            IFilterCondition unsupportedCondition => ResultDetails.Create(
                                "Protocol", Name,
                                "Operator", unsupportedCondition.Operator.Name,
                                "Field", unsupportedCondition.PropertyName),
                            FilterGroup unsupportedGroup => ResultDetails.Create(
                                "Protocol", Name,
                                // The group's own logical operator may well be expressible (OData
                                // supports "and" fine) -- what's actually unsupported is the SHAPE
                                // (a nested group, or more children than this protocol can join into
                                // one expression), so the count goes in the detail rather than
                                // implying the operator itself is the problem.
                                "Operator", $"{unsupportedGroup.Operator.Name} group of {unsupportedGroup.Nodes.Count}",
                                "Field", "(group)"),
                            _ => ResultDetails.Create(
                                "Protocol", Name,
                                "Operator", unsupported.GetType().Name,
                                "Field", "(unrecognized filter node)"),
                        });
                }

                var queryString = BuildFullQueryString(queryCommand, context);
                if (!string.IsNullOrEmpty(queryString))
                {
                    var separator = path.Contains('?') ? "&" : "?";
                    path = $"{path}{separator}{queryString}";
                }
            }

            var request = new HttpRequestMessage(method, path);

            // Add request body for non-GET requests
            if (method != HttpMethod.Get)
            {
                var bodyResult = await BuildRequestBodyContent(command, container, context, cancellationToken).ConfigureAwait(false);
                if (!bodyResult.IsSuccess)
                {
                    return bodyResult.ToNewResult<HttpRequestMessage>();
                }
                request.Content = bodyResult.Value;
            }

            // Configure headers
            ConfigureRequestHeaders(request, command, container, context);
            ConfigureAuthenticationHeaders(request, context);

            return GenericResult<HttpRequestMessage>.Success(request);
        }
        catch (Exception ex)
        {
            return GenericResult<HttpRequestMessage>.Failure(
                HttpResultCodes.ByName("RestRequestBuildFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
    }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage(Justification = "Orchestrating method - tested via integration tests")]
    public override async Task<IGenericResult<object?>> ProcessResponse(
        HttpResponseMessage response,
        IStorageContainer container,
        Type resultType,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        // Check for error responses
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = ParseErrorResponse(response, content, context);
            return GenericResult<object?>.Failure(
                HttpResultCodes.ByName("HttpErrorResponse"),
                ResultDetails.Create()
                    .With("StatusCode", (int)response.StatusCode)
                    .With("ReasonPhrase", response.ReasonPhrase ?? "Unknown")
                    .With("ErrorContent", errorContent));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return GenericResult<object?>.Success(null);
        }

        if (HasRowReaderForFormat(container))
        {
            if (IsRowCollectionType(resultType))
            {
                return ExtractRowsFromContent(RowContent(content, container), container);
            }

            if (RecordMapperFor(resultType) is { } mapper)
            {
                return MaterializeRecords(RowContent(content, container), container, mapper, context);
            }
        }

        // Extract pagination info if available
        var paginationInfo = ExtractPaginationInfo(response, content, context);

        // Parse response body
        return await ExtractResultFromResponse(content, resultType, paginationInfo, context, cancellationToken).ConfigureAwait(false);
    }

    #region Virtual Extension Points

    /// <summary>
    /// Builds the complete query string for a query command.
    /// </summary>
    /// <param name="command">The query command.</param>
    /// <param name="context">The protocol context.</param>
    /// <returns>The complete query string (without leading ?).</returns>
    protected virtual string BuildFullQueryString(IQueryCommand command, HttpProtocolContext context)
    {
        var parts = new List<string>();

        // Add filter parameters
        var filterPart = BuildFilterQueryString(command, context);
        if (!string.IsNullOrEmpty(filterPart))
        {
            parts.Add(filterPart);
        }

        // Add pagination parameters
        var pagingPart = BuildPaginationQueryString(command, context);
        if (!string.IsNullOrEmpty(pagingPart))
        {
            parts.Add(pagingPart);
        }

        // Add ordering parameters
        var orderingPart = BuildOrderingQueryString(command, context);
        if (!string.IsNullOrEmpty(orderingPart))
        {
            parts.Add(orderingPart);
        }

        // Add field selection if supported
        var fieldsPart = BuildFieldSelectionQueryString(command, context);
        if (!string.IsNullOrEmpty(fieldsPart))
        {
            parts.Add(fieldsPart);
        }

        return string.Join("&", parts);
    }

    /// <summary>
    /// Whether this protocol's <see cref="BuildFilterFromExpression"/> can correctly render the
    /// given operator.
    /// </summary>
    /// <remarks>
    /// The base "Simple: name=value" convention only ever means equality — sending it for any other
    /// operator (Contains, StartsWith, NotEqual, GreaterThan, ...) would silently mis-render that
    /// operator as an exact match rather than refuse it (NO FALLBACKS). Override alongside
    /// <see cref="BuildFilterFromExpression"/> when a derived protocol adds real support for more
    /// operators (see <see cref="ODataProtocol"/>, which supports every operator via
    /// <see cref="IFilterOperator.ODataOperator"/>).
    /// </remarks>
    /// <param name="operatorName">The filter operator's <c>IFilterOperator.Name</c>.</param>
    protected virtual bool SupportsOperator(string operatorName)
        => string.Equals(operatorName, "Equal", System.StringComparison.Ordinal);

    /// <summary>
    /// Whether this protocol's <see cref="BuildFilterFromExpression"/> can correctly render a group
    /// of <paramref name="childCount"/> nodes combined with <paramref name="groupOperator"/>.
    /// </summary>
    /// <remarks>
    /// The base joins each child with "&amp;" as its own query parameter -- a reasonable AND
    /// convention for a REST querystring (each parameter narrows the result), but there is no way to
    /// express OR that way, so this refuses Or and only ever refuses Or. Override alongside
    /// <see cref="BuildFilterFromExpression"/> when a derived protocol renders groups differently
    /// (see <see cref="ODataProtocol"/>, whose single "$filter=" parameter cannot hold more than one
    /// condition until its group rendering is written).
    /// </remarks>
    protected virtual bool SupportsGroup(LogicalOperator groupOperator, int childCount)
        => ReferenceEquals(groupOperator, LogicalOperator.And);

    /// <summary>
    /// Whether this protocol's group rendering recurses into a nested <see cref="FilterGroup"/> child,
    /// as opposed to only rendering direct <see cref="IFilterCondition"/> children.
    /// </summary>
    /// <remarks>
    /// True by default: the base's own group handling calls <see cref="BuildFilterFromExpression"/>
    /// per child, which is virtual dispatch, so a nested group is rendered by whatever this protocol's
    /// condition/group handling already does -- no special case needed. Override to
    /// <see langword="false"/> when a derived protocol's group rendering only enumerates
    /// <see cref="IFilterCondition"/> children directly (see <see cref="JsonApiProtocol"/>, whose
    /// filter[field]=value group loop skips anything that is not a condition, silently dropping a
    /// nested group rather than refusing it).
    /// </remarks>
    protected virtual bool SupportsNestedGroups => true;

    /// <summary>
    /// Walks a filter tree depth-first and returns the first node -- a condition whose operator, a
    /// group whose logical operator/shape, or any node that is neither a condition nor a group --
    /// this protocol cannot express, or <see langword="null"/> when the whole tree is supported.
    /// </summary>
    /// <remarks>
    /// An unrecognized <see cref="IFilterNode"/> implementation is treated as unsupported rather than
    /// silently skipped: <see cref="IFilterNode"/> is a public marker interface, and the safe reading
    /// for anything this protocol doesn't know how to render is "not expressed," never "fine, proceed"
    /// (the same principle <see cref="IQueryCapability"/> documents for a translator generally).
    /// </remarks>
    private IFilterNode? FirstUnsupportedNode(IFilterNode node)
    {
        if (node is IFilterCondition condition)
            return SupportsOperator(condition.Operator.Name) ? null : node;

        if (node is FilterGroup group)
        {
            if (!SupportsGroup(group.Operator, group.Nodes.Count))
                return group;

            foreach (var child in group.Nodes)
            {
                if (child is FilterGroup && !SupportsNestedGroups)
                    return child;

                var found = FirstUnsupportedNode(child);
                if (found is not null)
                    return found;
            }

            return null;
        }

        return node;
    }

    /// <summary>
    /// Builds query string parameters from filter expressions.
    /// </summary>
    /// <remarks>
    /// Override this method to customize filter translation for different API conventions:
    /// <list type="bullet">
    /// <item><description>OData: $filter=Name eq 'value'</description></item>
    /// <item><description>JSON:API: filter[name]=value</description></item>
    /// <item><description>Simple: name=value</description></item>
    /// </list>
    /// Pairs with <see cref="SupportsOperator"/> — <see cref="Translate"/> refuses the request before
    /// this is ever called with an operator <see cref="SupportsOperator"/> declined.
    /// </remarks>
    /// <param name="command">The query command with filter.</param>
    /// <param name="context">The protocol context.</param>
    /// <returns>The filter query string portion.</returns>
    protected virtual string BuildFilterQueryString(IQueryCommand command, HttpProtocolContext context)
    {
        if (command.Filter?.Root is null)
        {
            return string.Empty;
        }

        // Default: simple key=value for single conditions
        return BuildFilterFromExpression(command.Filter.Root);
    }

    /// <summary>
    /// Recursively builds filter query string from a filter node.
    /// </summary>
    /// <param name="node">The filter node.</param>
    /// <returns>The query string representation.</returns>
    protected virtual string BuildFilterFromExpression(IFilterNode node)
    {
        if (node is IFilterCondition condition)
        {
            var encodedValue = HttpUtility.UrlEncode(condition.Value?.ToString() ?? string.Empty);
            return $"{condition.PropertyName}={encodedValue}";
        }

        if (node is FilterGroup group)
        {
            var parts = new List<string>();
            foreach (var child in group.Nodes)
            {
                var part = BuildFilterFromExpression(child);
                if (!string.IsNullOrEmpty(part))
                {
                    parts.Add(part);
                }
            }
            return string.Join("&", parts);
        }

        return string.Empty;
    }

    /// <summary>
    /// Builds query string parameters for pagination.
    /// </summary>
    /// <remarks>
    /// Override this method to support different pagination styles:
    /// <list type="bullet">
    /// <item><description>Offset-based: offset=20&amp;limit=10</description></item>
    /// <item><description>Page-based: page=3&amp;per_page=10</description></item>
    /// <item><description>Cursor-based: cursor=abc123&amp;limit=10</description></item>
    /// <item><description>OData: $skip=20&amp;$top=10</description></item>
    /// </list>
    /// </remarks>
    /// <param name="command">The query command with paging.</param>
    /// <param name="context">The protocol context.</param>
    /// <returns>The pagination query string portion.</returns>
    protected virtual string BuildPaginationQueryString(IQueryCommand command, HttpProtocolContext context)
    {
        if (command.Paging is null)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        // Default: offset/limit style
        if (command.Paging.Skip > 0)
        {
            parts.Add($"offset={command.Paging.Skip}");
        }

        if (command.Paging.Take > 0)
        {
            parts.Add($"limit={command.Paging.Take}");
        }

        return string.Join("&", parts);
    }

    /// <summary>
    /// Builds query string parameters for ordering.
    /// </summary>
    /// <remarks>
    /// Override this method to support different ordering conventions:
    /// <list type="bullet">
    /// <item><description>Simple: sort=name,-created_at (- prefix for descending)</description></item>
    /// <item><description>OData: $orderby=name asc,createdAt desc</description></item>
    /// <item><description>Separate params: sort_by=name&amp;sort_dir=asc</description></item>
    /// </list>
    /// </remarks>
    /// <param name="command">The query command with ordering.</param>
    /// <param name="context">The protocol context.</param>
    /// <returns>The ordering query string portion.</returns>
    protected virtual string BuildOrderingQueryString(IQueryCommand command, HttpProtocolContext context)
    {
        if (command.Ordering?.OrderedFields is null || command.Ordering.OrderedFields.Count == 0)
        {
            return string.Empty;
        }

        // Default: sort=field1,-field2 (minus for descending)
        var sortParts = new List<string>();
        foreach (var field in command.Ordering.OrderedFields)
        {
            var prefix = string.Equals(field.Direction.Name, "Descending", StringComparison.Ordinal) ? "-" : "";
            sortParts.Add($"{prefix}{field.PropertyName}");
        }

        return $"sort={string.Join(",", sortParts)}";
    }

    /// <summary>
    /// Builds query string parameters for field selection (sparse fieldsets).
    /// </summary>
    /// <param name="command">The query command.</param>
    /// <param name="context">The protocol context.</param>
    /// <returns>The field selection query string portion.</returns>
    protected virtual string BuildFieldSelectionQueryString(IQueryCommand command, HttpProtocolContext context)
    {
        // Default: no field selection
        // Override for APIs that support sparse fieldsets (e.g., fields=id,name,email)
        return string.Empty;
    }

    /// <summary>
    /// Builds the HTTP request body content.
    /// </summary>
    /// <param name="command">The data command.</param>
    /// <param name="container">The storage container.</param>
    /// <param name="context">The protocol context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTTP content for the request body.</returns>
    [ExcludeFromCodeCoverage(Justification = "Exception path requires unserializable types")]
    protected virtual Task<IGenericResult<HttpContent?>> BuildRequestBodyContent(
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        if (command is not IDataCommandWithInput commandWithInput || commandWithInput.InputData is null)
        {
            return Task.FromResult(GenericResult<HttpContent?>.Success(null));
        }

        try
        {
            var json = JsonSerializer.Serialize(RequestInput(command, commandWithInput), DefaultJsonOptions);
            var content = new StringContent(json, Encoding.UTF8, DefaultContentType);
            return Task.FromResult(GenericResult<HttpContent?>.Success((HttpContent?)content));
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<HttpContent?>.Failure(
                HttpResultCodes.ByName("RestSerializationFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message)));
        }
    }

    /// <summary>
    /// Configures authentication headers on the request.
    /// </summary>
    /// <remarks>
    /// The resolved key goes out according to the connection's <c>AuthenticationType</c>:
    /// <list type="bullet">
    /// <item><description>Bearer: <c>Authorization: Bearer {key}</c>.</description></item>
    /// <item><description>Any other type that resolved a key (ApiKey): the key in the connection's
    /// <c>ApiKeyHeaderName</c>, or in <see cref="HttpAuthenticationTypes.ApiKeyHeaderName"/> when it names none.</description></item>
    /// </list>
    /// Override this method to add API-specific authentication.
    /// </remarks>
    /// <param name="request">The HTTP request.</param>
    /// <param name="context">The protocol context with resolved secrets.</param>
    protected virtual void ConfigureAuthenticationHeaders(HttpRequestMessage request, HttpProtocolContext context)
    {
        if (string.IsNullOrEmpty(context.ResolvedApiKey))
        {
            return;
        }

        var httpConfig = context.Configuration as HttpConnectionConfigurationBase;
        if (string.Equals(httpConfig?.AuthenticationType, "Bearer", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.ResolvedApiKey);
            return;
        }

        request.Headers.TryAddWithoutValidation(ApiKeyHeaderFor(httpConfig), context.ResolvedApiKey);
    }

    // The header an API key goes out in: the connection's own ApiKeyHeaderName when it is an ApiKey
    // connection that names one, otherwise the deployment-wide default.
    private static string ApiKeyHeaderFor(HttpConnectionConfigurationBase? httpConfig)
        => httpConfig is not null
            && string.Equals(httpConfig.AuthenticationType, "ApiKey", StringComparison.OrdinalIgnoreCase)
            && httpConfig.AdditionalProperties is { Count: > 0 } securityValues
            && securityValues.TryGetValue("ApiKeyHeaderName", out var configuredHeaderName)
            && !string.IsNullOrEmpty(configuredHeaderName)
                ? configuredHeaderName
                : HttpAuthenticationTypes.ApiKeyHeaderName;

    /// <summary>
    /// Parses an error response from the API.
    /// </summary>
    /// <remarks>
    /// Override this method to handle API-specific error formats:
    /// <list type="bullet">
    /// <item><description>Standard: { "error": "message" }</description></item>
    /// <item><description>Detailed: { "errors": [{ "code": "...", "message": "..." }] }</description></item>
    /// <item><description>RFC 7807: { "type": "...", "title": "...", "detail": "..." }</description></item>
    /// </list>
    /// </remarks>
    /// <param name="response">The HTTP response.</param>
    /// <param name="content">The response content.</param>
    /// <param name="context">The protocol context.</param>
    /// <returns>A human-readable error message.</returns>
    protected virtual string ParseErrorResponse(HttpResponseMessage response, string content, HttpProtocolContext context)
    {
        // Try to parse common error formats
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                // Check for common error properties
                if (root.TryGetProperty("error", out var error))
                {
                    if (error.ValueKind == JsonValueKind.String)
                    {
                        return error.GetString() ?? $"HTTP {(int)response.StatusCode}";
                    }
                    if (error.TryGetProperty("message", out var errorMsg))
                    {
                        return errorMsg.GetString() ?? $"HTTP {(int)response.StatusCode}";
                    }
                }

                if (root.TryGetProperty("message", out var message))
                {
                    return message.GetString() ?? $"HTTP {(int)response.StatusCode}";
                }

                // RFC 7807 Problem Details
                if (root.TryGetProperty("detail", out var detail))
                {
                    return detail.GetString() ?? $"HTTP {(int)response.StatusCode}";
                }

                if (root.TryGetProperty("title", out var title))
                {
                    return title.GetString() ?? $"HTTP {(int)response.StatusCode}";
                }
            }
            catch (JsonException ex)
            {
                // Expected: content is not valid JSON — fall through to default. No logger on protocol type-options.
                _ = ex;
            }
            catch (InvalidOperationException ex)
            {
                // Expected: JSON is not the anticipated object shape (TryGetProperty/GetString on a
                // non-object/non-string element throws) — fall through to default.
                _ = ex;
            }
        }

        return $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
    }

    /// <summary>
    /// Extracts pagination information from the response.
    /// </summary>
    /// <remarks>
    /// Override this method to extract pagination from:
    /// <list type="bullet">
    /// <item><description>Response headers (X-Total-Count, Link header)</description></item>
    /// <item><description>Response body (meta.total, links.next)</description></item>
    /// <item><description>Cursor tokens for cursor-based pagination</description></item>
    /// </list>
    /// </remarks>
    /// <param name="response">The HTTP response.</param>
    /// <param name="content">The response content.</param>
    /// <param name="context">The protocol context.</param>
    /// <returns>Pagination information, or null if not available.</returns>
    protected virtual RestPaginationInfo? ExtractPaginationInfo(
        HttpResponseMessage response,
        string content,
        HttpProtocolContext context)
    {
        // Check for common pagination headers
        int? totalCount = null;
        string? nextCursor = null;

        if (response.Headers.TryGetValues("X-Total-Count", out var totalValues))
        {
            if (int.TryParse(string.Join("", totalValues), System.Globalization.CultureInfo.InvariantCulture, out var total))
            {
                totalCount = total;
            }
        }

        // Check for Link header (RFC 5988)
        if (response.Headers.TryGetValues("Link", out var linkValues))
        {
            var linkHeader = string.Join(",", linkValues);
            // Parse for rel="next" - simplified parsing
            if (linkHeader.Contains("rel=\"next\"", StringComparison.OrdinalIgnoreCase) || linkHeader.Contains("rel=next", StringComparison.OrdinalIgnoreCase))
            {
                // Extract URL - this is simplified, real impl would parse properly
                var startIndex = linkHeader.IndexOf('<');
                var endIndex = linkHeader.IndexOf('>');
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    nextCursor = linkHeader.Substring(startIndex + 1, endIndex - startIndex - 1);
                }
            }
        }

        if (totalCount.HasValue || !string.IsNullOrEmpty(nextCursor))
        {
            return new RestPaginationInfo(totalCount, nextCursor);
        }

        return null;
    }

    /// <summary>
    /// Extracts the result from the response content.
    /// </summary>
    /// <param name="content">The response content.</param>
    /// <param name="resultType">The expected result type.</param>
    /// <param name="paginationInfo">Pagination info if available.</param>
    /// <param name="context">The protocol context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The extracted result.</returns>
    [ExcludeFromCodeCoverage(Justification = "Exception path requires malformed JSON that passes wrapper extraction")]
    protected virtual Task<IGenericResult<object?>> ExtractResultFromResponse(
        string content,
        Type resultType,
        RestPaginationInfo? paginationInfo,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            // Handle string result type
            if (resultType == typeof(string))
            {
                return Task.FromResult(GenericResult<object?>.Success(content));
            }

            // Try to extract data from common wrapper formats
            var dataContent = ExtractDataFromWrapper(content);

            var result = JsonSerializer.Deserialize(dataContent, resultType, DefaultJsonOptions);
            return Task.FromResult(GenericResult<object?>.Success(result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<object?>.Failure(
                HttpResultCodes.ByName("RestDeserializationFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message)));
        }
    }

    /// <summary>
    /// Extracts the data portion from common API wrapper formats.
    /// </summary>
    /// <param name="content">The full response content.</param>
    /// <returns>The data portion of the response.</returns>
    protected virtual string ExtractDataFromWrapper(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Check for common wrapper properties
            if (root.TryGetProperty("data", out var data))
            {
                return data.GetRawText();
            }

            if (root.TryGetProperty("results", out var results))
            {
                return results.GetRawText();
            }

            if (root.TryGetProperty("items", out var items))
            {
                return items.GetRawText();
            }

            if (root.TryGetProperty("value", out var value))
            {
                return value.GetRawText();
            }
        }
        catch (JsonException ex)
        {
            // Expected: content is not valid JSON or has no wrapper — return original. No logger on protocol type-options.
            _ = ex;
        }
        catch (InvalidOperationException ex)
        {
            // Expected: JSON is not an object (TryGetProperty on a non-object throws) — return original.
            _ = ex;
        }

        return content;
    }

    #endregion

    #region Format-driven row reader resolution

    /// <summary>
    /// The part of a JSON response the rows are read from. A container that names a RecordSelector says where
    /// they are; one that names none has this protocol's collection wrapper taken off (OData's value; data,
    /// results, items or value for REST), the same unwrapping a deserialized read gets.
    /// </summary>
    /// <remarks>
    /// Why: without it a wrapped response had no array at its root, and the row reader read it as no rows at
    /// all, with no error. A bare array, and any format other than JSON, is read as it came.
    /// </remarks>
    private string RowContent(string content, IStorageContainer container)
        => container.Format is { Name: "Json" }
           && !(container.Metadata.TryGetValue("RecordSelector", out var selector) && selector is string { Length: > 0 })
           && content.AsSpan().TrimStart().StartsWith("{", StringComparison.Ordinal)
            ? ExtractDataFromWrapper(content)
            : content;

    /// <summary>
    /// The generated mapper of the record a collection result is made of, when <paramref name="resultType"/>
    /// is a collection of a <c>[GenerateMapper]</c> record that the mapper's own list satisfies; otherwise null,
    /// and the result is deserialized as before.
    /// </summary>
    private static IPocoMapper? RecordMapperFor(Type resultType)
    {
        if (!resultType.IsGenericType || resultType.GetGenericArguments() is not [var element])
            return null;

        var mapper = PocoMapperCollection.ByName(element.Name);
        return mapper != PocoMapperCollection.NotFound
               && mapper.TargetType == element
               && resultType.IsAssignableFrom(mapper.CreateList().GetType())
            ? mapper
            : null;
    }

    /// <summary>
    /// Reads a collection of <c>[GenerateMapper]</c> records the way the record connectors read one: the
    /// response's rows, checked against the fields the container declares, then materialized through
    /// <see cref="RecordRowMaterializer"/>, whose reader coerces each value to its property's type.
    /// </summary>
    /// <remarks>
    /// Why not the deserializer: it honours no [Column] name and cannot read a nested JSON object into a string
    /// property, so a record whose source nests (YouTrack's reporter and custom fields) could not be read at
    /// all; the row reader keeps a nested value as its raw JSON text. Why not MapFromDictionary: it casts
    /// without converting, and a JSON number arrives as a long, so an int property would fail.
    /// </remarks>
    private IGenericResult<object?> MaterializeRecords(
        string content,
        IStorageContainer container,
        IPocoMapper mapper,
        HttpProtocolContext context)
    {
        if (container is not IDataContainer declared)
            return GenericResult<object?>.Failure(
                HttpResultCodes.ByName("ResponseRowExtractionFailed"),
                ResultDetails.Create().With("ErrorMessage",
                    $"'{container.Name}' is not a data container, and records of '{mapper.TargetType.Name}' are checked against a data container's declared fields."));

        var extracted = ExtractRowsFromContent(content, container);
        if (!extracted.IsSuccess || extracted.Value is not IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
            return extracted;

        var logger = context.LoggerFactory.CreateLogger<RestProtocolBase>();
        var valid = RecordRowValidator.Validate(rows, declared, logger);
        if (!valid.IsSuccess)
            return valid.ToNewResult<object?>();

        var records = RecordRowMaterializer.MaterializeByType(rows, mapper.TargetType, container, logger);
        if (!records.IsSuccess)
            return records.ToNewResult<object?>();

        var list = mapper.CreateList();
        foreach (var record in records.Value!)
            list.Add(record);

        return GenericResult<object?>.Success(list);
    }

    /// <inheritdoc />
    protected override bool HasRowReaderForFormat(IStorageContainer container)
    {
        if (container.Format is null) return false;
        return RecordSourceTypes.ByName(container.Format.Family) != RecordSourceTypes.NotFound;
    }

    /// <inheritdoc />
    protected override IRowSourceReader? TryCreateRowReader(IStorageContainer container, Stream content)
    {
        if (container.Format is null) return null;

        var sourceType = RecordSourceTypes.ByName(container.Format.Family);
        if (sourceType == RecordSourceTypes.NotFound) return null;

        return sourceType.CreateReader(content, BuildRowSourceOptions(container));
    }

    private static RowSourceOptions? BuildRowSourceOptions(IStorageContainer container)
        => container.Format.Family switch
        {
            "Json" => BuildJsonOptions(container.Metadata),
            "Xml" => BuildXmlOptions(container.Metadata),
            "Delimited" => BuildDelimitedOptions(container),
            "FixedWidth" => BuildFixedWidthOptions(container),
            // Unknown/registered-elsewhere format: let the type use its own defaults.
            _ => null
        };

    private static JsonRowSourceOptions BuildJsonOptions(IReadOnlyDictionary<string, object> meta)
        => new()
        {
            RowArrayPath = meta.TryGetValue("RecordSelector", out var sel) ? sel as string : null,
            FlattenNestedObjects = meta.TryGetValue("FlattenNestedObjects", out var fn) && fn is bool fb && fb,
            FlattenSeparator = meta.TryGetValue("FlattenSeparator", out var fs) && fs is string s && !string.IsNullOrEmpty(s) ? s : "."
        };

    private static XmlRowSourceOptions BuildXmlOptions(IReadOnlyDictionary<string, object> meta)
        => new()
        {
            RowElementName = meta.TryGetValue("RowElementName", out var rn) ? rn as string : null,
            RowElementPath = meta.TryGetValue("RecordSelector", out var rp) ? rp as string : null
        };

    private static DelimitedRowSourceOptions BuildDelimitedOptions(IStorageContainer container)
    {
        var meta = container.Metadata;
        var options = new DelimitedRowSourceOptions
        {
            HasHeader = meta.TryGetValue("HasHeader", out var hh) && hh is bool hb && hb,
            Separator = meta.TryGetValue("Separator", out var sep) && sep is string ss && !string.IsNullOrEmpty(ss) ? ss : ","
        };
        options.Columns = new List<string>(FieldNames(container));
        return options;
    }

    private static FixedWidthRowSourceOptions BuildFixedWidthOptions(IStorageContainer container)
    {
        var meta = container.Metadata;
        var options = new FixedWidthRowSourceOptions
        {
            HasHeader = meta.TryGetValue("HasHeader", out var hh) && hh is bool hb && hb
        };
        options.Fields = new List<FixedWidthField>(FixedWidthFields(container));
        return options;
    }

    private static IEnumerable<string> FieldNames(IStorageContainer container)
    {
        var fields = container.Schema?.Fields;
        if (fields is null) yield break;
        foreach (var field in fields) yield return field.Name;
    }

    private static IEnumerable<FixedWidthField> FixedWidthFields(IStorageContainer container)
    {
        var fields = container.Schema?.Fields;
        if (fields is null) yield break;
        foreach (var field in fields)
        {
            var fieldMeta = field.Metadata;
            if (fieldMeta is null
                || !fieldMeta.TryGetValue("StartIndex", out var startObj)
                || !fieldMeta.TryGetValue("Length", out var lenObj))
            {
                continue;
            }

            yield return new FixedWidthField
            {
                Name = field.Name,
                StartIndex = Convert.ToInt32(startObj, System.Globalization.CultureInfo.InvariantCulture),
                Length = Convert.ToInt32(lenObj, System.Globalization.CultureInfo.InvariantCulture)
            };
        }
    }

    #endregion

    /// <inheritdoc />
    /// <remarks>
    /// BuildFilterFromExpression, the ordering builder and the pagination builder all write into the
    /// query string, and JsonApi and OData inherit or refine them. Honest per
    /// <see cref="SupportsOperator"/>, <see cref="SupportsGroup"/> and <see cref="SupportsNestedGroups"/>
    /// rather than a blanket <see langword="true"/> — see <see cref="Translate"/>, which refuses a
    /// request any of those three decline instead of mis-rendering or silently dropping part of it.
    /// </remarks>
    public override bool CanExpressFilter(IFilterExpression filter)
        => filter.Root is null || FirstUnsupportedNode(filter.Root) is null;

    /// <inheritdoc />
    public override bool CanExpressOrdering(IOrderingExpression ordering) => true;

    /// <inheritdoc />
    public override bool CanExpressPaging(IPagingExpression paging) => true;
}
