using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>The route that removes one offer of a pipeline (FDW-6135).</summary>
public abstract class RemovePipelineOfferEndpointBase : Endpoint<RemovePipelineOfferRequest>
{
    private readonly IPipelineOfferService _offers;

    /// <summary>Initializes a new instance of the <see cref="RemovePipelineOfferEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="offers">Removes the offer.</param>
    protected RemovePipelineOfferEndpointBase(ILogger logger, IPipelineOfferService offers)
    {
        EndpointLogger = logger;
        _offers = offers;
    }

    /// <summary>Gets the logger.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/pipelines/{PipelineId}/offers/{OfferId}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:offer");
#endif
        Summary(s =>
        {
            s.Summary = "Remove an offer of a pipeline";
            s.Description = "Removes one offer; the pipeline can then be started through a variable only by those it is still offered to. An offer that does not exist is refused (404).";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(RemovePipelineOfferRequest req, CancellationToken ct)
    {
        var removed = await _offers.Remove(req.PipelineId, req.OfferId, ct).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(removed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
