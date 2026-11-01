using Fdw.Collections.Attributes;

namespace Fdw.Claims.Abstractions;

/// <summary>The user's assigned role names. Always serialized as a JSON array, even for one role.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimDefinitions), "roles")]
public sealed class RolesClaim : ClaimDefinitionBase
{
    /// <summary>Initializes a new instance of the <see cref="RolesClaim"/> class.</summary>
    public RolesClaim() : base(name: "roles", isArray: true, isSessionBound: false, TokenDestinations.AccessToken) { }
}
