using Fdw.Collections.Attributes;

namespace Fdw.Claims.Abstractions;

/// <summary>
/// The surrogate identifier of the agent key the caller presented.
/// </summary>
/// <remarks>
/// The identity value, not the Guid, because <c>agent.AgentAction.AgentKeyId</c> is a non-nullable
/// int foreign key: a consumer recording an agent action needs exactly this to write the row.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimDefinitions), "agentKeyId")]
public sealed class AgentKeyIdClaim : ClaimDefinitionBase
{
    /// <summary>Initializes a new instance of the <see cref="AgentKeyIdClaim"/> class.</summary>
    public AgentKeyIdClaim() : base(name: "agentKeyId", isArray: false, isSessionBound: true, TokenDestinations.AccessToken) { }
}
