using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Web.Analytics.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Web.Analytics.Components.PromotionReview;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="PromotionReviewProvider"/>.
/// Carries a single promotion request detail with approve/reject callbacks.
/// </summary>
// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class PromotionReviewContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the promotion request being reviewed.</summary>
    public PromotionPayload? Request { get; init; }



    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked to approve the promotion request.</summary>
    public Func<Task> OnApprove { get; init; } = () => Task.CompletedTask;

    /// <summary>Invoked to reject the promotion request.</summary>
    public Func<Task> OnReject { get; init; } = () => Task.CompletedTask;
}
