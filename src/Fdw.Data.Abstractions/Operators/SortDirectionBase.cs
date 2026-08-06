using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Sort directions for ORDER BY clauses in data queries.
/// Provides extensible sort directions that can be translated to different backend implementations.
/// </summary>
public abstract class SortDirectionBase : TypeOptionBase<int, SortDirectionBase>, ISortDirection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SortDirectionBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this sort direction.</param>
    /// <param name="name">The name of this sort direction.</param>
    /// <param name="description">The description of this sort direction.</param>
    /// <param name="isAscending">Whether this direction sorts in ascending order.</param>
    /// <param name="sqlKeyword">The SQL keyword representation of this direction.</param>
    /// <param name="category">The optional category for this sort direction.</param>
    protected SortDirectionBase(
        int id,
        string name,
        string description,
        bool isAscending,
        string sqlKeyword,
        string? category = null)
        : base(id, name, $"Data:Operators:Sort:{name}", $"{name} Sort Direction", description, category ?? "Sort")
    {
        IsAscending = isAscending;
        SqlKeyword = sqlKeyword;
    }

    /// <summary>
    /// Gets the SQL keyword representation of this direction.
    /// Used when translating queries to SQL databases.
    /// </summary>
    public string SqlKeyword { get; }

    /// <summary>
    /// Gets a value indicating whether this direction sorts in ascending order.
    /// </summary>
    public bool IsAscending { get; }
}
