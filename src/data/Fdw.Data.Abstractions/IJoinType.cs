using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Defines a type of join operation between data sources.
/// </summary>
/// <remarks>
/// Inherits Id, Name, and Description from ITypeOption.
/// </remarks>
public interface IJoinType : ITypeOption<int, JoinTypeBase>
{
    /// <summary>
    /// Gets the SQL keyword for this join type (e.g., "INNER JOIN", "LEFT JOIN").
    /// </summary>
    string SqlKeyword { get; }

    /// <summary>
    /// Gets whether this join type requires join conditions.
    /// </summary>
    /// <remarks>CROSS JOIN does not require conditions.</remarks>
    bool RequiresConditions { get; }

    /// <summary>
    /// Gets whether this join type includes all records from the left source.
    /// </summary>
    bool IncludesAllLeft { get; }

    /// <summary>
    /// Gets whether this join type includes all records from the right source.
    /// </summary>
    bool IncludesAllRight { get; }
}
