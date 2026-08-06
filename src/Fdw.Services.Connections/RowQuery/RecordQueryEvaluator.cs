using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.RowQuery;

/// <summary>
/// Orchestrates the shared, format-agnostic record-query pipeline: validate the command's filter/join
/// shape (<see cref="RecordQueryValidator"/>), validate every decoded row against its container's
/// DECLARED field schema (<see cref="RecordRowValidator"/>), validate every filter/join column against
/// the relevant container's declared fields (<see cref="RecordColumnValidator"/>), resolve the single
/// supported JOIN's parent rows via the caller-supplied <see cref="JoinedRowsLoader"/>, then
/// filter/join-match every primary row with real INNER JOIN semantics (<see cref="RecordRowMatcher"/>).
/// Materialization to a POCO type is a separate step (<see cref="RecordRowMaterializer"/>) — this type
/// only produces the matched row set.
/// </summary>
public static class RecordQueryEvaluator
{
    /// <summary>
    /// Applies <paramref name="filter"/>/<paramref name="joins"/> over <paramref name="rows"/>,
    /// resolving the single supported join's parent rows via <paramref name="loadJoinedRows"/> when
    /// present. Fails loud (structured MessageLogging) when: the filter/join shape is unsupported; a
    /// row (primary or joined) is missing a value for a field its container declares non-nullable; a
    /// filter/join column is not declared on its target container; or the joined rows cannot be loaded.
    /// Never a silent partial/empty match.
    /// </summary>
    /// <param name="rows">The primary container's decoded rows.</param>
    /// <param name="primaryContainer">The primary container — the source of its declared field schema.</param>
    /// <param name="filter">The command's filter expression, if any.</param>
    /// <param name="joins">The command's join expressions (at most one is supported).</param>
    /// <param name="loadJoinedRows">Loads the join target's container and rows.</param>
    /// <param name="logger">Logger for the structured evaluation trace/failure.</param>
    /// <param name="cancellationToken">Cancellation token for the join-rows load.</param>
    public static async Task<IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>> Evaluate(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IDataContainer primaryContainer,
        IFilterExpression? filter,
        IReadOnlyList<IJoinExpression> joins,
        JoinedRowsLoader loadJoinedRows,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        RecordQueryLog.EvaluatingQuery(logger, rows.Count, filter?.Root is not null, joins.Count);

        var shapeResult = RecordQueryValidator.ValidateShape(filter?.Root, joins, logger);
        if (!shapeResult.IsSuccess)
            return shapeResult.ToNewResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();

        // Why (fix #1): a declared non-nullable field missing/null on ANY primary row is a schema
        // violation in the source data — fail loud before the row is ever matched or materialized.
        var primaryRowsValid = RecordRowValidator.Validate(rows, primaryContainer, logger);
        if (!primaryRowsValid.IsSuccess)
            return primaryRowsValid.ToNewResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();

        var joinResult = await ResolveJoin(joins, primaryContainer, loadJoinedRows, logger, cancellationToken).ConfigureAwait(false);
        if (!joinResult.IsSuccess)
            return joinResult.ToNewResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();

        var join = joinResult.Value;

        // Why (fix #2): every column the filter references must be a declared field on its target
        // container — an undeclared column is a configuration error, not a "no match" condition.
        var filterColumnsValid = RecordColumnValidator.ValidateFilterColumns(
            filter?.Root, primaryContainer, join?.Container, join?.ParentContainerName, logger);
        if (!filterColumnsValid.IsSuccess)
            return filterColumnsValid.ToNewResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();

        var matched = MatchRows(rows, join, filter);

        RecordQueryLog.QueryEvaluated(logger, matched.Count, rows.Count);
        return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(matched);
    }

    // Why: isolates the join-target load + its own row/column validation from Evaluate's top-level
    // control flow (keeps Evaluate a flat sequence of "validate, else return" steps — extracted partly
    // to stay under the FDW007 complexity threshold, but also because "resolve the join" is a genuinely
    // separate concern from "match the rows"). Returns null (success, no join) when the command carries
    // no join at all.
    private static async Task<IGenericResult<JoinResolution?>> ResolveJoin(
        IReadOnlyList<IJoinExpression> joins,
        IDataContainer primaryContainer,
        JoinedRowsLoader loadJoinedRows,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (joins.Count == 0)
            return GenericResult<JoinResolution?>.Success(null);

        var join = joins[0];
        var (leftField, rightField) = join.JoinConditions[0];

        var joinedResult = await loadJoinedRows(join.TargetContainerName, cancellationToken).ConfigureAwait(false);
        if (!joinedResult.IsSuccess)
            return joinedResult.ToNewResult<JoinResolution?>();

        var joinedContainer = joinedResult.Value!.Container;
        var parentRows = joinedResult.Value!.Rows;
        RecordQueryLog.JoinTargetResolved(logger, joinedContainer.Name, parentRows.Count);

        // Why (fix #1, joined side): the same required-field guard applies to the joined container's
        // rows — this is exactly what closes finding #2's proof (deleting IsCurrent from
        // SecretManager.json silently excluded the row instead of failing loud).
        var joinedRowsValid = RecordRowValidator.Validate(parentRows, joinedContainer, logger);
        if (!joinedRowsValid.IsSuccess)
            return joinedRowsValid.ToNewResult<JoinResolution?>();

        // Why (fix #2): the join's own field pair must be declared columns before it is evaluated.
        var joinColumnsValid = RecordColumnValidator.ValidateJoinColumns(leftField, rightField, primaryContainer, joinedContainer, logger);
        if (!joinColumnsValid.IsSuccess)
            return joinColumnsValid.ToNewResult<JoinResolution?>();

        return GenericResult<JoinResolution?>.Success(
            new JoinResolution(joinedContainer, parentRows, leftField, rightField, join.TargetContainerName));
    }

    // Why (fix #4): real INNER JOIN semantics via RecordRowMatcher.MatchesJoinedRow — ALL matching
    // parent rows are considered; the child row survives if ANY pairing satisfies the parent-qualified
    // filter. Two parent rows sharing a join key (files carry no PK enforcement) each get a pairing
    // attempt, exactly as SQL's INNER JOIN would.
    private static List<IReadOnlyDictionary<string, object?>> MatchRows(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        JoinResolution? join,
        IFilterExpression? filter)
    {
        var matched = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var row in rows)
        {
            if (join is not null)
            {
                if (RecordRowMatcher.MatchesJoinedRow(row, join.Rows, join.LeftField, join.RightField, join.ParentContainerName, filter?.Root))
                    matched.Add(row);
                continue;
            }

            if (RecordRowMatcher.Matches(row, null, null, filter?.Root))
                matched.Add(row);
        }

        return matched;
    }

    // Why: carries the join target's resolved container + rows + field pair + name together, so
    // ResolveJoin/MatchRows pass one value instead of five loose locals.
    private sealed class JoinResolution
    {
        public JoinResolution(
            IDataContainer container,
            IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
            string leftField,
            string rightField,
            string parentContainerName)
        {
            Container = container;
            Rows = rows;
            LeftField = leftField;
            RightField = rightField;
            ParentContainerName = parentContainerName;
        }

        public IDataContainer Container { get; }

        public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; }

        public string LeftField { get; }

        public string RightField { get; }

        public string ParentContainerName { get; }
    }
}
