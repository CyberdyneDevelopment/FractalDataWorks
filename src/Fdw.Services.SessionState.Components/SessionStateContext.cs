using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.UI.Providers;

namespace Fdw.Services.SessionState.Components;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="SessionStateProvider"/>.
/// Carries the current session state snapshot and callback delegates for state manipulation.
/// </summary>
// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class SessionStateContext : ProviderContextBase
{
    /// <summary>
    /// Gets the current session state as a read-only dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, object?> State { get; init; }
        = new Dictionary<string, object?>(StringComparer.Ordinal);



    /// <summary>
    /// Callback to save a state value by key.
    /// </summary>
    public Func<string, object?, Task> OnSaveState { get; init; } = (_, _) => Task.CompletedTask;

    /// <summary>
    /// Callback to delete a state value by key.
    /// </summary>
    public Func<string, Task> OnDeleteState { get; init; } = _ => Task.CompletedTask;
}
