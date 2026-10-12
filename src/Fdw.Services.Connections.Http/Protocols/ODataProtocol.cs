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
        : base("OData", "OData protocol with $filter, $orderby, $skip, $top query options")
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

    /// <summary>
    /// Operators whose OData v4 shape is a function call (<c>contains(Field,'x')</c>) rather than an
    /// infix expression (<c>Field eq 'x'</c>). Each one's own <see cref="IFilterOperator.ODataOperator"/>
    /// IS the function name ("contains"/"startswith"/"endswith") -- this set decides the GRAMMAR
    /// (call vs. infix), not what the operator means, so it is not the kind of re-derived mapping
    /// <see cref="IFilterOperator"/> dispatch exists to replace.
    /// </summary>
    private static readonly HashSet<string> FunctionStyleOperators =
        new(System.StringComparer.Ordinal) { "Contains", "StartsWith", "EndsWith" };

    /// <inheritdoc/>
    /// <remarks>
    /// The operator symbol comes from <see cref="IFilterOperator.ODataOperator"/> rather than a
    /// hand-rolled switch -- every registered operator (including ones with no obvious "name eq
    /// value" shape, like IsNull's "eq null") already knows its own OData representation. Re-deriving
    /// that mapping locally is exactly the drift a TypeCollection dispatch exists to prevent: an
    /// operator missing from a local switch silently fell back to "eq" before this changed.
    /// <para>
    /// The VALUE is formatted locally rather than via <see cref="IFilterOperator.FormatODataValue"/>,
    /// which wraps Guid/DateTime in OData v3 literal syntax (<c>guid'...'</c>, <c>datetime'...'</c>);
    /// this codebase's OData targets v4 (see <c>ODataSchemaImporter</c>), where those are written as
    /// bare literals -- the v3 wrapper is what a v4 server rejects.
    /// </para>
    /// <para>
    /// The whole expression is percent-encoded as one unit (not per-value) so a value containing
    /// <c>+</c>, <c>&amp;</c> or <c>#</c> cannot corrupt the querystring it is embedded in or get
    /// silently decoded as a different character.
    /// </para>
    /// </remarks>
    protected override string BuildFilterFromExpression(IFilterNode node)
    {
        if (node is IFilterCondition condition)
        {
            var op = condition.Operator;
            var expression = op.RequiresValue
                ? RenderCondition(op, condition.PropertyName, condition.Value)
                : $"{condition.PropertyName} {op.ODataOperator}";

            return $"$filter={System.Uri.EscapeDataString(expression)}";
        }

        // For complex expressions, fall back to base
        return base.BuildFilterFromExpression(node);
    }

    private static string RenderCondition(IFilterOperator op, string propertyName, object? value)
    {
        var formattedValue = FormatValue(value);
        return FunctionStyleOperators.Contains(op.Name)
            ? $"{op.ODataOperator}({propertyName},{formattedValue})"
            : $"{propertyName} {op.ODataOperator} {formattedValue}";
    }

    /// <summary>Formats a single value as an OData v4 literal.</summary>
    /// <remarks>
    /// String/bool/null match OData v4 literal syntax exactly, with the single-quote escaping the
    /// code this replaced did not have. An IEnumerable (In/NotIn's array value) joins its own elements
    /// through this same formatter into "(a,b,c)" -- each element gets the correct v4 literal too, so
    /// a Guid inside the array renders bare rather than inheriting the shared FormatODataValue's v3
    /// wrapper. Everything else (numbers, Guid, DateTime) renders via its own <c>ToString()</c>, which
    /// is correct for numbers and a bare Guid; DateTime/DateTimeOffset need their own ISO-8601
    /// formatting to be v4-correct and culture-independent -- not fixed by this change, tracked on
    /// FDW-915. A null <c>ToString()</c> result (never observed on a BCL type, but the return is
    /// nullable) fails loud here rather than being defaulted to empty text -- Translate's catch-all
    /// turns it into a proper refusal instead of silently sending a filter that looks valid.
    /// </remarks>
    private static string FormatValue(object? value)
    {
        switch (value)
        {
            case null:
                return "null";
            case string s:
                return $"'{s.Replace("'", "''", System.StringComparison.Ordinal)}'";
            case bool b:
                return b.ToString().ToLowerInvariant();
            case System.Collections.IEnumerable items:
                var parts = new List<string>();
                foreach (var item in items)
                    parts.Add(FormatValue(item));
                return $"({string.Join(",", parts)})";
            default:
                return value.ToString()
                    ?? throw new System.InvalidOperationException(
                        $"'{value.GetType().Name}'.ToString() returned null while formatting an OData filter value.");
        }
    }

    /// <inheritdoc/>
    /// <remarks>Every operator carries its own <see cref="IFilterOperator.ODataOperator"/>, so none are refused.</remarks>
    protected override bool SupportsOperator(string operatorName) => true;

    /// <inheritdoc/>
    /// <remarks>
    /// A single "$filter=" parameter can hold exactly one condition. Rendering more -- AND or OR --
    /// correctly needs "$filter=A eq 1 and B eq 2" syntax this protocol does not build yet; without
    /// this override, the base's per-child "&amp;"-joined "$filter=...&amp;$filter=..." fallback would
    /// go out under a CanExpressFilter that claimed it was fine. Refusing keeps that promise honest
    /// until group rendering is written (tracked separately, not by this change).
    /// </remarks>
    protected override bool SupportsGroup(LogicalOperator groupOperator, int childCount) => childCount <= 1;

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