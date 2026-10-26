using Fdw.Collections.Attributes;

namespace Fdw.Claims.Abstractions;

/// <summary>The person who signed in, carried by a session that person started by acting with a role (FDW-2282). Absent from a session that is not acting.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimDefinitions), "actor")]
public sealed class ActorClaim : ClaimDefinitionBase
{
    /// <summary>Initializes a new instance of the <see cref="ActorClaim"/> class.</summary>
    public ActorClaim() : base(name: "actor", isArray: false, TokenDestinations.AccessToken) { }
}
