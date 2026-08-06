using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Abstractions.Visualization;
using Fdw.Services.Data.Logging;

namespace Fdw.Services.Data.Visualization;

/// <summary>
/// Computes statistical summaries for data columns via DataGateway queries.
/// </summary>
public sealed class StatSetService : IStatSetService
{
    private readonly ILogger<StatSetService> _logger;
    private readonly IDataGateway _dataGateway;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatSetService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="dataGateway">The data gateway for executing queries.</param>
    public StatSetService(
        ILogger<StatSetService>? logger,
        IDataGateway dataGateway)
    {
        _logger = logger ?? NullLogger<StatSetService>.Instance;
        _dataGateway = dataGateway;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<StatSetResponse>> ComputeStatSet(
        StatSetRequest request,
        CancellationToken cancellationToken = default)
    {
        StatSetServiceLog.ComputingStatSet(_logger, request.ColumnNames.Count, request.ContainerName, request.ConnectionName);

        var stopwatch = Stopwatch.StartNew();

        StatSetServiceLog.RetrievingDataForStats(_logger, request.ContainerName);

        // Why: DataStoreName / PathName / ContainerName are validated at the endpoint boundary
        // by StatSetRequestValidator (FluentValidation). A request that reaches here without
        // them is a contract violation — the null-suppression is intentional.
        var query = DataQuery.From<IEnumerable<Dictionary<string, object?>>>(request.DataStoreName!, request.PathName!, request.ContainerName).Build();

        var queryResult = await _dataGateway.Execute<IEnumerable<Dictionary<string, object?>>>(query, cancellationToken).ConfigureAwait(false);

        if (!queryResult.IsSuccess)
        {
            return GenericResult<StatSetResponse>.Failure(
                StatSetServiceLog.StatSetComputationFailed(_logger, request.ContainerName, queryResult.CurrentMessage));
        }

        if (queryResult.Value == null)
        {
            return GenericResult<StatSetResponse>.Failure(
                StatSetServiceLog.StatSetQueryReturnedNoData(_logger, request.ContainerName));
        }

        var rows = MaterializeRows(queryResult.Value);

        // Why: when the caller omits ColumnNames, auto-discover numeric columns from the first
        // materialized row so the endpoint can return useful stats without forcing clients to
        // pre-enumerate the schema. Non-numeric columns are skipped because ColumnStatSet only
        // exposes min/max/mean/median/percentile which require numeric values.
        var columnNames = request.ColumnNames.Count > 0
            ? request.ColumnNames
            : DiscoverNumericColumns(rows);

        var columnStats = new Dictionary<string, ColumnStatSet>(StringComparer.OrdinalIgnoreCase);

        foreach (var columnName in columnNames)
        {
            var values = ExtractNumericValues(rows, columnName);
            columnStats[columnName] = ComputeColumnStats(columnName, values);
        }

        stopwatch.Stop();
        StatSetServiceLog.StatSetComputed(_logger, request.ContainerName, stopwatch.Elapsed.TotalMilliseconds);

        return GenericResult<StatSetResponse>.Success(new StatSetResponse
        {
            ColumnStats = columnStats
        });
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<GroupedStatSetResponse>> ComputeGroupedStatSet(
        GroupedStatSetRequest request,
        CancellationToken cancellationToken = default)
    {
        StatSetServiceLog.ComputingGroupedStatSet(
            _logger,
            request.ColumnNames.Count,
            request.GroupByColumns.Count,
            request.ContainerName);

        var stopwatch = Stopwatch.StartNew();

        // Why: Validated at endpoint boundary by GroupedStatSetRequestValidator.
        var query = DataQuery.From<IEnumerable<Dictionary<string, object?>>>(request.DataStoreName!, request.PathName!, request.ContainerName).Build();

        var queryResult = await _dataGateway.Execute<IEnumerable<Dictionary<string, object?>>>(query, cancellationToken).ConfigureAwait(false);

        if (!queryResult.IsSuccess)
        {
            return GenericResult<GroupedStatSetResponse>.Failure(
                StatSetServiceLog.StatSetComputationFailed(_logger, request.ContainerName, queryResult.CurrentMessage));
        }

        if (queryResult.Value == null)
        {
            return GenericResult<GroupedStatSetResponse>.Failure(
                StatSetServiceLog.StatSetQueryReturnedNoData(_logger, request.ContainerName));
        }

        var rows = MaterializeRows(queryResult.Value);
        // Why: same auto-discovery as ComputeStatSet — caller may omit ColumnNames; use any
        // numeric column from the first row, minus the group-by columns.
        var requestedColumns = request.ColumnNames.Count > 0
            ? request.ColumnNames
            : DiscoverNumericColumns(rows)
                .Where(c => !request.GroupByColumns.Contains(c, StringComparer.OrdinalIgnoreCase))
                .ToList();

        var groups = GroupRows(rows, request.GroupByColumns);

        var resultGroups = new List<StatSetGroup>();

        foreach (var group in groups)
        {
            var groupColumnStats = new Dictionary<string, ColumnStatSet>(StringComparer.OrdinalIgnoreCase);

            foreach (var columnName in requestedColumns)
            {
                var values = ExtractNumericValues(group.Rows, columnName);
                groupColumnStats[columnName] = ComputeColumnStats(columnName, values);
            }

            resultGroups.Add(new StatSetGroup
            {
                GroupKeys = group.Keys,
                ColumnStats = groupColumnStats
            });
        }

        stopwatch.Stop();
        StatSetServiceLog.GroupedStatSetComputed(
            _logger,
            request.ContainerName,
            resultGroups.Count,
            stopwatch.Elapsed.TotalMilliseconds);

        return GenericResult<GroupedStatSetResponse>.Success(new GroupedStatSetResponse
        {
            Groups = resultGroups
        });
    }

    // Why: auto-discover columns by inspecting the first row's keys and keeping only those whose
    // value is numeric (or null — non-null check happens during stat extraction). Returns empty
    // if the row set is empty, in which case ColumnStats just comes back empty too.
    private static List<string> DiscoverNumericColumns(List<IDictionary<string, object?>> rows)
    {
        if (rows.Count == 0) return new List<string>();
        var first = rows[0];
        var discovered = new List<string>();
        foreach (var kv in first)
        {
            var value = kv.Value;
            if (value is null) continue;
            if (value is byte or sbyte or short or ushort or int or uint or long or ulong
                or float or double or decimal)
            {
                discovered.Add(kv.Key);
            }
        }
        return discovered;
    }

    // Why: stat queries request IEnumerable<Dictionary<string,object?>>, so every row already is a
    // case-insensitive dictionary (ExpandoObject also implements IDictionary<string,object?>). Cast
    // through the interface — there is no reflection projection of arbitrary POCOs.
    private static List<IDictionary<string, object?>> MaterializeRows(IEnumerable<object> data)
    {
        var result = new List<IDictionary<string, object?>>();

        foreach (var item in data)
        {
            if (item is IDictionary<string, object?> dict)
                result.Add(dict);
        }

        return result;
    }

    private static List<double> ExtractNumericValues(
        IReadOnlyList<IDictionary<string, object?>> rows,
        string columnName)
    {
        var values = new List<double>(rows.Count);

        foreach (var row in rows)
        {
            if (!row.TryGetValue(columnName, out var raw) || raw == null)
                continue;

            if (raw is double d) { values.Add(d); continue; }
            if (raw is int i) { values.Add(i); continue; }
            if (raw is long l) { values.Add(l); continue; }
            if (raw is float f) { values.Add(f); continue; }
            if (raw is decimal dec) { values.Add((double)dec); continue; }
            if (raw is short s) { values.Add(s); continue; }
            if (raw is byte b) { values.Add(b); continue; }

            if (double.TryParse(raw.ToString(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                values.Add(parsed);
            }
        }

        return values;
    }

    private static ColumnStatSet ComputeColumnStats(string columnName, List<double> values)
    {
        if (values.Count == 0)
        {
            return new ColumnStatSet { ColumnName = columnName };
        }

        values.Sort();
        long count = values.Count;
        double sum = 0;
        foreach (var v in values) sum += v;
        double mean = sum / count;

        double varianceSum = 0;
        foreach (var v in values)
        {
            var diff = v - mean;
            varianceSum += diff * diff;
        }
        double stdDev = count > 1 ? Math.Sqrt(varianceSum / (count - 1)) : 0;

        return new ColumnStatSet
        {
            ColumnName = columnName,
            Count = count,
            Sum = sum,
            Mean = mean,
            Median = Percentile(values, 0.50),
            StdDev = stdDev,
            Min = values[0],
            Max = values[values.Count - 1],
            P25 = Percentile(values, 0.25),
            P75 = Percentile(values, 0.75),
            P95 = Percentile(values, 0.95),
            DistinctCount = values.Distinct().Count()
        };
    }

    private static double Percentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) return 0;
        if (sortedValues.Count == 1) return sortedValues[0];

        double index = percentile * (sortedValues.Count - 1);
        int lower = (int)Math.Floor(index);
        int upper = (int)Math.Ceiling(index);

        if (lower == upper) return sortedValues[lower];

        double fraction = index - lower;
        return sortedValues[lower] + fraction * (sortedValues[upper] - sortedValues[lower]);
    }

    private static List<(IReadOnlyDictionary<string, object?> Keys, IReadOnlyList<IDictionary<string, object?>> Rows)> GroupRows(
        List<IDictionary<string, object?>> rows,
        IReadOnlyList<string> groupByColumns)
    {
        var groups = new Dictionary<string, (Dictionary<string, object?> Keys, List<IDictionary<string, object?>> Rows)>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var keyParts = new List<string>(groupByColumns.Count);
            var keyValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var col in groupByColumns)
            {
                row.TryGetValue(col, out var val);
                keyValues[col] = val;
                keyParts.Add(val is null ? string.Empty : val.ToString()!);
            }

            var groupKey = string.Join("|", keyParts);

            if (!groups.TryGetValue(groupKey, out var group))
            {
                group = (keyValues, new List<IDictionary<string, object?>>());
                groups[groupKey] = group;
            }

            group.Rows.Add(row);
        }

        return groups.Values
            .Select(g => ((IReadOnlyDictionary<string, object?>)g.Keys, (IReadOnlyList<IDictionary<string, object?>>)g.Rows))
            .ToList();
    }
}
