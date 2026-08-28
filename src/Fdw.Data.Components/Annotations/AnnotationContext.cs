using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Services.Catalog.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Data.Components.Annotations;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="AnnotationProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AnnotationContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the annotations for the current DataSet.</summary>
    public IReadOnlyList<DataSetAnnotationPayload> Annotations { get; init; } = [];



    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Creates a new annotation for the current DataSet.</summary>
    public Func<CreateAnnotationRequest, Task> OnCreate { get; init; } = _ => Task.CompletedTask;

    /// <summary>Deletes an annotation by its unique identifier.</summary>
    public Func<Guid, Task> OnDelete { get; init; } = _ => Task.CompletedTask;

    /// <summary>Resolves (marks as reviewed) an annotation by its unique identifier.</summary>
    public Func<Guid, Task> OnResolve { get; init; } = _ => Task.CompletedTask;

}
