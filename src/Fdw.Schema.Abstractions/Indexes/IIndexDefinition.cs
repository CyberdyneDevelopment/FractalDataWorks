#pragma warning disable CS1591
using System.Collections.Generic;
using Fdw.Schema.Properties;

namespace Fdw.Schema.Indexes;

/// <summary>
/// Defines an index on a schema.
/// </summary>
/// <typeparam name="TProperty">The property definition type.</typeparam>
public interface IIndexDefinition<TProperty> where TProperty : IPropertyDefinition
{
    /// <summary>
    /// Gets the index name.
    /// </summary>
    /// <remarks>
    /// For SQL: IX_TableName_ColumnName or similar naming convention.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Gets the index members (columns) in ordinal order.
    /// </summary>
    IReadOnlyList<IndexMember> Members { get; }

    /// <summary>
    /// Gets a value indicating whether this index enforces uniqueness.
    /// </summary>
    bool IsUnique { get; }

    /// <summary>
    /// Gets a value indicating whether this is a clustered index.
    /// </summary>
    /// <remarks>
    /// Only applicable to storage systems that support clustered indexes (e.g., SQL Server).
    /// </remarks>
    bool IsClustered { get; }

    /// <summary>
    /// Gets the optional list of columns to include in a covering index.
    /// </summary>
    /// <remarks>
    /// For SQL Server: INCLUDE clause columns.
    /// </remarks>
    IReadOnlyList<string>? IncludeColumns { get; }

    /// <summary>
    /// Gets an optional filter predicate for a filtered index.
    /// </summary>
    /// <remarks>
    /// For SQL Server: WHERE clause predicate (e.g., "IsDeleted = 0").
    /// </remarks>
    string? FilterPredicate { get; }
}
