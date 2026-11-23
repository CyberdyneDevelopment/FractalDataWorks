using System;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>What an answer did (FDW-6142): the request as it stands now, and the run the answer started.</summary>
/// <param name="Request">The request.</param>
/// <param name="ContinuationExecutionId">The run of the continuation.</param>
public sealed record AnswerRequestAnswered(AnswerRequest Request, Guid ContinuationExecutionId);
