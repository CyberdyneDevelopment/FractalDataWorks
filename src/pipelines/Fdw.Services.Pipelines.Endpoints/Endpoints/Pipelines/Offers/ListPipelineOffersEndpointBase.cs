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

/// <summary>The route that lists the offers of a pipeline (FDW-6135): which tenant, person or role may have a model start it through a variable.</summary>
public abstract class ListPipelineOffersEndpointBase : Endpoint<PipelineOffersRequest, List<PipelineOffer>>
{
    private readonly IPipelineOfferService _offers;

    /// <summary>Initializes a new instance of the <see cref="ListPipelineOffersEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="offers">Lists the offers.</param>
    protected ListPipelineOffersEndpointBase(ILogger logger, IPipelineOfferService offers)
    {
        EndpointLogger = logger;
        _offers = offers;
    }

    /// <summary>Gets the logger.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/pipelines/{PipelineId}/offers");
#if DEVELOP
        AllowAnonymous();
#else
        // Why its own permission: deciding which pipelines a model may start is a different right from starting runs.
        Policies("pipelines:offer");
#endif
        Summary(s =>
        {
            s.Summary = "List the offers of a pipeline";
            s.Description = "The offers that stand for the pipeline in the caller's tenant: the whole tenant (no person and no role), a person, or a role. A pipeline that does not exist is refused.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(PipelineOffersRequest req, CancellationToken ct)
    {
        var listed = await _offers.List(req.PipelineId, ct).ConfigureAwait(false);
        if (listed.IsFailure || listed.Value is null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync([.. listed.Value], ct).ConfigureAwait(false);
    }
}
