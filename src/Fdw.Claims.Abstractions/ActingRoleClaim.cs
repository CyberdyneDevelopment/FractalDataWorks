using Fdw.Collections.Attributes;

namespace Fdw.Claims.Abstractions;

/// <summary>The name of the role a session acts with (FDW-2282). Absent from a session that is not acting.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimDefinitions), "actingRole")]
public sealed class ActingRoleClaim : ClaimDefinitionBase
{
    /// <summary>Initializes a new instance of the <see cref="ActingRoleClaim"/> class.</summary>
    public ActingRoleClaim() : base(name: "actingRole", isArray: false, TokenDestinations.AccessToken) { }
}
