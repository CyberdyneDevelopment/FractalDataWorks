using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Services.Data.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Data.Components.DataSets;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="FieldMappingTransformProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class FieldMappingTransformContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the field mapping identifier this context operates on.</summary>
    public Guid FieldMappingId { get; init; }

    /// <summary>Gets the display name of the field mapping.</summary>
    public string FieldMappingName { get; init; } = string.Empty;

    /// <summary>Gets the ordered list of transforms in the chain.</summary>
    public IReadOnlyList<FieldMappingTransformPayload> Transforms { get; init; } = [];

    /// <summary>Gets the available transform types that can be added.</summary>
    public IReadOnlyList<TransformTypePayload> AvailableTransformTypes { get; init; } = [];

    /// <summary>Gets the currently selected transform for editing, or <c>null</c>.</summary>
    public FieldMappingTransformPayload? SelectedTransform { get; init; }


    /// <summary>Gets whether a save operation is in progress.</summary>
    public bool IsSaving { get; init; }


    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Loads transforms and available types for the field mapping.</summary>
    public Func<Task> OnLoadTransforms { get; init; } = () => Task.CompletedTask;

    /// <summary>Selects a transform for editing by its identifier.</summary>
    public Func<Guid, Task> OnSelectTransform { get; init; } = _ => Task.CompletedTask;

    /// <summary>Adds a new transform of the specified type name.</summary>
    public Func<string, Task> OnAddTransform { get; init; } = _ => Task.CompletedTask;

    /// <summary>Saves a transform (create or update).</summary>
    public Func<SaveFieldMappingTransformRequest, Task> OnSaveTransform { get; init; } = _ => Task.CompletedTask;

    /// <summary>Deletes a transform by its identifier.</summary>
    public Func<Guid, Task> OnDeleteTransform { get; init; } = _ => Task.CompletedTask;

    /// <summary>Reorders transforms by providing the full ordered list of identifiers.</summary>
    public Func<IReadOnlyList<Guid>, Task> OnReorderTransforms { get; init; } = _ => Task.CompletedTask;

    /// <summary>Moves a transform one position up in the chain.</summary>
    public Func<Guid, Task> OnMoveUp { get; init; } = _ => Task.CompletedTask;

    /// <summary>Moves a transform one position down in the chain.</summary>
    public Func<Guid, Task> OnMoveDown { get; init; } = _ => Task.CompletedTask;
}
