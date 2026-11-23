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
/// Answers an answer request (FDW-6142): <c>POST /answer-requests/{Id}/answer</c>. Only the person the request was kept for can answer it; the answer starts the continuation the run stated, once, as that person. A
/// second answer starts nothing.
/// </summary>
/// <remarks>
/// The request states the request and the answer's text and nothing else: what the person was shown and the arguments of the continuation were kept with the request, so no client can change them. The person and the
/// tenant are the caller's.
/// </remarks>
public abstract class SubmitAnswerEndpointBase : Endpoint<SubmitAnswerRequest, AnswerRequestAnsweredResponse>
{
    private readonly IAnswerRequests _requests;

    /// <summary>Initializes a new instance of the <see cref="SubmitAnswerEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="requests">Takes the answer.</param>
    protected SubmitAnswerEndpointBase(ILogger logger, IAnswerRequests requests)
    {
        EndpointLogger = logger;
        _requests = requests;
    }

    /// <summary>Gets the logger.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/answer-requests/{Id}/answer");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:answer");
#endif
        Summary(s =>
        {
            s.Summary = "Answer an answer request";
            s.Description = "Answers one of the signed-in person's requests and starts the continuation the run stated, once. A request that was answered, has expired or belongs to someone else is refused and starts nothing.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(SubmitAnswerRequest req, CancellationToken ct)
    {
        var answered = await _requests.Answer(req.Id, req.Answer, ct).ConfigureAwait(false);
        if (answered.IsFailure || answered.Value is not { } value)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(answered, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(AnswerRequestResponses.FromAnswered(value), ct).ConfigureAwait(false);
    }
}
