using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Connections.Abstractions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Limits;

/// <summary>
/// Hosted service that resets in-memory daily limit counters at midnight UTC.
///
/// Design:
/// - Runs once per calendar day at midnight UTC.
/// - Resets all in-memory counters in <see cref="ConnectionLimitCounterStore"/>.
/// - Logs the reset so it is traceable in the ops log.
///
/// The DB flush of counter values is handled separately by the configuration
/// persistence path; this job only clears the in-memory state for the new day.
/// </summary>
internal sealed class DailyLimitResetJob : BackgroundService
{
    private readonly ConnectionLimitCounterStore _counters;
    private readonly ILogger<DailyLimitResetJob> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DailyLimitResetJob"/>.
    /// </summary>
    public DailyLimitResetJob(
        ConnectionLimitCounterStore counters,
        ILoggerFactory? loggerFactory)
    {
        _counters = counters ?? throw new ArgumentNullException(nameof(counters));
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<DailyLimitResetJob>();
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan delay = ComputeDelayUntilMidnightUtc();

            try
            {
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested)
            {
                ConnectionLimitLog.ResetJobCancelledDuringShutdown(_logger, ex);
                return;
            }

            if (stoppingToken.IsCancellationRequested)
                return;

            int connectionCount = ResetCounters();
            ConnectionLimitLog.DailyCountersReset(_logger, connectionCount);
        }
    }

    private int ResetCounters()
    {
        int count = 0;
        foreach (var _ in _counters.Snapshot())
            count++;

        _counters.ResetAll();
        return count;
    }

    private static TimeSpan ComputeDelayUntilMidnightUtc()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset nextMidnight = new DateTimeOffset(now.UtcDateTime.Date.AddDays(1), TimeSpan.Zero);
        TimeSpan delay = nextMidnight - now;

        return delay > TimeSpan.Zero ? delay : TimeSpan.FromMinutes(1);
    }
}
