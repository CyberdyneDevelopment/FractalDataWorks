#pragma warning disable CS1591
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Schema.Properties;

namespace Fdw.Schema.Indexes;

/// <summary>
/// Concrete implementation of <see cref="IIndexDefinition{TProperty}"/>.
/// </summary>
/// <typeparam name="TProperty">The property definition type.</typeparam>
[ExcludeFromCodeCoverage]
public sealed class IndexDefinition<TProperty> : IIndexDefinition<TProperty>
    where TProperty : IPropertyDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IndexDefinition{TProperty}"/> class.
    /// </summary>
    /// <param name="name">The index name.</param>
    /// <param name="members">The index members in ordinal order.</param>
    /// <param name="isUnique">True if the index enforces uniqueness.</param>
    /// <param name="isClustered">True if this is a clustered index.</param>
    /// <param name="includeColumns">Optional list of columns to include in a covering index.</param>
    /// <param name="filterPredicate">Optional filter predicate for a filtered index.</param>
    public IndexDefinition(
        string name,
        IReadOnlyList<IndexMember> members,
        bool isUnique = false,
        bool isClustered = false,
        IReadOnlyList<string>? includeColumns = null,
        string? filterPredicate = null)
    {
        Name = name;
        Members = members;
        IsUnique = isUnique;
        IsClustered = isClustered;
        IncludeColumns = includeColumns;
        FilterPredicate = filterPredicate;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public IReadOnlyList<IndexMember> Members { get; }

    /// <inheritdoc/>
    public bool IsUnique { get; }

    /// <inheritdoc/>
    public bool IsClustered { get; }

    /// <inheritdoc/>
    public IReadOnlyList<string>? IncludeColumns { get; }

    /// <inheritdoc/>
    public string? FilterPredicate { get; }
}
