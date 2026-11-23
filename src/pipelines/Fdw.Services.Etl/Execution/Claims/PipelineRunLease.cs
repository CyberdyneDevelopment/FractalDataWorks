using System;
using System.Threading;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>
/// The lease the holder of a claim fences itself with (FDW-6629). It measures the time since the claim was granted or last renewed with the clock's monotonic timestamp, not with the wall clock,
/// and before every step of its run the holder asks <see cref="Check"/>: past the lease the claim may already be another run's, so the run stops.
/// </summary>
public sealed class PipelineRunLease
{
    private readonly TimeProvider _clock;
    private readonly TimeSpan _duration;
    private long _since;

    private PipelineRunLease(TimeProvider clock, TimeSpan duration)
    {
        _clock = clock;
        _duration = duration;
        _since = clock.GetTimestamp();
    }

    /// <summary>Starts a lease at the moment a claim is granted.</summary>
    /// <param name="clock">The clock whose monotonic timestamp measures the lease.</param>
    /// <param name="duration">How long the lease lasts; more than no time.</param>
    /// <returns>The lease.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The duration is not more than no time.</exception>
    public static PipelineRunLease Start(TimeProvider clock, TimeSpan duration)
        => duration > TimeSpan.Zero
            ? new PipelineRunLease(clock, duration)
            : throw new ArgumentOutOfRangeException(nameof(duration), duration, "A lease lasts longer than no time.");

    /// <summary>Restarts the measure at the moment the holder's renewal was answered.</summary>
    public void Renewed() => Volatile.Write(ref _since, _clock.GetTimestamp());

    /// <summary>Says whether the lease still holds.</summary>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>A success while the lease holds; a failure with its code once it has passed.</returns>
    public IGenericResult Check(ILogger logger)
        => _clock.GetElapsedTime(Volatile.Read(ref _since)) >= _duration
            ? GenericResult.Failure(PipelineRunClaimLog.LeaseLapsed(logger, _duration))
            : GenericResult.Success();
}
