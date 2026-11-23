using System.Linq;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// The join type a store states for an in-memory join (FDW-6969): the <see cref="JoinTypes"/> option that joins on a condition and keeps the same
/// unmatched sides, which both collections state of their options, matched by what each states and never by name.
/// </summary>
public static class PushdownJoinTypes
{
    /// <summary>Finds the join type that keeps these unmatched sides.</summary>
    /// <param name="keepsLeft">Whether the left rows nothing matched are kept.</param>
    /// <param name="keepsRight">Whether the right rows nothing matched are kept.</param>
    /// <returns>The one option an SQL statement performs that keeps those sides, or null when there is not exactly one.</returns>
    public static IJoinType? Keeping(bool keepsLeft, bool keepsRight)
    {
        var matching = JoinTypes.All()
            .Where(type => type.RequiresConditions && type.IncludesAllLeft == keepsLeft && type.IncludesAllRight == keepsRight && StatedJoinType.PerformedInSql(type))
            .ToList();
        return matching.Count == 1 ? matching[0] : null;
    }
}
