using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>
/// An answer request as its person reads it (FDW-6142): what they are shown and where the request stands. The arguments of the continuation and its roles are not here: they are the server's, saved with the
/// request, and the person is never given a way to change them.
/// </summary>
public sealed class AnswerRequest
{
    /// <summary>Gets the request's id.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the tenant of the request.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Gets the person who may answer.</summary>
    public required Guid PersonId { get; init; }

    /// <summary>Gets the run that asked.</summary>
    public required Guid ExecutionId { get; init; }

    /// <summary>Gets the kind of the request.</summary>
    public required IAnswerRequestKind Kind { get; init; }

    /// <summary>Gets the name of the answer policy the call was judged under.</summary>
    public required string PolicyName { get; init; }

    /// <summary>Gets the name of the call the request is for.</summary>
    public required string CallName { get; init; }

    /// <summary>Gets where the request stands.</summary>
    public required IAnswerRequestState State { get; init; }

    /// <summary>Gets when the request was asked.</summary>
    public required DateTimeOffset AskedAt { get; init; }

    /// <summary>Gets when the request ends, stated by the policy when it was asked.</summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Gets when the person was told about the request; null when they have not been.</summary>
    public DateTimeOffset? AnnouncedAt { get; init; }

    /// <summary>Gets when the request was answered; null when it was not.</summary>
    public DateTimeOffset? AnsweredAt { get; init; }

    /// <summary>Gets the run the answer started; null when none started.</summary>
    public Guid? ContinuationExecutionId { get; init; }

    /// <summary>Gets what the person is shown, in order, as name and text.</summary>
    public required IReadOnlyList<KeyValuePair<string, string>> Shown { get; init; }
}
