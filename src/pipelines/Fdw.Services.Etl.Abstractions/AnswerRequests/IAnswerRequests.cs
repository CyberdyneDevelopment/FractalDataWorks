using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>
/// The answer requests of runs (FDW-6142): a run keeps one for the person it acts as and ends; the person's answer starts the continuation the run stated, once. Every refusal is a coded message.
/// </summary>
/// <remarks>
/// <para>
/// Who answers is the caller, read by the rule every run's person is read by; no request carries a person. What the person is shown and the continuation's arguments and roles are kept with the request when the run asks,
/// so an answer states only its text. Asking, answering and starting are each refused a second time by the operational store itself (a unique key, and one conditional update judged by the rows it changed), not by memory.
/// </para>
/// </remarks>
public interface IAnswerRequests
{
    /// <summary>Keeps an answer request for the person a run acts as; a step that asked before gets its request back, not a second one.</summary>
    /// <param name="ask">What the run states.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The request and whether this call made it; or the refusal saying what is wrong.</returns>
    Task<IGenericResult<AnswerRequestKept>> Ask(AnswerRequestAsk ask, CancellationToken cancellationToken = default);

    /// <summary>Records that the person was told about a request.</summary>
    /// <param name="tenantId">The request's tenant.</param>
    /// <param name="requestId">The request.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success, or the refusal when it was not recorded.</returns>
    Task<IGenericResult> Announced(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default);

    /// <summary>Answers a request as the caller, who must be the person it was kept for, and starts its continuation once.</summary>
    /// <param name="requestId">The request.</param>
    /// <param name="answer">The answer: any text that is not blank, or one of the request's options for a choice.</param>
    /// <param name="cancellationToken">Cancels the answer.</param>
    /// <returns>The request and the run it started; or the refusal (not found, not theirs, already answered, expired, not an option, or the start failed).</returns>
    Task<IGenericResult<AnswerRequestAnswered>> Answer(Guid requestId, string answer, CancellationToken cancellationToken = default);

    /// <summary>Lists the caller's requests that are open.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The caller's open requests, oldest first; or the refusal when the caller is no person or the store could not be read.</returns>
    Task<IGenericResult<IReadOnlyList<AnswerRequest>>> List(CancellationToken cancellationToken = default);

    /// <summary>Reads one of the caller's requests.</summary>
    /// <param name="requestId">The request.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The request; or the refusal when there is none or it is not the caller's.</returns>
    Task<IGenericResult<AnswerRequest>> Read(Guid requestId, CancellationToken cancellationToken = default);
}
