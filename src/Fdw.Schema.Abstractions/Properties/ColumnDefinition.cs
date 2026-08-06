#pragma warning disable CS1591
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Schema.Properties;

/// <summary>
/// Default implementation of <see cref="IColumnDefinition"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ColumnDefinition : IColumnDefinition
{
    public required string Name { get; init; }
    public required IPropertyRole Role { get; init; }
    public bool IsRequired { get; init; }
    public string? Description { get; init; }
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    public SqlDbType SqlType { get; init; } = SqlDbType.NVarChar;
    public int? MaxLength { get; init; }
    public int? Precision { get; init; }
    public int? Scale { get; init; }
    public bool IsIdentity { get; init; }
    public string? DefaultExpression { get; init; }
    public string? ComputedExpression { get; init; }
    public string? Collation { get; init; }
}
