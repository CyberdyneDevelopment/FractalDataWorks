using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Services.Authentication.Components.Profile;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="ProfileProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
public sealed class ProfileContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the current user's profile information.</summary>
    public GetMePayload? Profile { get; init; }

    /// <summary>Gets the current user's preferences as key/value pairs.</summary>
    public IReadOnlyDictionary<string, string> Preferences { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);



    /// <summary>Gets the most recent success message, or <c>null</c> when there is no success.</summary>
    public string? SuccessMessage { get; init; }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked to change the current user's password.</summary>
    public Func<ChangePasswordRequest, Task> OnChangePassword { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to set a single preference key/value pair.</summary>
    public Func<string, string, Task> OnSetPreference { get; init; } = (_, _) => Task.CompletedTask;

}
