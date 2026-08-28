using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.UI.Pipelines.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Data.Components.DataSets;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="CalculatedDataSetProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CalculatedDataSetContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the pipeline ID if an existing pipeline was loaded.</summary>
    public Guid? PipelineId { get; init; }

    /// <summary>Gets the initial tasks from the loaded pipeline.</summary>
    public IReadOnlyList<TaskPayload> InitialTasks { get; init; } = [];

    /// <summary>Gets the initial connections from the loaded pipeline.</summary>
    public IReadOnlyList<TaskConnectionPayload> InitialConnections { get; init; } = [];


    /// <summary>Gets whether the pipeline is being saved.</summary>
    public bool IsSaving { get; init; }

    /// <summary>Gets whether the graph is being compiled into a calculation entity.</summary>
    public bool IsCompiling { get; init; }


    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked to load an existing pipeline designer by DataSet name.</summary>
    public Func<string, Task> OnLoadExistingDesigner { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to save the pipeline graph. Returns the saved pipeline ID.</summary>
    public Func<IReadOnlyList<TaskPayload>, IReadOnlyList<TaskConnectionPayload>, Task<Guid?>> OnSave { get; init; } =
        (_, _) => Task.FromResult<Guid?>(null);

    /// <summary>Invoked to compile the pipeline graph into a calculation entity. Returns the entity ID on success.</summary>
    public Func<IReadOnlyList<TaskPayload>, IReadOnlyList<TaskConnectionPayload>, Task<IGenericResult<Guid>>> OnCompile { get; init; } =
        static (_, _) => throw new InvalidOperationException("CalculatedDataSetContext.OnCompile was not wired by the provider.");
}
