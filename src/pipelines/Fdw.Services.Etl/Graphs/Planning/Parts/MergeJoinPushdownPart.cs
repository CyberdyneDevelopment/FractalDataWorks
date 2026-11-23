using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// A merge join pushed into its store (FDW-6969): its left side (a container, or a join pushed with it) joined to the container its right input
/// reads, on its key pairs, by the join type that keeps the same unmatched sides. A join that keeps the right rows nothing matched is pushed only
/// at the top, where the pushed query sends those rows last as the in-memory join does.
/// </summary>
[TypeOption(typeof(PushdownParts), "MergeJoin")]
public sealed class MergeJoinPushdownPart : PushdownPartBase
{
    /// <summary>Initializes a new instance of the <see cref="MergeJoinPushdownPart"/> class.</summary>
    public MergeJoinPushdownPart() : base("MergeJoin", true, FlowMergeJoin.OutputName, FlowMergeJoin.RightName)
    {
    }

    /// <inheritdoc />
    public override async Task<PushdownAttempt> Resolve(
        PushdownScope scope, PipelineComponentConfiguration component, bool top, CancellationToken cancellationToken = default)
    {
        var plain = PushdownChecks.Plain(scope, component);
        if (plain.Length > 0 || component.JoinKind is not { } kindName || component.RightPrefix is not { } prefix || component.BatchSize is not { } batchSize)
            return PushdownAttempt.Stays(plain.Length > 0 ? plain : "it does not state its join kind, right prefix and batch size");

        var kind = JoinKinds.ByName(kindName);
        if (ReferenceEquals(kind, JoinKinds.NotFound) || PushdownJoinTypes.Keeping(kind.KeepsUnmatchedLeft, kind.KeepsUnmatchedRight) is not { } type)
            return PushdownAttempt.Stays($"no join type a statement performs keeps the rows its join kind '{kindName}' keeps");

        if (!top && kind.KeepsUnmatchedRight)
            return PushdownAttempt.Stays("it keeps the right rows nothing matched, which go last, so it is pushed only where its rows leave the query");

        var left = await PushdownInputs.Side(scope, component, FlowMergeJoin.LeftName, true, cancellationToken).ConfigureAwait(false);
        if (left.Side is not { } leftSide)
            return left;

        var right = await PushdownInputs.Side(scope, component, FlowMergeJoin.RightName, false, cancellationToken).ConfigureAwait(false);
        if (right.Side is not { } rightSide)
            return right;

        return Joined(component, leftSide, rightSide, type, prefix, top && kind.KeepsUnmatchedRight) with { BatchSize = batchSize };
    }

    private static PushdownAttempt Joined(
        PipelineComponentConfiguration component, PushdownSide left, PushdownSide right, IJoinType type, string prefix, bool rightRowsLast)
    {
        var oneQuery = PushdownJoins.OneQuery(left, right);
        if (oneQuery.Length > 0)
            return PushdownAttempt.Stays(oneQuery);

        var (pairs, leftContainer, reason) = PushdownJoins.Keys(left, right, Columns(component, "leftKeys"), Columns(component, "rightKeys"));
        if (reason.Length > 0)
            return PushdownAttempt.Stays(reason);

        PushdownColumn[] renamed = [.. right.Fields.Select(field => field with { Name = prefix + field.Name })];
        if (PushdownJoins.Collision(left.Fields, renamed) is { } collision)
            return PushdownAttempt.Stays($"its output would hold field '{collision}' twice or under a name a statement does not write as it is");

        var side = new PushdownSide(
            left.DataStoreId,
            left.Primary,
            left.PrimaryKey,
            [.. left.Fields, .. renamed],
            [.. left.Joins, PushdownJoins.Join(left, right, type, pairs, leftContainer)],
            [.. left.Order, .. right.Order],
            [.. left.Containers, .. right.Containers],
            [.. left.Members, .. right.Members, component.Name],
            []);
        return rightRowsLast
            ? new PushdownAttempt(
                side,
                string.Empty,
                new PushdownColumn(PushdownJoins.LeftPresentField, left.Primary.Container, left.PrimaryKey[0], null),
                new FlowPushedAbsence(PushdownJoins.LeftPresentField, [], true, null, []))
            : PushdownAttempt.Pushed(side);
    }

    private static string[] Columns(PipelineComponentConfiguration component, string setting) =>
        [.. component.Columns
            .Where(column => string.Equals(column.SettingName, setting, StringComparison.OrdinalIgnoreCase))
            .OrderBy(column => column.Ordinal)
            .Select(column => column.ColumnName)];
}
