using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>The user's assigned role names. Always serialized as a JSON array, even for one role.</summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimDefinitions), "roles")]
public sealed class RolesClaim : ClaimDefinitionBase
{
    /// <summary>Initializes a new instance of the <see cref="RolesClaim"/> class.</summary>
    public RolesClaim() : base(id: 4, name: "roles", isArray: true, TokenDestinations.AccessToken) { }
}
