using Fdw.Configuration;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// An authentication service a host trusts to have issued a token.
/// </summary>
/// <remarks>
/// The typed domain contract. A constructor asking for this states which domain it reads; one asking
/// for the closed generic states only a shape, and two domains that share a shape become
/// interchangeable at the call site.
/// </remarks>
public interface IAuthenticationServiceConfiguration
    : IPlatformServiceConfiguration<IAuthenticationServiceImplementationConfiguration>
{
    /// <summary>Gets or sets whether this service is trusted.</summary>
    /// <remarks>
    /// A declared service that is not enabled takes no scheme, so a token naming its issuer routes
    /// nowhere and is refused. It is how a host stops trusting an issuer without forgetting it.
    /// </remarks>
    bool Enabled { get; set; }

    /// <summary>Gets or sets the issuer a token must name to be routed to this service.</summary>
    /// <remarks>
    /// On the domain row because every kind has one, and because it is what selects the scheme —
    /// that selection happens before any kind-specific check runs.
    /// </remarks>
    string? Authority { get; set; }
}
