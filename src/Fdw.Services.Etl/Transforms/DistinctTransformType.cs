using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;
using OptionTransformTypes = Fdw.Services.Etl.Abstractions.OptionTypes.TransformTypes;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Transform type that keeps the first record of each key and drops the rest.
/// </summary>
/// <remarks>
/// Why: which record of a key survives is the input order's call, so Distinct needs complete input and has no
/// single-record form. The key fields are the transform's <see cref="PipelineTransformConfiguration.GroupByFields"/>;
/// keys are compared the way the Aggregate transform groups (numbers by value, strings ignoring case). The survivors keep
/// their input order.
/// </remarks>
[TypeOption(typeof(OptionTransformTypes), "Distinct")]
public sealed class DistinctTransformType : TransformTypeBase
{
    private const string Kind = "Distinct";

    /// <summary>
    /// Initializes a new instance of the <see cref="DistinctTransformType"/> class.
    /// </summary>
    public DistinctTransformType() : base(
        name: Kind,
        displayName: "Distinct",
        description: "Keeps the first record of each key and drops the repeats",
        category: "Filter",
        modifiesStructure: false,
        canFilterRecords: true,
        inputPorts: ["input"],
        outputPorts: ["output"])
    {
    }

    /// <inheritdoc />
    public override bool RequiresCompleteInput => true;

    private static readonly StageMember[] OwnedMembers =
    [
        new("distinct", "Distinct", true, "Key columns; the first row of each key is kept")
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
        var name = (configuration as PipelineTransformConfiguration)?.Name ?? Kind;
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
                EtlLog.WrongConfigurationType(context.Logger, Kind, configuration.GetType().Name)));
        }

        var keyFields = config.GroupByFields.OrderBy(f => f.Ordinal).Select(f => f.FieldName).ToList();
        var problem = FirstProblem(keyFields);
        if (problem is not null)
        {
            return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                TransformLog.TransformParamsInvalid(context.Logger, config.Name, Kind, problem)));
        }

        var seen = new HashSet<object?[]>(new KeyValuesComparer());
        var kept = new List<IDictionary<string, object?>>();
        var inCount = 0;
        foreach (var record in inputs)
        {
            inCount++;
            if (!RecordKeyReader.TryRead(record, keyFields, out var key, out var missing))
            {
                return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                    TransformLog.RecordFieldMissing(context.Logger, config.Name, Kind, missing!)));
            }

            if (seen.Add(KeyValuesComparer.Normalise(key)))
            {
                kept.Add(record);
            }
        }

        TransformLog.DistinctApplied(context.Logger, config.Name, inCount, kept.Count, string.Join(",", keyFields));
        return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Success(kept));
    }

    /// <inheritdoc />
    public override IGenericResult MapSpecToConfiguration(ITransformOperationSpec spec, IGenericConfiguration target, ILogger logger)
    {
        if (target is not PipelineTransformConfiguration config)
        {
            return GenericResult.Failure(EtlLog.WrongConfigurationType(logger, spec.Name, target.GetType().Name));
        }

        var problem = FirstProblem(spec.GroupByFields);
        if (problem is not null)
        {
            return GenericResult.Failure(TransformLog.TransformParamsInvalid(logger, spec.Name, Kind, problem));
        }

        config.GroupByFields = spec.GroupByFields
            .Select((field, ordinal) => new PipelineTransformGroupByFieldConfiguration
            {
                Name = field,
                FieldName = field,
                Ordinal = ordinal
            })
            .ToList();

        EtlLog.TransformSpecMapped(logger, spec.Name, spec.OperationType);
        return GenericResult.Success();
    }

    private static string? FirstProblem(IReadOnlyList<string> keyFields)
    {
        if (keyFields.Count == 0)
        {
            return "a distinct names at least one key field";
        }

        if (keyFields.Any(string.IsNullOrWhiteSpace))
        {
            return "every key field of a distinct is named";
        }

        var twice = keyFields.GroupBy(f => f, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        return twice is null ? null : $"key field '{twice.Key}' is listed twice";
    }
}
