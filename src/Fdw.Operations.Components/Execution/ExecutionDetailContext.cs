using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Operations.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Operations.Components.Execution;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="ExecutionDetailProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class ExecutionDetailContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the execution detail, or <c>null</c> when not yet loaded.</summary>
    public ExecutionSummaryPayload? Execution { get; init; }

    /// <summary>Gets the child executions (steps/stages) for the loaded execution.</summary>
    public IReadOnlyList<ExecutionSummaryPayload> Children { get; init; } = [];



    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked to load execution detail by ID.</summary>
    public Func<Guid, Task> OnLoadExecution { get; init; } = _ => Task.CompletedTask;

}
