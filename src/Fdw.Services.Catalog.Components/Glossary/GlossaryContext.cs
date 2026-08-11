using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Catalog.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Services.Catalog.Components.Glossary;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="GlossaryProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
public sealed class GlossaryContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the list of glossary terms.</summary>
    public IReadOnlyList<GlossaryTermPayload> Terms { get; init; } = [];



    /// <summary>Gets the current search query.</summary>
    public string? SearchQuery { get; init; }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked to search glossary terms by query string.</summary>
    public Func<string, Task> OnSearch { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to create a new glossary term.</summary>
    public Func<CreateGlossaryTermRequest, Task> OnCreate { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to update an existing glossary term.</summary>
    public Func<Guid, UpdateGlossaryTermRequest, Task> OnUpdate { get; init; } = (_, _) => Task.CompletedTask;

    /// <summary>Invoked to delete a glossary term.</summary>
    public Func<Guid, Task> OnDelete { get; init; } = _ => Task.CompletedTask;

}
