using System;
using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>An answer request as its person reads it over HTTP (FDW-6142): what they are shown and where the request stands. Nothing of the continuation (its arguments, its roles) is here.</summary>
public sealed class AnswerRequestResponse
{
    /// <summary>Gets or sets the request's id, which the answer route takes.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the run that asked.</summary>
    public Guid ExecutionId { get; set; }

    /// <summary>Gets or sets the kind of the request: Question or Choice.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the answer policy the call was judged under.</summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>Gets or sets the call the request is for.</summary>
    public string CallName { get; set; } = string.Empty;

    /// <summary>Gets or sets where the request stands: Open, Answered, Started or Expired.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets when the request was asked.</summary>
    public DateTimeOffset AskedAt { get; set; }

    /// <summary>Gets or sets when the request ends.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Gets or sets when the person was told about the request; null when they were not.</summary>
    public DateTimeOffset? AnnouncedAt { get; set; }

    /// <summary>Gets or sets when the request was answered; null when it was not.</summary>
    public DateTimeOffset? AnsweredAt { get; set; }

    /// <summary>Gets or sets what the person is shown, in order.</summary>
    public List<AnswerRequestShownLine> Shown { get; set; } = [];
}
