using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Tokens.Outbound;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Acquires credentials for non-interactive outbound calls (service-to-service,
/// agent-to-service). Uses the client-credentials OAuth 2.0 flow by default;
/// token-exchange / on-behalf-of for delegated cases is reserved inside the active
/// <c>ITokenManager</c>.
///
/// A service that only issues or validates tokens for interactive users does NOT
/// implement this interface. Only implementations that support machine-to-machine
/// credential flows register this in DI.
/// </summary>
public interface IOutboundCredentialService : IDisposable
{
    /// <summary>
    /// Acquires an access token for a non-interactive client identity.
    /// Implementations SHOULD cache tokens and return the cached token until it
    /// is within the refresh threshold of expiry, then acquire a new one.
    /// </summary>
    /// <param name="request">
    /// The credential request carrying client identity, scopes, and optional audience.
    /// </param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <returns>
    /// Success with an <see cref="OutboundCredential"/> on successful token acquisition.
    /// Failure with a structured message if the client identity is invalid or the
    /// token endpoint is unreachable. Never returns <c>null</c> on success.
    /// </returns>
    Task<IGenericResult<OutboundCredential>> Acquire(
        OutboundCredentialRequest request,
        CancellationToken cancellationToken = default);
}
