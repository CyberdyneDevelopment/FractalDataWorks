#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Schema.Clients.Models;
using Fdw.Services.Data.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Schema.Components.Schema;

[ExcludeFromCodeCoverage]
public sealed class SchemaContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    public IReadOnlyList<SchemaCapableConnectionPayload> CapableConnections { get; init; } = [];
    public SchemaDiscoveryResponse? DiscoveryResult { get; init; }
    public DataPreviewResponsePayload? PreviewResult { get; init; }

    // Schema sync state
    public SyncSchemaResponse? SyncResult { get; init; }
    public bool IsSyncing { get; init; }
    public DateTimeOffset? LastRefreshedAt { get; init; }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    public Func<Task> OnLoadCapableConnections { get; init; } = () => Task.CompletedTask;
    public Func<string, Task> OnDiscover { get; init; } = _ => Task.CompletedTask;
    public Func<SchemaPreviewRequest, Task> OnPreviewData { get; init; } = _ => Task.CompletedTask;
    public Func<string, Task> OnSyncSchema { get; init; } = _ => Task.CompletedTask;
    public Func<string, Task> OnApplySchemaChanges { get; init; } = _ => Task.CompletedTask;
    public Func<string, Task> OnImportSchema { get; init; } = _ => Task.CompletedTask;
    public Func<Task> OnClearSyncResult { get; init; } = () => Task.CompletedTask;
}
