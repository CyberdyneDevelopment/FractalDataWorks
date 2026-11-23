using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Flow;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// A lookup pushed into its store (FDW-6969): its main side left-joined to the container its reference input reads, bringing the fields its bring
/// mappings name. Pushed only at the top, where a row that finds no reference row goes to the unmatched output or fails the run as in memory, and
/// only when its reference keys are the reference container's primary key: the in-memory lookup fails on a repeated reference key, and a store with
/// that key holds none.
/// </summary>
[TypeOption(typeof(PushdownParts), "Lookup")]
public sealed class LookupPushdownPart : PushdownPartBase
{
    private const string BringSetting = "bring";

    /// <summary>Initializes a new instance of the <see cref="LookupPushdownPart"/> class.</summary>
    public LookupPushdownPart() : base("Lookup", true, FlowLookup.OutputName, FlowLookup.ReferenceName)
    {
    }

    /// <inheritdoc />
    public override async Task<PushdownAttempt> Resolve(
        PushdownScope scope, PipelineComponentConfiguration component, bool top, CancellationToken cancellationToken = default)
    {
        var plain = PushdownChecks.Plain(scope, component);
        if (plain.Length > 0 || component.BatchSize is not { } batchSize)
            return PushdownAttempt.Stays(plain.Length > 0 ? plain : "it does not state its batch size");

        if (!top)
            return PushdownAttempt.Stays("its rows with no reference row leave by their own way, so it is pushed only where its rows leave the query");

        if (PushdownJoinTypes.Keeping(true, false) is not { } type)
            return PushdownAttempt.Stays("no join type a statement performs keeps every main row and drops the reference rows nothing matched");

        var main = await PushdownInputs.Side(scope, component, FlowLookup.InputName, true, cancellationToken).ConfigureAwait(false);
        if (main.Side is not { } mainSide)
            return main;

        var reference = await PushdownInputs.Side(scope, component, FlowLookup.ReferenceName, false, cancellationToken).ConfigureAwait(false);
        if (reference.Side is not { } referenceSide)
            return reference;

        var keys = Columns(component, "keys");
        var (pairs, leftContainer, reason) = PushdownJoins.Keys(mainSide, referenceSide, keys, Columns(component, "referenceKeys"));
        var refusal = reason.Length > 0 ? reason : PushdownJoins.OneQuery(mainSide, referenceSide);
        if (refusal.Length > 0)
            return PushdownAttempt.Stays(refusal);

        if (!IsPrimaryKey(pairs.Select(pair => pair.Right).ToList(), referenceSide.PrimaryKey))
            return PushdownAttempt.Stays("its reference keys are not the reference container's primary key, so a reference key could repeat");

        var (bring, bringReason) = PushdownMappings.Apply(component, null, BringSetting, referenceSide);
        if (bringReason.Length > 0 || bring.Count == 0)
            return PushdownAttempt.Stays(bringReason.Length > 0 ? bringReason : "it brings no field");

        if (PushdownJoins.Collision(mainSide.Fields, bring) is { } collision)
            return PushdownAttempt.Stays($"its output would hold field '{collision}' twice or under a name a statement does not write as it is");

        var side = new PushdownSide(
            mainSide.DataStoreId,
            mainSide.Primary,
            mainSide.PrimaryKey,
            [.. mainSide.Fields, .. bring],
            [.. mainSide.Joins, PushdownJoins.Join(mainSide, referenceSide, type, pairs, leftContainer)],
            mainSide.Order,
            [.. mainSide.Containers, .. referenceSide.Containers],
            [.. mainSide.Members, .. referenceSide.Members, component.Name],
            []);
        return new PushdownAttempt(
            side,
            string.Empty,
            new PushdownColumn(PushdownJoins.ReferencePresentField, referenceSide.Primary.Container, referenceSide.PrimaryKey[0], null),
            new FlowPushedAbsence(PushdownJoins.ReferencePresentField, [.. bring.Select(field => field.Name)], false, component.UnmatchedOutput, keys))
        {
            BatchSize = batchSize,
        };
    }

    private static bool IsPrimaryKey(List<string> columns, IReadOnlyList<string> primaryKey) =>
        columns.Count == primaryKey.Count && primaryKey.All(key => columns.Contains(key, StringComparer.OrdinalIgnoreCase));

    private static string[] Columns(PipelineComponentConfiguration component, string setting) =>
        [.. component.Columns
            .Where(column => string.Equals(column.SettingName, setting, StringComparison.OrdinalIgnoreCase))
            .OrderBy(column => column.Ordinal)
            .Select(column => column.ColumnName)];
}
