namespace Fdw.Data.Abstractions;

/// <summary>
/// Represents a single projected field (column/property to select).
/// </summary>
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
    public string? SourceContainer { get; init; }
}
