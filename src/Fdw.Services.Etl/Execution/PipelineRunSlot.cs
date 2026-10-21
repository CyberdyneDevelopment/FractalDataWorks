using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The one place a pipeline run happens at a time (FDW-1511). The worker takes the slot before it starts a run and gives it back
/// when the run ends; a run that waits for the pipeline it started hands the slot back while it waits, so the pipeline it started
/// can take it, and takes it again afterwards.
/// </summary>
/// <remarks>
/// Why a hand-back and not more workers: runs still never overlap, so two pipelines never write one table at the same time.
/// Why only a holder hands it back: a caller that does not hold the slot (an orchestration node waiting for a pipeline) has
/// nothing to give back, and releasing would let a second run in. Whether the calling run holds it is read from the flow of
/// the run itself.
/// </remarks>
public sealed class PipelineRunSlot : IDisposable
{
    private readonly SemaphoreSlim _slot = new(1, 1);
    private readonly AsyncLocal<bool> _held = new();
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineRunSlot"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public PipelineRunSlot(ILogger<PipelineRunSlot>? logger = null)
    {
        _logger = logger ?? NullLogger<PipelineRunSlot>.Instance;
    }

    /// <summary>Waits for the slot.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that ends when the slot is the caller's.</returns>
    public Task Acquire(CancellationToken cancellationToken = default) => _slot.WaitAsync(cancellationToken);

    /// <summary>Says that the run in the current flow holds the slot; the worker calls it on the task that runs the run.</summary>
    public void MarkHeld() => _held.Value = true;

    /// <summary>Gives the slot back when the run ends.</summary>
    public void Release()
    {
        _held.Value = false;
        _slot.Release();
    }

    /// <summary>Hands the slot back for as long as the returned handle lives, when the current flow holds it.</summary>
    /// <returns>A handle that takes the slot again when disposed; one that does nothing when the current flow does not hold it.</returns>
    public IAsyncDisposable Yield()
    {
        if (!_held.Value)
        {
            return NothingToGiveBack.Instance;
        }

        _slot.Release();
        PipelineRunSlotLog.SlotHandedBack(_logger);
        return new TakeAgain(this);
    }

    /// <inheritdoc />
    public void Dispose() => _slot.Dispose();

    private sealed class NothingToGiveBack : IAsyncDisposable
    {
        internal static readonly NothingToGiveBack Instance = new();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class TakeAgain : IAsyncDisposable
    {
        private readonly PipelineRunSlot _owner;

        internal TakeAgain(PipelineRunSlot owner)
        {
            _owner = owner;
        }

        // Not cancellable: the run ends by releasing the slot, so it has to hold it again first.
        public async ValueTask DisposeAsync()
        {
            await _owner._slot.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            PipelineRunSlotLog.SlotTakenAgain(_owner._logger);
        }
    }
}
