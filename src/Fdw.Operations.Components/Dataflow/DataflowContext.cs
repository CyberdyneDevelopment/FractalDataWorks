#pragma warning disable CS1591
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Operations.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Operations.Components.Dataflow;

// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class DataflowContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    public DataflowGraphPayload? Graph { get; init; }
    public DataSetLineagePayload? CurrentLineage { get; init; }
    public ImpactAnalysisPayload? LastImpactAnalysis { get; init; }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    public Func<Task> OnLoadGraph { get; init; } = () => Task.CompletedTask;
    public Func<string, Task> OnLoadLineage { get; init; } = _ => Task.CompletedTask;
    public Func<string, string, Task> OnAnalyzeImpact { get; init; } = (_, _) => Task.CompletedTask;
}
