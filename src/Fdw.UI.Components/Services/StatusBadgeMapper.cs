namespace Fdw.UI.Components.Services;

using System;
using Fdw.UI.Components.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Maps health/status values to display properties (color, label, CSS class hints).
/// Framework-agnostic — returns semantic values that consumers map to their styling system.
/// </summary>
public static class StatusBadgeMapper
{
    /// <summary>
    /// Gets a <see cref="StatusBadge"/> for the given health state.
    /// </summary>
    /// <param name="isHealthy">Whether the entity is healthy.</param>
    /// <param name="logger">Optional logger. Falls back to <see cref="NullLogger.Instance"/> when not supplied.</param>
    /// <returns>A status badge with label, color, and variant.</returns>
    // Why: NullLogger fallback is the only acceptable ?? fallback per FDW conventions; this static
    // helper has no DI-constructed instance to hold a logger, so it is threaded through as an
    // optional trailing parameter instead, mirroring the EntityPicker/ObjectPicker component pattern.
    public static StatusBadge FromHealth(bool isHealthy, ILogger? logger = null)
    {
        var effectiveLogger = logger ?? NullLogger.Instance;
        StatusBadgeMapperLog.MappingHealth(effectiveLogger, isHealthy);

        var badge = isHealthy
            ? new StatusBadge("Healthy", StatusColors.Green, StatusVariants.Success)
            : new StatusBadge("Unhealthy", StatusColors.Red, StatusVariants.Error);

        StatusBadgeMapperLog.MappedHealth(effectiveLogger, isHealthy, badge.Label);
        return badge;
    }

    /// <summary>
    /// Gets a <see cref="StatusBadge"/> for the given pipeline status.
    /// </summary>
    /// <param name="status">The pipeline execution status.</param>
    /// <param name="logger">Optional logger. Falls back to <see cref="NullLogger.Instance"/> when not supplied.</param>
    /// <returns>A status badge with label, color, and variant.</returns>
    public static StatusBadge FromPipelineStatus(string? status, ILogger? logger = null)
    {
        var effectiveLogger = logger ?? NullLogger.Instance;
        StatusBadgeMapperLog.MappingPipelineStatus(effectiveLogger, status);

        var badge = ResolvePipelineStatusBadge(status);

        StatusBadgeMapperLog.MappedPipelineStatus(effectiveLogger, status, badge.Label);
        return badge;
    }

    private static StatusBadge ResolvePipelineStatusBadge(string? status)
    {
        if (string.IsNullOrEmpty(status))
        {
            return new StatusBadge("Unknown", StatusColors.Gray, StatusVariants.Neutral);
        }

        if (string.Equals(status, "Running", StringComparison.OrdinalIgnoreCase))
        {
            return new StatusBadge("Running", StatusColors.Blue, StatusVariants.Info);
        }

        if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "Succeeded", StringComparison.OrdinalIgnoreCase))
        {
            return new StatusBadge("Completed", StatusColors.Green, StatusVariants.Success);
        }

        if (string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            return new StatusBadge("Failed", StatusColors.Red, StatusVariants.Error);
        }

        if (string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return new StatusBadge("Cancelled", StatusColors.Yellow, StatusVariants.Warning);
        }

        if (string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "Queued", StringComparison.OrdinalIgnoreCase))
        {
            return new StatusBadge("Pending", StatusColors.Gray, StatusVariants.Neutral);
        }

        return new StatusBadge(status, StatusColors.Gray, StatusVariants.Neutral);
    }

    /// <summary>
    /// Gets a <see cref="StatusBadge"/> for a schedule enabled/disabled state.
    /// </summary>
    /// <param name="isEnabled">Whether the schedule is enabled.</param>
    /// <param name="logger">Optional logger. Falls back to <see cref="NullLogger.Instance"/> when not supplied.</param>
    /// <returns>A status badge with label, color, and variant.</returns>
    public static StatusBadge FromScheduleState(bool isEnabled, ILogger? logger = null)
    {
        var effectiveLogger = logger ?? NullLogger.Instance;
        StatusBadgeMapperLog.MappingScheduleState(effectiveLogger, isEnabled);

        var badge = isEnabled
            ? new StatusBadge("Active", StatusColors.Green, StatusVariants.Success)
            : new StatusBadge("Disabled", StatusColors.Gray, StatusVariants.Neutral);

        StatusBadgeMapperLog.MappedScheduleState(effectiveLogger, isEnabled, badge.Label);
        return badge;
    }
}
