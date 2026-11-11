using System;
using System.Linq;

namespace Fdw.Data.Abstractions;

/// <summary>
/// The <see cref="JoinTypes"/> option a join states by name (FDW-5099): the one every reader of a join type resolves, so a stored data set
/// join, a query builder's join and an SQL translator all mean the same option by the same name.
/// </summary>
/// <remarks>
/// The name is matched ignoring case: the query builder states "INNER" and a stored data set "Inner", and both are the Inner option. A name
/// no option carries is <see cref="JoinTypes.NotFound"/>, never another option, so the reader refuses it with its own code.
/// </remarks>
public static class StatedJoinType
{
    /// <summary>Gets the option a join type name states.</summary>
    /// <param name="name">The join type as the join states it.</param>
    /// <returns>The option whose name matches, ignoring case, or <see cref="JoinTypes.NotFound"/> when none does or no name is stated.</returns>
    public static IJoinType Of(string? name) => string.IsNullOrWhiteSpace(name) ? JoinTypes.NotFound : Named(name.Trim());

    // The option carrying this name, ignoring case, or NotFound.
    private static IJoinType Named(string name)
    {
        var match = JoinTypes.All().FirstOrDefault(kind => string.Equals(kind.Name, name, StringComparison.OrdinalIgnoreCase));
        return match is null ? JoinTypes.NotFound : match;
    }

    /// <summary>Gets whether an option joins two sources: on a condition, or every row of one with every row of the other.</summary>
    /// <param name="kind">The option.</param>
    /// <returns>
    /// True for an option that joins on a condition (Inner, Left, Right, Full) or keeps both sides with none (Cross); false for one that
    /// does neither (None) and for <see cref="JoinTypes.NotFound"/>.
    /// </returns>
    public static bool Joins(IJoinType kind)
    {
        ArgumentNullException.ThrowIfNull(kind);
        return !ReferenceEquals(kind, JoinTypes.NotFound) && (kind.RequiresConditions || (kind.IncludesAllLeft && kind.IncludesAllRight));
    }
}
