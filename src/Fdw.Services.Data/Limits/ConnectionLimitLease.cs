using System;
using System.Collections.Generic;
using System.Threading;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Data.Limits;

/// <summary>
/// What a connection's limits hold for the duration of one call, or of one open cursor (FDW-949).
/// </summary>
/// <remarks>
/// A concurrency limit only limits anything if the slot it grants is occupied while the work runs, so
/// admission takes a slot per configured cap and keeps it here until the lease is disposed: at the end of
/// an <c>Execute</c>, or when the caller disposes a cursor. Disposing is idempotent, and it is also what
/// hands the slots back when a later check refuses a call that earlier checks had admitted.
/// </remarks>
internal sealed class ConnectionLimitLease : IDisposable
{
    private readonly List<SemaphoreSlim> _held = [];
    private int _released;

    /// <summary>Initializes a new instance of the <see cref="ConnectionLimitLease"/> class.</summary>
    /// <param name="limits">The limits that applied to the admitted call; empty when none are configured.</param>
    public ConnectionLimitLease(IReadOnlyList<ConnectionLimitConfiguration> limits)
    {
        Limits = limits ?? throw new ArgumentNullException(nameof(limits));
    }

    /// <summary>Gets the limits that applied to the admitted call.</summary>
    public IReadOnlyList<ConnectionLimitConfiguration> Limits { get; }

    /// <summary>Gets whether the lease is holding any concurrency slot.</summary>
    public bool HoldsSlots => _held.Count > 0;

    /// <summary>Records a slot that has been taken and must be handed back on dispose.</summary>
    /// <param name="semaphore">The semaphore a slot was acquired from.</param>
    public void Hold(SemaphoreSlim semaphore) => _held.Add(semaphore);

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _released, 1) != 0)
        {
            return;
        }

        foreach (var semaphore in _held)
        {
            semaphore.Release();
        }
    }
}
