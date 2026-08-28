using Fdw.Collections.Attributes;

namespace Fdw.Services.Agents.Abstractions.TypeCollections.AgentActionStatusTypeOptions.Options;

/// <summary>
/// Represents an agent action that has been approved by a human reviewer.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(AgentActionStatusTypes), "Approved")]
public sealed class ApprovedAgentActionStatus : AgentActionStatusTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovedAgentActionStatus"/> class.
    /// </summary>
    public ApprovedAgentActionStatus() : base(2, "Approved", isTerminal: true, isApproved: true)
    {
    }
}
