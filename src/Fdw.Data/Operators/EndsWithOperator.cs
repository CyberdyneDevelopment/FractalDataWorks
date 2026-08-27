using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// EndsWith operator (LIKE '%value', endswith).
/// </summary>
[TypeOption(typeof(FilterOperators), "EndsWith", RestrictToCurrentCompilation = true)]
public sealed class EndsWithOperator : FilterOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndsWithOperator"/> class.
    /// </summary>
    public EndsWithOperator()
        : base(
            id: 5,
            name: "EndsWith",
            sqlOperator: "LIKE",
            odataOperator: "endswith",
            requiresValue: true)
    {
    }

    /// <summary>
    /// Formats SQL parameter with leading wildcard.
    /// </summary>
    public override string FormatSqlParameter(string paramName) => $"'%' + @{paramName}";

    /// <summary>
    /// Escapes LIKE metacharacters so they are treated as literals.
    /// </summary>
    public override string PreprocessSqlValue(string value)
        => value.Replace("[", "[[]", StringComparison.Ordinal)
                .Replace("%", "[%]", StringComparison.Ordinal)
                .Replace("_", "[_]", StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string FormatODataValue(object? value)
    {
        return $"'{value?.ToString()?.Replace("'", "''")}'";
    }

    /// <inheritdoc />
    public override bool Matches(object? left, object? right)
        => FilterValueComparer.AsText(left).EndsWith(FilterValueComparer.AsText(right), StringComparison.OrdinalIgnoreCase);
}
