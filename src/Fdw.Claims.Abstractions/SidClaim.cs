using Fdw.Collections.Attributes;

namespace Fdw.Claims.Abstractions;

/// <summary>
/// The server-side session a principal was authenticated by (FDW-903). OpenID Connect names this claim
/// <c>sid</c>. It carries the session's own identity, never the id the browser presents, so it is safe
/// to read and log. Logout reads it to know which session to end; a principal authenticated by a bearer
/// token carries <see cref="JtiClaim"/> instead.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimDefinitions), "sid")]
public sealed class SidClaim : ClaimDefinitionBase
{
    /// <summary>Initializes a new instance of the <see cref="SidClaim"/> class.</summary>
    public SidClaim() : base(name: "sid", isArray: false, TokenDestinations.AccessToken) { }
}
