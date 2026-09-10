using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines;
using Fdw.Web.RestEndpoints.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Endpoint to get detailed pipeline configuration by name.
/// </summary>
public abstract class GetPipelineDetailEndpointBase : Endpoint<PipelineNameRequest, PipelineDetailResponse>
{
    /// <summary>Initializes a new instance of the <see cref="GetPipelineDetailEndpointBase"/> class.</summary>
        private readonly PipelineServiceConfigurationProvider _pipelineProvider;

    /// <summary>Initializes a new instance of the <see cref="GetPipelineDetailEndpointBase"/> class.</summary>
    protected GetPipelineDetailEndpointBase(ILogger logger, PipelineServiceConfigurationProvider pipelineProvider)
    {
        EndpointLogger = logger;
        _pipelineProvider = pipelineProvider;
    }


    /// <summary>Gets the logger instance. Resolved during HandleAsync.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/pipelines/{Name}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get pipeline configuration by name";
            s.Description = "Returns detailed configuration for a specific pipeline. Requires Admin or Operator role.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(PipelineNameRequest req, CancellationToken ct)
    {
        
        OnFetchingPipeline(req.Name);

        var result = await _pipelineProvider.Get(req.Name, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            OnPipelineFetchFailed(req.Name, result.CurrentMessage);
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to fetch pipeline", Details = result.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        var pipeline = result.Value;
        if (pipeline is null)
        {
            OnPipelineNotFound(req.Name);
            await HttpContext.WriteNotFound("Pipeline", req.Name, ct).ConfigureAwait(false);
            return;
        }

        if (string.IsNullOrEmpty(pipeline.Implementation))
        {
            OnPipelineFetchFailed(req.Name, $"Pipeline '{req.Name}' has no kind (Implementation).");
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Invalid pipeline", Details = $"Pipeline '{req.Name}' has no kind (Implementation)." }, ct).ConfigureAwait(false);
            return;
        }

        OnPipelineRetrieved(req.Name);

        await Send.OkAsync(MapToDetailDto(pipeline, ExtractTransforms(pipeline)), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Extracts the transform DTOs the pipeline's implementation carries.
    /// Returns an empty list for an implementation that has no transforms.
    /// </summary>
    protected virtual IList<PipelineTransformDto> ExtractTransforms(IPipelineImplementationConfiguration pipeline)
    {
        var transforms = new List<PipelineTransformDto>();
        if (pipeline is IEtlPipelineImplementationConfiguration { Transforms: { } operations })
        {
            foreach (var op in operations)
                transforms.Add(MapTransformToDto(op));
        }
        return transforms;
    }

    /// <summary>
    /// Maps the domain pipeline configuration to a detail DTO.
    /// </summary>
    protected virtual PipelineDetailResponse MapToDetailDto(IPipelineImplementationConfiguration pipeline, IList<PipelineTransformDto> transforms)
    {
        // A domain read hands back the implementation, so the linkage is on the record itself --
        // there is no header to reach through. Description and the schedule flag are still columns on
        // the pipe.Pipeline domain row, which this read does not carry, so they are the app's to fill.
        var etl = pipeline as IEtlPipelineImplementationConfiguration;

        return new PipelineDetailResponse
        {
            Id = pipeline.Id,
            Name = pipeline.Name,
            PipelineType = pipeline.Implementation,
            SourceConnectionName = etl?.SourceConnectionName ?? string.Empty,
            DestinationConnectionName = etl?.DestinationConnectionName ?? string.Empty,
            SourceDataSet = etl?.SourceDataSet,
            DestinationDataSet = etl?.DestinationDataSet,
            IsEnabled = etl?.IsEnabled ?? false,
            CreatedAt = default,
            UpdatedAt = default,
            Transforms = transforms
        };
    }

    /// <summary>Maps a transform configuration to its DTO.</summary>
    protected virtual PipelineTransformDto MapTransformToDto(PipelineTransformConfiguration transform)
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
            Lookup = MapLookupDto(transform),
            Calculation = MapCalculationDto(transform),
            FieldMappings = MapFieldMappingsDto(transform),
        };
    }

    /// <summary>Maps the typed FieldMappings children to a list of <see cref="PipelineFieldMappingDto"/>.</summary>
    private static List<PipelineFieldMappingDto> MapFieldMappingsDto(PipelineTransformConfiguration transform)
    {
        return transform.FieldMappings
            .Select(m => new PipelineFieldMappingDto
            {
                Name = m.Name,
                SourceField = m.SourceField,
                DestinationField = m.DestinationField,
                TransformExpression = m.TransformExpression,
                TargetType = m.TargetType,
                IsEnabled = m.IsEnabled,
                IsRequired = m.IsRequired,
                DefaultValue = m.DefaultValue,
            })
            .ToList();
    }

    /// <summary>Maps the typed GroupByFields/Aggregations children to an <see cref="AggregationDto"/>, or null when absent.</summary>
    private static AggregationDto? MapAggregationDto(PipelineTransformConfiguration transform)
    {
        if (transform.GroupByFields.Count == 0 && transform.Aggregations.Count == 0)
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
            LookupConnectionName = first.LookupConnectionName,
            LookupDataSet = first.LookupDataSet,
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

    /// <summary>Called when fetching pipeline. Override for custom logging.</summary>
    protected virtual void OnFetchingPipeline(string name) { }

    /// <summary>Called when pipeline fetch fails. Override for custom logging.</summary>
    protected virtual void OnPipelineFetchFailed(string name, string? error) { }

    /// <summary>Called when pipeline is not found. Override for custom logging.</summary>
    protected virtual void OnPipelineNotFound(string name) { }

    /// <summary>Called when pipeline is retrieved. Override for custom logging.</summary>
    protected virtual void OnPipelineRetrieved(string name) { }
}
