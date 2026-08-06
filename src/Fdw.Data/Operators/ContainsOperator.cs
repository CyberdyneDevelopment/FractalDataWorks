using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// Contains operator (LIKE '%value%', contains).
/// Overrides FormatSqlParameter to add wildcards for SQL LIKE.
/// </summary>
[TypeOption(typeof(FilterOperators), "Contains", RestrictToCurrentCompilation = true)]
public sealed class ContainsOperator : FilterOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainsOperator"/> class.
    /// </summary>
    public ContainsOperator()
        : base(
            id: 3,
            name: "Contains",
            sqlOperator: "LIKE",
            odataOperator: "contains",
            requiresValue: true)
    {
    }

    /// <summary>
    /// Formats SQL parameter with wildcards for LIKE pattern matching.
    /// </summary>
    public override string FormatSqlParameter(string paramName) => $"'%' + @{paramName} + '%'";

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
        return value switch
        {
            string str => $"'{str.Replace("'", "''")}'",
            _ => $"'{value}'"
        };
    }
}
