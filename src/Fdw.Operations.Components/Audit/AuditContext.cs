using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Operations.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Operations.Components.Audit;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="AuditProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
public sealed class AuditContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the list of execution history entries.</summary>
    public IReadOnlyList<ExecutionSummaryPayload> Entries { get; init; } = [];

    /// <summary>Gets the total count of entries matching the current filter.</summary>
    public int TotalCount { get; init; }



    /// <summary>Gets the current filter item type value.</summary>
    public string? FilterItemType { get; set; }

    /// <summary>Gets the current filter state value.</summary>
    public string? FilterState { get; set; }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked to reload execution history with current filters.</summary>
    public Func<Task> OnLoadHistory { get; init; } = () => Task.CompletedTask;


    /// <summary>Invoked when the user changes filter parameters. Accepts itemType and state.</summary>
    public Func<string?, string?, Task> OnFilterChanged { get; init; } = (_, _) => Task.CompletedTask;
}
