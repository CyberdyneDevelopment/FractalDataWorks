using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;
using OptionTransformTypes = Fdw.Services.Etl.Abstractions.OptionTypes.TransformTypes;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Transform type that aggregates records by grouping and applying aggregate functions.
/// </summary>
/// <remarks>
/// Why: aggregation is inherently set-based (it reduces N rows to M groups) — there is no meaningful
/// single-record aggregation, so <see cref="Transform"/> always fails loud and the real work lives in
/// <see cref="TransformBatch"/>, invoked by <c>BatchCopyPipeline</c>'s set-based transform fold.
/// </remarks>
[TypeOption(typeof(OptionTransformTypes), "Aggregate")]
public sealed class AggregateTransformType : TransformTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateTransformType"/> class.
    /// </summary>
    public AggregateTransformType() : base(
        id: 3,
        name: "Aggregate",
        displayName: "Aggregation",
        description: "Aggregates records by grouping and applying functions like SUM, COUNT, AVG, MIN, MAX",
        category: "Aggregate",
        modifiesStructure: true,
        canFilterRecords: true)
    {
    }

    /// <inheritdoc />
    public override Task<IGenericResult<IDictionary<string, object?>>> Transform(
        IDictionary<string, object?> input,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        var name = (configuration as PipelineTransformConfiguration)?.Name ?? "Aggregate";
        return Task.FromResult(GenericResult<IDictionary<string, object?>>.Failure(
            EtlLog.TransformRequiresBatchExecution(context.Logger, name)));
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
                EtlLog.WrongConfigurationType(context.Logger, "Aggregate", configuration.GetType().Name)));
        }

        if (config.GroupByFields.Count == 0 || config.Aggregations.Count == 0)
        {
            return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                EtlLog.AggregateParamsMissing(context.Logger, config.Name)));
        }

        var inputList = inputs.ToList();
        var groupByFields = config.GroupByFields.OrderBy(f => f.Ordinal).Select(f => f.FieldName).ToList();

        var groups = inputList.GroupBy(
            record => string.Join("|", groupByFields.Select(f =>
                record.TryGetValue(f, out var v) ? v?.ToString() ?? "" : "")),
            StringComparer.OrdinalIgnoreCase);

        var results = new List<IDictionary<string, object?>>();

        foreach (var group in groups)
        {
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            var groupRecords = group.ToList();
            var firstRecord = groupRecords[0];

            foreach (var field in groupByFields)
            {
                if (firstRecord.TryGetValue(field, out var value))
                {
                    result[field] = value;
                }
            }

            foreach (var agg in config.Aggregations.OrderBy(a => a.ExecutionOrder))
            {
                var function = AggregateFunctions.ByName(agg.AggregateFunction);
                if (function == AggregateFunctions.NotFound)
                {
                    return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                        EtlLog.UnknownAggregateFunction(context.Logger, agg.AggregateFunction, config.Name)));
                }

                var values = groupRecords
                    .Where(r => r.TryGetValue(agg.SourceField, out var v) && v != null)
                    .Select(r => r[agg.SourceField])
                    .ToList();

                result[agg.OutputField] = values.Count == 0 ? null : function.Apply(values);
            }

            results.Add(result);
        }

        EtlLog.AggregateGrouped(context.Logger, config.Name, inputList.Count, results.Count, string.Join(",", groupByFields));

        return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Success(results));
    }

    /// <inheritdoc />
    public override IGenericResult MapSpecToConfiguration(ITransformOperationSpec spec, IGenericConfiguration target, ILogger logger)
    {
        if (target is not PipelineTransformConfiguration config)
        {
            return GenericResult.Failure(EtlLog.WrongConfigurationType(logger, spec.Name, target.GetType().Name));
        }

        if (spec.GroupByFields.Count == 0 || spec.Aggregations.Count == 0)
        {
            return GenericResult.Failure(EtlLog.AggregateParamsMissing(logger, spec.Name));
        }

        foreach (var agg in spec.Aggregations)
        {
            if (AggregateFunctions.ByName(agg.Function) == AggregateFunctions.NotFound)
            {
                return GenericResult.Failure(EtlLog.UnknownAggregateFunction(logger, agg.Function, spec.Name));
            }
        }

        config.GroupByFields = spec.GroupByFields
            .Select((field, ordinal) => new PipelineTransformGroupByFieldConfiguration
            {
                PipelineTransformId = config.Id,
                Name = field,
                FieldName = field,
                Ordinal = ordinal
            })
            .ToList();

        config.Aggregations = spec.Aggregations
            .Select((agg, executionOrder) => new PipelineTransformAggregationConfiguration
            {
                PipelineTransformId = config.Id,
                Name = agg.OutputField,
                SourceField = agg.SourceField,
                AggregateFunction = agg.Function,
                OutputField = agg.OutputField,
                ExecutionOrder = executionOrder
            })
            .ToList();

        EtlLog.TransformSpecMapped(logger, spec.Name, spec.OperationType);
        return GenericResult.Success();
    }
}
