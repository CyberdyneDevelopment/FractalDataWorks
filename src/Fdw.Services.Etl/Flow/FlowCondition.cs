using System;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// Makes a loop condition from the filter expression language a pipeline already uses (FDW-1403): the variables in scope are
/// the fields the expression names.
/// </summary>
public static class FlowCondition
{
    /// <summary>Reads a condition such as <c>Attempt &lt; 5 AND Status != 'Done'</c>.</summary>
    /// <param name="expression">The expression.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The condition, or the failure that says why the expression cannot be read.</returns>
    public static IGenericResult<Func<FlowVariables, bool>> FromExpression(string expression, ILogger logger)
    {
        var predicate = FlowPredicate.FromExpression(expression, logger);
        if (predicate.IsFailure || predicate.Value is null)
        {
            return predicate.ToNewResult<Func<FlowVariables, bool>>();
        }

        var filter = predicate.Value;
        return GenericResult<Func<FlowVariables, bool>>.Success(variables => filter(variables.Snapshot()));
    }
}
