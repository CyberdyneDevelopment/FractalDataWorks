using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// StartsWith operator (LIKE 'value%', startswith).
/// </summary>
[TypeOption(typeof(FilterOperators), "StartsWith", RestrictToCurrentCompilation = true)]
public sealed class StartsWithOperator : FilterOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartsWithOperator"/> class.
    /// </summary>
    public StartsWithOperator()
        : base(
            id: 4,
            name: "StartsWith",
            sqlOperator: "LIKE",
            odataOperator: "startswith",
            requiresValue: true)
    {
    }

    /// <summary>
    /// Formats SQL parameter with trailing wildcard.
    /// </summary>
    public override string FormatSqlParameter(string paramName) => $"@{paramName} + '%'";

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
}
