using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Web.Analytics.Clients.Logging;
using Fdw.Web.Analytics.Clients.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Analytics.Clients.Services;

/// <summary>
/// In-memory analytics service for development/demo purposes.
/// Production would use a proper time-series database.
/// </summary>
public sealed class AnalyticsService : IAnalyticsService
{
    private readonly ILogger<AnalyticsService> _logger;
    private readonly ConcurrentBag<ExecutionEntry> _executions = [];

    private long _totalExecutions;
    private long _successfulExecutions;
    private long _cacheHits;
    private long _cacheMisses;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyticsService"/> class.
    /// </summary>
    public AnalyticsService(ILogger<AnalyticsService>? logger)
    {
        _logger = logger ?? NullLogger<AnalyticsService>.Instance;
    }

    /// <inheritdoc />
    #pragma warning disable MA0051
    public Task<IGenericResult> RecordExecution(CalculationExecutionRecord record, CancellationToken cancellationToken = default)
    #pragma warning restore MA0051
    {
        AnalyticsLog.RecordExecutionEntering(_logger, record.CalculationType);

        try
        {
            var entry = new ExecutionEntry
            {
                CalculationType = record.CalculationType,
                DurationMs = record.DurationMs,
                Success = record.Success,
                FromCache = record.FromCache,
                InputSize = record.InputSize,
                UserId = record.UserId,
                Timestamp = DateTimeOffset.UtcNow
            };

            _executions.Add(entry);
            Interlocked.Increment(ref _totalExecutions);

            if (record.Success)
            {
                Interlocked.Increment(ref _successfulExecutions);
            }

            if (record.FromCache)
            {
                Interlocked.Increment(ref _cacheHits);
            }
            else
            {
                Interlocked.Increment(ref _cacheMisses);
            }

            AnalyticsLog.PerformanceRecorded(_logger, record.CalculationType, record.DurationMs);
            AnalyticsLog.RecordExecutionCompleted(_logger, record.CalculationType);

            return Task.FromResult(GenericResult.Success());
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(GenericResult.Failure(
                AnalyticsLog.InvalidArgument(_logger, ex, nameof(RecordExecution), ex.Message)));
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(GenericResult.Failure(
                AnalyticsLog.InvalidOperation(_logger, ex, nameof(RecordExecution), ex.Message)));
        }
        catch (OperationCanceledException) { throw; }
        catch (OutOfMemoryException) { throw; }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult.Failure(
                ExceptionResultExtensions.FlattenException(ex)));
        }
    }

    /// <inheritdoc />
    #pragma warning disable MA0051
    public Task<IGenericResult<AnalyticsResponse>> GetAnalytics(AnalyticsRequest request, CancellationToken cancellationToken = default)
    #pragma warning restore MA0051
    {
        var startStr = request.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endStr = request.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        AnalyticsLog.GetAnalyticsEntering(_logger, startStr, endStr);

        try
        {
            var entries = _executions
                .Where(e => e.Timestamp >= request.StartDate && e.Timestamp <= request.EndDate)
                .Where(e => string.IsNullOrEmpty(request.CalculationType) || string.Equals(e.CalculationType, request.CalculationType, StringComparison.Ordinal))
                .ToList();

            var summary = BuildSummary(entries, request.StartDate, request.EndDate);
            var byType = BuildByType(entries);
            var timeSeries = BuildTimeSeries(entries, request.StartDate, request.EndDate);
            var topCalcs = byType.OrderByDescending(x => x.ExecutionCount).Take(5).ToList();

            AnalyticsLog.AnalyticsRetrieved(_logger, startStr, endStr);
            AnalyticsLog.GetAnalyticsCompleted(_logger, entries.Count);

            var response = new AnalyticsResponse
            {
                Summary = summary,
                ByCalculationType = byType,
                TimeSeries = timeSeries,
                TopCalculations = topCalcs
            };

            return Task.FromResult(GenericResult<AnalyticsResponse>.Success(response));
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(GenericResult<AnalyticsResponse>.Failure(
                AnalyticsLog.InvalidArgument(_logger, ex, nameof(GetAnalytics), ex.Message)));
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(GenericResult<AnalyticsResponse>.Failure(
                AnalyticsLog.InvalidOperation(_logger, ex, nameof(GetAnalytics), ex.Message)));
        }
        catch (OverflowException ex)
        {
            return Task.FromResult(GenericResult<AnalyticsResponse>.Failure(
                AnalyticsLog.ArithmeticOverflow(_logger, ex, nameof(GetAnalytics), ex.Message)));
        }
        catch (OperationCanceledException) { throw; }
        catch (OutOfMemoryException) { throw; }
        catch (Exception ex)
        {
            AnalyticsLog.GetAnalyticsFailed(_logger, ex, startStr, endStr, ex.Message);
            return Task.FromResult(GenericResult<AnalyticsResponse>.Failure(
                ExceptionResultExtensions.FlattenException(ex)));
        }
    }

    /// <inheritdoc />
    #pragma warning disable MA0051
    public Task<IGenericResult<TopCalculationsResponse>> GetTopCalculations(TopCalculationsRequest request, CancellationToken cancellationToken = default)
    #pragma warning restore MA0051
    {
        AnalyticsLog.GetTopCalculationsEntering(_logger, request.Count);

        try
        {
            var entries = _executions
                .Where(e => e.Timestamp >= request.Since)
                .ToList();

            var byType = BuildByType(entries)
                .OrderByDescending(x => x.ExecutionCount)
                .Take(request.Count)
                .ToArray();

            AnalyticsLog.GetTopCalculationsCompleted(_logger, byType.Length);

            return Task.FromResult(GenericResult<TopCalculationsResponse>.Success(
                new TopCalculationsResponse { Calculations = byType }));
        }
        catch (ArgumentException ex)
        {
            return Task.FromResult(GenericResult<TopCalculationsResponse>.Failure(
                AnalyticsLog.InvalidArgument(_logger, ex, nameof(GetTopCalculations), ex.Message)));
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(GenericResult<TopCalculationsResponse>.Failure(
                AnalyticsLog.InvalidOperation(_logger, ex, nameof(GetTopCalculations), ex.Message)));
        }
        catch (OverflowException ex)
        {
            return Task.FromResult(GenericResult<TopCalculationsResponse>.Failure(
                AnalyticsLog.ArithmeticOverflow(_logger, ex, nameof(GetTopCalculations), ex.Message)));
        }
        catch (OperationCanceledException) { throw; }
        catch (OutOfMemoryException) { throw; }
        catch (Exception ex)
        {
            AnalyticsLog.GetTopCalculationsFailed(_logger, ex, ex.Message);
            return Task.FromResult(GenericResult<TopCalculationsResponse>.Failure(
                ExceptionResultExtensions.FlattenException(ex)));
        }
    }

    #pragma warning disable MA0051
    private AnalyticsSummary BuildSummary(List<ExecutionEntry> entries, DateTimeOffset start, DateTimeOffset end)
    #pragma warning restore MA0051
    {
        var successful = entries.Count(e => e.Success);
        var failed = entries.Count - successful;
        var avgDuration = entries.Count > 0 ? entries.Average(e => e.DurationMs) : 0;
        var p95Duration = entries.Count > 0 ? CalculatePercentile(entries.Select(e => e.DurationMs).ToList(), 95) : 0;
        var cacheHits = entries.Count(e => e.FromCache);
        var cacheHitRate = entries.Count > 0 ? (double)cacheHits / entries.Count : 0;
        var uniqueTypes = entries.Select(e => e.CalculationType).Distinct(StringComparer.Ordinal).Count();
        var uniqueUsers = entries.Where(e => !string.IsNullOrEmpty(e.UserId)).Select(e => e.UserId).Distinct(StringComparer.Ordinal).Count();

        AnalyticsLog.UsageSummary(_logger, entries.Count, uniqueTypes, avgDuration);

        return new AnalyticsSummary
        {
            TotalExecutions = entries.Count,
            SuccessfulExecutions = successful,
            FailedExecutions = failed,
            AverageDurationMs = avgDuration,
            P95DurationMs = p95Duration,
            CacheHitRate = cacheHitRate,
            UniqueCalculationTypes = uniqueTypes,
            UniqueUsers = uniqueUsers,
            PeriodStart = start,
            PeriodEnd = end
        };
    }

    private static List<CalculationTypeStats> BuildByType(List<ExecutionEntry> entries)
    {
        return entries
            .GroupBy(e => e.CalculationType, StringComparer.Ordinal)
            .Select(g => new CalculationTypeStats
            {
                CalculationType = g.Key,
                ExecutionCount = g.Count(),
                AverageDurationMs = g.Average(e => e.DurationMs),
                MinDurationMs = g.Min(e => e.DurationMs),
                MaxDurationMs = g.Max(e => e.DurationMs),
                SuccessRate = (double)g.Count(e => e.Success) / g.Count(),
                CacheHitRate = (double)g.Count(e => e.FromCache) / g.Count(),
                LastExecuted = g.Max(e => e.Timestamp)
            })
            .ToList();
    }

    private static List<TimeSeriesDataPoint> BuildTimeSeries(List<ExecutionEntry> entries, DateTimeOffset start, DateTimeOffset end)
    {
        var duration = end - start;
        var bucketCount = duration.TotalDays > 7 ? 24 : 48;
        var bucketSize = duration / bucketCount;

        var result = new List<TimeSeriesDataPoint>();

        for (var i = 0; i < bucketCount; i++)
        {
            var bucketStart = start + (bucketSize * i);
            var bucketEnd = bucketStart + bucketSize;

            var bucketEntries = entries
                .Where(e => e.Timestamp >= bucketStart && e.Timestamp < bucketEnd)
                .ToList();

            result.Add(new TimeSeriesDataPoint
            {
                Timestamp = bucketStart,
                ExecutionCount = bucketEntries.Count,
                AverageDurationMs = bucketEntries.Count > 0 ? bucketEntries.Average(e => e.DurationMs) : 0,
                ErrorCount = bucketEntries.Count(e => !e.Success)
            });
        }

        return result;
    }

    private static double CalculatePercentile(List<long> values, int percentile)
    {
        if (values.Count == 0) return 0;

        var sorted = values.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling((percentile / 100.0) * sorted.Count) - 1;
        return sorted[Math.Max(0, index)];
    }

    private sealed class ExecutionEntry
    {
        public required string CalculationType { get; init; }
        public required long DurationMs { get; init; }
        public required bool Success { get; init; }
        public bool FromCache { get; init; }
        public int InputSize { get; init; }
        public string? UserId { get; init; }
        public DateTimeOffset Timestamp { get; init; }
    }
}
