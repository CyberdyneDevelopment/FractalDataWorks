using Fdw.Collections.Attributes;

namespace Fdw.Services.Agents.Abstractions.TypeCollections.AgentActionStatusTypeOptions.Options;

/// <summary>
/// Represents an agent action that has been denied by a human reviewer.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(AgentActionStatusTypes), "Denied")]
public sealed class DeniedAgentActionStatus : AgentActionStatusTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeniedAgentActionStatus"/> class.
    /// </summary>
    public DeniedAgentActionStatus() : base(3, "Denied", isTerminal: true, isApproved: false)
    {
    }
}
