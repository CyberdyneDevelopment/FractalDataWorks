using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Etl.Predicates;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// Makes a row filter from the filter expression a pipeline already uses (FDW-1386): one expression language for stage filters,
/// output-node filters and splits.
/// </summary>
public static class FlowPredicate
{
    /// <summary>Reads a filter expression.</summary>
    /// <param name="expression">The expression, for example <c>Amount &gt; 100 AND Region = 'ERCOT'</c>.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The filter, or the failure that says why the expression cannot be read.</returns>
    public static IGenericResult<Func<IDictionary<string, object?>, bool>> FromExpression(string expression, ILogger logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);

        var parsed = RecordPredicates.Parse(expression, logger);
        if (parsed.IsFailure || parsed.Value is null)
        {
            return GenericResult<Func<IDictionary<string, object?>, bool>>.Failure(
                FlowLog.FilterInvalid(logger, expression, parsed.CurrentMessage));
        }

        var predicate = parsed.Value;
        return GenericResult<Func<IDictionary<string, object?>, bool>>.Success(row => predicate.Evaluate(row));
    }
}
