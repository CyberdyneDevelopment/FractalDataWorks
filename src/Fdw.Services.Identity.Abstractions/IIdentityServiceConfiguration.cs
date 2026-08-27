using Fdw.Configuration;

namespace Fdw.Services.Identity.Abstractions;

/// <summary>
/// Marker interface for typed identity-service body configurations (e.g. a client-credentials body
/// carrying issuer/client id/secret reference, or a JWT-assertion body carrying issuer and
/// assertion source).
/// </summary>
/// <remarks>
/// Each typed body implements this interface directly rather than inheriting a concrete header
/// class. The header (<c>IdentityServiceConfiguration</c>) carries a
/// <c>[NotMapped] IIdentityServiceConfiguration? Configuration</c> populated on the read path,
/// mirroring every other polymorphic header/typed-body domain (Connection, SecretManager,
/// TokenManager).
/// </remarks>
public interface IIdentityServiceConfiguration : IImplementationConfiguration
{
    /// <summary>
    /// Gets the identity provider's issuer URL — the authority this configuration obtains tokens
    /// from, and the <c>iss</c> a receiving service will see on the resulting token.
    /// </summary>
    string? Issuer { get; }
}
