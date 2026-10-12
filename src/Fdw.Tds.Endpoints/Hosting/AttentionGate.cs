using System;
using System.Threading;

namespace Fdw.Tds.Endpoints.Hosting;

/// <summary>
/// Joins a client's attention signal to the request it cancels.
/// </summary>
/// <remarks>
/// The connection's read loop signals; the request loop opens and closes the request. An attention that
/// finds a request running cancels it and is acknowledged by that request's response; one that finds none
/// is acknowledged on its own.
/// </remarks>
internal sealed class AttentionGate
{
    private readonly Lock _lock = new();
    private CancellationTokenSource? _current;
    private bool _requested;

    /// <summary>Opens a request.</summary>
    /// <param name="request">The request's cancellation source.</param>
    public void Begin(CancellationTokenSource request)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (_lock)
        {
            _current = request;
            _requested = false;
        }
    }

    /// <summary>Signals an attention.</summary>
    /// <returns>True when a request was running and is now cancelled; false when none was.</returns>
    public bool Signal()
    {
        lock (_lock)
        {
            if (_current is null)
                return false;

            _requested = true;
            _current.Cancel();
            return true;
        }
    }

    /// <summary>Closes the request.</summary>
    /// <returns>Whether an attention arrived while it ran.</returns>
    public bool End()
    {
        lock (_lock)
        {
            var requested = _requested;
            _current = null;
            _requested = false;
            return requested;
        }
    }
}
