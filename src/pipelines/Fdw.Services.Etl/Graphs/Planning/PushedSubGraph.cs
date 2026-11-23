using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// A sub-graph the planner pushes into its store (FDW-6969): the components it stands in for, and the one query that reads what they would have sent
/// out of the top one.
/// </summary>
/// <param name="Top">The top component: the pushed query takes its name and outputs.</param>
/// <param name="Members">Every component it stands in for, the top one included.</param>
/// <param name="Target">The primary container the query reads, in its store and path.</param>
/// <param name="Query">The query: its joins, projection and order.</param>
/// <param name="Output">The output the matched rows leave by.</param>
/// <param name="Absence">What happens to a row whose stated side is absent; null when there is none.</param>
/// <param name="BatchSize">How many rows go in an output batch: the top component's.</param>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record PushedSubGraph(
    string Top,
    IReadOnlyList<string> Members,
    DataStoreTarget Target,
    QueryCommand<Dictionary<string, object?>> Query,
    string Output,
    FlowPushedAbsence? Absence,
    int BatchSize)
{
    /// <summary>Makes the sub-graph of a top component the planner pushed.</summary>
    /// <param name="top">The top component.</param>
    /// <param name="output">The output its rows leave by.</param>
    /// <param name="attempt">The planner's answer for it, with its side.</param>
    /// <param name="side">The side it reads.</param>
    /// <param name="batchSize">Its batch size.</param>
    /// <returns>The sub-graph.</returns>
    public static PushedSubGraph Of(PipelineComponentConfiguration top, string output, PushdownAttempt attempt, PushdownSide side, int batchSize)
    {
        IEnumerable<PushdownColumn> projected = attempt.Marker is { } marker ? [.. side.Fields, marker] : side.Fields;
        var query = new QueryCommand<Dictionary<string, object?>>
        {
            Joins = side.Joins,
            Unions = side.Unions,
            Projection = new ProjectionExpression
            {
                Fields = [.. projected.Select(field => new ProjectionField { PropertyName = field.Column, Alias = field.Name, SourceContainer = field.Container })],
            },
            Ordering = new OrderingExpression
            {
                OrderedFields = [.. side.Order.Select(column => new OrderedField { PropertyName = column, Direction = SortDirections.Ascending })],
            },
        };
        return new PushedSubGraph(top.Name, side.Members, side.Primary, query, output, attempt.Absence, batchSize);
    }

    /// <summary>Gets the query as the trace states it: the containers, joins and order.</summary>
    /// <remarks>A join's left columns are those of the primary container unless the join names the container it continues from.</remarks>
    public string Shape => string.Concat(
        $"from {Target.Container}",
        string.Concat(Query.Joins.Select(join =>
            $" {join.JoinType} join {join.TargetContainerName}{ContinuedFrom(join)} on {string.Join(" and ", join.JoinConditions.Select(pair => $"{pair.LeftField} = {join.TargetContainerName}.{pair.RightField}"))}")),
        $", order by {string.Join(", ", Query.Ordering?.OrderedFields.Select(ordered => ordered.PropertyName) ?? [])}");

    // The words a join adds when it names the container its left columns belong to; none when they are the primary container's.
    private static string ContinuedFrom(IJoinExpression join) =>
        join.SourceContainerName is { Length: > 0 } from ? $" (continuing from {from})" : string.Empty;

    /// <summary>Builds the component that runs the query in place of the sub-graph.</summary>
    /// <param name="gateway">The data gateway the read arrived on.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The component.</returns>
    public IFlowComponent Build(IDataGateway gateway, ILogger logger) => new FlowPushedQuery(Top, gateway, Target, Query, Output, Absence, BatchSize, logger);

    /// <summary>Gets whether a component is one the sub-graph stands in for, other than its top.</summary>
    /// <param name="component">The component's name.</param>
    /// <returns>True for a member that is removed from the graph.</returns>
    public bool Removes(string component) =>
        !string.Equals(component, Top, StringComparison.OrdinalIgnoreCase) && Members.Contains(component, StringComparer.OrdinalIgnoreCase);
}
