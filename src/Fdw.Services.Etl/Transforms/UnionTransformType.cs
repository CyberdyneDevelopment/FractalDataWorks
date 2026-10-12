using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;
using OptionTransformTypes = Fdw.Services.Etl.Abstractions.OptionTypes.TransformTypes;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// The Union stage: appends all the rows of a second data set to the records that reach it (UNION ALL).
/// </summary>
/// <remarks>
/// FDW-1120 phase 4. The stage names the data set (<see cref="PipelineTransformConfiguration.UnionSourceDataSet"/>) and maps
/// its fields to the fields the stage outputs, one mapping per field, source field to destination field. The appended rows
/// enter at the stage's position, after every record the pipeline's own source produced, and pass through the stages that
/// follow it; chain a Distinct after the Union to drop duplicates. A Union keeps duplicates by design.
/// <para>
/// A Union needs the whole input: the records before it and the rows it appends must reach the stages after it together, so a
/// stage after it that needs the whole input (Aggregate, Sort, Distinct, LatestBy) sees both. It therefore sets
/// <see cref="TransformTypeBase.RequiresCompleteInput"/>, and a pipeline that holds one does not stream. A Union inside a Branch
/// path appends its rows to the records of that path, after the records the path took, and they do not meet the Branch's
/// conditions again.
/// </para>
/// <para>
/// The second data set is read through the run's data gateway as a data set (the same read path the pipeline's own data set
/// source uses), page by page, under the identity the run has. A run that handles one record at a time (a Streaming pipeline)
/// has no moment after the whole source, so the kind runs on <see cref="PipelineTypes"/> BatchCopy only: a Union stage is
/// refused when it is saved on any other pipeline type, and a run that reaches one anyway fails, loudly, instead of appending
/// nothing.
/// </para>
/// </remarks>
[TypeOption(typeof(OptionTransformTypes), "Union")]
public sealed class UnionTransformType : TransformTypeBase
{
    private const string Kind = "Union";

    // How many rows one read of the union source asks for. A page size is how the source is read, not a choice about what is read.
    private const int PageSize = 1000;

    // The pipeline types that hold the whole source before the stages after it run, named as a pipeline's pipelineType states them.
    private static readonly string[] BatchPipelineTypes = ["BatchCopy"];

    private static readonly StageMember[] OwnedMembers =
    [
        new("unionSource", "DataSetName", true, "The data set whose rows the stage appends after the last record the pipeline's source produced"),
        new("fieldMappings", "MappingList", true, "How the fields of the union source become the fields the stage outputs: each mapping states only a source field and a destination field")
    ];

    /// <summary>Initializes a new instance of the <see cref="UnionTransformType"/> class.</summary>
    public UnionTransformType() : base(
        name: Kind,
        displayName: "Union",
        description: "Appends all the rows of a second data set to the records that reach it, keeping duplicates",
        category: "Combine",
        modifiesStructure: false,
        canFilterRecords: false,
        inputPorts: ["input"],
        outputPorts: ["output"])
    {
    }

    /// <inheritdoc />
    public override bool RequiresCompleteInput => true;

    /// <inheritdoc />
    public override IReadOnlyList<StageMember> Members => OwnedMembers;

    /// <inheritdoc />
    /// <remarks>
    /// A Union appends after the whole source, which only a pipeline that works on a batch has; a pipeline that handles one
    /// record at a time has no such moment, so a Union stage is refused when it is saved on one.
    /// </remarks>
    public override IReadOnlyList<string>? PipelineTypes => BatchPipelineTypes;

    /// <inheritdoc />
    public override Task<IGenericResult<IDictionary<string, object?>>> Transform(
        IDictionary<string, object?> input,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        var name = configuration is PipelineTransformConfiguration config ? config.Name : Kind;
        return Task.FromResult(GenericResult<IDictionary<string, object?>>.Failure(UnionLog.UnionNeedsABatchRun(context.Logger, name)));
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<IEnumerable<IDictionary<string, object?>>>> TransformBatch(
        IEnumerable<IDictionary<string, object?>> inputs,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        if (configuration is not PipelineTransformConfiguration config)
        {
            return GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                EtlLog.WrongConfigurationType(context.Logger, Kind, configuration.GetType().Name));
        }

        if (string.IsNullOrWhiteSpace(config.UnionSourceDataSet))
        {
            return GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                TransformLog.TransformParamsInvalid(context.Logger, config.Name, Kind, "a union states the data set it appends (unionSource)"));
        }

