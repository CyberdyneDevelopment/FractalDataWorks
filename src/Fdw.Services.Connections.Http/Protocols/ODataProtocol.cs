using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;

namespace Fdw.Services.Connections.Http.Protocols;

/// <summary>
/// OData-style REST protocol implementation.
/// </summary>
/// <remarks>
/// <para>
/// Uses OData query conventions:
/// <list type="bullet">
/// <item><description>Pagination: $skip=20&amp;$top=10</description></item>
/// <item><description>Ordering: $orderby=name asc,createdAt desc</description></item>
/// <item><description>Filtering: $filter=name eq 'value'</description></item>
/// <item><description>Field selection: $select=id,name,email</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpProtocols), "OData")]
public sealed class ODataProtocol : RestProtocolBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ODataProtocol"/> class.
    /// </summary>
    public ODataProtocol()
        : base(5, "OData", "OData protocol with $filter, $orderby, $skip, $top query options")
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

        if (command.Paging.Skip > 0)
        {
            parts.Add($"$skip={command.Paging.Skip}");
        }

        if (command.Paging.Take > 0)
        {
            parts.Add($"$top={command.Paging.Take}");
        }

        return string.Join("&", parts);
    }

    /// <inheritdoc/>
    protected override string BuildOrderingQueryString(IQueryCommand command, HttpProtocolContext context)
    {
        if (command.Ordering?.OrderedFields is null || command.Ordering.OrderedFields.Count == 0)
        {
            return string.Empty;
        }

        var orderParts = new List<string>();
        foreach (var field in command.Ordering.OrderedFields)
        {
            var direction = string.Equals(field.Direction.Name, "Descending", System.StringComparison.Ordinal) ? " desc" : " asc";
            orderParts.Add($"{field.PropertyName}{direction}");
        }

        return $"$orderby={string.Join(",", orderParts)}";
    }

    /// <inheritdoc/>
    protected override string BuildFilterFromExpression(IFilterNode node)
    {
        if (node is IFilterCondition condition)
        {
            var value = condition.Value;
            var formattedValue = value switch
            {
                string s => $"'{s}'",
                bool b => b.ToString().ToLowerInvariant(),
                null => "null",
                _ => value.ToString()
            };

            var op = MapOperatorToOData(condition.Operator?.Name ?? "Equal");
            return $"$filter={condition.PropertyName} {op} {formattedValue}";
        }

        // For complex expressions, fall back to base
        return base.BuildFilterFromExpression(node);
    }

    private static string MapOperatorToOData(string operatorName)
    {
        return operatorName switch
        {
            "Equal" => "eq",
            "NotEqual" => "ne",
            "GreaterThan" => "gt",
            "GreaterThanOrEqual" => "ge",
            "LessThan" => "lt",
            "LessThanOrEqual" => "le",
            "Contains" => "contains",
            "StartsWith" => "startswith",
            "EndsWith" => "endswith",
            _ => "eq"
        };
    }

    /// <inheritdoc/>
    protected override string ExtractDataFromWrapper(string content)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            var root = doc.RootElement;

            // OData uses "value" for collections
            if (root.TryGetProperty("value", out var value))
            {
                return value.GetRawText();
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