using System.Collections.Generic;
using System.Linq;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads.Builders;

/// <summary>
/// Builds the filter that matches one row on its key columns (FDW-1109).
/// </summary>
internal static class LoadKeyFilters
{
    /// <summary>Builds <c>key1 = v1 AND key2 = v2</c> from a row's key values.</summary>
    /// <param name="owner">What the load is, for the refusal message.</param>
    /// <param name="keys">The key columns.</param>
    /// <param name="row">The row, which carries every key column.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The filter, or a failure when a key value is null (a null key matches nothing).</returns>
    public static IGenericResult<IFilterExpression> ForRow(
        string owner,
        IReadOnlyList<string> keys,
        IDictionary<string, object?> row,
        ILogger logger)
    {
        var conditions = new List<IFilterNode>(keys.Count);
        foreach (var key in keys)
        {
            if (!LoadRows.TryFind(row, key, out var value) || value is null)
                return GenericResult<IFilterExpression>.Failure(PipelineRunLog.KeyValueMissing(logger, owner, key));
            conditions.Add(new FilterCondition { PropertyName = key, Operator = FilterOperators.ByName("Equal"), Value = value });
        }

        IFilterNode root = conditions.Count == 1
            ? conditions[0]
            : new FilterGroup { Operator = LogicalOperator.And, Nodes = conditions.ToList() };
        return GenericResult<IFilterExpression>.Success(new FilterExpression { Root = root });
    }
}
