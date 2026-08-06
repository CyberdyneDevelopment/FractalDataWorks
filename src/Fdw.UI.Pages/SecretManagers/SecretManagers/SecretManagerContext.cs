using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.SecretManagers.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Services.SecretManagers.Components.SecretManagers;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="SecretManagerProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
public sealed class SecretManagerContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the list of secret manager summaries.</summary>
    public IReadOnlyList<SecretManagerSummaryPayload> SecretManagers { get; init; } = [];

    /// <summary>Gets the list of available secret manager types (populated from the server's registered types).</summary>
    public IReadOnlyList<SecretManagerTypeSummaryPayload> AvailableTypes { get; init; } = [];

    /// <summary>Gets the currently selected secret manager detail, or <c>null</c> when none is selected.</summary>
    public SecretManagerDetailPayload? SelectedManager { get; init; }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked to create a new secret manager configuration.</summary>
    public Func<CreateSecretManagerPayload, Task<IGenericResult>> OnCreate { get; init; } = _ => CallbackNotProvided();

    /// <summary>Invoked to update an existing secret manager configuration.</summary>
    public Func<string, UpdateSecretManagerPayload, Task<IGenericResult>> OnUpdate { get; init; } = (_, _) => CallbackNotProvided();

    /// <summary>Invoked to delete a secret manager configuration by name.</summary>
    public Func<string, Task<IGenericResult>> OnDelete { get; init; } = _ => CallbackNotProvided();

    /// <summary>Invoked to select a secret manager by name for detail view.</summary>
    public Func<string, Task<IGenericResult>> OnSelect { get; init; } = _ => CallbackNotProvided();
}
