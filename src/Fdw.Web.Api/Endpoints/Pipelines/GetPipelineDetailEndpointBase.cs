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

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Endpoint to get detailed pipeline configuration by name.
/// </summary>
public abstract class GetPipelineDetailEndpointBase : Endpoint<PipelineNameRequest, PipelineDetailResponse>
{
    // Why: PipelineServiceConfigurationProvider is the sole domain-owned gateway path for all
    // pipeline data. Endpoints inject the provider — never IDataGateway or IConfigurationGateway.
    private readonly PipelineServiceConfigurationProvider _pipelineProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPipelineDetailEndpointBase"/> class.
    /// </summary>
    protected GetPipelineDetailEndpointBase(PipelineServiceConfigurationProvider pipelineProvider)
    {
        _pipelineProvider = pipelineProvider;
    }

    /// <summary>Gets the logger instance. Resolved during HandleAsync.</summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

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
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        OnFetchingPipeline(req.Name);

        var result = await _pipelineProvider.Get(req.Name, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            // Why: a failed result carries its own structured CurrentMessage; surface it verbatim instead of
            // a magic-string fallback. The guard mirrors GenericEndpoint's failure-message handling so an
            // empty message yields an empty Details rather than an invented literal.
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

        // Why: the KIND discriminator (ServiceOptionType) is NOT NULL on a persisted pipeline header. A null
        // here is a data-integrity defect, not a display default — fail loud with a 500 rather than papering
        // over it with an empty-string fallback in the DTO mapping.
        if (string.IsNullOrEmpty(pipeline.ServiceOptionType))
        {
            OnPipelineFetchFailed(req.Name, $"Pipeline '{req.Name}' has no kind (ServiceOptionType).");
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Invalid pipeline", Details = $"Pipeline '{req.Name}' has no kind (ServiceOptionType)." }, ct).ConfigureAwait(false);
            return;
        }

        OnPipelineRetrieved(req.Name);

        // Why: provider.Get returns the fully composed aggregate — the ETL-kind typed body
        // (pipeline.Configuration) already carries its Transforms. Read them off the aggregate; no second
        // gateway round-trip and no graph types.
        await Send.OkAsync(MapToDetailDto(pipeline, ExtractTransforms(pipeline)), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Extracts the transform DTOs from the composed pipeline aggregate's ETL-kind typed body.
    /// Returns an empty list when the pipeline is not an ETL kind or has no transforms.
    /// </summary>
    // Why: Transforms live on the ETL-kind body (EtlPipelineConfiguration). A non-ETL kind (or an ETL
    // pipeline with no operations) legitimately has none — that is an empty list, not a defect.
    protected virtual IList<PipelineTransformDto> ExtractTransforms(PipelineConfiguration pipeline)
    {
        var transforms = new List<PipelineTransformDto>();
        if (pipeline.Configuration is EtlPipelineConfiguration etlBody && etlBody.Transforms is { } operations)
        {
            foreach (var op in operations)
                transforms.Add(MapTransformToDto(op));
        }
        return transforms;
    }

    /// <summary>
    /// Maps the domain pipeline configuration to a detail DTO.
    /// </summary>
    // Why: PipelineConfiguration is the parent (identity-only) row in pipe.Pipeline. Source/target
    // connection + dataset bindings are promoted onto the ENGINE typed body
    // (IEtlPipelineTypedConfiguration, e.g. BatchCopyPipelineConfiguration) reachable via the ETL-kind
    // body's Configuration property — read them off there instead of hardcoding empty/null.
    protected virtual PipelineDetailResponse MapToDetailDto(PipelineConfiguration pipeline, IList<PipelineTransformDto> transforms)
    {
        var engine = (pipeline.Configuration as EtlPipelineConfiguration)?.Configuration;

        return new PipelineDetailResponse
        {
            Id = pipeline.Id,
            Name = pipeline.Name,
            PipelineType = pipeline.PipelineType!,
            SourceConnectionName = engine?.SourceConnectionName ?? string.Empty,
            DestinationConnectionName = engine?.DestinationConnectionName ?? string.Empty,
            SourceDataSet = engine?.SourceDataSet,
            DestinationDataSet = engine?.DestinationDataSet,
            Description = pipeline.Description,
            IsEnabled = !pipeline.IsScheduled || pipeline.ScheduleId.HasValue,
            CreatedAt = default,
            UpdatedAt = default,
            Transforms = transforms
        };
    }

    /// <summary>Maps a transform configuration to its DTO.</summary>
    // Why: the provider already composes children on read (Parts 1-4 cascade), so this is a pure
    // in-memory projection off the aggregate — no extra gateway round-trip.
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
                .Select(c => new ComputedColumnDto { OutputField = c.OutputField, Formula = c.Expression, FormulaLanguage = c.FormulaLanguage })
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
