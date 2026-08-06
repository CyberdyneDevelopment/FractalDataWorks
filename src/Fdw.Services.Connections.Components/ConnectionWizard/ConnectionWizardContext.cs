using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Operations.Clients.Models;
using Fdw.Services.Connections.Clients.Models;
using Fdw.UI.Wizard;
using Fdw.UI.Providers;

namespace Fdw.Services.Connections.Components.ConnectionWizard;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="ConnectionWizardProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class ConnectionWizardContext : ProviderContextBase
{
    // ── Wizard Navigation ─────────────────────────────────────────────────────

    /// <summary>Gets the shared wizard navigation and status state from the base provider.</summary>
    public IWizardContext Wizard { get; init; } = new WizardContext();

    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the current wizard step (0=Configure, 1=Test, 2=Save/Complete).</summary>
    public int Step { get; init; }

    /// <summary>Gets the live form model for the new connection.</summary>
    public CreateConnectionClientRequest ConnectionConfig { get; init; } = new();

    /// <summary>Gets the available connection types from the ConnectionApi.</summary>
    public IReadOnlyList<ConnectionTypePayload> ConnectionTypes { get; init; } = [];

    /// <summary>Gets the authentication types available for the currently selected service type.</summary>
    public IReadOnlyList<TypeCollectionValueSummary> AuthenticationTypes { get; init; } = [];

    /// <summary>Gets whether auth types are being loaded after a service type change.</summary>
    public bool IsLoadingAuthTypes { get; init; }

    /// <summary>Gets the most recent test result, or <c>null</c> if no test has been run.</summary>
    public TestConnectionClientResponse? TestResult { get; init; }



    /// <summary>Gets whether the wizard has completed successfully.</summary>
    public bool IsComplete { get; init; }

    /// <summary>Gets whether the wizard is on the first step (<see cref="Step"/> == 0).</summary>
    public bool IsFirstStep { get; init; }

    /// <summary>Gets whether the wizard is on the last step (<see cref="Step"/> == 2).</summary>
    public bool IsLastStep { get; init; }

    // ── Secret Manager State ───────────────────────────────────────────────────

    /// <summary>Gets the names of available secret managers, loaded from the API.</summary>
    public IReadOnlyList<string> AvailableSecretManagers { get; init; } = [];

    /// <summary>Gets the name of the secret manager the user has selected, or <c>null</c> if none selected.</summary>
    public string? SelectedSecretManagerName { get; init; }

    /// <summary>
    /// Gets the secret storage mode: "new" to enter a password and store it, "existing" to type an existing key name.
    /// Only relevant when <see cref="SelectedSecretManagerName"/> is non-null.
    /// </summary>
    public string SecretStorageMode { get; init; } = "new";

    /// <summary>Gets the current plain-text password entered by the user (cleared after secret is stored).</summary>
    public string? PlainPassword { get; init; }

    /// <summary>Gets the key name the password was stored under, or <c>null</c> if not yet stored.</summary>
    public string? StoredSecretKeyName { get; init; }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Invoked when the user selects a different service type.
    /// Fetches auth types for the new service type and resets authentication on the model.
    /// </summary>
    public Func<string, Task> OnServiceTypeChanged { get; init; } = _ => Task.CompletedTask;

    /// <summary>
    /// Advances to the next step. On step 1→2, runs a test and only advances on success.
    /// </summary>
    public Func<Task> OnNextStep { get; init; } = () => Task.CompletedTask;

    /// <summary>Returns to the previous step.</summary>
    public Action OnPreviousStep { get; init; } = () => { };

    /// <summary>Explicitly re-runs the connection test on step 1.</summary>
    public Func<Task> OnTestConnection { get; init; } = () => Task.CompletedTask;

    /// <summary>Saves (creates) the connection on the final step.</summary>
    public Func<Task> OnSaveConnection { get; init; } = () => Task.CompletedTask;

    /// <summary>Invoked when the user selects a different secret manager from the dropdown.</summary>
    public Action<string> OnSecretManagerChanged { get; init; } = _ => { };

    /// <summary>Invoked when the user switches between "new" and "existing" secret storage mode.</summary>
    public Action<string> OnSecretStorageModeChanged { get; init; } = _ => { };

    /// <summary>Updates the plain-text password field (used when storage mode is "new").</summary>
    public Action<string> OnPasswordChanged { get; init; } = _ => { };
}
