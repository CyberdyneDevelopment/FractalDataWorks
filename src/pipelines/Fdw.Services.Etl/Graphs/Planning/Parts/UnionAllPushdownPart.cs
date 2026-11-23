using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Etl.Flow;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// A union of rows pushed into its store (FDW-6969, over FDW-6946's UNION ALL): every input a container of one data store record and path, giving the
/// same fields in the same order with the same types. Pushed only at the top.
/// </summary>
/// <remarks>
/// The in-memory union keeps each input's order and promises none between inputs. The pushed query orders the stacked rows by the inputs' primary keys,
/// which every input must give under the same names: that keeps each input's rows in its key order, an order the in-memory union could give.
/// </remarks>
[TypeOption(typeof(PushdownParts), "UnionAll")]
public sealed class UnionAllPushdownPart : PushdownPartBase
{
    /// <summary>Initializes a new instance of the <see cref="UnionAllPushdownPart"/> class.</summary>
    public UnionAllPushdownPart() : base("UnionAll", true, FlowUnionAll.OutputName, string.Empty)
    {
    }

    /// <inheritdoc />
    public override async Task<PushdownAttempt> Resolve(
        PushdownScope scope, PipelineComponentConfiguration component, bool top, CancellationToken cancellationToken = default)
    {
        var plain = PushdownChecks.Plain(scope, component);
        if (plain.Length > 0 || !top)
            return PushdownAttempt.Stays(plain.Length > 0 ? plain : "its rows are pushed only where they leave the query");

        var inputs = scope.Rows
            .Where(line => string.Equals(line.ToComponent, component.Name, StringComparison.OrdinalIgnoreCase))
            .Select(line => line.ToInput)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (inputs.Count < 2)
            return PushdownAttempt.Stays("it stacks fewer than two inputs");

        var sides = new List<PushdownSide>(inputs.Count);
        foreach (var input in inputs)
        {
            var fed = await PushdownInputs.Side(scope, component, input, false, cancellationToken).ConfigureAwait(false);
            if (fed.Side is not { } side)
                return fed;

            sides.Add(side);
        }

        var refusal = Refusal(sides);
        if (refusal.Length > 0)
            return PushdownAttempt.Stays(refusal);

        // Why the sources' batch size: a union states none of its own and forwards each input's batches; one query has one, so its inputs must agree.
        var batchSizes = sides.Select(side => scope.Component(side.Members[0])?.BatchSize).Distinct().ToList();
        return batchSizes is [{ } batchSize]
            ? PushdownAttempt.Pushed(Stacked(component, sides)) with { BatchSize = batchSize }
            : PushdownAttempt.Stays("its inputs' sources do not state one batch size");
    }

    // Why the inputs cannot be one UNION ALL, or empty when they can.
    private static string Refusal(List<PushdownSide> sides)
    {
        var first = sides[0];
        foreach (var side in sides.Skip(1))
        {
            if (side.DataStoreId != first.DataStoreId || !string.Equals(side.Primary.Path, first.Primary.Path, StringComparison.OrdinalIgnoreCase))
                return "its inputs read more than one data store or path";
            if (!side.Fields.Select(field => field.Name).SequenceEqual(first.Fields.Select(field => field.Name), StringComparer.Ordinal))
                return "its inputs do not give the same fields in the same order, which one UNION ALL needs";
            if (side.Fields.Zip(first.Fields).Any(pair => pair.First.ClrType is null || pair.First.ClrType != pair.Second.ClrType))
                return "its inputs give a field with different data types, which a store would convert";
        }

        var keys = KeyNames(first);
        return keys is null || sides.Skip(1).Any(side => KeyNames(side) is not { } names || !names.SequenceEqual(keys, StringComparer.Ordinal))
            ? "its inputs do not each give their primary key under the same names, so the order of the stacked rows cannot be stated"
            : string.Empty;
    }

    // The names an input gives its primary key columns by, in key order; null when it does not give every one.
    private static List<string>? KeyNames(PushdownSide side)
    {
        var names = side.PrimaryKey
            .Select(key => side.Fields.FirstOrDefault(field => string.Equals(field.Column, key, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        return names.Exists(field => field is null) ? null : [.. names.OfType<PushdownColumn>().Select(field => field.Name)];
    }

    private static PushdownSide Stacked(PipelineComponentConfiguration component, List<PushdownSide> sides)
    {
        var first = sides[0];
        return new PushdownSide(
            first.DataStoreId,
            first.Primary,
            first.PrimaryKey,
            first.Fields,
            [],
            KeyNames(first) ?? [],
            [.. sides.SelectMany(side => side.Containers)],
            [.. sides.SelectMany(side => side.Members), component.Name],
            [.. sides.Skip(1).Select(side => new UnionBranch
            {
                ContainerName = side.Primary.Container,
                Fields = [.. side.Fields.Select(field => new ProjectionField { PropertyName = field.Column, Alias = field.Name })],
            })]);
    }
}
