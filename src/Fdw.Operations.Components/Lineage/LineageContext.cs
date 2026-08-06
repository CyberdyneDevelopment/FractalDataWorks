#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Operations.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Operations.Components.Lineage;

// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class LineageContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    public LineageGraphPayload? CurrentGraph { get; init; }

    // ── Entity Name Lists (for entity selector dropdowns) ───────────────────

    public IReadOnlyList<string> AvailableConnections { get; init; } = [];
    public IReadOnlyList<string> AvailableDataSets { get; init; } = [];
    public IReadOnlyList<string> AvailablePipelines { get; init; } = [];
    public IReadOnlyList<string> AvailableDataStores { get; init; } = [];
    public IReadOnlyList<string> AvailableCalculations { get; init; } = [];
    public bool IsLoadingEntityNames { get; init; }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    public Func<string, string, Task> OnLoadLineage { get; init; } = (_, _) => Task.CompletedTask;
    public Func<string, string, string, Task> OnLoadColumnLineage { get; init; } = (_, _, _) => Task.CompletedTask;
    public Func<string, Task> OnLoadEntityNames { get; init; } = _ => Task.CompletedTask;
}
