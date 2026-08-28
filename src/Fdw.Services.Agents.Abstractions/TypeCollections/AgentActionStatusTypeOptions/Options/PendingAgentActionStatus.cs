using Fdw.Collections.Attributes;

namespace Fdw.Services.Agents.Abstractions.TypeCollections.AgentActionStatusTypeOptions.Options;

/// <summary>
/// Represents an agent action that is awaiting human review.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(AgentActionStatusTypes), "Pending")]
public sealed class PendingAgentActionStatus : AgentActionStatusTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PendingAgentActionStatus"/> class.
    /// </summary>
    public PendingAgentActionStatus() : base(1, "Pending", isTerminal: false, isApproved: false)
    {
    }
}
