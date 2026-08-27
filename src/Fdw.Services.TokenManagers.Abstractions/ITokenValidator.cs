using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.TokenManagers.Abstractions;

/// <summary>
/// Checks an inbound token. Request-time only.
/// </summary>
/// <remarks>
/// Runs on every request, so it is never part of a flow. Whatever the implementation, it must verify
/// issuer, <b>audience</b>, expiry, signature, and pin the algorithm rather than reading it from the
/// token — a valid token minted for a different audience is the most common real-world break, and an
/// algorithm taken from the header is how confusion attacks work.
/// </remarks>
public interface ITokenValidator
{
    /// <summary>Validates <paramref name="token"/> and returns what it establishes.</summary>
    /// <param name="token">The presented token.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult<ValidatedToken>> Validate(
        string token, CancellationToken cancellationToken = default);
}
