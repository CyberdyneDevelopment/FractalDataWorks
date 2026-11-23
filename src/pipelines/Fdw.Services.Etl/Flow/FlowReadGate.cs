using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// Lets the reads that feed a join, lookup or union across data stores run as the federation strategy says (FDW-6970): no more than its degree at
/// once, and, when it orders them, every build-side read started before any other.
/// </summary>
/// <remarks>
/// <para>
/// A read holds its slot until it has sent its last row. The flow's joins and lookups pump their probe side while they build, and a union reads every
/// input at once, so a read waiting for a slot never holds up the one that has it.
/// </para>
/// <para>
/// Why the build side first: a join holds its build side whole in memory and streams its probe side past it, so reading the build side first lets the
/// probe side's rows go straight through instead of waiting in the pump.
/// </para>
/// <para>
/// The slots are tokens in a channel rather than a semaphore, so the gate owns nothing to dispose and lives exactly as long as the read it gates.
/// </para>
/// </remarks>
public sealed class FlowReadGate
{
    private readonly Channel<bool> _slots = Channel.CreateUnbounded<bool>();
    private readonly TaskCompletionSource _buildSideStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _buildSideWaiting;

    /// <summary>Initializes a new instance of the <see cref="FlowReadGate"/> class.</summary>
    /// <param name="settings">How the reads are let run.</param>
    /// <param name="buildSideReads">How many of the gated reads are on a build side.</param>
    public FlowReadGate(FlowReadGateSettings settings, int buildSideReads)
    {
        if (settings.MaxConcurrentReads < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(settings), settings.MaxConcurrentReads, "A read gate lets at least one read run at once.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(buildSideReads);
        for (var slot = 0; slot < settings.MaxConcurrentReads; slot++)
        {
            _slots.Writer.TryWrite(true);
        }

        _buildSideWaiting = buildSideReads;
        Settings = settings;
        if (!settings.BuildSideFirst || buildSideReads == 0)
        {
            _buildSideStarted.TrySetResult();
        }
    }

    /// <summary>Gets how the reads are let run.</summary>
    public FlowReadGateSettings Settings { get; }

    /// <summary>Waits until a read may start, and holds its slot until the returned handle is disposed.</summary>
    /// <param name="buildSide">Whether the read is on a join's build side.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The slot, released when disposed.</returns>
    public async Task<IAsyncDisposable> Enter(bool buildSide, CancellationToken cancellationToken = default)
    {
        if (!buildSide)
        {
            await _buildSideStarted.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        await _slots.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (buildSide && Interlocked.Decrement(ref _buildSideWaiting) == 0)
        {
            _buildSideStarted.TrySetResult();
        }

        return new Slot(_slots.Writer);
    }

    private sealed class Slot(ChannelWriter<bool> slots) : IAsyncDisposable
    {
        private int _released;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                slots.TryWrite(true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
