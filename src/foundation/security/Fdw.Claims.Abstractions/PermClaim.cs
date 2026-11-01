using Fdw.Collections.Attributes;

namespace Fdw.Claims.Abstractions;

/// <summary>One claim per resolved permission string (e.g. <c>data.read</c>). Baked at issue time.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimDefinitions), "perm")]
public sealed class PermClaim : ClaimDefinitionBase
{
    /// <summary>Initializes a new instance of the <see cref="PermClaim"/> class.</summary>
    public PermClaim() : base(name: "perm", isArray: false, isSessionBound: false, TokenDestinations.AccessToken) { }
}
