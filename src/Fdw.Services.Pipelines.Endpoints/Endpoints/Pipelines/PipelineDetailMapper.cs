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
/// Maps a stored pipeline to <see cref="PipelineDetailResponse"/>, and a tool's stage to its DTO.
/// </summary>
/// <remarks>
/// A graph pipeline's sources, destinations and stages are its components, read through the graph endpoints, so the detail
/// answers what the pipeline is (identity, description, enabled flag, dates) and the run parameters it declares. The response's
/// members for the older engines (one source, one destination, stages, load steps) are not filled: nothing can fill them, and they
/// leave the contract with the console (FDW-1591).
/// </remarks>
public static class PipelineDetailMapper
{
    /// <summary>
    /// Maps a stored pipeline to its detail DTO. GET, create and update all answer with this, so a
    /// write returns exactly what a later read of the same pipeline returns.
    /// </summary>
    /// <param name="pipeline">The stored pipeline.</param>
    /// <returns>The detail.</returns>
    public static PipelineDetailResponse Map(IPipelineImplementationConfiguration pipeline)
    {
        var response = new PipelineDetailResponse
        {
            Id = pipeline.Id,
            Name = pipeline.Name,
            PipelineType = pipeline.Implementation,
            Description = pipeline.Description,
            IsEnabled = pipeline is IEnablablePipelineConfiguration { IsEnabled: true },
            RequireApprovalToRun = pipeline is IApprovalGatedPipelineConfiguration { RequireApprovalToRun: true },
            CreatedAt = pipeline.CreateDate,
            UpdatedAt = pipeline.ModifyDate,
        };

        // The run parameters the pipeline declares, whatever kind it is (FDW-1557): the forms that start or schedule a run read them here.
        if (pipeline is IDeclaresRunParameters declaring)
        {
            response.Parameters = PipelineLoadReader.Parameters(declaring);
        }

        return response;
    }

    /// <summary>Maps a transform configuration to its DTO.</summary>
    public static PipelineTransformDto MapTransformToDto(PipelineTransformConfiguration transform, IReadOnlyDictionary<Guid, string> transformationNames)
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
            Compute = MapComputeDto(transform, transformationNames),
            Lookup = MapLookupDto(transform),
            Calculation = MapCalculationDto(transform),
        };
    }

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
