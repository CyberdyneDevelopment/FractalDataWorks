using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Web.Analytics.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Web.Analytics.Components.Promotions;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="PromotionProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class PromotionContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the list of promotion requests.</summary>
    public IReadOnlyList<PromotionPayload> Requests { get; init; } = [];



    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked to create a new promotion request.</summary>
    public Func<CreatePromotionPayload, Task> OnCreate { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to approve a pending promotion request.</summary>
    public Func<Guid, Task> OnApprove { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to reject a pending promotion request.</summary>
    public Func<Guid, Task> OnReject { get; init; } = _ => Task.CompletedTask;

}
