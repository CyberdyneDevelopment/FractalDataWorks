using System.Linq;
using System.Globalization;
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
    /// <param name="ordering">The command's ordering, if any. Applied here because a file cannot.</param>
    /// <param name="paging">The command's page, if any. Applied after ordering, as SQL would.</param>
    public static async Task<IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>> Evaluate(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IDataContainer primaryContainer,
        IFilterExpression? filter,
        IReadOnlyList<IJoinExpression> joins,
        JoinedRowsLoader loadJoinedRows,
        ILogger logger,
        IOrderingExpression? ordering = null,
        IPagingExpression? paging = null,
        CancellationToken cancellationToken = default)
    {
        RecordQueryLog.EvaluatingQuery(logger, rows.Count, filter?.Root is not null, joins.Count);

        var shapeResult = RecordQueryValidator.ValidateShape(filter?.Root, joins, logger);
        if (!shapeResult.IsSuccess)
            return shapeResult.ToNewResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();

        var primaryRowsValid = RecordRowValidator.Validate(rows, primaryContainer, logger);
        if (!primaryRowsValid.IsSuccess)
            return primaryRowsValid.ToNewResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();

        var joinResult = await ResolveJoin(joins, primaryContainer, loadJoinedRows, logger, cancellationToken).ConfigureAwait(false);
        if (!joinResult.IsSuccess)
            return joinResult.ToNewResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();

        var join = joinResult.Value;

        var filterColumnsValid = RecordColumnValidator.ValidateFilterColumns(
            filter?.Root, primaryContainer, join?.Container, join?.ParentContainerName, logger);
        if (!filterColumnsValid.IsSuccess)
            return filterColumnsValid.ToNewResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();

        var matched = MatchRows(rows, join, filter);

        var ordered = ApplyOrdering(matched, ordering);
        var paged = ApplyPaging(ordered, paging);

        RecordQueryLog.QueryEvaluated(logger, paged.Count, rows.Count);
        return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(paged);
    }

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

        var joinedRowsValid = RecordRowValidator.Validate(parentRows, joinedContainer, logger);
        if (!joinedRowsValid.IsSuccess)
            return joinedRowsValid.ToNewResult<JoinResolution?>();

        var joinColumnsValid = RecordColumnValidator.ValidateJoinColumns(leftField, rightField, primaryContainer, joinedContainer, logger);
        if (!joinColumnsValid.IsSuccess)
            return joinColumnsValid.ToNewResult<JoinResolution?>();

        return GenericResult<JoinResolution?>.Success(
            new JoinResolution(joinedContainer, parentRows, leftField, rightField, join.TargetContainerName));
    }

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

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> ApplyOrdering(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IOrderingExpression? ordering)
    {
        if (ordering is null || ordering.OrderedFields.Count == 0)
            return rows;

        IOrderedEnumerable<IReadOnlyDictionary<string, object?>>? sorted = null;
        foreach (var field in ordering.OrderedFields)
        {
            var name = field.PropertyName;
            Func<IReadOnlyDictionary<string, object?>, object?> key =
                row => row.TryGetValue(name, out var value) ? value : null;

            bool ascending = field.Direction?.IsAscending ?? true;
            sorted = sorted is null
                ? (ascending
                    ? rows.OrderBy(key, RowValueComparer.Instance)
                    : rows.OrderByDescending(key, RowValueComparer.Instance))
                : (ascending
                    ? sorted.ThenBy(key, RowValueComparer.Instance)
                    : sorted.ThenByDescending(key, RowValueComparer.Instance));
        }

        return sorted!.ToList();
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> ApplyPaging(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IPagingExpression? paging)
    {
        if (paging is null)
            return rows;

        IEnumerable<IReadOnlyDictionary<string, object?>> window = rows;
        if (paging.Skip > 0)
            window = window.Skip(paging.Skip);
        if (paging.Take is int take && take >= 0)
            window = window.Take(take);

        return ReferenceEquals(window, rows) ? rows : window.ToList();
    }

    private sealed class RowValueComparer : IComparer<object?>
    {
        public static readonly RowValueComparer Instance = new();

        public int Compare(object? x, object? y)
        {
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            if (x is IComparable c && x.GetType() == y.GetType())
                return c.CompareTo(y);

            if (decimal.TryParse(x.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var dx)
                && decimal.TryParse(y.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var dy))
                return dx.CompareTo(dy);

            return string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
