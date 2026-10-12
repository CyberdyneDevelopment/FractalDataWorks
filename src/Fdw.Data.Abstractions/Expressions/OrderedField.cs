namespace Fdw.Data.Abstractions;

/// <summary>
/// Represents a single ordered field (property and direction).
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record OrderedField : IOrderedField
{
    /// <summary>
    /// Gets the property name to order by.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Gets the sort direction.
    /// This is a SortDirection TypeCollection, not a traditional enum!
    /// </summary>
    public required ISortDirection Direction { get; init; }
}
