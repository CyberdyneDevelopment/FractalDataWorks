using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.AnswerRequests;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Lists the signed-in person's open answer requests (FDW-6142): <c>GET /answer-requests</c>. A person sees their own and nobody's else.
/// </summary>
/// <remarks>The person and the tenant are the caller's, read by the rule every run's person is read by; nothing in the request names either.</remarks>
public abstract class ListAnswerRequestsEndpointBase : EndpointWithoutRequest<List<AnswerRequestResponse>>
{
    private readonly IAnswerRequests _requests;

    /// <summary>Initializes a new instance of the <see cref="ListAnswerRequestsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="requests">Lists the requests.</param>
    protected ListAnswerRequestsEndpointBase(ILogger logger, IAnswerRequests requests)
    {
        EndpointLogger = logger;
        _requests = requests;
    }

    /// <summary>Gets the logger.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/answer-requests");
#if DEVELOP
        AllowAnonymous();
#else
        // Why its own permission: answering a request is a different right from starting and cancelling runs, so a deployment gives it to the people who are asked.
        Policies("pipelines:answer");
#endif
        Summary(s =>
        {
            s.Summary = "List my open answer requests";
            s.Description = "The answer requests that runs kept for the signed-in person and that are still open, oldest first. A request that passed its end is not listed.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var listed = await _requests.List(ct).ConfigureAwait(false);
        if (listed.IsFailure || listed.Value is null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync([.. listed.Value.Select(request => AnswerRequestResponses.From(request))], ct).ConfigureAwait(false);
    }
}
