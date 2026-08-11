using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Catalog.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Services.Catalog.Components.Catalog;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="CatalogProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
public sealed class CatalogContext : ProviderContextBase
{
    // -- State ------------------------------------------------------------------

    /// <summary>Gets the list of catalog entities (DataSets).</summary>
    public IReadOnlyList<CatalogEntityPayload> DataSets { get; init; } = [];

    /// <summary>Gets the current search query.</summary>
    public string SearchQuery { get; init; } = string.Empty;



    // -- Callbacks --------------------------------------------------------------

    /// <summary>Invoked to search the catalog by query string.</summary>
    public Func<string, Task> OnSearch { get; init; } = _ => Task.CompletedTask;


    /// <summary>Invoked when a DataSet is selected by name.</summary>
    public Func<string, Task> OnSelectDataSet { get; init; } = _ => Task.CompletedTask;

    /// <summary>
    /// Invoked to start the Pipeline Builder pre-seeded with the named DataSet as the source.
    /// Navigates to /pipelines/new?sourceDataSet={name}.
    /// </summary>
    public Func<string, Task> OnStartBuilder { get; init; } = _ => Task.CompletedTask;

    /// <summary>
    /// Invoked to open the DataSet wizard pre-seeded with the named DataSet as the base (derive).
    /// Navigates to /datasets/new?baseDataSet={name}.
    /// </summary>
    public Func<string, Task> OnDeriveDataSet { get; init; } = _ => Task.CompletedTask;
}
