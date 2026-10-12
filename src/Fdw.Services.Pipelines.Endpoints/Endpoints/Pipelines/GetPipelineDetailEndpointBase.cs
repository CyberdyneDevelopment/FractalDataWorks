using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines;
using Fdw.Web.RestEndpoints.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Endpoint to get detailed pipeline configuration by name or id.
/// </summary>
public abstract class GetPipelineDetailEndpointBase : Endpoint<GetPipelineRequest, PipelineDetailResponse>
{
    private readonly PipelineServiceConfigurationProvider _pipelineProvider;
    private readonly ITransformationConfigurationProvider _transformations;

    /// <summary>Initializes a new instance of the <see cref="GetPipelineDetailEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="pipelineProvider">Reads the pipeline.</param>
    /// <param name="transformations">Reads the names of the Transformations the pipeline's field mappings chain.</param>
    protected GetPipelineDetailEndpointBase(
        ILogger logger,
        PipelineServiceConfigurationProvider pipelineProvider,
        ITransformationConfigurationProvider transformations)
    {
        EndpointLogger = logger;
        _pipelineProvider = pipelineProvider;
        _transformations = transformations ?? throw new ArgumentNullException(nameof(transformations));
    }


    /// <summary>Gets the logger instance. Resolved during HandleAsync.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc/>
    public override void Configure()
    {
        // A read of one record is a POST, its name in the body: a GET is a plain paged list and nothing else (FDW-978).
        Post("/pipelines/get");
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
    public override async Task HandleAsync(GetPipelineRequest req, CancellationToken ct)
    {
        // Exactly one of id and name: both would leave which one wins to chance, neither names nothing.
        var byName = !string.IsNullOrWhiteSpace(req.Name);
        if (byName == req.Id.HasValue)
        {
            AddError("Name exactly one of 'id' or 'name'.");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        var asked = byName ? req.Name! : req.Id!.Value.ToString("D", System.Globalization.CultureInfo.InvariantCulture);
        OnFetchingPipeline(asked);

        var result = byName
            ? await _pipelineProvider.Get(req.Name!, ct).ConfigureAwait(false)
            : await _pipelineProvider.Get(req.Id!.Value, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            OnPipelineFetchFailed(asked, result.CurrentMessage);
            await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to fetch pipeline", Details = result.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        var pipeline = result.Value;
        if (pipeline is null)
        {
            OnPipelineNotFound(asked);
            await HttpContext.WriteNotFound("Pipeline", asked, ct).ConfigureAwait(false);
            return;
        }

        if (string.IsNullOrEmpty(pipeline.Implementation))
        {
            OnPipelineFetchFailed(asked, $"Pipeline '{asked}' has no kind (Implementation).");
            await Send.ResultAsync(TypedResults.Json(new { Error = "Invalid pipeline", Details = $"Pipeline '{asked}' has no kind (Implementation)." }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        var mapped = await PipelineDetailMapper.Detail(pipeline, _transformations, ct).ConfigureAwait(false);
        if (mapped.IsFailure)
        {
            OnPipelineFetchFailed(asked, mapped.CurrentMessage);
            await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to read the Transformations the pipeline chains", Details = mapped.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        OnPipelineRetrieved(asked);

        await Send.OkAsync(mapped.Value!, ct).ConfigureAwait(false);
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
