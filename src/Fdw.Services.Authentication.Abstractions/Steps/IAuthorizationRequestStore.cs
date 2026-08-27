using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// Holds an authorization request between sending a caller to a provider and their return.
/// </summary>
/// <remarks>
/// <c>TryConsume</c> rather than a read, for the same reason the execution store consumes: state is
/// single-use, and an authorization code that can be presented twice can be replayed.
/// </remarks>
public interface IAuthorizationRequestStore
{
    /// <summary>Stores <paramref name="request"/> against <paramref name="state"/>.</summary>
    /// <param name="state">The opaque state value sent to the provider.</param>
    /// <param name="request">What must be remembered.</param>
    IGenericResult Store(string state, AuthorizationRequest request);

    /// <summary>Consumes the request stored against <paramref name="state"/>, exactly once.</summary>
    /// <param name="state">The state the provider echoed back.</param>
    IGenericResult<AuthorizationRequest> TryConsume(string state);
}
