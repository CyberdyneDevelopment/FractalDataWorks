using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Operations.Clients.Models;
using Fdw.Services.Connections.Clients.Models;
using Fdw.Services.Data.Clients.Models;
using Fdw.UI.Wizard;
using Fdw.UI.Providers;

namespace Fdw.Data.Components.DataStores;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="DataStoreEditorProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class DataStoreEditorContext : ProviderContextBase
{
    // ── Wizard Navigation ─────────────────────────────────────────────────────

    /// <summary>Gets the shared wizard navigation and status state from the base provider.</summary>
    public IWizardContext Wizard { get; init; } = new WizardContext();

    // ── Wizard State ───────────────────────────────────────────────────────────

    /// <summary>Gets the current wizard step (0=Basic Info, 1=Store Type &amp; Write Config, 2=Container Management).</summary>
    public int Step { get; init; }

    /// <summary>Gets whether the wizard is on the first step (<see cref="Step"/> == 0).</summary>
    public bool IsFirstStep { get; init; }

    /// <summary>Gets whether the wizard is on the last step (<see cref="Step"/> == 2).</summary>
    public bool IsLastStep { get; init; }

    // ── Form Model ─────────────────────────────────────────────────────────────

    /// <summary>Gets the live editor form model.</summary>
    public DataStoreEditorModel Form { get; init; } = new();

    // ── Data State ─────────────────────────────────────────────────────────────

    /// <summary>Gets the list of available connections for the connection picker.</summary>
    public IReadOnlyList<ConnectionPayload> Connections { get; init; } = [];

    /// <summary>Gets the available DataStore types loaded from the configuration API.</summary>
    public IReadOnlyList<ConfigurationTypeSummary> DataStoreTypes { get; init; } = [];

    /// <summary>Gets the capabilities for the currently selected connection type, or <c>null</c> if none loaded.</summary>
    public ConnectionTypeCapabilitiesPayload? Capabilities { get; init; }

    /// <summary>Gets the pending container entries being managed on step 2.</summary>
    public IReadOnlyList<DataPathRequest> Paths { get; init; } = [];

    // ── System Guard ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets whether the DataStore being edited is a system DataStore (ctrl schema, read-only).
    /// When true, the save button should be disabled and fields should be read-only.
    /// </summary>
    public bool IsReadOnly { get; init; }

    // ── Loading / Error State ──────────────────────────────────────────────────


    /// <summary>Gets whether a save/submit is in progress.</summary>
    public bool IsSaving { get; init; }

    /// <summary>Gets whether capabilities are being loaded after a connection change.</summary>
    public bool IsLoadingCapabilities { get; init; }


    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Advances to the next wizard step.</summary>
    public Func<Task> OnNextStep { get; init; } = () => Task.CompletedTask;

    /// <summary>Returns to the previous wizard step.</summary>
    public Action OnPreviousStep { get; init; } = () => { };

    /// <summary>Invoked when the user selects a different connection. Loads capabilities for the selected connection type.</summary>
    public Func<string, Task> OnConnectionChanged { get; init; } = _ => Task.CompletedTask;

    /// <summary>Adds a new path/container entry to the list on step 2.</summary>
    public Action<DataPathRequest> OnAddPath { get; init; } = _ => { };

    /// <summary>Removes a path/container entry by index from the list on step 2.</summary>
    public Action<int> OnRemovePath { get; init; } = _ => { };

    /// <summary>Saves the data store (create or update) and navigates away on success.</summary>
    public Func<Task> OnSave { get; init; } = () => Task.CompletedTask;

    /// <summary>Reloads the connection list from the API without changing the current selection.</summary>
    public Func<Task> OnRefreshConnections { get; init; } = () => Task.CompletedTask;

    /// <summary>Reloads the connection list and auto-selects the named connection, loading its capabilities.</summary>
    public Func<string, Task> OnRefreshAndSelectConnection { get; init; } = _ => Task.CompletedTask;
}
