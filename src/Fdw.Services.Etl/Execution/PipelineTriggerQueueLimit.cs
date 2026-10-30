using System;
using Fdw.Results;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Reads how many pipeline triggers the ETL host holds before it starts them (FDW-3155).
/// </summary>
/// <remarks>
/// The number is read for the host, with no tenant. A number that is not set to a positive value fails and names the setting; no number
/// is assumed.
/// </remarks>
public static class PipelineTriggerQueueLimit
{
    /// <summary>Reads the number from the platform's settings.</summary>
    /// <param name="settings">The settings the number is resolved from.</param>
    /// <param name="logger">The logger a failure is written to.</param>
    /// <returns>The number, or the failure naming the setting.</returns>
    public static IGenericResult<int> Read(IEffectiveSettingsProvider settings, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        var capacity = settings.GetEffectiveValue<int>(SettingDefinitions.MaxQueuedPipelineTriggers);
        return capacity <= 0
            ? GenericResult<int>.Failure(PipelineRunSchedulerLog.LimitSettingMissing(logger, SettingDefinitions.MaxQueuedPipelineTriggers))
            : GenericResult<int>.Success(capacity);
    }
}
