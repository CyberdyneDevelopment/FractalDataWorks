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
/// <item><description>Pagination: in the paging style the connection states, typically PageNumber with page[number] and page[size] (FDW-4938)</description></item>
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
        : base("JsonApi", "JSON:API specification with filter[field] and sort conventions; paging in the style the connection states")
    {
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
    /// <remarks>
    /// The group loop above only enumerates <see cref="IFilterCondition"/> children directly -- a
    /// nested <see cref="FilterGroup"/> child is silently skipped, not rendered, so a request built
    /// from And[And[A,B], C] would go out carrying only C's filter. Refusing here keeps that from
    /// happening quietly; teaching the loop to recurse into a nested group is a separate change.
    /// </remarks>
    protected override bool SupportsNestedGroups => false;

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