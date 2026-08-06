using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base class for join type implementations.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class JoinTypeBase : TypeOptionBase<int, JoinTypeBase>, IJoinType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JoinTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this join type.</param>
    /// <param name="name">The name of this join type.</param>
    /// <param name="sqlKeyword">The SQL keyword for this join type.</param>
    /// <param name="description">A description of this join type's behavior.</param>
    /// <param name="requiresConditions">Whether this join type requires join conditions.</param>
    /// <param name="includesAllLeft">Whether this join includes all records from the left source.</param>
    /// <param name="includesAllRight">Whether this join includes all records from the right source.</param>
    protected JoinTypeBase(
        int id,
        string name,
        string sqlKeyword,
        string description,
        bool requiresConditions,
        bool includesAllLeft,
        bool includesAllRight)
        : base(id, name, description)
    {
        SqlKeyword = sqlKeyword;
        RequiresConditions = requiresConditions;
        IncludesAllLeft = includesAllLeft;
        IncludesAllRight = includesAllRight;
    }

    /// <inheritdoc />
    public string SqlKeyword { get; }

    /// <inheritdoc />
    public bool RequiresConditions { get; }

    /// <inheritdoc />
    public bool IncludesAllLeft { get; }

    /// <inheritdoc />
    public bool IncludesAllRight { get; }
}
