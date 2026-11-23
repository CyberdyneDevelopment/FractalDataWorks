using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Fdw.Services.Etl.Execution.Claims;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution.AtOnceRecovery;

/// <summary>
/// Ends the runs carried out at once whose durable claim lapsed (FDW-6986, slice 4 of FDW-6141), as their pipelines' half-done rule says. A run renews its claim every
/// <c>PipelineRunClaimRenewalSeconds</c> inside <c>PipelineRunClaimLeaseSeconds</c>, so a claim whose lease has passed belongs to a run its host no longer carries on: whichever instance held it, and
/// a copy that died and never came back included. The claim procedures delete a lapsed row and answer it to whoever called, so every answer is given to <see cref="Seen"/>, and a sweep asks the
/// procedure for the lapsed rows of nobody's in particular.
/// </summary>
/// <remarks>
/// A lapsed claim of a run that is not an open at-once run (a queued run, whose stored request the queue's own recovery takes up, or a run already ended) is left as it is. A run whose pipeline or rule
/// cannot be read is left open and named at every sweep until it can be judged: no rule is assumed for it (a lapsed row is deleted, so the sweeps remember it in memory; a host that restarts forgets it,
/// which is the known limit of this recovery). A run that holds no claim row at all has no lease to lapse and ends with its parent.
/// </remarks>
internal sealed class PipelineAtOnceRecovery : IPipelineLapsedClaims
{
    private const string AtOnceRunMode = "AtOnce";

    private readonly IHalfDoneRunRecords _records;
    private readonly IHalfDonePipelines _pipelines;
    private readonly ILogger _logger;
    private readonly Channel<PipelineRunLapsedClaim> _seen = Channel.CreateUnbounded<PipelineRunLapsedClaim>();
    private readonly ConcurrentDictionary<Guid, PipelineRunLapsedClaim> _unresolved = new();

    /// <summary>Initializes a new instance of the <see cref="PipelineAtOnceRecovery"/> class.</summary>
    /// <param name="records">Reads and closes the runs' records.</param>
    /// <param name="pipelines">Reads the pipelines' half-done rules.</param>
    /// <param name="logger">The logger.</param>
    public PipelineAtOnceRecovery(IHalfDoneRunRecords records, IHalfDonePipelines pipelines, ILogger<PipelineAtOnceRecovery>? logger = null)
    {
        _records = records;
        _pipelines = pipelines;
        _logger = logger ?? NullLogger<PipelineAtOnceRecovery>.Instance;
    }

    /// <summary>Gets the number of lapsed claims that were seen and not yet judged, or that could not be judged.</summary>
    public int Pending => _seen.Reader.Count + _unresolved.Count;

    /// <inheritdoc />
    public void Seen(IReadOnlyList<PipelineRunLapsedClaim> lapsed)
    {
        if (lapsed.Count > 0)
        {
            _ = PipelineAtOnceRecoveryLog.LapsedClaimsSeen(_logger, lapsed.Count, lapsed.Select(claim => claim.ExecutionId).Distinct().Count());
        }

        foreach (var claim in lapsed)
        {
            _ = _seen.Writer.TryWrite(claim);
        }
    }

    /// <summary>Judges every lapsed claim seen so far and every one that could not be judged before.</summary>
    /// <param name="cancellationToken">Cancels the judging.</param>
    /// <returns>A task that completes when each claim has been judged or left open.</returns>
    public async Task Settle(CancellationToken cancellationToken = default)
    {
        while (_seen.Reader.TryRead(out var claim))
        {
            _unresolved[claim.ExecutionId] = claim;
        }

        foreach (var claim in _unresolved.Values.OrderBy(unresolved => unresolved.ExecutionId))
        {
            if (await Judge(claim, cancellationToken).ConfigureAwait(false))
            {
                _ = _unresolved.TryRemove(claim.ExecutionId, out _);
            }
        }
    }

    /// <summary>
    /// Ends a run its host is stopping, if it is an open run carried out at once: by the same judgment as a run whose claim lapsed, because a run its host stopped and a run its host lost are the same
    /// case, a run that ended half done. The run mode is the one the run's own record states.
    /// </summary>
    /// <param name="tenantId">The tenant of the run.</param>
    /// <param name="executionId">The run.</param>
    /// <param name="cancellationToken">Cancels the judging.</param>
    /// <returns>Whether the run was ended, is not an open run carried out at once, or could not be judged and was left open.</returns>
    internal Task<IAtOnceRunEnd> EndStoppedRun(Guid tenantId, Guid executionId, CancellationToken cancellationToken = default) =>
        Decide(tenantId, executionId, cancellationToken);

    // True when the claim is settled (the run was closed, or is not one this recovery closes); false when the run is left open for the next sweep.
    private async Task<bool> Judge(PipelineRunLapsedClaim claim, CancellationToken cancellationToken) =>
        (await Decide(claim.TenantId, claim.ExecutionId, cancellationToken).ConfigureAwait(false)).Settled;

    private async Task<IAtOnceRunEnd> Decide(Guid tenantId, Guid executionId, CancellationToken cancellationToken)
    {
        var read = await _records.Read(tenantId, executionId, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
        {
            _ = PipelineAtOnceRecoveryLog.RunRecordNotReadable(_logger, executionId, read.CurrentMessage);
            return AtOnceRunEnds.Unjudged;
        }

        if (read.Value is not { } row)
        {
            _ = PipelineAtOnceRecoveryLog.RunHasNoRecord(_logger, executionId);
            return AtOnceRunEnds.NotOpenAtOnce;
        }

        if (!row.IsOpen || !string.Equals(row.RunMode, AtOnceRunMode, StringComparison.Ordinal))
        {
            _ = PipelineAtOnceRecoveryLog.RunNotOpenAtOnce(_logger, executionId, row.IsOpen, row.RunMode ?? string.Empty);
            return AtOnceRunEnds.NotOpenAtOnce;
        }

        var found = await _pipelines.RuleOf(tenantId, row.PipelineName, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure)
        {
            _ = PipelineAtOnceRecoveryLog.PipelineNotReadable(_logger, executionId, row.PipelineName, found.CurrentMessage);
            return AtOnceRunEnds.Unjudged;
        }

        // End is the only rule in 1.6.0, so a rule that is an option ends the run; the next option has its own branch here.
        var rule = found.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rule) || ReferenceEquals(HalfDoneRules.ByName(rule), HalfDoneRules.NotFound))
        {
            _ = PipelineAtOnceRecoveryLog.RuleNotUsable(_logger, executionId, row.PipelineName, rule);
            return AtOnceRunEnds.Unjudged;
        }

        var reason = PipelineAtOnceRecoveryLog.RunEndedHalfDone(_logger, executionId, row.PipelineName, rule).Message;
        var closed = await _records.Close(tenantId, executionId, reason, cancellationToken).ConfigureAwait(false);
        if (closed.IsFailure)
        {
            _ = PipelineAtOnceRecoveryLog.RunNotClosed(_logger, executionId, closed.CurrentMessage);
            return AtOnceRunEnds.Unjudged;
        }

        return AtOnceRunEnds.Ended;
    }
}
