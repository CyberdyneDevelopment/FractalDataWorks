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
/// Transform type that keeps one record per key: the one with the first or the last value of an order field.
/// </summary>
/// <remarks>
/// Why: choosing the winner of a key needs every record of that key, so LatestBy needs complete input and has no
/// single-record form. The key fields are the transform's <see cref="PipelineTransformConfiguration.GroupByFields"/>; the
/// <see cref="PipelineTransformConfiguration.KeepRule"/> says whether the smallest (First) or the largest (Last) order
/// value wins, and of records equal on the order value First keeps the earliest and Last the latest in input order. The
/// survivors come out in the order their keys first appeared.
/// </remarks>
[TypeOption(typeof(OptionTransformTypes), "LatestBy")]
public sealed class LatestByTransformType : TransformTypeBase
{
    private const string Kind = "LatestBy";

    /// <summary>
    /// Initializes a new instance of the <see cref="LatestByTransformType"/> class.
    /// </summary>
    public LatestByTransformType() : base(
        name: Kind,
        displayName: "Latest by key",
        description: "Keeps one record per key: the one with the first or last value of an order field",
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
        new("latestBy", "LatestBy", true, "Key columns, the ordering column, and whether the first or last row of each key is kept")
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
        var problem = FirstProblem(keyFields, config.OrderField, config.KeepRule);
        if (problem is not null)
        {
            return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                TransformLog.TransformParamsInvalid(context.Logger, config.Name, Kind, problem)));
        }

        return Task.FromResult(Latest(inputs, config, keyFields, context));
    }

    /// <inheritdoc />
    public override IGenericResult MapSpecToConfiguration(ITransformOperationSpec spec, IGenericConfiguration target, ILogger logger)
    {
        if (target is not PipelineTransformConfiguration config)
        {
            return GenericResult.Failure(EtlLog.WrongConfigurationType(logger, spec.Name, target.GetType().Name));
        }

        if (spec is not ITransformOrderingSpec ordering)
        {
            return GenericResult.Failure(TransformLog.TransformParamsInvalid(logger, spec.Name, Kind, "the request cannot carry an order field and a keep rule"));
        }

        var problem = FirstProblem(spec.GroupByFields, ordering.OrderField, ordering.KeepRule);
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
        config.OrderField = ordering.OrderField;
        config.KeepRule = ordering.KeepRule;

        EtlLog.TransformSpecMapped(logger, spec.Name, spec.OperationType);
        return GenericResult.Success();
    }

    private static string? FirstProblem(IReadOnlyList<string> keyFields, string? orderField, string? keepRule)
    {
        if (keyFields.Count == 0)
        {
            return "a latest-by names at least one key field";
        }

        if (keyFields.Any(string.IsNullOrWhiteSpace))
        {
            return "every key field of a latest-by is named";
        }

        var twice = keyFields.GroupBy(f => f, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (twice is not null)
        {
            return $"key field '{twice.Key}' is listed twice";
        }

        if (string.IsNullOrWhiteSpace(orderField))
        {
            return "a latest-by names its order field";
        }

        if (string.IsNullOrWhiteSpace(keepRule))
        {
            return "a latest-by states its keep rule, First or Last";
        }

        return ReferenceEquals(KeepRules.ByName(keepRule), KeepRules.NotFound)
            ? $"keep rule '{keepRule}' is not First or Last"
            : null;
    }

    private static IGenericResult<IEnumerable<IDictionary<string, object?>>> Latest(
        IEnumerable<IDictionary<string, object?>> inputs,
        PipelineTransformConfiguration config,
        List<string> keyFields,
        ITransformContext context)
    {
        var rule = KeepRules.ByName(config.KeepRule!);
        var order = new OrderValueComparer();
        var kept = new Dictionary<object?[], (int Position, IDictionary<string, object?> Record, object? OrderValue)>(new KeyValuesComparer());
        var inCount = 0;
        foreach (var record in inputs)
        {
            if (!RecordKeyReader.TryRead(record, keyFields, out var key, out var missing))
            {
                return GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                    TransformLog.RecordFieldMissing(context.Logger, config.Name, Kind, missing!));
            }

            if (!record.TryGetValue(config.OrderField!, out var orderValue))
            {
                return GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                    TransformLog.RecordFieldMissing(context.Logger, config.Name, Kind, config.OrderField!));
            }

            var normalised = KeyValuesComparer.Normalise(key);
            var seen = kept.TryGetValue(normalised, out var current);
            if (!seen || rule.Replaces(order.Compare(orderValue, current.OrderValue)))
            {
                kept[normalised] = (seen ? current.Position : inCount, record, orderValue);
            }

            inCount++;
        }

        if (order.Problem is { } incomparable)
        {
            return GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                TransformLog.ValuesNotComparable(context.Logger, config.Name, Kind, config.OrderField!, incomparable.Left, incomparable.Right));
        }

        var result = kept.Values.OrderBy(entry => entry.Position).Select(entry => entry.Record).ToList();
        TransformLog.LatestByApplied(context.Logger, config.Name, inCount, result.Count, string.Join(",", keyFields), config.OrderField!, config.KeepRule!);
        return GenericResult<IEnumerable<IDictionary<string, object?>>>.Success(result);
    }
}
