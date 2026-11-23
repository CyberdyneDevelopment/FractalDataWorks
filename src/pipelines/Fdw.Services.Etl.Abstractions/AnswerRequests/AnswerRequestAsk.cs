using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>
/// What a run states when it asks its person to answer (FDW-6142): the request is kept with all of it, so the answer call states none of it.
/// </summary>
/// <remarks>
/// Nothing here has a default. The person is the person the run acts as, and the request is kept for no other; the chain root, component and iteration are the step's claim, so a restart that reaches the same step
/// asks no second time.
/// </remarks>
public sealed class AnswerRequestAsk
{
    /// <summary>Gets the tenant of the run.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Gets the person the run acts as, who is the only one who may answer.</summary>
    public required Guid PersonId { get; init; }

    /// <summary>Gets the run that asks.</summary>
    public required Guid ExecutionId { get; init; }

    /// <summary>Gets the first run of the chain of restarts the asking run belongs to (<c>FlowRunIdentity.ClaimRootId</c>).</summary>
    public required Guid ClaimRootId { get; init; }

    /// <summary>Gets the name of the component that asks.</summary>
    public required string ComponentName { get; init; }

    /// <summary>Gets how many times the component asked in the asking run before this one.</summary>
    public required int Iteration { get; init; }

    /// <summary>Gets the name of the kind of the request, one of <see cref="AnswerRequestKinds"/>.</summary>
    public required string KindName { get; init; }

    /// <summary>Gets the name of the answer policy the call was judged under.</summary>
    public required string PolicyName { get; init; }

    /// <summary>Gets the name of the call the request is for.</summary>
    public required string CallName { get; init; }

    /// <summary>Gets how many seconds the request stays open, which the policy states.</summary>
    public required int ExpiresAfterSeconds { get; init; }

    /// <summary>Gets the pipeline the person's answer starts.</summary>
    public required Guid ContinuationPipelineId { get; init; }

    /// <summary>Gets the name of the continuation's parameter that receives the answer.</summary>
    public required string AnswerParameterName { get; init; }

    /// <summary>Gets the stored grant the call was judged against; null when none was stated.</summary>
    public Guid? GrantId { get; init; }

    /// <summary>Gets what the person is shown, in order, as name and text (a title, a body and, for a choice, one <c>option</c> for each).</summary>
    public required IReadOnlyList<KeyValuePair<string, string>> Shown { get; init; }

    /// <summary>Gets the arguments the continuation is started with besides the answer, as name and text.</summary>
    public required IReadOnlyList<KeyValuePair<string, string>> Arguments { get; init; }

    /// <summary>Gets the roles the continuation acts with, which are the asking run's.</summary>
    public required IReadOnlyList<string> RunRoles { get; init; }
}
