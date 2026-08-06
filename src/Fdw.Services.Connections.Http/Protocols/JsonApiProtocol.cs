using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;

namespace Fdw.Services.Connections.Http.Protocols;

/// <summary>
/// JSON:API specification protocol implementation.
/// </summary>
/// <remarks>
/// <para>
/// Uses JSON:API conventions (https://jsonapi.org):
/// <list type="bullet">
/// <item><description>Pagination: page[number]=3&amp;page[size]=10</description></item>
/// <item><description>Ordering: sort=name,-created_at</description></item>
/// <item><description>Filtering: filter[name]=value</description></item>
/// <item><description>Field selection: fields[type]=id,name</description></item>
/// <item><description>Response wrapper: { "data": [...], "meta": {...}, "links": {...} }</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpProtocols), "JsonApi")]
public sealed class JsonApiProtocol : RestProtocolBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonApiProtocol"/> class.
    /// </summary>
    public JsonApiProtocol()
        : base(6, "JsonApi", "JSON:API specification with page[number], filter[field], sort conventions")
    {
    }

    /// <inheritdoc/>
    protected override string BuildPaginationQueryString(IQueryCommand command, HttpProtocolContext context)
    {
        if (command.Paging is null)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        // JSON:API uses page-based pagination
        var pageSize = command.Paging.Take > 0 ? command.Paging.Take : 25;
        var pageNumber = (command.Paging.Skip / pageSize) + 1;

        parts.Add($"page[number]={pageNumber}");
        parts.Add($"page[size]={pageSize}");

        return string.Join("&", parts);
    }

    /// <inheritdoc/>
    protected override string BuildFilterFromExpression(IFilterNode node)
    {
        if (node is IFilterCondition condition)
        {
            var encodedValue = System.Web.HttpUtility.UrlEncode(condition.Value?.ToString() ?? string.Empty);
            return $"filter[{condition.PropertyName}]={encodedValue}";
        }

        if (node is FilterGroup group)
        {
            var parts = new List<string>();
            foreach (var child in group.Nodes)
            {
                if (child is IFilterCondition childCondition)
                {
                    var encodedValue = System.Web.HttpUtility.UrlEncode(childCondition.Value?.ToString() ?? string.Empty);
                    parts.Add($"filter[{childCondition.PropertyName}]={encodedValue}");
                }
            }
            return string.Join("&", parts);
        }

        return string.Empty;
    }

    /// <inheritdoc/>
    protected override RestPaginationInfo? ExtractPaginationInfo(
        System.Net.Http.HttpResponseMessage response,
        string content,
        HttpProtocolContext context)
    {
        // JSON:API provides pagination in meta and links
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            var root = doc.RootElement;

            int? totalCount = null;
            string? nextCursor = null;

            // Check meta for total
            if (root.TryGetProperty("meta", out var meta))
            {
                if (meta.TryGetProperty("total", out var total) && total.TryGetInt32(out var totalValue))
                {
                    totalCount = totalValue;
                }
            }

            // Check links for next
            if (root.TryGetProperty("links", out var links))
            {
                if (links.TryGetProperty("next", out var next) && next.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    nextCursor = next.GetString();
                }
            }

            if (totalCount.HasValue || !string.IsNullOrEmpty(nextCursor))
            {
                return new RestPaginationInfo(totalCount, nextCursor);
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            // Expected: content is not a valid JSON:API response — fall back to base extraction.
            // No logger on protocol type-options (singletons); the specific catch + fallback IS the outcome.
            _ = ex;
        }
        catch (System.InvalidOperationException ex)
        {
            // Expected: JSON is not the anticipated object shape — fall back to base extraction.
            _ = ex;
        }

        return base.ExtractPaginationInfo(response, content, context);
    }

    /// <inheritdoc/>
    protected override string ExtractDataFromWrapper(string content)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            var root = doc.RootElement;

            // JSON:API always uses "data"
            if (root.TryGetProperty("data", out var data))
            {
                return data.GetRawText();
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            // Expected: content is not valid JSON — return original. No logger on protocol type-options.
            _ = ex;
        }
        catch (System.InvalidOperationException ex)
        {
            // Expected: JSON is not an object (TryGetProperty on a non-object throws) — return original.
            _ = ex;
        }

        return content;
    }
}