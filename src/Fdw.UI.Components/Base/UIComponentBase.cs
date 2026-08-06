using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Fdw.UI.Components.Base;

/// <summary>
/// Base class for FDW Blazor components that provides a <see cref="CancellationToken"/>
/// tied to the component lifecycle. The token is cancelled when the component is disposed,
/// ensuring that in-flight async operations (API calls, SignalR subscriptions, etc.) are
/// cancelled promptly when the user navigates away.
/// </summary>
public abstract class UIComponentBase : ComponentBase, IAsyncDisposable
{
    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// Gets a <see cref="CancellationToken"/> that is cancelled when this component is disposed.
    /// Pass this token to all async operations (API calls, etc.) to ensure they are cancelled
    /// when the component is removed from the render tree.
    /// </summary>
    protected CancellationToken ComponentCt => _cts.Token;

    /// <summary>
    /// Disposes the component by cancelling the <see cref="ComponentCt"/> token and releasing
    /// the underlying <see cref="CancellationTokenSource"/>. Override this method in derived
    /// components that need additional cleanup (e.g., disposing SignalR subscriptions),
    /// and call <c>await base.DisposeAsync()</c> to ensure the cancellation token is handled.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
    public virtual async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