        if (context.DataGateway is not IDataGateway gateway)
        {
            return GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                UnionLog.UnionHasNoGateway(context.Logger, config.Name, config.UnionSourceDataSet));
        }

        var appended = await ReadSource(gateway, config, config.UnionSourceDataSet, context, cancellationToken).ConfigureAwait(false);
        if (!appended.IsSuccess)
        {
            return appended.ToNewResult<IEnumerable<IDictionary<string, object?>>>();
        }

        var output = inputs.ToList();
        var mainCount = output.Count;
        output.AddRange(appended.Value!);
        UnionLog.UnionApplied(context.Logger, config.Name, mainCount, appended.Value!.Count, config.UnionSourceDataSet);
        return GenericResult<IEnumerable<IDictionary<string, object?>>>.Success(output);
    }

    /// <inheritdoc />
    public override IGenericResult MapSpecToConfiguration(ITransformOperationSpec spec, IGenericConfiguration target, ILogger logger)
    {
        if (target is not PipelineTransformConfiguration config)
        {
            return GenericResult.Failure(EtlLog.WrongConfigurationType(logger, spec.Name, target.GetType().Name));
        }

        if (spec is not ITransformUnionSpec union)
        {
            return GenericResult.Failure(TransformLog.TransformParamsInvalid(logger, spec.Name, Kind, "the request does not state a union source"));
        }

        var problem = FirstProblem(spec, union);
        if (problem is not null)
        {
            return GenericResult.Failure(TransformLog.TransformParamsInvalid(logger, spec.Name, Kind, problem));
        }

        config.UnionSourceDataSet = union.UnionSource;
        config.FieldMappings = spec.FieldMappings
            .Select(mapping => new PipelineTransformFieldMappingConfiguration
            {
                Name = mapping.DestinationField,
                SourceField = mapping.SourceField,
                DestinationField = mapping.DestinationField
            })
            .ToList();

        EtlLog.TransformSpecMapped(logger, spec.Name, spec.OperationType);
        return GenericResult.Success();
    }

    // Reads the whole union source, page by page, and maps every row to the fields the stage outputs.
    private static async Task<IGenericResult<List<IDictionary<string, object?>>>> ReadSource(
        IDataGateway gateway,
        PipelineTransformConfiguration config,
        string dataSet,
        ITransformContext context,
        CancellationToken cancellationToken)
    {
        var appended = new List<IDictionary<string, object?>>();
        var skip = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var query = new QueryCommand<Dictionary<string, object?>>
            {
                Paging = new PagingExpression { Skip = skip, Take = PageSize }
            };

            var page = await gateway.Execute<IEnumerable<Dictionary<string, object?>>>(query, new DataSetTarget(dataSet), cancellationToken).ConfigureAwait(false);
            if (!page.IsSuccess)
            {
                return GenericResult<List<IDictionary<string, object?>>>.Failure(
                    UnionLog.UnionSourceUnreadable(context.Logger, config.Name, dataSet, string.Concat(page.CurrentMessage)));
            }

            if (page.Value is null)
            {
                return GenericResult<List<IDictionary<string, object?>>>.Failure(
                    UnionLog.UnionSourceUnreadable(context.Logger, config.Name, dataSet, "the read answered success without rows"));
            }

            var rows = page.Value.ToList();
            foreach (var row in rows)
            {
                var mapped = MapRow(row, config, dataSet, context);
                if (!mapped.IsSuccess)
                {
                    return mapped.ToNewResult<List<IDictionary<string, object?>>>();
                }

                appended.Add(mapped.Value!);
            }

            skip += rows.Count;
            if (rows.Count < PageSize)
            {
                return GenericResult<List<IDictionary<string, object?>>>.Success(appended);
            }
        }
    }

    // One row of the union source as a record of the stage's output: each destination field takes the value of its source field.
    // A row that lacks a source field fails the run: the field was checked when the pipeline was saved, so its absence is a
    // data set that changed since, not something to fill in.
    private static IGenericResult<IDictionary<string, object?>> MapRow(
        Dictionary<string, object?> row, PipelineTransformConfiguration config, string dataSet, ITransformContext context)
    {
        var record = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in config.FieldMappings)
        {
            if (!row.TryGetValue(mapping.SourceField, out var value))
            {
                return GenericResult<IDictionary<string, object?>>.Failure(
                    UnionLog.UnionRowLacksField(context.Logger, config.Name, dataSet, mapping.SourceField));
            }

            record[mapping.DestinationField] = value;
        }

        return GenericResult<IDictionary<string, object?>>.Success(record);
    }

    // What a request for a Union stage must state: the data set, and at least one mapping that states a source field and a
    // destination field and nothing else (a Map stage after the Union does conversions and expressions).
    private static string? FirstProblem(ITransformOperationSpec spec, ITransformUnionSpec union)
    {
        if (string.IsNullOrWhiteSpace(union.UnionSource))
        {
            return "a union states the data set it appends (unionSource)";
        }

        if (spec.FieldMappings.Count == 0)
        {
            return "a union maps at least one field of its source (fieldMappings)";
        }

        var mappingProblem = spec.FieldMappings.Select(MappingProblem).FirstOrDefault(problem => problem is not null);
        return mappingProblem is null ? DuplicateDestination(spec.FieldMappings) : mappingProblem;
    }

    private static string? MappingProblem(IFieldMapping mapping)
    {
        if (string.IsNullOrWhiteSpace(mapping.SourceField) || string.IsNullOrWhiteSpace(mapping.DestinationField))
        {
            return "every field mapping of a union states a source field and a destination field";
        }

        var foreign = ForeignMember(mapping);
        return foreign is null ? null : $"a field mapping of a union states only a source field and a destination field; '{foreign}' on '{mapping.DestinationField}' belongs to a Map stage";
    }

    private static string? ForeignMember(IFieldMapping mapping)
    {
        if (!string.IsNullOrEmpty(mapping.TransformExpression))
        {
            return "transformExpression";
        }

        if (mapping.DefaultValue is not null)
        {
            return "defaultValue";
        }

        if (!string.IsNullOrEmpty(mapping.TargetType))
        {
            return "targetType";
        }

        if (mapping.IsRequired)
        {
            return "isRequired";
        }

        return mapping.IsEnabled ? null : "isEnabled";
    }

    private static string? DuplicateDestination(IReadOnlyList<IFieldMapping> mappings)
    {
        var twice = mappings.GroupBy(mapping => mapping.DestinationField, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
        return twice is null ? null : $"destination field '{twice.Key}' is mapped more than once";
    }
}
