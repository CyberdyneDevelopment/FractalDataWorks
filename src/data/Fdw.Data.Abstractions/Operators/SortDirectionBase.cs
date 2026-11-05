using Fdw.Collections;
using Fdw.Conventions;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Sort directions for ORDER BY clauses in data queries.
/// Provides extensible sort directions that can be translated to different backend implementations.
/// </summary>
public abstract class SortDirectionBase : TypeOptionBase<SortDirectionBase>, ISortDirection
{
    /// <summary>The category a type option states none of its own sits under.</summary>
    [PlatformDefault]
    private const string DefaultCategory = "Sort";

    /// <summary>
    /// Initializes a new instance of the <see cref="SortDirectionBase"/> class.
    /// </summary>
    /// <param name="name">The name of this sort direction.</param>
    /// <param name="description">The description of this sort direction.</param>
    /// <param name="isAscending">Whether this direction sorts in ascending order.</param>
    /// <param name="sqlKeyword">The SQL keyword representation of this direction.</param>
    /// <param name="category">The optional category for this sort direction.</param>
    protected SortDirectionBase(
        string name,
        string description,
        bool isAscending,
        string sqlKeyword,
        string? category = null)
        : base(name, $"Data:Operators:Sort:{name}", $"{name} Sort Direction", description, category ?? DefaultCategory)
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
