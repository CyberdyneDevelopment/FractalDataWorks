using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Per-token identifier, distinct from the principal it was issued for. RFC&nbsp;7519 &sect;4.1.7 fixes
/// this claim's wire name globally — not deployment-specific like <c>roles</c>/<c>perm</c> naming can
/// be for an external validator, so there is nothing to make configurable here, only a name to not
/// repeat as a literal. Read by this host's own revocation check
/// (<c>ITokenRevocationStore</c>/<c>LocalKeyAuthenticationHandler</c>) — nothing external needs to
/// agree on it.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimDefinitions), "jti")]
public sealed class JtiClaim : ClaimDefinitionBase
{
    /// <summary>Initializes a new instance of the <see cref="JtiClaim"/> class.</summary>
    public JtiClaim() : base(id: 10, name: "jti", isArray: false, TokenDestinations.AccessToken) { }
}
