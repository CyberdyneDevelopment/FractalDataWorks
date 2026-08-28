using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// The human-readable label of the agent key the caller presented (for example
/// <c>"mike - claude code"</c>).
/// </summary>
/// <remarks>
/// WHICH agent, where <see cref="AgentClaim"/> only says THAT it is one. An audit trail that records
/// every agent action identically cannot answer which agent to revoke.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimDefinitions), "agentLabel")]
public sealed class AgentLabelClaim : ClaimDefinitionBase
{
    /// <summary>Initializes a new instance of the <see cref="AgentLabelClaim"/> class.</summary>
    public AgentLabelClaim() : base(id: 8, name: "agentLabel", isArray: false, TokenDestinations.AccessToken) { }
}
