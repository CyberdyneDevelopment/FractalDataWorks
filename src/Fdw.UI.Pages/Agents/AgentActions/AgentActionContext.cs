using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Agents.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Agents.Components.AgentActions;

/// <summary>
/// Context passed from <see cref="AgentActionProvider"/> to consumer render fragments.
/// Exposes the current state and action callbacks for agent action review operations.
/// </summary>
public sealed class AgentActionContext : ProviderContextBase
{
    /// <summary>Gets the list of agent actions.</summary>
    public IReadOnlyList<AgentActionPayload> Actions { get; init; } = [];

    /// <summary>Gets the currently loaded individual action (for detail/review view).</summary>
    public AgentActionPayload? CurrentAction { get; init; }



    /// <summary>Gets the callback that loads (or reloads) the full action list.</summary>
    public Func<Task> OnLoadActions { get; init; } = () => Task.CompletedTask;

    /// <summary>Gets the callback that loads a single action by identifier.</summary>
    public Func<int, Task<AgentActionPayload?>> OnGetAction { get; init; } = _ => Task.FromResult<AgentActionPayload?>(null);

    /// <summary>Gets the callback that approves a pending action.</summary>
    public Func<int, Task<bool>> OnApprove { get; init; } = _ => Task.FromResult(false);

    /// <summary>Gets the callback that denies a pending action.</summary>
    public Func<int, Task<bool>> OnDeny { get; init; } = _ => Task.FromResult(false);
}
