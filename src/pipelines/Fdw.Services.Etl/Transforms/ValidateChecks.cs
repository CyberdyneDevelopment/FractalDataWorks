using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Etl.Predicates;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// The checks of one Validate stage for one run: each condition read, in order, with the run's parameters placed in it.
/// </summary>
/// <remarks>
/// Why prepared once: a check that cannot be read, and a stage that does not say what a failing record causes, stop the
/// stage before it sees a record. There is no default action and no check is skipped.
/// </remarks>
internal sealed class ValidateChecks
{
    private const string Kind = "Validate";

    private readonly List<(string Name, IRecordPredicate Predicate)> _checks;

    private ValidateChecks(IValidateFailAction action, List<(string Name, IRecordPredicate Predicate)> checks)
    {
        FailAction = action;
        _checks = checks;
    }

    /// <summary>Gets what a failing record causes.</summary>
    public IValidateFailAction FailAction { get; }

    public static IGenericResult<ValidateChecks> Prepare(PipelineTransformConfiguration config, ITransformContext context)
    {
        if (string.IsNullOrWhiteSpace(config.OnFail))
        {
            return GenericResult<ValidateChecks>.Failure(TransformLog.TransformParamsInvalid(
                context.Logger, config.Name, Kind, "a validate states what a failing record causes (onFail)"));
        }

        var action = ValidateFailActions.ByName(config.OnFail);
        if (ReferenceEquals(action, ValidateFailActions.NotFound))
        {
            return GenericResult<ValidateChecks>.Failure(TransformLog.TransformParamsInvalid(
                context.Logger, config.Name, Kind, $"onFail '{config.OnFail}' is not a validate failing action"));
        }

        if (config.Checks.Count == 0)
        {
            return GenericResult<ValidateChecks>.Failure(TransformLog.TransformParamsInvalid(
                context.Logger, config.Name, Kind, "a validate names at least one check"));
        }

        var checks = new List<(string Name, IRecordPredicate Predicate)>();
        foreach (var check in config.Checks.OrderBy(c => c.Ordinal))
        {
            var read = Read(config, check, context);
            if (read.IsFailure)
            {
                return read.ToNewResult<ValidateChecks>();
            }

            checks.Add((check.Name, read.Value!));
        }

        return GenericResult<ValidateChecks>.Success(new ValidateChecks(action, checks));
    }

    /// <summary>
    /// Asks the checks in order and returns the name of the first the record fails, or <see langword="null"/> when it
    /// passes them all.
    /// </summary>
    public string? FirstFailed(IDictionary<string, object?> record)
    {
        foreach (var (name, predicate) in _checks)
        {
            if (!predicate.Evaluate(record))
            {
                return name;
            }
        }

        return null;
    }

    private static IGenericResult<IRecordPredicate> Read(PipelineTransformConfiguration config, PipelineTransformCheckConfiguration check, ITransformContext context)
    {
        var expression = PipelineRunParameterReferences.Resolve(
            string.Concat("transform '", config.Name, "' check '", check.Name, "'"), check.Expression, context.Parameters, context.Logger);
        return expression.IsFailure
            ? expression.ToNewResult<IRecordPredicate>()
            : RecordPredicates.Parse(expression.Value!, context.Logger);
    }
}
