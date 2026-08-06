namespace Fdw.Data.Abstractions;

/// <summary>
/// Represents a single projected field (column/property to select).
/// </summary>
// Why: pure positional record (DTO), auto-generated properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record ProjectionField
{
    /// <summary>
    /// Gets the property name to project.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Gets the alias for this field (optional).
    /// </summary>
    public string? Alias { get; init; }

    /// <summary>
    /// Gets the source container (table/view name) that owns this field.
    /// Set for multi-source compound queries to qualify each column: <c>[SourceContainer].[PropertyName] AS [Alias]</c>.
    /// Null for single-source queries where the translator applies a uniform table qualifier.
    /// </summary>
    // Why: per-field source qualifier is needed for compound (pushed-down JOIN) queries where columns
    // from different tables share the same physical name. The translator emits
    // [Container].[PhysicalColumn] AS [LogicalAlias] for each field. Single-source paths
    // never set this, so they render byte-for-byte the same as before.
    public string? SourceContainer { get; init; }
}
