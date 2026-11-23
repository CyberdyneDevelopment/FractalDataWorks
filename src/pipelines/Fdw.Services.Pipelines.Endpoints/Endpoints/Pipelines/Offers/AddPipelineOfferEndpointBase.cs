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

/// <summary>The route that offers a pipeline (FDW-6135) to the whole tenant, to a person or to a role.</summary>
public abstract class AddPipelineOfferEndpointBase : Endpoint<AddPipelineOfferRequest, PipelineOffer>
{
    private readonly IPipelineOfferService _offers;

    /// <summary>Initializes a new instance of the <see cref="AddPipelineOfferEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="offers">Adds the offer.</param>
    protected AddPipelineOfferEndpointBase(ILogger logger, IPipelineOfferService offers)
    {
        EndpointLogger = logger;
        _offers = offers;
    }

    /// <summary>Gets the logger.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/pipelines/{PipelineId}/offers");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:offer");
#endif
        Summary(s =>
        {
            s.Summary = "Offer a pipeline";
            s.Description = "Offers the pipeline to the whole tenant (no person and no role), to one person, or to the holders of one role; not to a person and a role at once. An offer that already stands is refused (409) and a pipeline that does not exist is refused (404).";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(AddPipelineOfferRequest req, CancellationToken ct)
    {
        var added = await _offers.Add(req.PipelineId, req.PersonId, req.RoleName, ct).ConfigureAwait(false);
        if (added.IsFailure || added.Value is null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(added, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(added.Value, ct).ConfigureAwait(false);
    }
}
