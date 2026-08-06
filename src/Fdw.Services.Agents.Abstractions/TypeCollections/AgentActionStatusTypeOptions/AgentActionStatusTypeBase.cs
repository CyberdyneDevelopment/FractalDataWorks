using Fdw.Collections;

namespace Fdw.Services.Agents.Abstractions.TypeCollections.AgentActionStatusTypeOptions;

/// <summary>
/// Base class for agent action status types using the CRTP pattern.
/// </summary>
public abstract class AgentActionStatusTypeBase : TypeOptionBase<int, AgentActionStatusTypeBase>, IAgentActionStatusType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentActionStatusTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The unique name.</param>
    /// <param name="isTerminal">Whether this status is a terminal (final) state.</param>
    /// <param name="isApproved">Whether this status represents an approved action.</param>
    protected AgentActionStatusTypeBase(
        int id,
        string name,
        bool isTerminal,
        bool isApproved)
        : base(id, name)
    {
        IsTerminal = isTerminal;
        IsApproved = isApproved;
    }

    /// <inheritdoc/>
    public bool IsTerminal { get; }

    /// <inheritdoc/>
    public bool IsApproved { get; }
}
