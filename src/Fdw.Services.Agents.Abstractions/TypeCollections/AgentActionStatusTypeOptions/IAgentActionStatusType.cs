using Fdw.Collections;

namespace Fdw.Services.Agents.Abstractions.TypeCollections.AgentActionStatusTypeOptions;

/// <summary>
/// Represents a review status for an agent action.
/// </summary>
public interface IAgentActionStatusType : ITypeOption<int, AgentActionStatusTypeBase>
{
    /// <summary>
    /// Gets a value indicating whether this status is a terminal (final) state.
    /// </summary>
    bool IsTerminal { get; }

    /// <summary>
    /// Gets a value indicating whether this status represents an approved action.
    /// </summary>
    bool IsApproved { get; }
}
