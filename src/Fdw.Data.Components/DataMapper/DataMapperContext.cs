using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Services.Data.Clients.Models;
using Fdw.Data.Components.Models;
using Fdw.UI.Providers;

namespace Fdw.Data.Components.DataMapper;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="DataMapperProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DataMapperContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the root-level DataStore nodes shared by both source and target pickers.</summary>
    public IReadOnlyList<DataStoreNode> DataStorePickerItems { get; init; } = [];

    /// <summary>Gets the discovered fields from the selected source container.</summary>
    public IReadOnlyList<DataStoreFieldPayload> SourceFields { get; init; } = [];

    /// <summary>Gets the discovered fields from the selected target container.</summary>
    public IReadOnlyList<DataStoreFieldPayload> TargetFields { get; init; } = [];

    /// <summary>Gets the current list of field mappings.</summary>
    public IReadOnlyList<FieldMappingDto> Mappings { get; init; } = [];

    /// <summary>Gets the name of the selected source DataStore.</summary>
    public string SourceDataStore { get; init; } = string.Empty;

    /// <summary>Gets the name of the selected source path.</summary>
    public string SourcePath { get; init; } = string.Empty;

    /// <summary>Gets the name of the selected source container.</summary>
    public string SourceContainer { get; init; } = string.Empty;

    /// <summary>Gets the name of the selected target DataStore.</summary>
    public string TargetDataStore { get; init; } = string.Empty;

    /// <summary>Gets the name of the selected target path.</summary>
    public string TargetPath { get; init; } = string.Empty;

    /// <summary>Gets the name of the selected target container.</summary>
    public string TargetContainer { get; init; } = string.Empty;

    /// <summary>Gets the derived dataset name built from the source and target container selections.</summary>
    public string DatasetName { get; init; } = string.Empty;


    /// <summary>Gets whether container field details are being loaded.</summary>
    public bool IsLoadingSchema { get; init; }


    /// <summary>Gets the most recent validation result, or <c>null</c> before the first validation run.</summary>
    public ValidationResultDto? ValidationResult { get; init; }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Async child-loader for the source DataStore → Path → Container picker.</summary>
    public Func<DataStoreNode, Task<IReadOnlyList<DataStoreNode>>> GetSourcePickerChildren { get; init; }
        = _ => Task.FromResult<IReadOnlyList<DataStoreNode>>([]);

    /// <summary>Async child-loader for the target DataStore → Path → Container picker.</summary>
    public Func<DataStoreNode, Task<IReadOnlyList<DataStoreNode>>> GetTargetPickerChildren { get; init; }
        = _ => Task.FromResult<IReadOnlyList<DataStoreNode>>([]);

    /// <summary>Invoked when the source picker selection chain changes.</summary>
    public Func<IReadOnlyList<DataStoreNode>, Task> OnSourcePickerSelectionChanged { get; init; }
        = _ => Task.CompletedTask;

    /// <summary>Invoked when the target picker selection chain changes.</summary>
    public Func<IReadOnlyList<DataStoreNode>, Task> OnTargetPickerSelectionChanged { get; init; }
        = _ => Task.CompletedTask;

    /// <summary>Invoked when the consumer edits the mapping list directly.</summary>
    public Func<IReadOnlyList<FieldMappingDto>, Task> OnMappingsChanged { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to trigger automatic name-based field mapping.</summary>
    public Func<Task> OnAutoMap { get; init; } = () => Task.CompletedTask;

    /// <summary>Invoked to run mapping validation and populate <see cref="ValidationResult"/>.</summary>
    public Func<Task> OnValidate { get; init; } = () => Task.CompletedTask;

    /// <summary>Invoked to persist the current mappings.</summary>
    public Func<Task> OnSave { get; init; } = () => Task.CompletedTask;
}
