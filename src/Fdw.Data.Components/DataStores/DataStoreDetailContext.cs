using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Services.Data.Clients.Models;
using Fdw.UI.DrillDown;
using Fdw.UI.Providers;

namespace Fdw.Data.Components.DataStores;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="DataStoreDetailProvider"/>.
/// Wraps the <see cref="ConfigurationDrillDownContext"/> with DataStore-specific selection state
/// and domain actions (schema import, sync).
/// </summary>
// Why: pure view-model — state + callback delegates + a trivial pass-through property, no logic.
[ExcludeFromCodeCoverage]
public sealed class DataStoreDetailContext : ProviderContextBase
{
    // ── DrillDown State ────────────────────────────────────────────────────────

    /// <summary>Gets the configuration-driven drill-down context for tree navigation.</summary>
    public ConfigurationDrillDownContext ConfigurationContext { get; init; } = new();

    /// <summary>
    /// Gets the generic drill-down context for tree navigation.
    /// Provides backward compatibility by delegating to <see cref="ConfigurationContext"/>.
    /// </summary>
    public ConfigurationDrillDownContext DrillDown => ConfigurationContext;

    /// <summary>Gets the loaded DataStore detail, or <c>null</c> if not yet loaded.</summary>
    public DataStoreDetailPayload? DataStore { get; init; }

    // ── Selected Node Detail ───────────────────────────────────────────────────

    /// <summary>Gets the selected path when a Path node is selected, or <c>null</c> otherwise.</summary>
    public DataStorePathPayload? SelectedPath { get; init; }

    /// <summary>Gets the selected container when a Container node is selected, or <c>null</c> otherwise.</summary>
    public DataStoreContainerPayload? SelectedContainer { get; init; }

    /// <summary>Gets the selected field when a Field node is selected, or <c>null</c> otherwise.</summary>
    public DataStoreFieldPayload? SelectedField { get; init; }

    // ── System Guard ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets whether the loaded DataStore is a system DataStore (ctrl schema, read-only).
    /// When true, schema import/sync and other mutation actions should be disabled.
    /// </summary>
    public bool IsReadOnly { get; init; }

    // ── Domain Actions ─────────────────────────────────────────────────────────

    /// <summary>Imports schema for the specified connection name.</summary>
    public Func<string, Task> OnImportSchema { get; init; } = _ => Task.CompletedTask;

    /// <summary>Synchronizes schema for the specified connection name.</summary>
    public Func<string, Task> OnSyncSchema { get; init; } = _ => Task.CompletedTask;

    /// <summary>Adds a container to the named path. Parameters: pathName, containerName, containerType (nullable).</summary>
    public Func<string, string, string?, Task> OnAddContainer { get; init; } = (_, _, _) => Task.CompletedTask;

    /// <summary>Gets the error message from the last add-container attempt, or <c>null</c> if none.</summary>
    public string? AddContainerError { get; init; }
}
