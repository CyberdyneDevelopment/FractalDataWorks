using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Etl.Predicates;
using Microsoft.Extensions.Logging;
using OptionTransformTypes = Fdw.Services.Etl.Abstractions.OptionTypes.TransformTypes;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Transform type that filters records based on a condition.
/// </summary>
/// <remarks>
/// The condition is written in the pipeline's condition language (see <see cref="RecordPredicates"/>) and read strictly: a
/// condition that cannot be read is refused when the pipeline is saved or validated and fails the run, naming the text. It
/// is never a condition that every record meets.
/// </remarks>
[TypeOption(typeof(OptionTransformTypes), "Filter")]
public sealed class FilterTransformType : TransformTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilterTransformType"/> class.
    /// </summary>
    public FilterTransformType() : base(
        name: "Filter",
        displayName: "Record Filter",
        description: "Filters records based on an expression - records not matching are excluded",
        category: "Filter",
        modifiesStructure: false,
        canFilterRecords: true,
        inputPorts: ["input"],
        outputPorts: ["output"])
    {
    }

    private static readonly StageMember[] OwnedMembers =
    [
        new("filterExpression", "Expression", true, "The condition a record must meet to stay; may reference {{run parameters}}")
    ];

    /// <inheritdoc />
    public override IReadOnlyList<StageMember> Members => OwnedMembers;

    /// <inheritdoc />
    public override Task<IGenericResult<IDictionary<string, object?>>> Transform(
        IDictionary<string, object?> input,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        if (configuration is not PipelineTransformConfiguration config)
        {
            return Task.FromResult(GenericResult<IDictionary<string, object?>>.Failure(
                EtlLog.WrongConfigurationType(context.Logger, "Filter", configuration.GetType().Name)));
        }

        if (string.IsNullOrWhiteSpace(config.FilterExpression))
        {
            return Task.FromResult(GenericResult<IDictionary<string, object?>>.Failure(
                EtlLog.FilterExpressionMissing(context.Logger, config.Name)));
        }

        var expression = PipelineRunParameterReferences.Resolve(
            string.Concat("transform '", config.Name, "'"), config.FilterExpression, context.Parameters, context.Logger);
        if (expression.IsFailure)
        {
            return Task.FromResult(expression.ToNewResult<IDictionary<string, object?>>());
        }

        var condition = FilterStageCondition.Prepare(expression.Value!, context);
        if (condition.IsFailure)
        {
            return Task.FromResult(condition.ToNewResult<IDictionary<string, object?>>());
        }

        var passes = condition.Value!.Passes(input);
        if (passes.IsFailure)
        {
            return Task.FromResult(passes.ToNewResult<IDictionary<string, object?>>());
        }

        if (passes.Value)
        {
            return Task.FromResult(GenericResult<IDictionary<string, object?>>.Success(input));
        }

        // Return null to indicate record should be filtered out
        return Task.FromResult(GenericResult<IDictionary<string, object?>>.Success(null!));
    }

    /// <inheritdoc />
    public override Task<IGenericResult<IEnumerable<IDictionary<string, object?>>>> TransformBatch(
        IEnumerable<IDictionary<string, object?>> inputs,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        if (configuration is not PipelineTransformConfiguration config)
        {
            return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                EtlLog.WrongConfigurationType(context.Logger, "Filter", configuration.GetType().Name)));
        }

        if (string.IsNullOrWhiteSpace(config.FilterExpression))
        {
            return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                EtlLog.FilterExpressionMissing(context.Logger, config.Name)));
        }

        var expression = PipelineRunParameterReferences.Resolve(
            string.Concat("transform '", config.Name, "'"), config.FilterExpression, context.Parameters, context.Logger);
        if (expression.IsFailure)
        {
            return Task.FromResult(expression.ToNewResult<IEnumerable<IDictionary<string, object?>>>());
        }

        var condition = FilterStageCondition.Prepare(expression.Value!, context);
        if (condition.IsFailure)
        {
            return Task.FromResult(condition.ToNewResult<IEnumerable<IDictionary<string, object?>>>());
        }

        var results = new List<IDictionary<string, object?>>();

        foreach (var input in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var passes = condition.Value!.Passes(input);
            if (passes.IsFailure)
            {
                return Task.FromResult(passes.ToNewResult<IEnumerable<IDictionary<string, object?>>>());
            }

            if (passes.Value)
            {
                results.Add(input);
            }
        }

        return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Success(results));
    }

    /// <inheritdoc />
    public override IGenericResult MapSpecToConfiguration(ITransformOperationSpec spec, IGenericConfiguration target, ILogger logger)
    {
        if (target is not PipelineTransformConfiguration config)
        {
            return GenericResult.Failure(EtlLog.WrongConfigurationType(logger, spec.Name, target.GetType().Name));
        }

        if (string.IsNullOrWhiteSpace(spec.FilterExpression))
        {
            return GenericResult.Failure(EtlLog.FilterExpressionMissing(logger, spec.Name));
        }

        // The text is read here, with any {{parameter}} placeholders still in it, so a condition that cannot be read is
        // refused when the pipeline is saved or validated and not first met by a run.
        var read = RecordPredicates.Parse(spec.FilterExpression, logger);
        if (read.IsFailure)
        {
            return read;
        }

        config.FilterExpression = spec.FilterExpression;

        EtlLog.TransformSpecMapped(logger, spec.Name, spec.OperationType);
        return GenericResult.Success();
    }
}
