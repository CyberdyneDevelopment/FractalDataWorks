using System;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>What an answer did (FDW-6142): the request as it stands now and the run the answer started.</summary>
public sealed class AnswerRequestAnsweredResponse
{
    /// <summary>Gets or sets the request as it stands.</summary>
    public AnswerRequestResponse Request { get; set; } = new();

    /// <summary>Gets or sets the run the answer started.</summary>
    public Guid ContinuationExecutionId { get; set; }
}
