using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Execution;

/// <summary>
/// Holds a suspended flow between the redirect out and the caller's return.
/// </summary>
/// <remarks>
/// <para>
/// <c>TryConsume</c> rather than <c>Get</c>, and there is deliberately no read-without-consume
/// method: a resume token is single-use, and a caller able to read without consuming could replay
/// someone else's half-finished login. Consuming must be atomic — a check-then-act implementation
/// is a race and a replay window.
/// </para>
/// <para>
/// An expired record must fail exactly as a missing one does. Distinguishing them tells an attacker
/// that a token was real, only stale.
/// </para>
/// </remarks>
public interface IAuthenticationExecutionStore
{
    /// <summary>Stores <paramref name="record"/> and returns the token that resumes it.</summary>
    /// <param name="record">What the flow had established.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A cryptographically random, single-use resume token.</returns>
    Task<IGenericResult<string>> Suspend(ExecutionRecord record, CancellationToken cancellationToken = default);

    /// <summary>Consumes <paramref name="resumeToken"/>, returning its record exactly once.</summary>
    /// <param name="resumeToken">The token handed out when the flow suspended.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult<ExecutionRecord>> TryConsume(string resumeToken, CancellationToken cancellationToken = default);
}
