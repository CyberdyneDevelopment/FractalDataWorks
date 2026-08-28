using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Marks the caller as an AI agent acting on behalf of the user named by <c>sub</c>.
/// </summary>
/// <remarks>
/// Its own claim rather than something inferred from <c>sub</c>, because an agent acts FOR its owner
/// — the subject is identical for both, so nothing else in the token can tell them apart. Anything
/// that must distinguish them (an audit row, a message attributed on screen, a policy that gates
/// what an agent may do unattended) has no other signal to read.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimDefinitions), "agent")]
public sealed class AgentClaim : ClaimDefinitionBase
{
    /// <summary>Initializes a new instance of the <see cref="AgentClaim"/> class.</summary>
    public AgentClaim() : base(id: 7, name: "agent", isArray: false, TokenDestinations.AccessToken) { }
}
