using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// Joins a pushed side to the one container its right or reference input reads (FDW-6969): both in one data store record and path, every container
/// read once, the key pairs matched in the store as in memory, all left keys from one container.
/// </summary>
public static class PushdownJoins
{
    /// <summary>The field a pushed full join adds to say whether a row has a left side: a key column of its primary container.</summary>
    public const string LeftPresentField = "fdw_pushdown_left_present";

    /// <summary>The field a pushed lookup adds to say whether a row found its reference row: a key column of the reference container.</summary>
    public const string ReferencePresentField = "fdw_pushdown_reference_present";

    /// <summary>Checks that two sides may be one query.</summary>
    /// <param name="left">The left side.</param>
    /// <param name="right">The side that reads one container.</param>
    /// <returns>Why they may not, or empty.</returns>
    public static string OneQuery(PushdownSide left, PushdownSide right)
    {
        if (left.DataStoreId != right.DataStoreId)
            return "its inputs read two data stores; a join across stores runs in memory (FDW-6970)";
        if (!string.Equals(left.Primary.Path, right.Primary.Path, StringComparison.OrdinalIgnoreCase))
            return "its inputs read two paths of the store";
        return left.Containers.Contains(right.Primary.Container, StringComparer.OrdinalIgnoreCase)
            ? $"it reads container '{right.Primary.Container}' twice, which one statement cannot name apart"
            : string.Empty;
    }

    /// <summary>Pairs the key fields of the two sides as the columns a join's conditions name.</summary>
    /// <param name="left">The left side.</param>
    /// <param name="right">The side that reads one container.</param>
    /// <param name="leftKeys">The left key fields, in order.</param>
    /// <param name="rightKeys">The right key fields, in the same order.</param>
    /// <returns>The column pairs and the container the left columns belong to, or why the keys are not pushed.</returns>
    public static (IReadOnlyList<(string Left, string Right)> Pairs, string LeftContainer, string Reason) Keys(
        PushdownSide left, PushdownSide right, IReadOnlyList<string> leftKeys, IReadOnlyList<string> rightKeys)
    {
        if (leftKeys.Count == 0 || leftKeys.Count != rightKeys.Count)
            return ([], string.Empty, "it states no key fields, or not as many on each side");

        var pairs = new List<(PushdownColumn Left, PushdownColumn Right)>(leftKeys.Count);
        for (var index = 0; index < leftKeys.Count; index++)
        {
            if (left.Field(leftKeys[index]) is not { } leftKey || right.Field(rightKeys[index]) is not { } rightKey)
                return ([], string.Empty, $"its key pair '{leftKeys[index]}' = '{rightKeys[index]}' names a field its input does not carry");

            var judged = PushdownKeyTypes.Judge(leftKey.ClrType, rightKey.ClrType);
            if (judged.Length > 0)
                return ([], string.Empty, $"its key pair '{leftKeys[index]}' = '{rightKeys[index]}': {judged}");

            pairs.Add((leftKey, rightKey));
        }

        var containers = pairs.Select(pair => pair.Left.Container).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return containers.Count == 1
            ? ([.. pairs.Select(pair => (pair.Left.Column, pair.Right.Column))], containers[0], string.Empty)
            : ([], string.Empty, "its left key fields come from more than one container, which one join condition cannot state");
    }

    /// <summary>States the join of a side to the container the other side reads.</summary>
    /// <param name="left">The left side.</param>
    /// <param name="right">The side that reads one container.</param>
    /// <param name="type">The join type.</param>
    /// <param name="pairs">The column pairs.</param>
    /// <param name="leftContainer">The container the left columns belong to.</param>
    /// <returns>The join.</returns>
    public static IJoinExpression Join(PushdownSide left, PushdownSide right, IJoinType type, IReadOnlyList<(string Left, string Right)> pairs, string leftContainer) =>
        new JoinExpression
        {
            TargetContainerName = right.Primary.Container,
            // The join continues from the container its left columns belong to; the primary needs no name, as the translators read it.
            SourceContainerName = string.Equals(leftContainer, left.Primary.Container, StringComparison.OrdinalIgnoreCase) ? null : leftContainer,
            JoinType = type.Name,
            JoinConditions = pairs,
        };

    /// <summary>Finds the first field name two lists of fields share, or that the planner's own fields take.</summary>
    /// <param name="left">The fields of the left side.</param>
    /// <param name="added">The fields added to them.</param>
    /// <returns>The first name that collides or that a statement does not write as it is, or null.</returns>
    public static string? Collision(IReadOnlyList<PushdownColumn> left, IReadOnlyList<PushdownColumn> added)
    {
        var names = new HashSet<string>(left.Select(field => field.Name), StringComparer.OrdinalIgnoreCase) { LeftPresentField, ReferencePresentField };
        return added.Select(field => field.Name).FirstOrDefault(name => !names.Add(name) || !PlainIdentifiers.IsPlain(name));
    }
}
