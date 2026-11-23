using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>
/// What a host holds while a run it carries out holds its claim in the operational database (FDW-6939): it claims, asks again at the poll setting while the run waits, renews the lease at the renewal setting
/// while the run runs, fences the run when the claim is lost, hands the claim back while the run waits for a pipeline it started, and releases the claim when it is disposed. A run that claims no key is
/// claimed at once and has nothing to renew, yield or release.
/// </summary>
public sealed class PipelineRunClaimHold : IAsyncDisposable
{
    private static readonly AsyncLocal<PipelineRunClaimHold?> Ambient = new();

    private readonly IPipelineRunClaimService _service;
    private readonly PipelineRunClaimRequest _request;
    private readonly PipelineRunClaimSettings _settings;
    private readonly TimeProvider _clock;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private volatile PipelineRunLease? _lease;
    private readonly ILogger _logger;
    private CancellationTokenSource _ended = new();
    private SemaphoreSlim? _stopped;
    private int _lost;
    private int _yielded;
    private int _disposed;

    private PipelineRunClaimHold(
        IPipelineRunClaimService service, PipelineRunClaimRequest request, PipelineRunClaimSettings settings, TimeProvider clock, Func<TimeSpan, CancellationToken, Task> delay, ILogger logger)
    {
        _logger = logger;
        _service = service;
        _request = request;
        _settings = settings;
        _clock = clock;
        _delay = delay;
    }

    /// <summary>
    /// Gets or sets the hold of the run in the current flow, which a run that waits for a pipeline it started hands back (<see cref="YieldCurrent"/>). Set by the run code around the run, as the scheduler keeps
    /// the place of the run in the current flow.
    /// </summary>
    public static PipelineRunClaimHold? Current
    {
        get => Ambient.Value;
        set => Ambient.Value = value;
    }

    /// <summary>Claims the keys of a run and waits, asking again, until the run holds them.</summary>
    /// <param name="service">The claim service.</param>
    /// <param name="request">The run and its keys.</param>
    /// <param name="settings">The four settings of the claim.</param>
    /// <param name="clock">The clock whose monotonic timestamp measures the lease.</param>
    /// <param name="delay">Waits the time asked, or until the token ends the wait.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">Cancels the claim; the rows the run left waiting are released and the cancellation reaches the caller.</param>
    /// <returns>The hold, or the failure of a claim call.</returns>
    public static async Task<IGenericResult<PipelineRunClaimHold>> Acquire(
        IPipelineRunClaimService service,
        PipelineRunClaimRequest request,
        PipelineRunClaimSettings settings,
        TimeProvider clock,
        Func<TimeSpan, CancellationToken, Task> delay,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var held = await ClaimUntilHeld(service, request, settings, delay, cancellationToken).ConfigureAwait(false);
        if (held.IsFailure)
            return held.ToNewResult<PipelineRunClaimHold>();

        var hold = new PipelineRunClaimHold(service, request, settings, clock, delay, logger);
        hold.Begin();
        return GenericResult<PipelineRunClaimHold>.Success(hold);
    }

    /// <summary>Hands the claim of the run in the current flow back for as long as the returned handle lives.</summary>
    /// <param name="cancellationToken">Cancels the release.</param>
    /// <returns>A handle that claims again when disposed; one that does nothing when the current flow holds no claim.</returns>
    public static async Task<IAsyncDisposable> YieldCurrent(CancellationToken cancellationToken = default)
        => Ambient.Value is { } hold ? await hold.Yield(cancellationToken).ConfigureAwait(false) : NothingToGiveBack.Instance;

    /// <summary>
    /// Hands the claim back while the run waits for a pipeline it started (FDW-6939): the claim is released, which promotes the runs waiting behind it, so a child whose claim conflicts with the parent's can run
    /// and end; disposing the handle claims again, behind whatever has been asked for since, and the run goes on once it holds the claim.
    /// </summary>
    /// <param name="cancellationToken">Cancels the release, and the wait of the claim again when the handle is disposed: a cancel during it ends the wait, releases the waiting rows and leaves the run fenced.</param>
    /// <returns>A handle that claims again when disposed; one that does nothing for a run that claims no key, a hold that is disposed, or a claim already handed back.</returns>
    public async Task<IAsyncDisposable> Yield(CancellationToken cancellationToken = default)
    {
        if (_lease is null || Volatile.Read(ref _disposed) == 1 || Interlocked.CompareExchange(ref _yielded, 1, 0) == 1)
            return NothingToGiveBack.Instance;

        await EndRenewal().ConfigureAwait(false);
        var released = await _service.Release(_request.ExecutionId, cancellationToken).ConfigureAwait(false);
        if (released.IsFailure)
        {
            // The claim is still held: the renewal goes on, and the run keeps what it has.
            Volatile.Write(ref _yielded, 0);
            Begin();
            return NothingToGiveBack.Instance;
        }

        return new TakeAgain(this, cancellationToken);
    }

