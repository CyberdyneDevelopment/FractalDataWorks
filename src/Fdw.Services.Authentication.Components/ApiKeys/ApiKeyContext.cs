using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Services.Authentication.Components.ApiKeys;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="ApiKeyProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
public sealed class ApiKeyContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the list of personal access token summaries.</summary>
    public IReadOnlyList<PersonalAccessTokenSummaryPayload> PersonalTokens { get; init; } = [];

    /// <summary>Gets the list of agent key summaries.</summary>
    public IReadOnlyList<AgentKeySummaryPayload> AgentKeys { get; init; } = [];



    /// <summary>Gets the raw token value returned after creation (only available once).</summary>
    public string? NewTokenValue { get; init; }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked to create a new personal access token.</summary>
    public Func<CreateTokenRequest, Task> OnCreateToken { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to revoke a personal access token by its ID.</summary>
    public Func<Guid, Task> OnRevokeToken { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to create a new agent key.</summary>
    public Func<CreateAgentKeyRequest, Task> OnCreateAgentKey { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to delete an agent key by its ID.</summary>
    public Func<Guid, Task> OnDeleteAgentKey { get; init; } = _ => Task.CompletedTask;

}
