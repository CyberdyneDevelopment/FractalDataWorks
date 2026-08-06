using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Operations.Clients.Models;
using Fdw.Services.Connections.Clients.Models;
using Fdw.Services.Data.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Data.Components.DataSets;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="DataSetWizardProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class DataSetWizardContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the list of available data stores.</summary>
    public IReadOnlyList<DataStoreSummaryPayload> DataStores { get; init; } = [];

    /// <summary>Gets loaded DataStore details keyed by DataStore name, populated on demand.</summary>
    public IReadOnlyDictionary<string, DataStoreDetailPayload> LoadedDataStoreDetails { get; init; } = new Dictionary<string, DataStoreDetailPayload>(StringComparer.Ordinal);

    /// <summary>Gets the existing DataSet detail when editing, or <c>null</c> for new.</summary>
    public DataSetDetailPayload? ExistingDataSet { get; init; }


    /// <summary>Gets whether a DataSet is being submitted.</summary>
    public bool IsSubmitting { get; init; }

    /// <summary>Gets whether container details are being loaded for a DataStore.</summary>
    public bool IsContainerBusy { get; init; }


    /// <summary>Gets the available DataSet service option types loaded from the TypeCollection.</summary>
    public IReadOnlyList<ConfigurationTypeSummary> DataSetTypes { get; init; } = [];

    /// <summary>Gets the available DataStore store types loaded from the TypeCollection.</summary>
    public IReadOnlyList<ConfigurationTypeSummary> DataStoreTypes { get; init; } = [];

    /// <summary>
    /// Gets the capabilities (including field types) for the currently selected DataStore's connection type.
    /// <c>null</c> when no DataStore has been selected in the import panel.
    /// </summary>
    public ConnectionTypeCapabilitiesPayload? SelectedDataStoreCapabilities { get; init; }

    /// <summary>
    /// Gets the field lists for each source, keyed by source name.
    /// Populated on demand when a join source is selected on the Advanced step.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<DataStoreFieldPayload>> LoadedSourceFields { get; init; }
        = new Dictionary<string, IReadOnlyList<DataStoreFieldPayload>>(StringComparer.Ordinal);

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked to reload data stores.</summary>
    public Func<Task> OnLoadDataStores { get; init; } = () => Task.CompletedTask;

    /// <summary>Invoked to load an existing DataSet by name for editing.</summary>
    public Func<string, Task> OnLoadExistingDataSet { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to load the full DataStore detail (including containers) for the given DataStore name.</summary>
    public Func<string, Task> OnLoadContainersForDataStore { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to load connection type capabilities for the given DataStore name.</summary>
    public Func<string, Task> OnLoadCapabilitiesForDataStore { get; init; } = _ => Task.CompletedTask;

    /// <summary>
    /// Invoked to initialize the wizard from an existing container.
    /// Loads container fields, maps them to abstract DataSet types, and returns a pre-built result
    /// the consumer can apply to its form model.
    /// </summary>
    public Func<string, string, Task<ContainerInitializationResult?>> OnInitializeFromContainer { get; init; }
        = (_, _) => Task.FromResult<ContainerInitializationResult?>(null);

    /// <summary>
    /// Invoked to load fields for the named source.
    /// Returns the fields from the source's associated container, or an empty list when the source
    /// has not been configured with a DataStore and container.
    /// </summary>
    public Func<string, Task<IReadOnlyList<DataStoreFieldPayload>>> OnLoadFieldsForSource { get; init; }
        = _ => Task.FromResult<IReadOnlyList<DataStoreFieldPayload>>([]);

    /// <summary>Invoked to submit (create or update) a DataSet. Returns the created/updated detail.</summary>
    public Func<CreateDataSetPayload, Task<DataSetDetailPayload?>> OnSubmit { get; init; } = _ => Task.FromResult<DataSetDetailPayload?>(null);
}
