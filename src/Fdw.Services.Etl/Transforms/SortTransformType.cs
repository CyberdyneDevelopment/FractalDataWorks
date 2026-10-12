using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;
using OptionTransformTypes = Fdw.Services.Etl.Abstractions.OptionTypes.TransformTypes;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Transform type that orders the records by one or more fields, each ascending or descending.
/// </summary>
/// <remarks>
/// Why: an ordering is a property of the whole set, so like the aggregate it needs complete input and has no
/// single-record form. The ordering is stable (records equal on every sort field keep their input order), nulls sort
/// first in ascending order, and values of one field that have no common order fail the step rather than produce an
/// arbitrary one.
/// </remarks>
[TypeOption(typeof(OptionTransformTypes), "Sort")]
public sealed class SortTransformType : TransformTypeBase
{
    private const string Kind = "Sort";

    /// <summary>
    /// Initializes a new instance of the <see cref="SortTransformType"/> class.
    /// </summary>
    public SortTransformType() : base(
        name: Kind,
        displayName: "Sort",
        description: "Orders the records by one or more fields, each ascending or descending",
        category: "Order",
        modifiesStructure: false,
        canFilterRecords: false,
        inputPorts: ["input"],
        outputPorts: ["output"])
    {
    }

    /// <inheritdoc />
    public override bool RequiresCompleteInput => true;

    private static readonly StageMember[] OwnedMembers =
    [
        new("sort", "SortFieldList", true, "Fields the output is ordered by, each ascending or descending")
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

        var problem = FirstProblem(config.SortFields.Select(f => (f.FieldName, f.Direction)).ToList());
        if (problem is not null)
        {
            return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                TransformLog.TransformParamsInvalid(context.Logger, config.Name, Kind, problem)));
        }

        var fields = config.SortFields.OrderBy(f => f.Ordinal).ToList();
        var records = inputs.ToList();
        var names = fields.Select(f => f.FieldName).ToList();
        foreach (var record in records)
        {
            if (!RecordKeyReader.TryRead(record, names, out _, out var missing))
            {
                return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                    TransformLog.RecordFieldMissing(context.Logger, config.Name, Kind, missing!)));
            }
        }

        var values = new OrderValueComparer();
        var ordered = Order(records, fields, values);
        if (values.Problem is { } incomparable)
        {
            return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                TransformLog.ValuesNotComparable(context.Logger, config.Name, Kind, string.Join(",", names), incomparable.Left, incomparable.Right)));
        }

        TransformLog.SortApplied(context.Logger, config.Name, ordered.Count, string.Join(",", fields.Select(f => $"{f.FieldName} {f.Direction}")));
        return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Success(ordered));
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
            return GenericResult.Failure(TransformLog.TransformParamsInvalid(logger, spec.Name, Kind, "the request cannot carry sort fields"));
        }

        var problem = FirstProblem(ordering.SortFields.Select(f => (f.FieldName, f.Direction)).ToList());
        if (problem is not null)
        {
            return GenericResult.Failure(TransformLog.TransformParamsInvalid(logger, spec.Name, Kind, problem));
        }

        config.SortFields = ordering.SortFields
            .Select((field, ordinal) => new PipelineTransformSortFieldConfiguration
            {
                Name = field.FieldName,
                FieldName = field.FieldName,
                Direction = field.Direction,
                Ordinal = ordinal
            })
            .ToList();

        EtlLog.TransformSpecMapped(logger, spec.Name, spec.OperationType);
        return GenericResult.Success();
    }

    // Why stated here and shared by the runtime and the spec mapping: a Sort step that cannot run is refused when it is
    // created, and the engine refuses the same step again if it ever reaches a run.
    private static string? FirstProblem(List<(string FieldName, string Direction)> fields)
    {
        if (fields.Count == 0)
        {
            return "a sort names at least one field";
        }

        if (fields.Any(f => string.IsNullOrWhiteSpace(f.FieldName)))
        {
            return "every sort field names its field";
        }

        var twice = fields.GroupBy(f => f.FieldName, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (twice is not null)
        {
            return $"field '{twice.Key}' is sorted twice";
        }

        var unknown = fields.FirstOrDefault(f => string.IsNullOrWhiteSpace(f.Direction) || ReferenceEquals(SortDirections.ByName(f.Direction), SortDirections.NotFound));
        return unknown.FieldName is null
            ? null
            : $"sort field '{unknown.FieldName}' has direction '{unknown.Direction}', which is not Ascending or Descending";
    }

    private static List<IDictionary<string, object?>> Order(
        List<IDictionary<string, object?>> records,
        List<PipelineTransformSortFieldConfiguration> fields,
        OrderValueComparer values)
    {
        IOrderedEnumerable<IDictionary<string, object?>>? ordered = null;
        foreach (var field in fields)
        {
            var direction = SortDirections.ByName(field.Direction);
            var comparer = Comparer<object?>.Create((left, right) => direction.IsAscending ? values.Compare(left, right) : -values.Compare(left, right));
            ordered = ordered is null
                ? records.OrderBy(r => r[field.FieldName], comparer)
                : ordered.ThenBy(r => r[field.FieldName], comparer);
        }

        return ordered!.ToList();
    }
}