    /// <summary>Says whether the run still holds its claim: asked before each step of the run, so a run whose claim may be another run's stops.</summary>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>A success while the claim holds; a failure with its code once the lease has passed or a renewal found the run holds no row.</returns>
    public IGenericResult Check(ILogger logger)
    {
        if (_lease is not { } lease || Volatile.Read(ref _yielded) == 1)
            return GenericResult.Success();

        return Volatile.Read(ref _lost) == 1
            ? GenericResult.Failure(PipelineRunClaimLog.LeaseLapsed(logger, TimeSpan.FromSeconds(_settings.LeaseSeconds)))
            : lease.Check(logger);
    }

    /// <summary>Stops renewing and releases the claim, which promotes the runs waiting behind it.</summary>
    /// <returns>A task that completes when the claim is released.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        await EndRenewal().ConfigureAwait(false);
        if (_lease is not null)
            _ = await _service.Release(_request.ExecutionId, CancellationToken.None).ConfigureAwait(false);

        _stopped?.Dispose();
        _ended.Dispose();
    }

    private static async Task<IGenericResult<bool>> ClaimUntilHeld(
        IPipelineRunClaimService service, PipelineRunClaimRequest request, PipelineRunClaimSettings settings, Func<TimeSpan, CancellationToken, Task> delay, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var claimed = await service.Claim(request, cancellationToken).ConfigureAwait(false);
                if (claimed.IsFailure || claimed.Value is not { } outcome)
                    return claimed.ToNewResult<bool>();

                if (ReferenceEquals(outcome.State, PipelineRunClaimStates.ByName("Claimed")))
                    return GenericResult<bool>.Success(true);

                await delay(TimeSpan.FromSeconds(settings.WaitingPollSeconds), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // The rows a cancelled run left waiting would hold its place for every run behind it until their lease lapsed.
            _ = await service.Release(request.ExecutionId, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    // Starts the lease and its renewal; a run that claims no key has neither.
    private void Begin()
    {
        if (_request.Keys.Count == 0)
            return;

        _lease = PipelineRunLease.Start(_clock, TimeSpan.FromSeconds(_settings.LeaseSeconds));
        Volatile.Write(ref _lost, 0);
        _ended = new CancellationTokenSource();
        var ended = _ended.Token;
        var stopped = new SemaphoreSlim(0, 1);
        _stopped = stopped;
        _ = Task.Run(() => RenewWhileHeld(stopped, ended), CancellationToken.None);
    }

    // Stops the renewal and waits until its loop has ended, so nothing renews a claim that is being released.
    private async Task EndRenewal()
    {
        await _ended.CancelAsync().ConfigureAwait(false);
        if (_stopped is { } stopped)
        {
            await stopped.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            stopped.Dispose();
            _stopped = null;
        }
    }

    private async Task RenewWhileHeld(SemaphoreSlim stopped, CancellationToken ended)
    {
        try
        {
            while (true)
            {
                await _delay(TimeSpan.FromSeconds(_settings.RenewalSeconds), ended).ConfigureAwait(false);
                var renewed = await _service.Renew([_request.ExecutionId], _request.ExecutingHost, _request.ExecutingSlot, ended).ConfigureAwait(false);

                // A renewal that failed is the service's to report; the lease decides when the run stops.
                if (renewed.IsFailure || renewed.Value is not { } answer)
                    continue;

                if (answer.Renewals.Any(renewal => renewal.ExecutionId == _request.ExecutionId && renewal.Renewed))
                {
                    _lease?.Renewed();
                    continue;
                }

                Volatile.Write(ref _lost, 1);
                return;
            }
        }
        catch (OperationCanceledException ex)
        {
            // The hold was disposed or handed back: the renewal ends with it.
            _ = PipelineRunClaimLog.RenewalEnded(_logger, _request.ExecutionId, ex);
        }
        finally
        {
            _ = stopped.Release();
        }
    }

    // Claims again after a yield, behind whatever has been asked for since. A claim that cannot be made again, or a wait that is cancelled, leaves the run fenced: its next step is refused
    // with the lease code. A cancel releases the waiting rows (ClaimUntilHeld) and reaches the caller.
    private async ValueTask Reclaim(CancellationToken cancellationToken)
    {
        IGenericResult<bool> held;
        try
        {
            held = await ClaimUntilHeld(_service, _request, _settings, _delay, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Volatile.Write(ref _lost, 1);
            Volatile.Write(ref _yielded, 0);
            throw;
        }

        if (held.IsFailure)
        {
            Volatile.Write(ref _lost, 1);
            Volatile.Write(ref _yielded, 0);
            return;
        }

        Begin();
        Volatile.Write(ref _yielded, 0);
    }

    private sealed class TakeAgain(PipelineRunClaimHold hold, CancellationToken cancellationToken) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => hold.Reclaim(cancellationToken);
    }

    private sealed class NothingToGiveBack : IAsyncDisposable
    {
        public static readonly NothingToGiveBack Instance = new();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
