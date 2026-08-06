using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Operations.Clients.Models;
using Fdw.Services.Connections.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Services.Connections.Components.ConnectionEditor;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="ConnectionEditorProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class ConnectionEditorContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the live form model (create or edit).</summary>
    public CreateConnectionClientRequest Model { get; init; } = new();

    /// <summary>Gets whether this is a new connection (<c>true</c>) or an update (<c>false</c>).</summary>
    public bool IsNew { get; init; } = true;

    /// <summary>Gets the active state toggle value (edit mode only).</summary>
    public bool IsActive { get; init; } = true;

    /// <summary>Gets the available connection types from the ConnectionApi.</summary>
    public IReadOnlyList<ConnectionTypePayload> ConnectionTypes { get; init; } = [];

    /// <summary>Gets the authentication types available for the currently selected service type.</summary>
    public IReadOnlyList<TypeCollectionValueSummary> AuthenticationTypes { get; init; } = [];

    /// <summary>Gets whether auth types are being loaded after a service type change.</summary>
    public bool IsLoadingAuthTypes { get; init; }



    // ── System Guard ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets whether the connection being edited is a system connection (ctrl schema, read-only).
    /// When true, the submit button should be disabled and fields should be read-only.
    /// </summary>
    public bool IsReadOnly { get; init; }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Invoked when the user selects a different service type.
    /// Fetches auth types for the new service type and resets authentication on the model.
    /// </summary>
    public Func<string, Task> OnServiceTypeChanged { get; init; } = _ => Task.CompletedTask;

    /// <summary>
    /// Invoked to submit the form (creates or updates depending on <see cref="IsNew"/>).
    /// </summary>
    public Func<Task> OnSubmit { get; init; } = () => Task.CompletedTask;

    /// <summary>
    /// Invoked when the user toggles the Active state. The provider owns the actual value
    /// (<see cref="IsActive"/>) that is submitted, so the toggle must route through this callback.
    /// </summary>
    public Action<bool> OnActiveChanged { get; init; } = _ => { };

    /// <summary>
    /// Returns <c>true</c> if the current auth type requires (or expects) the given property name.
    /// Used to drive conditional field visibility (e.g., Username, SecretKeyName).
    /// </summary>
    public Func<string, bool> AuthTypeRequiresProperty { get; init; } = _ => false;
}
