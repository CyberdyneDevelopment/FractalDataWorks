using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Results;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Maps a stored pipeline, transforms included, to <see cref="PipelineDetailResponse"/>.
/// </summary>
public static class PipelineDetailMapper
{
    /// <summary>
    /// Reads a pipeline through its domain provider and maps it. A write answers with this so the
    /// response is the pipeline as stored, not an echo of the request.
    /// </summary>
    public static async Task<IGenericResult<PipelineDetailResponse>> Read(
        PipelineServiceConfigurationProvider provider, ITransformationConfigurationProvider transformations, string name, CancellationToken ct)
    {
        var result = await provider.Get(name, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.ToNewResult<PipelineDetailResponse>();
        }

        return result.Value is null
            ? GenericResult<PipelineDetailResponse>.Failure(ServicesResultCodes.ByName("ConfigurationNotFound"))
            : await Detail(result.Value, transformations, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps a stored pipeline to its detail DTO with the name of each Transformation its field mappings chain beside the id,
    /// read from the Transformation provider with one query for the whole pipeline (see <see cref="PipelineChainNames"/>).
    /// </summary>
    /// <param name="pipeline">The stored pipeline.</param>
    /// <param name="transformations">The Transformation provider, read as the caller.</param>
    /// <param name="ct">Cancels the read.</param>
    /// <returns>The detail, or the failure of reading the Transformation names.</returns>
    public static async Task<IGenericResult<PipelineDetailResponse>> Detail(
        IPipelineImplementationConfiguration pipeline, ITransformationConfigurationProvider transformations, CancellationToken ct)
    {
        var names = await PipelineChainNames.Read(pipeline, transformations, ct).ConfigureAwait(false);
        return names.IsSuccess
            ? GenericResult<PipelineDetailResponse>.Success(Map(pipeline, names.Value!))
            : names.ToNewResult<PipelineDetailResponse>();
    }

    /// <summary>
    /// Extracts the transform DTOs the pipeline's implementation carries.
    /// Returns an empty list for an implementation that has no transforms.
    /// </summary>
    private static List<PipelineTransformDto> ExtractTransforms(
        IPipelineImplementationConfiguration pipeline, IReadOnlyDictionary<Guid, string> transformationNames)
    {
        var transforms = new List<PipelineTransformDto>();
        if (pipeline is IEtlPipelineImplementationConfiguration { Transforms: { } operations })
        {
            foreach (var op in operations)
                transforms.Add(MapTransformToDto(op, transformationNames));
        }
        return transforms;
    }

    /// <summary>
    /// Maps a stored pipeline to its detail DTO without the names of the Transformations its field mappings chain: each chain
    /// step carries its id only. For a caller that does not hand the chain on; a read the caller sees uses
    /// <see cref="Detail"/>.
    /// </summary>
    /// <param name="pipeline">The stored pipeline.</param>
    /// <returns>The detail.</returns>
    public static PipelineDetailResponse Map(IPipelineImplementationConfiguration pipeline) =>
        Map(pipeline, new Dictionary<Guid, string>());

    /// <summary>
    /// Maps a stored pipeline to its detail DTO. GET, create and update all answer with this, so a
    /// write returns exactly what a later read of the same pipeline returns.
    /// </summary>
    /// <param name="pipeline">The stored pipeline.</param>
    /// <param name="transformationNames">
    /// The name of each Transformation the caller can read, by id. A chain step whose Transformation is not in it carries its
    /// id and a null name.
    /// </param>
    /// <returns>The detail.</returns>
    public static PipelineDetailResponse Map(IPipelineImplementationConfiguration pipeline, IReadOnlyDictionary<Guid, string> transformationNames)
    {
        // A domain read hands back the implementation, so the linkage is on the record itself --
        // there is no header to reach through. Description is a column of the engine's own row and rides on
        // the implementation. The schedule flag is still the app's to fill.
        var etl = pipeline as IEtlPipelineImplementationConfiguration;
        var dataStoreSource = pipeline as IDataStoreSourcedPipelineConfiguration;
        var dataStoreDestination = pipeline as IDataStoreDestinedPipelineConfiguration;

        var response = new PipelineDetailResponse
        {
            Id = pipeline.Id,
            Name = pipeline.Name,
            PipelineType = pipeline.Implementation,
            Description = pipeline.Description,
            SourceDataSet = etl?.SourceDataSet,
            SourceDataStoreName = dataStoreSource?.SourceDataStoreName,
            SourceDataStorePath = dataStoreSource?.SourceDataStorePath,
            SourceContainerName = dataStoreSource?.SourceContainerName,
            DestinationDataSet = etl?.DestinationDataSet,
            DestinationDataStoreName = dataStoreDestination?.DestinationDataStoreName,
            DestinationDataStorePath = dataStoreDestination?.DestinationDataStorePath,
            DestinationContainerName = dataStoreDestination?.DestinationContainerName,
            IsEnabled = etl?.IsEnabled ?? false,
            CreatedAt = pipeline.CreateDate,
            UpdatedAt = pipeline.ModifyDate,
            Transforms = ExtractTransforms(pipeline, transformationNames)
        };

        // The load definition exactly as saved (FDW-1110): the pipeline's load mode, its own load's options and value
        // sources, its steps in order, and its declared run parameters.
        if (pipeline is IPipelineLoadConfiguration load)
        {
            response.LoadMode = PipelineLoadReader.LoadMode(load);
            response.LoadOptions = PipelineLoadReader.Options(load);
            response.ValueSources = PipelineLoadReader.ValueSources(load);
            response.LoadSteps = PipelineLoadReader.Steps(load);
            response.RejectSteps = PipelineLoadReader.RejectSteps(load);
            response.Parameters = PipelineLoadReader.Parameters(load);
            response.TruncateBeforeLoad = PipelineTruncateConsistency.Derive(load.LoadMode);
        }

        return response;
    }

    /// <summary>Maps a transform configuration to its DTO.</summary>
    private static PipelineTransformDto MapTransformToDto(PipelineTransformConfiguration transform, IReadOnlyDictionary<Guid, string> transformationNames)
    {
        return new PipelineTransformDto
        {
            Id = transform.Id,
            Name = transform.Name,
            OperationType = transform.OperationType,
            ExecutionOrder = transform.ExecutionOrder,
            IsEnabled = transform.IsEnabled,
            FilterExpression = transform.FilterExpression,
            Aggregation = MapAggregationDto(transform),
            Sort = MapSortDto(transform),
            LatestBy = MapLatestByDto(transform),
            Distinct = MapDistinctDto(transform),
            Checks = MapChecksDto(transform),
            OnFail = transform.OnFail,
            Paths = MapPathsDto(transform, transformationNames),
            Unmatched = IsComposite(transform) ? transform.Unmatched : null,
            Notification = MapNotificationDto(transform),
            UnionSource = IsUnion(transform) ? transform.UnionSourceDataSet : null,
            Compute = MapComputeDto(transform, transformationNames),
            Lookup = MapLookupDto(transform),
            Calculation = MapCalculationDto(transform),
            FieldMappings = MapFieldMappingsDto(transform, transformationNames),
        };
    }

    /// <summary>Maps a Branch stage's paths to the contract's <c>paths</c> in the order they are tried, each with its stages in execution order, or null for any other stage.</summary>
    private static List<PipelinePathDto>? MapPathsDto(PipelineTransformConfiguration transform, IReadOnlyDictionary<Guid, string> transformationNames) =>
        IsComposite(transform)
            ? [.. transform.Paths
                .OrderBy(path => path.Ordinal)
                .Select(path => new PipelinePathDto
                {
                    Name = path.Name,
                    Condition = path.Condition,
                    Stages = [.. path.Stages.OrderBy(stage => stage.ExecutionOrder).Select(stage => MapTransformToDto(stage, transformationNames))],
                })]
            : null;

    private static bool IsComposite(PipelineTransformConfiguration transform) =>
        TransformTypes.ByName(transform.OperationType) is ICompositeTransformType;

    /// <summary>Maps a Compute stage's Built transformation and input mapping to the contract's <c>compute</c>, or null for any other stage; the transformation is named when the caller can read it and always given by id.</summary>
    private static PipelineCompute? MapComputeDto(PipelineTransformConfiguration transform, IReadOnlyDictionary<Guid, string> transformationNames) =>
        TransformTypes.ByName(transform.OperationType) is ComputeTransformType
            ? new PipelineCompute
            {
                TransformationId = transform.ComputeTransformationId,
                Transformation = transform.ComputeTransformationId is { } id && transformationNames.TryGetValue(id, out var name) ? name : null,
                Inputs = [.. transform.ComputeInputs.Select(input => new PipelineComputeInput { InputName = input.InputName, SourceField = input.SourceField })],
                Tables = [.. transform.ComputeTables.Select(table => new PipelineComputeTable { Input = table.TableName, From = table.FromKind, Name = table.FromName })],
                Outputs = [.. transform.ComputeOutputs.Select(output => new PipelineComputeOutput { Output = output.OutputName, Route = output.Route, Name = output.RouteName })],
                Scalars = [.. transform.ComputeScalars.Select(scalar => new PipelineComputeScalar { Input = scalar.InputName, Value = scalar.ValueText, ValueType = scalar.ValueType })],
                OnRowFailure = transform.ComputeOnRowFailure,
            }
            : null;

    private static bool IsUnion(PipelineTransformConfiguration transform) =>
        TransformTypes.ByName(transform.OperationType) is UnionTransformType;

    /// <summary>Maps the typed FieldMappings children to a list of <see cref="PipelineFieldMappingDto"/>.</summary>
    private static List<PipelineFieldMappingDto> MapFieldMappingsDto(PipelineTransformConfiguration transform, IReadOnlyDictionary<Guid, string> transformationNames)
    {
        return transform.FieldMappings
            .Select(m => new PipelineFieldMappingDto
            {
                Id = m.Id,
                Name = m.Name,
                SourceField = m.SourceField,
                DestinationField = m.DestinationField,
                TargetType = m.TargetType,
                IsEnabled = m.IsEnabled,
                IsRequired = m.IsRequired,
                DefaultValue = m.DefaultValue,
                Transformations = ChainOf(m, transformationNames),
            })
            .ToList();
    }

    // The chain of named Transformations the mapping carries, in order; null (not empty) when it states none, as the request does.
    // Each step carries the id it is stored by and, when the caller can read that Transformation, its name; a Transformation the
    // caller cannot read has a null name and nothing is put in its place.
    private static List<PipelineFieldTransformationReference>? ChainOf(
        PipelineTransformFieldMappingConfiguration mapping, IReadOnlyDictionary<Guid, string> transformationNames) =>
        mapping.TransformationChain.Count == 0
            ? null
            : [.. mapping.TransformationChain
                .OrderBy(step => step.Ordinal)
                .Select(step => new PipelineFieldTransformationReference
                {
                    TransformationId = step.TransformationId,
                    Transformation = transformationNames.TryGetValue(step.TransformationId, out var name) ? name : null,
                    Ordinal = step.Ordinal,
                })];

    /// <summary>Maps the typed GroupByFields/Aggregations children to an <see cref="AggregationDto"/>, or null when absent.</summary>
    private static AggregationDto? MapAggregationDto(PipelineTransformConfiguration transform)
    {
        // Why the guard: LatestBy and Distinct keep their key columns in the GroupByFields an Aggregate groups by; they are
        // not Aggregates and carry no aggregation DTO.
        if ((transform.GroupByFields.Count == 0 && transform.Aggregations.Count == 0) || IsLatestBy(transform) || IsDistinct(transform))
        {
            return null;
        }

        return new AggregationDto
        {
            GroupByFields = transform.GroupByFields
                .OrderBy(f => f.Ordinal)
                .Select(f => f.FieldName)
                .ToList(),
            Aggregations = transform.Aggregations
                .OrderBy(a => a.ExecutionOrder)
                .Select(a => new AggregationItemDto { SourceField = a.SourceField, Function = a.AggregateFunction, OutputField = a.OutputField })
                .ToList(),
        };
    }

    // Why the stored OperationType decides: LatestBy and Distinct keep their key columns in the same GroupByFields an
    // Aggregate groups by, so the transform's own type says which of them a row is. The transform type option answers it.
    /// <summary>Whether the transform is a LatestBy, by its stored operation type.</summary>
    private static bool IsLatestBy(PipelineTransformConfiguration transform) => TransformTypes.ByName(transform.OperationType) is LatestByTransformType;

    /// <summary>Whether the transform is a Distinct, by its stored operation type.</summary>
    private static bool IsDistinct(PipelineTransformConfiguration transform) => TransformTypes.ByName(transform.OperationType) is DistinctTransformType;

    /// <summary>Maps a Sort transform's ordered fields to the contract's <c>sort</c>, or null for any other transform.</summary>
    private static List<PipelineSortField>? MapSortDto(PipelineTransformConfiguration transform) =>
        TransformTypes.ByName(transform.OperationType) is SortTransformType
            ? [.. transform.SortFields.OrderBy(f => f.Ordinal).Select(f => new PipelineSortField { Field = f.FieldName, Direction = f.Direction })]
            : null;

    /// <summary>Maps a Validate transform's checks to the contract's <c>checks</c>, or null for any other transform.</summary>
    private static List<PipelineCheck>? MapChecksDto(PipelineTransformConfiguration transform) =>
        TransformTypes.ByName(transform.OperationType) is ValidateTransformType
            ? [.. transform.Checks.OrderBy(c => c.Ordinal).Select(c => new PipelineCheck { Name = c.Name, Expression = c.Expression })]
            : null;

    /// <summary>Maps a Notify stage's rule, run point and message to the contract's <c>notification</c>, or null for any other stage.</summary>
    private static PipelineNotification? MapNotificationDto(PipelineTransformConfiguration transform) =>
        TransformTypes.ByName(transform.OperationType) is NotifyTransformType
            ? new PipelineNotification { RuleName = transform.NotificationRuleName!, RunPoint = transform.RunPoint!, Message = transform.NotificationMessage! }
            : null;

    /// <summary>Maps a LatestBy transform to the contract's <c>latestBy</c>, or null for any other transform.</summary>
    private static PipelineLatestBy? MapLatestByDto(PipelineTransformConfiguration transform) =>
        IsLatestBy(transform)
            ? new PipelineLatestBy { KeyColumns = KeyColumns(transform), OrderColumn = transform.OrderField!, Keep = transform.KeepRule! }
            : null;

    /// <summary>Maps a Distinct transform to the contract's <c>distinct</c>, or null for any other transform.</summary>
    private static PipelineDistinct? MapDistinctDto(PipelineTransformConfiguration transform) =>
        IsDistinct(transform) ? new PipelineDistinct { KeyColumns = KeyColumns(transform) } : null;

    /// <summary>The transform's key columns in order: the group-by fields LatestBy and Distinct keep their keys in.</summary>
    private static List<string> KeyColumns(PipelineTransformConfiguration transform) =>
        [.. transform.GroupByFields.OrderBy(f => f.Ordinal).Select(f => f.FieldName)];

    /// <summary>Maps the typed Lookups children (one row per brought-across column) to a <see cref="LookupDto"/>, or null when absent.</summary>
    private static LookupDto? MapLookupDto(PipelineTransformConfiguration transform)
    {
        if (transform.Lookups.Count == 0)
        {
            return null;
        }

        var first = transform.Lookups[0];
        return new LookupDto
        {
            LookupDataSet = first.LookupDataSet,
            LookupDataStoreName = first.LookupDataStoreName,
            LookupDataStorePath = first.LookupDataStorePath,
            LookupContainerName = first.LookupContainerName,
            LookupKeyField = first.LookupKeyField,
            SourceKeyField = first.SourceKeyField,
            OutputFieldPrefix = first.OutputFieldPrefix,
            LookupColumns = transform.Lookups.Select(l => l.LookupValueField).ToList(),
            JoinType = first.JoinType,
        };
    }

    /// <summary>Maps the typed Calculations children to a <see cref="CalculationDto"/>, or null when absent.</summary>
    private static CalculationDto? MapCalculationDto(PipelineTransformConfiguration transform)
    {
        if (transform.Calculations.Count == 0)
        {
            return null;
        }

        return new CalculationDto
        {
            ComputedColumns = transform.Calculations
                .OrderBy(c => c.ExecutionOrder)
                .Select(c => new ComputedColumnDto { OutputField = c.OutputField, Formula = c.Expression })
                .ToList(),
        };
    }
}
