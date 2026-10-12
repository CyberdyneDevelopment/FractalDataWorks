using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Predicates;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// The condition of one Filter stage for one run: asked of every record, read strictly.
/// </summary>
/// <remarks>
/// Why: an <see cref="IExpressionEvaluator"/> supplied through the transform context is still asked first, per record, as it
/// always was (no pipeline supplies one today). Whenever it is absent or fails, the condition is read by
/// <see cref="RecordPredicates"/>, once, and text it cannot read is a failure that names it. The stage no longer passes every
/// record when it cannot read its own condition.
/// </remarks>
internal sealed class FilterStageCondition
{
    private readonly string _expression;
    private readonly ILogger _logger;
    private readonly IExpressionEvaluator? _evaluator;
    private IGenericResult<IRecordPredicate>? _read;

    private FilterStageCondition(string expression, ITransformContext context)
    {
        _expression = expression;
        _logger = context.Logger;
        _evaluator = context.CalculationEngine as IExpressionEvaluator;
    }

    /// <summary>
    /// Prepares the condition for a run. With no evaluator in play the text is read now, so a text that cannot be read
    /// stops the stage before it sees a record.
    /// </summary>
    public static IGenericResult<FilterStageCondition> Prepare(string expression, ITransformContext context)
    {
        var condition = new FilterStageCondition(expression, context);
        if (condition._evaluator is not null)
        {
            return GenericResult<FilterStageCondition>.Success(condition);
        }

        var read = condition.Read();
        return read.IsFailure
            ? read.ToNewResult<FilterStageCondition>()
            : GenericResult<FilterStageCondition>.Success(condition);
    }

    /// <summary>
    /// Asks whether the record meets the condition.
    /// </summary>
    public IGenericResult<bool> Passes(IDictionary<string, object?> record)
    {
        if (_evaluator is not null)
        {
            var variables = new Dictionary<string, object?>(record, StringComparer.OrdinalIgnoreCase);
            var answered = _evaluator.EvaluatePredicate(_expression, variables);
            if (answered.IsSuccess)
            {
                return GenericResult<bool>.Success(answered.Value);
            }
        }

        var read = Read();
        return read.IsFailure
            ? read.ToNewResult<bool>()
            : GenericResult<bool>.Success(read.Value!.Evaluate(record));
    }

    private IGenericResult<IRecordPredicate> Read()
    {
        _read ??= RecordPredicates.Parse(_expression, _logger);
        return _read;
    }
}
