using System.Linq;
using Fdw.Services.Etl.Abstractions.AnswerRequests;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Makes what a person is sent about an answer request (FDW-6142) from the request the service read.</summary>
public static class AnswerRequestResponses
{
    /// <summary>Makes the response for a request.</summary>
    /// <param name="request">The request.</param>
    /// <returns>The response.</returns>
    public static AnswerRequestResponse From(AnswerRequest request) => new()
    {
        Id = request.Id,
        ExecutionId = request.ExecutionId,
        Kind = request.Kind.Name,
        PolicyName = request.PolicyName,
        CallName = request.CallName,
        State = request.State.Name,
        AskedAt = request.AskedAt,
        ExpiresAt = request.ExpiresAt,
        AnnouncedAt = request.AnnouncedAt,
        AnsweredAt = request.AnsweredAt,
        Shown = [.. request.Shown.Select(line => new AnswerRequestShownLine(line.Key, line.Value))],
    };

    /// <summary>Makes the response for an answer.</summary>
    /// <param name="answered">What the answer did.</param>
    /// <returns>The response.</returns>
    public static AnswerRequestAnsweredResponse FromAnswered(AnswerRequestAnswered answered) => new() { Request = From(answered.Request), ContinuationExecutionId = answered.ContinuationExecutionId };
}
