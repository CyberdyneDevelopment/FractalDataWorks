using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Answers where the member a record cites stands, by asking the provider of the domain the citation names.
/// </summary>
/// <remarks>
/// The check reads and the caller then writes, so a member retired between the two is not caught, and it reads as the caller, so a row hidden
/// from them by row-level security reads as absent. The owning domain turns an absent member and a retired one into a coded refusal each.
/// </remarks>
public interface ICitedConfigurations
{
    /// <summary>Asks where a cited member stands.</summary>
    /// <param name="citation">What the citing record says about the member.</param>
    /// <param name="id">The durable id of the cited member.</param>
    /// <param name="cancellationToken">A token to cancel the operation: the check then answers <c>CitationCheckCancelled</c>.</param>
    /// <returns>
    /// Success with the member's state: <see cref="CurrentCitedState"/>, <see cref="RetiredCitedState"/> or <see cref="AbsentCitedState"/>, or
    /// <see cref="UncheckedCitedState"/> when the citation is not checked here (the database enforces it, or it cites no domain). A failure when no provider is
    /// registered for the container, two are, a read failed, or the token was cancelled.
    /// </returns>
    Task<IGenericResult<ICitedState>> StateOf(Citation citation, Guid id, CancellationToken cancellationToken = default);
}
